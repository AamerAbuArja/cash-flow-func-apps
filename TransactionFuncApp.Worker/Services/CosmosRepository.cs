using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System;
using System.Net;
using System.Collections.Generic;

public class CosmosRepository
{
    private readonly CosmosClient _client;
    private readonly Container _container;
    private readonly string _databaseId;
    private readonly string _containerId;

    public CosmosRepository(CosmosClient client, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _client = client;
        _databaseId = config["Cosmos__DatabaseId"] ?? "transactions-db";
        _containerId = config["Cosmos__ContainerId"] ?? "transactions";

        // ensure DB & container exist (idempotent)
        var db = _client.CreateDatabaseIfNotExistsAsync(_databaseId).GetAwaiter().GetResult();
        var containerResponse = db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties {
                Id = _containerId,
                PartitionKeyPath = "/CompanyId"
            }).GetAwaiter().GetResult();
        _container = containerResponse.Container;
    }

    public async Task UpsertTransactionAsync(TransactionDto t)
    {
        // Idempotent upsert keyed by Id
        await _container.UpsertItemAsync(t, new PartitionKey(t.CompanyId));
    }

    public async Task DeleteTransactionAsync(string id, string companyId)
    {
        try
        {
            await _container.DeleteItemAsync<TransactionDto>(id, new PartitionKey(companyId));
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            // already gone — ignore
        }
    }

    public async Task<List<TransactionDto>> QueryTransactionsAsync(string companyId, int? top = 100)
    {
        var q = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId")
            .WithParameter("@companyId", companyId);

        var it = _container.GetItemQueryIterator<TransactionDto>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(companyId), MaxItemCount = top });
        var results = new List<TransactionDto>();
        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync();
            results.AddRange(page);
        }
        return results;
    }

    public async Task<TransactionDto?> GetByIdAsync(string id, string companyId)
    {
        try
        {
            var resp = await _container.ReadItemAsync<TransactionDto>(id, new PartitionKey(companyId));
            return resp.Resource;
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task BulkInsertAsync(IEnumerable<TransactionDto> items)
    {
        // naive bulk (you can use Bulk mode SDK or transactional batches)
        foreach (var t in items)
            await UpsertTransactionAsync(t);
    }
}
