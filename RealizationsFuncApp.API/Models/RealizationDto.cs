using System;

public record RealizationDto
{
    public string? tenantId { get; init; }
    public string? companyId { get; init; }
    public string? transactionId { get; init; }
    public string? id { get; init; }
    public int installmentNum { get; init; }
    public string? type { get; init; }
    public string? flow { get; init; } // e.g. "in","out","internal"
    public string? status { get; init; }
    public decimal? amount { get; init; }
    public string? currency { get; init; }
    public decimal? fxRate { get; init; }
    public decimal? amountInBase { get; init; }
    public DateTime? expectedDate { get; init; }
    public DateTime? actualDate { get; init; }
    public DateTime? createdAt { get; init; }
    public DateTime? updatedAt { get; init; }
}
