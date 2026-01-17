using FluentValidation;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;

namespace TransactionFuncApp.API.Services;

public class TenantService : ITenantService
{
    private readonly ICosmosRepository<Tenant> _repo;
    private readonly IValidator<CreateTenantRequest> _createValidator;
    private readonly IValidator<UpdateTenantRequest>? _updateValidator;

    public TenantService(ICosmosRepository<Tenant> repo,
                         IValidator<CreateTenantRequest> createValidator,
                         IValidator<UpdateTenantRequest>? updateValidator = null)
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Tenant> CreateAsync(CreateTenantRequest dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var tenant = new Tenant
        {
            id = Guid.NewGuid().ToString(),
            tenantId = dto.tenantId,
            name = dto.name,
            baseCurrency = dto.baseCurrency,
            subscription = dto.subscription
        };

        await _repo.CreateAsync(tenant, tenant.PartitionKey);
        return tenant;
    }

    public async Task<Tenant?> GetAsync(string tenantId)
    {
        return await _repo.GetAsync(tenantId, tenantId);
    }

    public async Task<Tenant?> UpdateAsync(string tenantId, UpdateTenantRequest dto)
    {
        var current = await _repo.GetAsync(tenantId, tenantId);
        if (current == null) return null;

        // Optionally validate update fields
        if (_updateValidator != null)
        {
            var vres = await _updateValidator.ValidateAsync(dto);
            if (!vres.IsValid) throw new ValidationException(vres.Errors);
        }

        current.name = dto.name;
        current.baseCurrency = dto.baseCurrency;
        current.subscription = dto.subscription;

        await _repo.UpsertAsync(current, current.PartitionKey);
        return current;
    }

    public async Task DeleteAsync(string tenantId)
    {
        await _repo.DeleteAsync(tenantId, tenantId);
    }
}
