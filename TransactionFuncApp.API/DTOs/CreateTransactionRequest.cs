using TransactionFuncApp.API.Enums.TransactionEnums;

namespace TransactionFuncApp.API.DTOs;

public class CreateTransactionRequest
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

    public string? dueDate { get; set; }

    // -------------------------
    // Installment Configuration
    // -------------------------

    public InstallmentMode installmentMode { get; set; } = InstallmentMode.None;

    // Auto
    public int? installmentCount { get; set; }
    public int? installmentInterval { get; set; } // in days

    // Manual
    public List<ManualInstallmentDto>? manualInstallments { get; set; }

    // Percentage
    public List<PercentageInstallmentDto>? percentageInstallments { get; set; }

    // Recurring
    public string? recurringFrequency { get; set; }  // "Weekly" | "Monthly" | "Quarterly" | "Yearly"
    public int? recurringEndAfter { get; set; }       // max occurrence count, null = indefinite
    public string? recurringEndDate { get; set; }     // hard stop date (YYYY-MM-DD), alternative to EndAfter

    public DateTimeOffset createdAt { get; set; }
}