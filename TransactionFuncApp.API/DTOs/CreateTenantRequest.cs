namespace TransactionFuncApp.API.DTOs;

public class CreateTenantRequest
{
    public string name { get; set; } = default!;
    public string? baseCurrency { get; set; }
    public string? subscription { get; set; }
}
