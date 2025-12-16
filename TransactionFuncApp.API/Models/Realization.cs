namespace TransactionFuncApp.API.Models;

public class Realization
{
    public string id { get; set; } = default!;
    public string tenantId { get; set; } = default!;
    public string companyId { get; set; } = default!;
    public string transactionId { get; set; } = default!;

    public int installmentNum { get; set; }
    public string? type { get; set; }
    public string? flow { get; set; }       // e.g. In / Out
    public string? status { get; set; }     // e.g. Pending / Paid

    public decimal? amount { get; set; }
    public string? currency { get; set; }
    public decimal? fxRate { get; set; }
    public decimal? amountInBase { get; set; }

    public DateTime? expectedDate { get; set; }
    public DateTime? actualDate { get; set; }

    public DateTimeOffset createdAt { get; set; }
    public DateTimeOffset? updatedAt { get; set; }

    public string PartitionKey => tenantId;
}
