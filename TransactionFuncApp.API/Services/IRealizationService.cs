using Microsoft.Azure.Cosmos;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;

namespace TransactionFuncApp.API.Services;

public interface IRealizationService
{
    Task<Realization> CreateAsync(string tenantId, string companyId, string transactionId, CreateRealizationRequest dto);
    Task<Realization?> GetAsync(string tenantId, string companyId, string transactionId, string realizationId);
    Task<Realization?> UpdateAsync(string tenantId, string companyId, string transactionId, string realizationId, UpdateRealizationRequest dto);
    Task DeleteAsync(string tenantId, string companyId, string transactionId, string realizationId);
    Task<Realization> CreateAsync(Realization realization, PartitionKey partitionKey);
    Task<IEnumerable<Realization>> ListByTransactionAsync(string tenantId, string companyId, string transactionId);
}
