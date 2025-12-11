namespace TransactionFuncApp.API.Models;

public class Tenant
{
    // Cosmos SQL API requires 'id' as string identifier
    public string id { get; set; } = default!;
    public string name { get; set; } = default!;
    public string? baseCurrency { get; set; }
    public string? subscription { get; set; }

    // convenience computed property for partition key (tenantId)
    // For Tenant documents partitionKey == id
    public string PartitionKey => id;
}
