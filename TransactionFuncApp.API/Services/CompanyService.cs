using FluentValidation;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;

namespace TransactionFuncApp.API.Services;

public class CompanyService : ICompanyService
{
    private readonly ICosmosRepository<Company> _repo;
    private readonly IValidator<CreateCompanyRequest> _createValidator;

    public CompanyService(ICosmosRepository<Company> repo, IValidator<CreateCompanyRequest> createValidator)
    {
        _repo = repo;
        _createValidator = createValidator;
    }

    public async Task<Company> CreateAsync(string tenantId, CreateCompanyRequest dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var company = new Company
        {
            id = Guid.NewGuid().ToString(),
            tenantId = tenantId,
            name = dto.name,
            baseCurrency = dto.baseCurrency,
            openingBalance = dto.openingBalance,
            closingBalance = dto.closingBalance,
            industry = dto.industry
        };

        await _repo.CreateAsync(company, tenantId);
        return company;
    }

    public async Task<IEnumerable<Company>> ListAsync(string tenantId)
    {
        string q = "SELECT * FROM c";
        var list = await _repo.QueryAsync(q, tenantId);
        return list;
    }

    public async Task<Company?> GetAsync(string tenantId, string companyId)
    {
        var c = await _repo.GetAsync(companyId, tenantId);
        if (c == null) return null;
        if (c.tenantId != tenantId) return null; // safety check
        return c;
    }

    public async Task<Company?> UpdateAsync(string tenantId, string companyId, UpdateCompanyRequest dto)
    {
        var current = await _repo.GetAsync(companyId, tenantId);
        if (current == null) return null;
        if (current.tenantId != tenantId) return null;

        // Optionally validate update; using create validator for simplicity
        var vv = await _createValidator.ValidateAsync(new CreateCompanyRequest
        {
            name = dto.name,
            baseCurrency = dto.baseCurrency,
            openingBalance = dto.openingBalance,
            closingBalance = dto.closingBalance,
            industry = dto.industry
        });

        if (!vv.IsValid) throw new ValidationException(vv.Errors);

        current.name = dto.name;
        current.baseCurrency = dto.baseCurrency;
        current.openingBalance = dto.openingBalance;
        current.closingBalance = dto.closingBalance;
        current.industry = dto.industry;

        await _repo.UpsertAsync(current, tenantId);
        return current;
    }

    public async Task DeleteAsync(string tenantId, string companyId)
    {
        await _repo.DeleteAsync(companyId, tenantId);
    }
}
