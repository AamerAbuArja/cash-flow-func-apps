using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;

namespace TransactionFuncApp.API.Services;

public interface ICompanyService
{
    Task<Company> CreateAsync(string tenantId, CreateCompanyRequest dto);
    Task<IEnumerable<Company>> ListAsync(string tenantId);
    Task<Company?> GetAsync(string tenantId, string companyId);
    Task<Company?> UpdateAsync(string tenantId, string companyId, UpdateCompanyRequest dto);
    Task DeleteAsync(string tenantId, string companyId);
}
