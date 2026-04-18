using FluentValidation;
using Microsoft.Azure.Cosmos;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;
using TransactionFuncApp.API.Enums.TransactionEnums;

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
			id = dto.id,
			tenantId = tenantId,
			companyId = companyId,
			type = dto.type,
			category = dto.category,
			description = dto.description,
			relatedTo = dto.relatedTo,
			amount = dto.amount,
			currency = dto.currency,
			installmentMode = dto.installmentMode,
			installmentCount = dto.installmentCount,
			installmentInterval = dto.installmentInterval,
			dueDate = dto.dueDate,
			createdAt = DateTimeOffset.UtcNow
		};

		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		await _trxRepo.CreateAsync(trx, partitionKey);

		if (dto.installmentMode == InstallmentMode.None)
		{
			await CreateInstallmentRealizationNone(trx);
		}

		if (dto.installmentMode == InstallmentMode.Manual)
		{
			if (dto.manualInstallments == null || !dto.manualInstallments.Any())
				throw new ArgumentException("Manual mode requires at least one installment entry.");

			var totalEntered = dto.manualInstallments.Sum(i => i.amount);
#pragma warning disable CS8629 // Nullable value type may be null.
			if (Math.Abs((decimal)(totalEntered - trx.amount)) > 0.01m)
				throw new ArgumentException($"Manual installment amounts ({totalEntered}) must sum to the transaction amount ({trx.amount}).");
#pragma warning restore CS8629 // Nullable value type may be null.

			await CreateInstallmentRealizationsManual(trx, dto.manualInstallments);
		}

		if (dto.installmentMode == InstallmentMode.Auto && trx.installmentCount.GetValueOrDefault() > 0)
		{
			await CreateInstallmentRealizationsAuto(trx);
		}

		if (dto.installmentMode == InstallmentMode.Percentage)
		{
			if (dto.percentageInstallments == null || !dto.percentageInstallments.Any())
				throw new ArgumentException("Percentage mode requires at least one installment entry.");

			var totalPct = dto.percentageInstallments.Sum(i => i.percentage);
			if (Math.Abs(totalPct - 100m) > 0.01m)
				throw new ArgumentException($"Percentage installments must sum to 100%. Currently: {totalPct}%.");

			await CreateInstallmentRealizationsPercentage(trx, dto.percentageInstallments);
		}

		if (dto.installmentMode == InstallmentMode.Recurring)
		{
			await CreateInstallmentRealizationsRecurring(trx);
		}

		return trx;
	}

	public async Task<Transaction?> GetAsync(string tenantId, string companyId, string transactionId)
	{
		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		var t = await _trxRepo.GetAsync(transactionId, partitionKey); // ✅
		if (t == null) return null;
		if (t.companyId != companyId) return null;
		return t;
	}

	public async Task<Transaction?> UpdateAsync(string tenantId, string companyId, string transactionId, UpdateTransactionRequest dto)
	{
		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		var current = await _trxRepo.GetAsync(transactionId, partitionKey); // ✅
		if (current == null || current.companyId != companyId)
			return null;

		current.type = dto.type;
		current.category = dto.category;
		current.description = dto.description;
		current.relatedTo = dto.relatedTo;
		current.amount = dto.amount;
		current.currency = dto.currency;
		current.installmentMode = dto.installmentMode;
		current.installmentCount = dto.installmentCount;
		current.installmentInterval = dto.installmentInterval;
		current.dueDate = dto.dueDate;

		await _trxRepo.UpsertAsync(current, partitionKey); // ✅
		return current;
	}

	public async Task DeleteAsync(string tenantId, string companyId, string transactionId)
	{
		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		await _trxRepo.DeleteAsync(transactionId, partitionKey); // ✅
	}

	public async Task<IEnumerable<Transaction>> ListByCompanyAsync(string tenantId, string companyId)
	{
		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		var query = new QueryDefinition(
				"SELECT * FROM c WHERE c.companyId = @companyId")
				.WithParameter("@companyId", companyId);

		return await _trxRepo.QueryAsync(query, partitionKey); // ✅
	}

	public async Task<IEnumerable<Transaction>> GetByInstallmentModeAsync(InstallmentMode mode)
	{
		var query = new QueryDefinition(
			"SELECT * FROM c WHERE c.installmentMode = @mode")
			.WithParameter("@mode", mode.ToString()); // or (int)mode depending on how you store it

		return await _trxRepo.QueryAsync(query, PartitionKey.None); // No partition key since this is a cross-partition query
	}

	// still needs more work to handle realizations
	public async Task<IEnumerable<Transaction>> CreateBatchAsync(string tenantId, string companyId, IEnumerable<CreateTransactionRequest> dtos)
	{
		var partitionKey = new PartitionKeyBuilder()
				.Add(tenantId)
				.Add(companyId)
				.Build();

		var transactions = new List<Transaction>();

		foreach (var dto in dtos)
		{
			var v = await _createValidator.ValidateAsync(dto);
			if (!v.IsValid) throw new ValidationException(v.Errors);

			transactions.Add(new Transaction
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
				installmentMode = dto.installmentMode,
				installmentCount = dto.installmentCount,
				installmentInterval = dto.installmentInterval,
				dueDate = dto.dueDate,
				createdAt = DateTimeOffset.UtcNow
			});
		}

		await _trxRepo.BatchCreateAsync(transactions, partitionKey);

		return transactions;
	}

	private async Task CreateInstallmentRealizationsAuto(Transaction trx)
	{
		var count = trx.installmentCount.GetValueOrDefault();
		var intervalDays = trx.installmentInterval.GetValueOrDefault();

		var totalCents = (long)(trx.amount * 100);
		var baseCents = totalCents / count;
		var remainder = totalCents - (baseCents * count);

		DateTime baseDate;
		if (!string.IsNullOrEmpty(trx.dueDate) &&
				DateTime.TryParse(trx.dueDate, out var parsed))
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
				//type = trx.type,
				flow = null,
				status = "Pending",
				amount = installmentAmount,
				currency = trx.currency,
				fxRate = null,
				amountInBase = null,
				expectedDate = baseDate.AddDays((i - 1) * intervalDays),
				createdAt = DateTimeOffset.UtcNow
			};

			await _realRepo.CreateAsync(realization, new PartitionKeyBuilder()
					.Add(trx.tenantId)
					.Add(trx.companyId)
					.Add(trx.id)
					.Build());
		}
	}

	private async Task CreateInstallmentRealizationNone(Transaction trx)
	{
		DateTime dueDate;
		if (!string.IsNullOrEmpty(trx.dueDate) &&
				DateTime.TryParse(trx.dueDate, out var parsed))
			dueDate = parsed;
		else
			dueDate = DateTime.UtcNow.Date;

		var realization = new Realization
		{
			id = Guid.NewGuid().ToString(),
			tenantId = trx.tenantId,
			companyId = trx.companyId,
			transactionId = trx.id,
			installmentNum = 1,
			flow = null,
			status = "Pending",
			amount = trx.amount,
			currency = trx.currency,
			fxRate = null,
			amountInBase = null,
			expectedDate = dueDate,
			createdAt = DateTimeOffset.UtcNow
		};

		await _realRepo.CreateAsync(realization, new PartitionKeyBuilder()
				.Add(trx.tenantId)
				.Add(trx.companyId)
				.Add(trx.id)
				.Build());
	}

	private async Task CreateInstallmentRealizationsManual(Transaction trx, List<ManualInstallmentDto> installments)
	{
		for (int i = 0; i < installments.Count; i++)
		{
			var entry = installments[i];

			if (entry.amount <= 0)
				throw new ArgumentException(
						$"Installment {i + 1} has an invalid amount: {entry.amount}.");

			if (!entry.expectedDate.HasValue)
				throw new ArgumentException(
						$"Installment {i + 1} has an invalid or missing date.");

			var expectedDate = entry.expectedDate.Value;

			var realization = new Realization
			{
				id = Guid.NewGuid().ToString(),
				tenantId = trx.tenantId,
				companyId = trx.companyId,
				transactionId = trx.id,
				installmentNum = entry.installmentNum > 0 ? entry.installmentNum : (i + 1),
				flow = null,
				status = "Pending",
				amount = entry.amount,
				currency = trx.currency,
				fxRate = null,
				amountInBase = null,
				expectedDate = expectedDate,
				createdAt = DateTimeOffset.UtcNow
			};

			await _realRepo.CreateAsync(realization, new PartitionKeyBuilder()
					.Add(trx.tenantId)
					.Add(trx.companyId)
					.Add(trx.id)
					.Build());
		}
	}

	private async Task CreateInstallmentRealizationsPercentage(Transaction trx, List<PercentageInstallmentDto> installments)
	{
		var totalCents = (long)(trx.amount * 100);
		long allocatedCents = 0;

		for (int i = 0; i < installments.Count; i++)
		{
			var entry = installments[i];
			var isLast = i == installments.Count - 1;

			if (entry.percentage <= 0)
				throw new ArgumentException(
						$"Installment {i + 1} has an invalid percentage: {entry.percentage}.");

			if (!entry.expectedDate.HasValue)
				throw new ArgumentException(
						$"Installment {i + 1} has an invalid or missing date.");

			// Last installment absorbs any rounding remainder
			long cents = isLast
					? totalCents - allocatedCents
					: (long)Math.Round(totalCents * (entry.percentage / 100m), MidpointRounding.AwayFromZero);

			allocatedCents += cents;

			var realization = new Realization
			{
				id = Guid.NewGuid().ToString(),
				tenantId = trx.tenantId,
				companyId = trx.companyId,
				transactionId = trx.id,
				installmentNum = entry.installmentNum > 0 ? entry.installmentNum : (i + 1),
				flow = null,
				status = "Pending",
				amount = cents / 100m,
				currency = trx.currency,
				fxRate = null,
				amountInBase = null,
				expectedDate = entry.expectedDate.Value,
				createdAt = DateTimeOffset.UtcNow
			};

			await _realRepo.CreateAsync(realization, new PartitionKeyBuilder()
					.Add(trx.tenantId)
					.Add(trx.companyId)
					.Add(trx.id)
					.Build());
		}
	}

	private async Task CreateInstallmentRealizationsRecurring(Transaction trx)
	{
		if (!Enum.TryParse<RecurringFrequency>(trx.recurringFrequency, out var frequency))
			throw new ArgumentException($"Invalid recurring frequency: '{trx.recurringFrequency}'.");

		DateTime baseDate;
		if (!string.IsNullOrEmpty(trx.dueDate) && DateTime.TryParse(trx.dueDate, out var parsed))
			baseDate = parsed;
		else
			baseDate = DateTime.UtcNow.Date;

		DateTime? hardStop = null;
		if (!string.IsNullOrEmpty(trx.recurringEndDate) &&
				DateTime.TryParse(trx.recurringEndDate, out var endDateParsed))
			hardStop = endDateParsed;

		var maxOccurrences = trx.recurringEndAfter;   // null = indefinite
		var horizonEnd = DateTime.UtcNow.Date.AddMonths(3);

		var currentDate = baseDate;
		var installmentNum = 1;

		while (true)
		{
			// Stop if we've hit the max occurrence count
			if (maxOccurrences.HasValue && installmentNum > maxOccurrences.Value) break;

			// Stop if we've passed the hard stop date
			if (hardStop.HasValue && currentDate > hardStop.Value) break;

			// Stop seeding once we're beyond the rolling horizon
			if (currentDate > horizonEnd) break;

			// TODO: Fix Error Here!!!
			var realization = RecurringRealizationsBuilder.BuildRecurringRealization(trx, installmentNum, currentDate);

			await _realRepo.CreateAsync(realization, new PartitionKeyBuilder()
				.Add(trx.tenantId)
				.Add(trx.companyId)
				.Add(trx.id)
				.Build());

			currentDate = frequency.NextDate(currentDate);
			installmentNum++;
		}
	}
}
