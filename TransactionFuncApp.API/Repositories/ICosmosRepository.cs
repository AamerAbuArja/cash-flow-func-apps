using Microsoft.Azure.Cosmos;

namespace TransactionFuncApp.API.Repositories;

public interface ICosmosRepository<T>
{
    Task<T?> GetAsync(string id, PartitionKey partitionKey);
    Task<IEnumerable<T>> QueryAsync(string query, PartitionKey partitionKey);
    Task<IEnumerable<T>> QueryAsync(QueryDefinition query, PartitionKey partitionKey);
    Task CreateAsync(T entity, PartitionKey partitionKey);
    Task UpsertAsync(T entity, PartitionKey partitionKey);
    Task DeleteAsync(string id, PartitionKey partitionKey);
    Task BatchCreateAsync(IEnumerable<T> entities, PartitionKey partitionKey);
}