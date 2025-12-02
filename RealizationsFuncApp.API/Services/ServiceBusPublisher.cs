using System.Text.Json;
using Azure.Messaging.ServiceBus;

public class ServiceBusPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(string connectionString, string entityName)
    {
        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = 5,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30)
            }
        };
        _client = new ServiceBusClient(connectionString, clientOptions);
        _sender = _client.CreateSender(entityName);
    }

    public async Task SendCommandAsync<T>(T payload, string messageType, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
        };
        message.ApplicationProperties["messageType"] = messageType;
        await _sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
