using TransactionFuncApp.API.Enums.TransactionEnums;

namespace TransactionFuncApp.API.DTOs;

public class UpdateTransactionRequest
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
    public int? installmentInterval { get; set; } // in days

    /// <summary>
    /// ISO date string (YYYY-MM-DD), optional
    /// </summary>
    public string? dueDate { get; set; }

    public DateTimeOffset createdAt { get; set; }
}
