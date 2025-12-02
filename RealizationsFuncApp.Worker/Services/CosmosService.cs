using Azure.Cosmos;

public class CosmosService
{
    private readonly CosmosClient _client;
    private readonly Container _container;

    public CosmosService(string endpoint, string key, string databaseName, string containerName)
    {
        var options = new CosmosClientOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(30),
            // tune retry settings as needed: MaxRetryAttemptsOnRateLimitedRequests etc.
        };

        _client = new CosmosClient(endpoint, key, options);
        _client.CreateDatabaseIfNotExistsAsync(databaseName).GetAwaiter().GetResult();
        var db = _client.GetDatabase(databaseName);
        db.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/companyId"
        }, throughput: 400).GetAwaiter().GetResult();

        _container = db.GetContainer(containerName);
    }

    public async Task CreateAsync(Realization item)
    {
        if (string.IsNullOrEmpty(item.id))
            item.id = Guid.NewGuid().ToString();

        await _container.CreateItemAsync(item, new PartitionKey(item.companyId ?? item.id));
    }

    public async Task UpsertAsync(Realization item)
    {
        if (string.IsNullOrEmpty(item.id))
            item.id = Guid.NewGuid().ToString();

        await _container.UpsertItemAsync(item, new PartitionKey(item.companyId ?? item.id));
    }

    public async Task DeleteAsync(string id, string? partitionKey = null)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            // if partition not known, attempt to delete using id as partition (may fail)
            partitionKey = id;
        }
        await _container.DeleteItemAsync<Realization>(id, new PartitionKey(partitionKey));
    }
}
