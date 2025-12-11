namespace TransactionFuncApp.API.Repositories;

public interface ICosmosRepository<T>
{
    Task<T?> GetAsync(string id, string partitionKey);
    Task<IEnumerable<T>> QueryAsync(string query, string partitionKey);
    Task CreateAsync(T entity, string partitionKey);
    Task UpsertAsync(T entity, string partitionKey);
    Task DeleteAsync(string id, string partitionKey);
}
