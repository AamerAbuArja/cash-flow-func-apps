using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;

namespace TransactionFuncApp.API.Services;

public interface ITenantService
{
    Task<Tenant> CreateAsync(CreateTenantRequest dto);
    Task<Tenant?> GetAsync(string tenantId);
    Task<Tenant?> UpdateAsync(string tenantId, UpdateTenantRequest dto);
    Task DeleteAsync(string tenantId);
}
