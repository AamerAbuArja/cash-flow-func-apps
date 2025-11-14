using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using Polly;
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
	public async Task Run([ServiceBusTrigger("transactions-queue", Connection = "ServiceBusConnectionString")] string message)
	{
		// Envelope: { command, payload, correlationId, timestamp }
		var doc = JsonSerializer.Deserialize<JsonElement>(message);
		if (!doc.TryGetProperty("command", out var cmdProp))
			throw new InvalidOperationException("Invalid envelope (no command)");

		var cmd = cmdProp.GetString();

		await _retryPolicy.ExecuteAsync(async () =>
		{
			switch (cmd)
			{
				case "CreateTransaction":
					{
						var payload = doc.GetProperty("payload").GetRawText();
						var dto = JsonSerializer.Deserialize<TransactionDto>(payload)!;
						await _repo.UpsertTransactionAsync(dto);
						break;
					}
				case "CreateBulkTransactions":
					{
						var payload = doc.GetProperty("payload").GetRawText();
						var arr = JsonSerializer.Deserialize<TransactionDto[]>(payload)!;
						await _repo.BulkInsertAsync(arr);
						break;
					}
				case "UpdateTransaction":
					{
						var payload = doc.GetProperty("payload").GetRawText();
						var dto = JsonSerializer.Deserialize<TransactionDto>(payload)!;
						await _repo.UpsertTransactionAsync(dto);
						break;
					}
				case "DeleteTransaction":
					{
						var payload = doc.GetProperty("payload");
						var id = payload.GetProperty("Id").GetString();
						// CompanyId may be required to delete; try to read additional props
						string? companyId = null;
						if (payload.TryGetProperty("CompanyId", out var c))
							companyId = c.GetString();

						if (string.IsNullOrEmpty(companyId))
						{
							// best-effort: attempt to delete by reading the item first is expensive; we expect caller to include CompanyId
							throw new InvalidOperationException("Delete requires CompanyId in payload for partition key");
						}

						await _repo.DeleteTransactionAsync(id!, companyId);
						break;
					}
				default:
					throw new InvalidOperationException($"Unknown command {cmd}");
			}
		});
	}
}
