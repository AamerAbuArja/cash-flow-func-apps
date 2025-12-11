namespace TransactionFuncApp.API.Models;

public class Company
{
    public string id { get; set; } = default!;
    public string tenantId { get; set; } = default!;
    public string name { get; set; } = default!;
    public string? baseCurrency { get; set; }
    public decimal? openingBalance { get; set; }
    public decimal? closingBalance { get; set; }
    public string? industry { get; set; }

    public string PartitionKey => tenantId;
}
