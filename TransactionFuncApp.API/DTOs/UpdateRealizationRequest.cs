namespace TransactionFuncApp.API.DTOs;

public class UpdateRealizationRequest
{
    public string? status { get; set; }
    public DateTime? actualDate { get; set; }
    public decimal? amount { get; set; }
    public decimal? fxRate { get; set; }
    public decimal? amountInBase { get; set; }
}
