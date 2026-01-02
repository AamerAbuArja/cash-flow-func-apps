using FluentValidation;
using Microsoft.Azure.Cosmos;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;

namespace TransactionFuncApp.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ICosmosRepository<Transaction> _trxRepo;
    private readonly ICosmosRepository<Realization> _realRepo;
    private readonly IValidator<CreateTransactionRequest> _createValidator;

    public TransactionService(ICosmosRepository<Transaction> trxRepo,
                              ICosmosRepository<Realization> realRepo,
                              IValidator<CreateTransactionRequest> createValidator)
    {
        _trxRepo = trxRepo;
        _realRepo = realRepo;
        _createValidator = createValidator;
    }

    public async Task<Transaction> CreateAsync(string tenantId, string companyId, CreateTransactionRequest dto)
    {
        var v = await _createValidator.ValidateAsync(dto);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        var trx = new Transaction
        {
            id = Guid.NewGuid().ToString(),
            tenantId = tenantId,
            companyId = companyId,
            type = dto.type,
            category = dto.category,
            description = dto.description,
            relatedTo = dto.relatedTo,
            amount = dto.amount,
            currency = dto.currency,
            installmentMode = dto.installmentMode ?? "None",
            installmentCount = dto.installmentCount,
            installmentInterval = dto.installmentInterval,
            dueDate = dto.dueDate,
            createdAt = DateTimeOffset.UtcNow
        };

        await _trxRepo.CreateAsync(trx, tenantId);

        // Create realizations if needed
        if (!string.IsNullOrEmpty(trx.installmentMode) &&
            !trx.installmentMode.Equals("None", StringComparison.OrdinalIgnoreCase) &&
            trx.installmentCount.GetValueOrDefault() > 0)
        {
            await CreateInstallmentRealizations(trx);
        }

        return trx;
    }

    public async Task<Transaction?> GetAsync(string tenantId, string companyId, string transactionId)
    {
        var t = await _trxRepo.GetAsync(transactionId, tenantId);
        if (t == null) return null;
        if (t.companyId != companyId) return null;
        return t;
    }

    public async Task<Transaction?> UpdateAsync(string tenantId, string companyId, string transactionId, UpdateTransactionRequest dto)
    {
        var current = await _trxRepo.GetAsync(transactionId, tenantId);
        if (current == null) return null;
        if (current.companyId != companyId) return null;

        // Simple update; you can add a validator for UpdateTransactionRequest if desired
        current.type = dto.type;
        current.category = dto.category;
        current.description = dto.description;
        current.relatedTo = dto.relatedTo;
        current.amount = dto.amount;
        current.currency = dto.currency;
        current.installmentMode = dto.installmentMode ?? "None";
        current.installmentCount = dto.installmentCount;
        current.installmentInterval = dto.installmentInterval;
        current.dueDate = dto.dueDate;

        await _trxRepo.UpsertAsync(current, tenantId);

        // Optionally adjust realizations if installment count changed — not implemented automatically here.
        // Consider adding reconciliation logic (create additional realizations or mark extras as canceled).

        return current;
    }

    public async Task DeleteAsync(string tenantId, string companyId, string transactionId)
    {
        // Note: does not cascade delete realizations. Consider cascading or background cleanup.
        await _trxRepo.DeleteAsync(transactionId, tenantId);
    }

    public async Task<IEnumerable<Transaction>> ListByCompanyAsync(string tenantId, string companyId)
    {
        
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.companyId = @companyId")
            .WithParameter("@companyId", companyId);

        var list = await _trxRepo.QueryAsync(query, tenantId);
        return list;
    }

    private async Task CreateInstallmentRealizations(Transaction trx)
    {
        var count = trx.installmentCount.GetValueOrDefault();
        var intervalDays = trx.installmentInterval.GetValueOrDefault();

        // divide amount evenly; last installment gets remainder
        var totalCents = (long)(trx.amount * 100);
        var baseCents = totalCents / count;
        var remainder = totalCents - (baseCents * count);

        DateTime? baseDate = null;
        if (!string.IsNullOrEmpty(trx.dueDate) && DateTime.TryParse(trx.dueDate, out var parsed))
            baseDate = parsed;
        else
            baseDate = DateTime.UtcNow.Date;

        for (int i = 1; i <= count; i++)
        {
            var cents = baseCents + (i == count ? remainder : 0);
            decimal installmentAmount = cents / 100m;

            var realization = new Realization
            {
                id = Guid.NewGuid().ToString(),
                tenantId = trx.tenantId,
                companyId = trx.companyId,
                transactionId = trx.id,
                installmentNum = i,
                type = trx.type,
                flow = null,
                status = "Pending",
                amount = installmentAmount,
                currency = trx.currency,
                fxRate = null,
                amountInBase = null,
                expectedDate = baseDate?.AddDays((i - 1) * intervalDays),
                createdAt = DateTimeOffset.UtcNow
            };

            await _realRepo.CreateAsync(realization, realization.tenantId);
        }
    }
}
