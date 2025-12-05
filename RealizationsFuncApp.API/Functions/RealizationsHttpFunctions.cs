using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using Azure.Messaging.ServiceBus;

using FluentValidation;

public class RealizationsHttpFunctions
{
    private readonly IValidator<RealizationDto> _validator;
    private readonly ServiceBusClient? _serviceBusClient;

    public RealizationsHttpFunctions(
        IValidator<RealizationDto> validator,
        ServiceBusClient? serviceBusClient = null)
    {
        _validator = validator;
        _serviceBusClient = serviceBusClient;
    }

    // -------------------------------------------------------------------
    // ExtractRealizationsFromTransaction
    // -------------------------------------------------------------------
    [Function("ExtractRealizationsFromTransaction")]
    public async Task<HttpResponseData> Extract(
        [HttpTrigger(AuthorizationLevel.Function, "post", 
            Route = "realizations/extract")] HttpRequestData req)
    {
        var dto = await req.ReadFromJsonAsync<RealizationDto>();
        if (dto == null)
            return await BadRequest(req, "Invalid body");

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return await BadRequest(req, JsonSerializer.Serialize(validation.Errors));

        var envelope = new
        {
            command = "ExtractRealizationsFromTransaction",
            payload = dto,
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        if (_serviceBusClient != null)
        {
            var sender = _serviceBusClient.CreateSender("realizations-queue");
            await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
        }

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(
            new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // -------------------------------------------------------------------
    // GetRealizations
    // -------------------------------------------------------------------
    [Function("GetRealizations")]
    public async Task<HttpResponseData> GetRealizations(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "realizations/{transactionId}")] HttpRequestData req,
        string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
            return await BadRequest(req, "transactionId is required");

        var envelope = new
        {
            command = "GetRealizations",
            payload = new { transactionId },
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        if (_serviceBusClient != null)
        {
            var sender = _serviceBusClient.CreateSender("realizations-queue");
            await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
        }

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(
            new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // -------------------------------------------------------------------
    // UpdateRealization
    // -------------------------------------------------------------------
    [Function("UpdateRealization")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Function, "put",
            Route = "realizations/{id}")] HttpRequestData req,
        string id)
    {
        var dto = await req.ReadFromJsonAsync<RealizationDto>();
        if (dto == null)
            return await BadRequest(req, "Invalid body");

        // Ensure the path id overrides any missing body id
        if (string.IsNullOrEmpty(dto.id))
        {
            dto = dto with { id = id };
        }

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return await BadRequest(req, JsonSerializer.Serialize(validation.Errors));

        var envelope = new
        {
            command = "UpdateRealization",
            payload = dto,
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        if (_serviceBusClient != null)
        {
            var sender = _serviceBusClient.CreateSender("realizations-queue");
            await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
        }

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(
            new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // -------------------------------------------------------------------
    // DeleteRealization
    // -------------------------------------------------------------------
    [Function("DeleteRealization")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "delete",
            Route = "realizations/{id}")] HttpRequestData req,
        string id)
    {
        if (string.IsNullOrEmpty(id))
            return await BadRequest(req, "id is required");

        var envelope = new
        {
            command = "DeleteRealization",
            payload = new { id },
            correlationId = Guid.NewGuid().ToString(),
            timestamp = DateTime.UtcNow
        };

        if (_serviceBusClient != null)
        {
            var sender = _serviceBusClient.CreateSender("realizations-queue");
            await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(envelope)));
        }

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteStringAsync(JsonSerializer.Serialize(
            new { status = "queued", correlationId = envelope.correlationId }));
        return accepted;
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
    {
        var resp = req.CreateResponse(HttpStatusCode.BadRequest);
        await resp.WriteStringAsync(message);
        return resp;
    }
}
