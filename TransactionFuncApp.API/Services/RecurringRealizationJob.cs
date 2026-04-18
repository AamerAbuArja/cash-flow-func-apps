using Microsoft.Azure.Cosmos;
using TransactionFuncApp.API.Enums.TransactionEnums;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;

namespace TransactionFuncApp.API.Services;

public class RecurringRealizationJob
{
    private readonly ITransactionService _trxRepo;
    private readonly IRealizationService _realRepo;

    public RecurringRealizationJob(
        ITransactionService trxRepo,
        IRealizationService realRepo)
    {
        _trxRepo = trxRepo;
        _realRepo = realRepo;
    }

    public async Task RunAsync()
    {
        // TODO: Create the GetByInstallmentModeAsync Method
        // Fetch all active recurring transactions
        var recurringTrxs = await _trxRepo.GetByInstallmentModeAsync(InstallmentMode.Recurring);

        foreach (var trx in recurringTrxs)
        {
            // Skip cancelled series
            if (trx.recurringCancelledAt.HasValue)
                continue;

            await ExtendRecurringRealizationsAsync(trx);
        }
    }

    public async Task ExtendRecurringRealizationsAsync(Transaction trx)
    {
        if (!Enum.TryParse<RecurringFrequency>(trx.recurringFrequency, out var frequency))
            return;

        DateTime? hardStop = null;
        if (!string.IsNullOrEmpty(trx.recurringEndDate) &&
            DateTime.TryParse(trx.recurringEndDate, out var endDateParsed))
            hardStop = endDateParsed;

        var maxOccurrences = trx.recurringEndAfter;
        var horizonEnd = DateTime.UtcNow.Date.AddMonths(3);

        // Fetch existing realizations to find where we left off
        var existing = await _realRepo.ListByTransactionAsync(trx.tenantId, trx.companyId, trx.id);
        var lastRealization = existing.OrderByDescending(r => r.installmentNum).FirstOrDefault();

        if (lastRealization == null) return;

        // Add this:
        if (!lastRealization.expectedDate.HasValue) return;

        var nextDate = frequency.NextDate(lastRealization.expectedDate.Value);
        var installmentNum = lastRealization.installmentNum + 1;

        while (true)
        {
            if (maxOccurrences.HasValue && installmentNum > maxOccurrences.Value) break;
            if (hardStop.HasValue && nextDate > hardStop.Value) break;
            if (nextDate > horizonEnd) break;

            var realization = RecurringRealizationsBuilder.BuildRecurringRealization(trx, installmentNum, nextDate);

            // need to override this method or create a duplicate with different parameters
            // to make this code work
            var partitionKey = new PartitionKeyBuilder()
                .Add(trx.tenantId)
                .Add(trx.companyId)
                .Add(trx.id)
                .Build();

            await _realRepo.CreateAsync(realization, partitionKey);

            nextDate = frequency.NextDate(nextDate);
            installmentNum++;
        }
    }

}


public static class RecurringRealizationsBuilder
{
    public static Realization BuildRecurringRealization(Transaction trx, int installmentNum, DateTime expectedDate)
    {
        return new Realization
        {
            id = Guid.NewGuid().ToString(),
            tenantId = trx.tenantId,
            companyId = trx.companyId,
            transactionId = trx.id,
            installmentNum = installmentNum,
            flow = null,
            status = "Pending",
            amount = trx.amount,   // full amount every occurrence
            currency = trx.currency,
            fxRate = null,
            amountInBase = null,
            expectedDate = expectedDate,
            createdAt = DateTimeOffset.UtcNow
        };
    }
}

public static class RecurringFrequencyExtensions
{
    public static DateTime NextDate(this RecurringFrequency frequency, DateTime from) =>
        frequency switch
        {
            RecurringFrequency.Weekly => from.AddDays(7),
            RecurringFrequency.Monthly => from.AddMonths(1),
            RecurringFrequency.Quarterly => from.AddMonths(3),
            RecurringFrequency.Yearly => from.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency))
        };
}