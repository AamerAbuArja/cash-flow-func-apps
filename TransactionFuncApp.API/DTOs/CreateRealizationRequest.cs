namespace TransactionFuncApp.API.DTOs;

public class CreateRealizationRequest
{
    public int installmentNum { get; set; }
    public string? type { get; set; }
    public string? flow { get; set; }
    public string? status { get; set; }

    public decimal? amount { get; set; }
    public string? currency { get; set; }
    public decimal? fxRate { get; set; }
    public decimal? amountInBase { get; set; }

    public DateTime? expectedDate { get; set; }
}
