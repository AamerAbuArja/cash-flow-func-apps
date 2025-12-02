using System;
using System.Text.Json.Serialization;

public class Realization
{
    [JsonPropertyName("id")]
    public string? id { get; set; }

    public string? tenantId { get; set; }
    public string? companyId { get; set; }
    public string? transactionId { get; set; }
    public int installmentNum { get; set; }
    public string? type { get; set; }
    public string? flow { get; set; }
    public string? status { get; set; }
    public decimal? amount { get; set; }
    public string? currency { get; set; }
    public decimal? fxRate { get; set; }
    public decimal? amountInBase { get; set; }
    public DateTime? expectedDate { get; set; }
    public DateTime? actualDate { get; set; }
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}
