namespace TransactionFuncApp.API.Models;

public class Transaction
{
    public string id { get; set; } = default!;
    public string tenantId { get; set; } = default!;
    public string companyId { get; set; } = default!;

    public string type { get; set; } = default!;          // e.g. Invoice, Expense
    public string category { get; set; } = default!;      // category code/name
    public string description { get; set; } = default!;
    public string? relatedTo { get; set; }

    public decimal amount { get; set; }
    public string currency { get; set; } = default!;

    public string installmentMode { get; set; } = "None"; // None/Manual/Auto
    public int? installmentCount { get; set; }
    public int? installmentInterval { get; set; }         // in days (assumption)

    public string? dueDate { get; set; }                   // ISO date string (YYYY-MM-DD) optional

    public DateTimeOffset createdAt { get; set; }

    // Partition key
    public string PartitionKey => tenantId;
}
