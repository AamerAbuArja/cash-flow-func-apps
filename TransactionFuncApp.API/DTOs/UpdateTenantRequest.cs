namespace TransactionFuncApp.API.DTOs;

public class UpdateTenantRequest
{
    public string name { get; set; } = default!;
    public string? baseCurrency { get; set; }
    public string? subscription { get; set; }
}
