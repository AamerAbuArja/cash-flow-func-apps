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

    public async Task<T?> GetAsync(string id, string partitionKey)
    {
        try
        {
            var resp = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync(string query, string partitionKey)
    {
        var def = new QueryDefinition(query);
        var it = _container.GetItemQueryIterator<T>(def, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(partitionKey)
        });

        var list = new List<T>();
        while (it.HasMoreResults)
        {
            var feed = await it.ReadNextAsync();
            list.AddRange(feed);
        }

        return list;
    }

    public async Task<IEnumerable<T>> QueryAsync(QueryDefinition query, string partitionKey)
    {
        var it = _container.GetItemQueryIterator<T>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey)
            });

        var list = new List<T>();

        while (it.HasMoreResults)
        {
            var feed = await it.ReadNextAsync();
            list.AddRange(feed);
        }

        return list;
    }

    public async Task CreateAsync(T entity, string partitionKey)
    {
        await _container.CreateItemAsync(entity, new PartitionKey(partitionKey));
    }

    public async Task UpsertAsync(T entity, string partitionKey)
    {
        await _container.UpsertItemAsync(entity, new PartitionKey(partitionKey));
    }

    public async Task DeleteAsync(string id, string partitionKey)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
    }
}
