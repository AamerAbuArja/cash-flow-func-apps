using Microsoft.Azure.Cosmos;
using System.Net;

namespace TransactionFuncApp.API.Repositories;

public class CosmosRepository<T> : ICosmosRepository<T>
{
    private readonly Container _container;

    public CosmosRepository(CosmosClient client, string databaseId, string containerId)
    {
        _container = client.GetDatabase(databaseId).GetContainer(containerId);
    }

    public async Task<T?> GetAsync(string id, PartitionKey partitionKey)
    {
        try
        {
            var resp = await _container.ReadItemAsync<T>(id, partitionKey);
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync(string query, PartitionKey partitionKey)
    {
        var def = new QueryDefinition(query);
        var it = _container.GetItemQueryIterator<T>(def, requestOptions: new QueryRequestOptions
        {
            PartitionKey = partitionKey
        });

        var list = new List<T>();
        while (it.HasMoreResults)
        {
            var feed = await it.ReadNextAsync();
            list.AddRange(feed);
        }

        return list;
    }

    public async Task<IEnumerable<T>> QueryAsync(QueryDefinition query, PartitionKey partitionKey)
    {
        var it = _container.GetItemQueryIterator<T>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = partitionKey
            });

        var list = new List<T>();
        while (it.HasMoreResults)
        {
            var feed = await it.ReadNextAsync();
            list.AddRange(feed);
        }

        return list;
    }

    public async Task CreateAsync(T entity, PartitionKey partitionKey)
    {
        await _container.CreateItemAsync(entity, partitionKey);
    }

    public async Task UpsertAsync(T entity, PartitionKey partitionKey)
    {
        await _container.UpsertItemAsync(entity, partitionKey);
    }

    public async Task DeleteAsync(string id, PartitionKey partitionKey)
    {
        await _container.DeleteItemAsync<T>(id, partitionKey);
    }

    public async Task BatchCreateAsync(IEnumerable<T> entities, PartitionKey partitionKey)
    {
        const int maxBatchSize = 100;
        var chunks = entities
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / maxBatchSize)
            .Select(g => g.Select(x => x.item).ToList());

        foreach (var chunk in chunks)
        {
            var batch = _container.CreateTransactionalBatch(partitionKey);

            foreach (var entity in chunk)
            {
                batch.CreateItem(entity);
            }

            using var response = await batch.ExecuteAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new CosmosException(
                    $"Batch create failed with status {response.StatusCode}",
                    response.StatusCode,
                    0,
                    response.ActivityId,
                    response.RequestCharge);
            }
        }
    }
}