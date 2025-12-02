using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

public class ServiceBusListener
{
    private readonly CosmosService _cosmos;

    public ServiceBusListener(CosmosService cosmos)
    {
        _cosmos = cosmos;
    }

    [Function("ServiceBusListener")]
    public async Task Run([ServiceBusTrigger("%SERVICEBUS_ENTITY%", Connection = "SERVICEBUS_CONNECTION")] ServiceBusReceivedMessage message, FunctionContext context)
    {
        var body = message.Body.ToString();
        var messageType = message.ApplicationProperties.ContainsKey("messageType")
            ? message.ApplicationProperties["messageType"]?.ToString()
            : null;

        try
        {
            switch (messageType)
            {
                case "CreateRealization":
                    {
                        var createObj = JsonSerializer.Deserialize<Realization>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (createObj != null)
                            await _cosmos.CreateAsync(createObj);
                        break;
                    }
                case "UpdateRealization":
                    {
                        var updateObj = JsonSerializer.Deserialize<Realization>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (updateObj != null)
                            await _cosmos.UpsertAsync(updateObj);
                        break;
                    }
                case "DeleteRealization":
                    {
                        var delObj = JsonSerializer.Deserialize<Realization>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (delObj != null && !string.IsNullOrEmpty(delObj.id))
                            await _cosmos.DeleteAsync(delObj.id, delObj.companyId);
                        break;
                    }
                case "GetRealizations":
                    {
                        // Minimal: processor could write results to a reply queue or other store.
                        // For now: no-op or extend as needed.
                        break;
                    }
                default:
                    {
                        // unknown message type - decide policy (log, DLQ, etc.)
                        break;
                    }
            }
        }
        catch
        {
            // by rethrowing, message will be retried until Service Bus subscription's MaxDeliveryCount is reached.
            throw;
        }
    }
}
