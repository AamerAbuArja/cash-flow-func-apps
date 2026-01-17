using Newtonsoft.Json;

namespace TransactionFuncApp.API.Models;

public class Tenant
{
    [JsonProperty("id")]
    public string id { get; set; } = default!;
    
    [JsonProperty("tenantId")]  // Add this if partition key is /tenantId
    public string tenantId { get; set; } = default!;
    
    [JsonProperty("name")]
    public string name { get; set; } = default!;
    
    [JsonProperty("baseCurrency")]
    public string? baseCurrency { get; set; }
    
    [JsonProperty("subscription")]
    public string? subscription { get; set; }

    // Change PartitionKey to match your container's partition key path
    public string PartitionKey => tenantId; // or whatever field matches
}