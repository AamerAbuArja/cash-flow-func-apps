using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;

namespace AggregationFunction.Functions;

public class AggregateRealizationsFunction
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _realizations;

    public AggregateRealizationsFunction(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
        _realizations = _cosmosClient
            .GetDatabase("CashFlowDB")
            .GetContainer("Realizations");
    }

    [Function("AggregateRealizations")]
    public async Task Run(
        [TimerTrigger("0 0 1 * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var logger = context.GetLogger("AggregateRealizations");

        var query = new QueryDefinition(
            "SELECT c.tenantId, c.companyId, c.amount, c.direction, c.realizationDate FROM c");

        var iterator = _realizations.GetItemQueryIterator<dynamic>(query);

        var items = new List<dynamic>();

        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync())
                items.Add(item);
        }

        await Aggregate(items, "DailyAggregates", GetDayKey);
        await Aggregate(items, "WeeklyAggregates", GetWeekKey);
        await Aggregate(items, "MonthlyAggregates", GetMonthKey);
        await Aggregate(items, "QuarterlyAggregates", GetQuarterKey);

        logger.LogInformation("Aggregation completed successfully");
    }
    private async Task Aggregate(
        IEnumerable<dynamic> realizations,
        string containerName,
        Func<DateTime, (DateTime Period, string PeriodString)> periodSelector)
    {
        var container = _cosmosClient
            .GetDatabase("CashFlowDB")
            .GetContainer(containerName);

        var groups = realizations
            .GroupBy(r =>
            {
                var period = periodSelector(r.realizationDate);
                return new
                {
                    r.tenantId,
                    r.companyId,
                    period.Period,
                    period.PeriodString
                };
            });

        foreach (var g in groups)
        {
            var inflow = g
                .Where(x => x.direction == "Inflow")
                .Sum(x => (decimal)x.amount);

            var outflow = g
                .Where(x => x.direction == "Outflow")
                .Sum(x => (decimal)x.amount);

            var id = $"{g.Key.tenantId}_{g.Key.companyId}_{g.Key.PeriodString}";

            var doc = new AggregateDocument
            {
                Id = id,
                TenantId = g.Key.tenantId,
                CompanyId = g.Key.companyId,
                TotalInflow = inflow,
                TotalOutflow = outflow,
                Period = g.Key.Period,
                PeriodString = g.Key.PeriodString,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await container.UpsertItemAsync(doc, new PartitionKey(doc.TenantId));
        }
    }
    private static (DateTime, string) GetDayKey(DateTime date)
    {
        var d = date.Date;
        return (d, d.ToString("yyyy-MM-dd"));
    }

    private static (DateTime, string) GetWeekKey(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var start = date.Date.AddDays(-diff);
        return (start, $"W{ISOWeek.GetWeekOfYear(start)}-{start.Year}");
    }

    private static (DateTime, string) GetMonthKey(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1);
        return (start, start.ToString("yyyy-MM"));
    }

    private static (DateTime, string) GetQuarterKey(DateTime date)
    {
        var quarter = (date.Month - 1) / 3 + 1;
        var start = new DateTime(date.Year, (quarter - 1) * 3 + 1, 1);
        return (start, $"Q{quarter}-{date.Year}");
    }
}