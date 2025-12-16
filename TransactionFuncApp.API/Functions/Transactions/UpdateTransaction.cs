using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Transactions;

public class UpdateTransactionFunction
{
    private readonly ITransactionService _service;
    public UpdateTransactionFunction(ITransactionService service) => _service = service;

    [Function("UpdateTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}")] HttpRequestData req, string tenantId, string companyId, string transactionId)
    {
        var dto = await req.ReadFromJsonAsync<UpdateTransactionRequest>();
        if (dto == null) return req.CreateResponse(HttpStatusCode.BadRequest);

        try
        {
            var updated = await _service.UpdateAsync(tenantId, companyId, transactionId, dto);
            if (updated == null) return req.CreateResponse(HttpStatusCode.NotFound);
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(updated);
            return res;
        }
        catch (ValidationException vex)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => e.ErrorMessage) });
            return res;
        }
    }
}
