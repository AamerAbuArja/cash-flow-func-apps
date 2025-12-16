using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Transactions;

public class CreateTransactionFunction
{
    private readonly ITransactionService _service;

    public CreateTransactionFunction(ITransactionService service) => _service = service;

    [Function("CreateTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenants/{tenantId}/companies/{companyId}/transactions")] HttpRequestData req, string tenantId, string companyId)
    {
        var dto = await req.ReadFromJsonAsync<CreateTransactionRequest>();
        if (dto == null) return req.CreateResponse(HttpStatusCode.BadRequest);

        try
        {
            var trx = await _service.CreateAsync(tenantId, companyId, dto);
            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(trx);
            return res;
        }
        catch (ValidationException vex)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => e.ErrorMessage) });
            return res;
        }
        catch (Exception ex)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { error = ex.Message });
            return res;
        }
    }
}
