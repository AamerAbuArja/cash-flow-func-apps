using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;

namespace TransactionFuncApp.API.Services;

public interface ITransactionService
{
    Task<Transaction> CreateAsync(string tenantId, string companyId, CreateTransactionRequest dto);
    Task<Transaction?> GetAsync(string tenantId, string companyId, string transactionId);
    Task<Transaction?> UpdateAsync(string tenantId, string companyId, string transactionId, UpdateTransactionRequest dto);
    Task DeleteAsync(string tenantId, string companyId, string transactionId);

    Task<IEnumerable<Transaction>> ListByCompanyAsync(string tenantId, string companyId);
}
