namespace TransactionFuncApp.API.DTOs;

public class UpdateCompanyRequest
{
    public string name { get; set; } = default!;
    public string? baseCurrency { get; set; }
    public decimal? openingBalance { get; set; }
    public decimal? closingBalance { get; set; }
    public string? industry { get; set; }
}
