using System;
using System.Collections.Generic;

public record InstallmentDto(int Number, decimal Amount, DateTime DueDate);

public record TransactionDto(
    string TenantId,
    string CompanyId,
    string Id,
    string Type,
    string Category,
    string Description,
    string? RelatedTo,
    decimal Amount,
    string Currency,
    DateTime CreatedAt,
    string InstallmentMode,
    int? InstallmentCount,
    int? InstallmentInterval,
    List<InstallmentDto>? Installments,
    string? DueDate
);
