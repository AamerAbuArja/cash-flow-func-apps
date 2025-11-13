using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;
using System;
using Azure.Messaging.ServiceBus;


public class TransactionsHttpFunctions
{
	private readonly IValidator<TransactionDto> _validator;
	private readonly IHttpClientFactory _httpFactory;
	private readonly ServiceBusClient? _serviceBusClient;

	public TransactionsHttpFunctions(IValidator<TransactionDto> validator, IHttpClientFactory httpFactory, ServiceBusClient? serviceBusClient = null)
	{
		_validator = validator;
		_httpFactory = httpFactory;
		_serviceBusClient = serviceBusClient;
	}

	// CreateTransaction
	[Function("CreateTransaction")]
	public async Task<HttpResponseData> CreateTransaction(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequestData req)
	{
		// Parse request
		var dto = await req.ReadFromJsonAsync<TransactionDto>();
		if (dto == null)
		{
			await WriteBadRequestAsync(req, "Invalid request body");
			return null;
		}

		// Validate input
		var validation = await _validator.ValidateAsync(dto);
		if (!validation.IsValid)
		{
			await WriteBadRequestAsync(req, JsonSerializer.Serialize(validation.Errors));
			return null;
		}

		// Prepare envelope for Service Bus
		var envelope = new
		{
			command = "CreateTransaction",
			payload = dto,
			correlationId = Guid.NewGuid().ToString(),
			timestamp = DateTime.UtcNow
		};

		if (_serviceBusClient != null)
		{
			var sender = _serviceBusClient.CreateSender("transactions-queue");
			var message = new ServiceBusMessage(JsonSerializer.Serialize(envelope));
			await sender.SendMessageAsync(message);
		}

		var accepted = req.CreateResponse(HttpStatusCode.Accepted);
		await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
		return accepted;
	}

	private static async Task<HttpResponseData> WriteBadRequestAsync(HttpRequestData req, string message)
	{
		var response = req.CreateResponse(HttpStatusCode.BadRequest);
		await response.WriteStringAsync(message);
		return response;
	}

	// CreateBulkTransactions
	[Function("CreateBulkTransactions")]
	public async Task<HttpResponseData> CreateBulkTransactions(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions/bulk")] HttpRequestData req)
	{
		var dtos = await req.ReadFromJsonAsync<TransactionDto[]>();
		if (dtos == null || dtos.Length == 0)
			return await BadRequest(req, "Invalid or empty body");

		// Validate each
		foreach (var dto in dtos)
		{
			var res = await _validator.ValidateAsync(dto);
			if (!res.IsValid)
				return await BadRequest(req, JsonSerializer.Serialize(res.Errors));
		}

		var envelope = new
		{
			command = "CreateBulkTransactions",
			payload = dtos,
			correlationId = Guid.NewGuid().ToString(),
			timestamp = DateTime.UtcNow
		};

		if (_serviceBusClient != null)
		{
			var sender = _serviceBusClient.CreateSender("transactions-queue");
			await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
		}

		var accepted = req.CreateResponse(HttpStatusCode.Accepted);
		await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
		return accepted;
	}

	// UpdateTransaction
	[Function("UpdateTransaction")]
	public async Task<HttpResponseData> UpdateTransaction(
			[HttpTrigger(AuthorizationLevel.Function, "put", Route = "transactions/{id}")] HttpRequestData req,
			string id)
	{
		var dto = await req.ReadFromJsonAsync<TransactionDto>();
		if (dto == null)
			return await BadRequest(req, "Invalid body");

		if (dto.Id != id)
			return await BadRequest(req, "Path id and body id mismatch");

		var validation = await _validator.ValidateAsync(dto);
		if (!validation.IsValid)
			return await BadRequest(req, JsonSerializer.Serialize(validation.Errors));

		var envelope = new
		{
			command = "UpdateTransaction",
			payload = dto,
			correlationId = Guid.NewGuid().ToString(),
			timestamp = DateTime.UtcNow
		};

		if (_serviceBusClient != null)
		{
			var sender = _serviceBusClient.CreateSender("transactions-queue");
			await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
		}

		var accepted = req.CreateResponse(HttpStatusCode.Accepted);
		await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
		return accepted;
	}

	// DeleteTransaction
	[Function("DeleteTransaction")]
	public async Task<HttpResponseData> DeleteTransaction(
			[HttpTrigger(AuthorizationLevel.Function, "delete", Route = "transactions/{id}")] HttpRequestData req,
			string id)
	{
		// Build a small envelope for delete (only ids needed)
		var envelope = new
		{
			command = "DeleteTransaction",
			payload = new { Id = id },
			correlationId = Guid.NewGuid().ToString(),
			timestamp = DateTime.UtcNow
		};

		if (_serviceBusClient != null)
		{
			var sender = _serviceBusClient.CreateSender("transactions-queue");
			await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
		}

		var accepted = req.CreateResponse(HttpStatusCode.Accepted);
		await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
		return accepted;
	}

	// Helper
	private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
	{
		var resp = req.CreateResponse(HttpStatusCode.BadRequest);
		await resp.WriteStringAsync(message);
		return resp;
	}
}
