using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Transactions;

public class CreateTransactionFunction
{
    private readonly ITransactionService _service;
    private readonly ILogger<CreateTransactionFunction> _logger;

    public CreateTransactionFunction(ITransactionService service, ILogger<CreateTransactionFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("CreateTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenants/{tenantId}/companies/{companyId}/transactions")] HttpRequestData req, string tenantId, string companyId)
    {
        _logger.LogInformation(
            "CreateTransaction triggered. TenantId: {TenantId}, CompanyId: {CompanyId}",
            tenantId,
            companyId);

        var dto = await req.ReadFromJsonAsync<CreateTransactionRequest>();
        if (dto == null)
        {
            _logger.LogWarning("Request body deserialization failed. DTO is null.");
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        try
        {
            _logger.LogInformation("Calling TransactionService.CreateAsync...");
            var trx = await _service.CreateAsync(tenantId, companyId, dto);
            _logger.LogInformation(
                "Transaction created successfully. TransactionId: {TransactionId}",
                trx?.id);
            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(trx);
            return res;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(
                "Validation failed: {Errors}",
                string.Join(", ", vex.Errors.Select(e => e.ErrorMessage)));
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => e.ErrorMessage) });
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception occurred while creating transaction.");
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { error = ex.Message });
            return res;
        }
    }
}
