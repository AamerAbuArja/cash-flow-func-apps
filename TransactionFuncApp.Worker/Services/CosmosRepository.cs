using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System;
using System.Net;
using System.Collections.Generic;

public class CosmosRepository
{
    private readonly Container _container;
    private readonly string _databaseId;
    private readonly string _containerId;

    public CosmosRepository(CosmosClient client, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _databaseId = config["Cosmos__DatabaseId"] ?? "CashflowDB";
        _containerId = config["Cosmos__ContainerId"] ?? "Transaction";

        // Create database
        var db = client.CreateDatabaseIfNotExistsAsync(_databaseId)
                       .GetAwaiter().GetResult();

        // Create container with HIERARCHICAL partition keys
        var containerResponse = db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = _containerId,
                PartitionKeyPaths = new List<string>
                {
                    "/TenantId",
                    "/CompanyId"
                }
            }).GetAwaiter().GetResult();

        _container = containerResponse.Container;
    }

    // Build hierarchical PK
    private static PartitionKey BuildPk(string tenantId, string companyId)
    {
        return new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Build();
    }

    private static PartitionKey BuildPk(TransactionDto t)
    {
        return BuildPk(t.TenantId, t.CompanyId);
    }

    // --------------------------
    // Upsert
    // --------------------------

    public async Task UpsertTransactionAsync(TransactionDto t)
    {
        await _container.UpsertItemAsync(
            t,
            BuildPk(t)
        );
    }

    // --------------------------
    // Delete
    // --------------------------

    public async Task DeleteTransactionAsync(string id, string tenantId, string companyId)
    {
        try
        {
            await _container.DeleteItemAsync<TransactionDto>(id, BuildPk(tenantId, companyId));
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            // ignore
        }
    }

    // --------------------------
    // Query by company
    // --------------------------

    public async Task<List<TransactionDto>> QueryTransactionsAsync(string tenantId, string companyId, int? top = 100)
    {
        var pk = BuildPk(tenantId, companyId);

        var q = new QueryDefinition(
            "SELECT * FROM c WHERE c.TenantId = @tenantId AND c.CompanyId = @companyId")
            .WithParameter("@tenantId", tenantId)
            .WithParameter("@companyId", companyId);

        var it = _container.GetItemQueryIterator<TransactionDto>(
            q,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = pk,
                MaxItemCount = top
            });

        var results = new List<TransactionDto>();

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync();
            results.AddRange(page);
        }

        return results;
    }

    // --------------------------
    // Get by Id
    // --------------------------

    public async Task<TransactionDto?> GetByIdAsync(string id, string tenantId, string companyId)
    {
        try
        {
            var resp = await _container.ReadItemAsync<TransactionDto>(id, BuildPk(tenantId, companyId));
            return resp.Resource;
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // --------------------------
    // Bulk Insert
    // --------------------------

    public async Task BulkInsertAsync(IEnumerable<TransactionDto> items)
    {
        foreach (var t in items)
        {
            await UpsertTransactionAsync(t);
        }
    }
}
