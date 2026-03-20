using TransactionFuncApp.API.Enums.TransactionEnums;

namespace TransactionFuncApp.API.Models;

public class Transaction
{
    public string id { get; set; } = default!;
    public string tenantId { get; set; } = default!;
    public string companyId { get; set; } = default!;
    public TransactionType type { get; set; } = TransactionType.Income;
    public string category { get; set; } = default!;
    public string description { get; set; } = default!;
    public string? relatedTo { get; set; }
    public decimal amount { get; set; }
    public string currency { get; set; } = default!;
    public InstallmentMode installmentMode { get; set; } = InstallmentMode.None;
    public int? installmentCount { get; set; }
    public int? installmentInterval { get; set; }         // in days (assumption)
    public string? dueDate { get; set; }                  // ISO date string (YYYY-MM-DD) optional
    public DateTimeOffset createdAt { get; set; }

    // Hierarchical partition key: [tenantId, companyId]
    public string PartitionKey => $"[\"{tenantId}\",\"{companyId}\"]";
}