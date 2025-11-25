using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using Polly;
using Polly.Retry;
using System.Threading.Tasks;
using System;

public class TransactionSbProcessor
{
    private readonly CosmosRepository _repo;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TransactionSbProcessor(CosmosRepository repo, AsyncRetryPolicy retryPolicy)
    {
        _repo = repo;
        _retryPolicy = retryPolicy;
    }

    [Function("TransactionSbProcessor")]
    public async Task Run(
        [ServiceBusTrigger("transactions-queue", Connection = "ServiceBusConnectionString")] string message)
    {
        // Parse envelope
        var doc = JsonSerializer.Deserialize<JsonElement>(message);

        if (!doc.TryGetProperty("command", out var cmdProp))
            throw new InvalidOperationException("Invalid envelope: missing 'command'");

        var command = cmdProp.GetString();

        // All commands expect payload
        var payload = doc.GetProperty("payload");

        // Extract hierarchical keys
        string tenantId = payload.TryGetProperty("TenantId", out var t) ? t.GetString()! : null!;
        string companyId = payload.TryGetProperty("CompanyId", out var c) ? c.GetString()! : null!;

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(companyId))
            throw new InvalidOperationException("Payload must include TenantId and CompanyId for hierarchical partition keys.");

        await _retryPolicy.ExecuteAsync(async () =>
        {
            switch (command)
            {
                case "CreateTransaction":
                case "UpdateTransaction":
                    {
                        var dto = JsonSerializer.Deserialize<TransactionDto>(payload.GetRawText())!;
                        await _repo.UpsertTransactionAsync(dto);
                        break;
                    }

                case "CreateBulkTransactions":
                    {
                        var arr = JsonSerializer.Deserialize<TransactionDto[]>(payload.GetRawText())!;
                        await _repo.BulkInsertAsync(arr);
                        break;
                    }

                case "DeleteTransaction":
                    {
                        var id = payload.GetProperty("Id").GetString();

                        if (string.IsNullOrWhiteSpace(id))
                            throw new InvalidOperationException("DeleteTransaction requires Id.");

                        // Correct hierarchical delete
                        await _repo.DeleteTransactionAsync(id!, tenantId, companyId);
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unknown command '{command}'");
            }
        });
    }
}
