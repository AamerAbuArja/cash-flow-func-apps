using FluentValidation;
using Microsoft.Azure.Cosmos;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;

namespace TransactionFuncApp.API.Services;

public class RealizationService : IRealizationService
{
    private readonly ICosmosRepository<Realization> _repo;
    private readonly ICosmosRepository<Transaction> _trxRepo;
    private readonly IValidator<CreateRealizationRequest> _createValidator;

    public RealizationService(ICosmosRepository<Realization> repo,
                              ICosmosRepository<Transaction> trxRepo,
                              IValidator<CreateRealizationRequest> createValidator)
    {
        _repo = repo;
        _trxRepo = trxRepo;
        _createValidator = createValidator;
    }

    public async Task<Realization> CreateAsync(string tenantId, string companyId, string transactionId, CreateRealizationRequest dto)
    {
        var tranpPrtitionKey = new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Build();

        // validate parent transaction exists and belongs to company/tenant
        var trx = await _trxRepo.GetAsync(transactionId, tranpPrtitionKey);
        if (trx == null || trx.companyId != companyId)
            throw new InvalidOperationException("Transaction not found or does not belong to the specified company.");

        var v = await _createValidator.ValidateAsync(dto);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        var realization = new Realization
        {
            id = Guid.NewGuid().ToString(),
            tenantId = tenantId,
            companyId = companyId,
            transactionId = transactionId,
            installmentNum = dto.installmentNum,
            type = dto.type,
            flow = dto.flow,
            status = dto.status ?? "Pending",
            amount = dto.amount,
            currency = dto.currency,
            fxRate = dto.fxRate,
            amountInBase = dto.amountInBase,
            expectedDate = dto.expectedDate,
            createdAt = DateTimeOffset.UtcNow
        };

        var partitionKey = new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Add(transactionId)
            .Build();

        await _repo.CreateAsync(realization, partitionKey);
        return realization;
    }

    public async Task<Realization> CreateAsync(Realization realization, PartitionKey partitionKey)
    {
        // Validate parent transaction exists within tenant/company scope
        var tranPartitionKey = new PartitionKeyBuilder()
            .Add(realization.tenantId)
            .Add(realization.companyId)
            .Build();

        var trx = await _trxRepo.GetAsync(realization.transactionId, tranPartitionKey);

        if (trx == null || trx.companyId != realization.companyId)
            throw new InvalidOperationException("Transaction not found or does not belong to the specified company.");

        // Persist directly (caller is responsible for constructing a valid entity)
        await _repo.CreateAsync(realization, partitionKey);

        return realization;
    }

    public async Task<Realization?> GetAsync(string tenantId, string companyId, string transactionId, string realizationId)
    {
        var partitionKey = new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Add(transactionId)
            .Build();

        var r = await _repo.GetAsync(realizationId, partitionKey);
        if (r == null) return null;
        if (r.companyId != companyId || r.transactionId != transactionId) return null;
        return r;
    }

    public async Task<Realization?> UpdateAsync(string tenantId, string companyId, string transactionId, string realizationId, UpdateRealizationRequest dto)
    {
        var partitionKey = new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Add(transactionId)
            .Build();
        var current = await _repo.GetAsync(realizationId, partitionKey);
        if (current == null) return null;
        if (current.companyId != companyId || current.transactionId != transactionId) return null;

        // apply allowed updates
        if (dto.amount.HasValue) current.amount = dto.amount;
        if (!string.IsNullOrEmpty(dto.amountInBase?.ToString())) current.amountInBase = dto.amountInBase;
        if (dto.fxRate.HasValue) current.fxRate = dto.fxRate;
        if (!string.IsNullOrEmpty(dto.status)) current.status = dto.status;
        if (dto.actualDate.HasValue) current.actualDate = dto.actualDate;
        current.updatedAt = DateTimeOffset.UtcNow;

        await _repo.UpsertAsync(current, partitionKey);
        return current;
    }

    public async Task DeleteAsync(string tenantId, string companyId, string transactionId, string realizationId)
    {
        // optional safety checks could be added
        var partitionKey = new PartitionKeyBuilder()
            .Add(tenantId)
            .Add(companyId)
            .Add(transactionId)
            .Build();
        await _repo.DeleteAsync(realizationId, partitionKey);
    }

    public async Task<IEnumerable<Realization>> ListByTransactionAsync(string tenantId, string companyId, string transactionId)
    {
        var partitionKey = new PartitionKeyBuilder()
                .Add(tenantId)
                .Add(companyId)
                .Add(transactionId)
                .Build();
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.companyId = @companyId AND c.transactionId = @transactionId")
            .WithParameter("@companyId", companyId)
            .WithParameter("@transactionId", transactionId);

        return await _repo.QueryAsync(query, partitionKey);
    }

}
