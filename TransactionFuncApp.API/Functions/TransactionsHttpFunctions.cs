using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;
using System;

public class TransactionsHttpFunctions
{
    private readonly IValidator<TransactionDto> _validator;
    private readonly IHttpClientFactory _httpFactory;

    public TransactionsHttpFunctions(IValidator<TransactionDto> validator, IHttpClientFactory httpFactory)
    {
        _validator = validator;
        _httpFactory = httpFactory;
    }

    // CreateTransaction
    [Function("CreateTransaction")]
    [OpenApiOperation(operationId: "CreateTransaction", tags: new[] { "Transactions" })]
    [OpenApiRequestBody("application/json", typeof(TransactionDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> CreateTransaction(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequestData req,
        [ServiceBusOutput("transactions-queue", Connection = "ServiceBusConnectionString")] IAsyncCollector<string> output)
    {
        var dto = await req.ReadFromJsonAsync<TransactionDto>();
        if (dto == null)
            return await BadRequest(req, "Invalid body");

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return await BadRequest(req, JsonSerializer.Serialize(validation.Errors));

        var envelope = new
        {
            command = "CreateTransaction",
            payload = dto,
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        await output.AddAsync(JsonSerializer.Serialize(envelope));

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // CreateBulkTransactions
    [Function("CreateBulkTransactions")]
    [OpenApiOperation(operationId: "CreateBulkTransactions", tags: new[] { "Transactions" })]
    [OpenApiRequestBody("application/json", typeof(TransactionDto[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> CreateBulkTransactions(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions/bulk")] HttpRequestData req,
        [ServiceBusOutput("transactions-queue", Connection = "ServiceBusConnectionString")] IAsyncCollector<string> output)
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

        await output.AddAsync(JsonSerializer.Serialize(envelope));
        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // UpdateTransaction
    [Function("UpdateTransaction")]
    [OpenApiOperation(operationId: "UpdateTransaction", tags: new[] { "Transactions" })]
    [OpenApiRequestBody("application/json", typeof(TransactionDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> UpdateTransaction(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "transactions/{id}")] HttpRequestData req,
        string id,
        [ServiceBusOutput("transactions-queue", Connection = "ServiceBusConnectionString")] IAsyncCollector<string> output)
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

        await output.AddAsync(JsonSerializer.Serialize(envelope));
        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // DeleteTransaction
    [Function("DeleteTransaction")]
    [OpenApiOperation(operationId: "DeleteTransaction", tags: new[] { "Transactions" })]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> DeleteTransaction(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "transactions/{id}")] HttpRequestData req,
        string id,
        [ServiceBusOutput("transactions-queue", Connection = "ServiceBusConnectionString")] IAsyncCollector<string> output)
    {
        // Build a small envelope for delete (only ids needed)
        var envelope = new
        {
            command = "DeleteTransaction",
            payload = new { Id = id },
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        await output.AddAsync(JsonSerializer.Serialize(envelope));
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
