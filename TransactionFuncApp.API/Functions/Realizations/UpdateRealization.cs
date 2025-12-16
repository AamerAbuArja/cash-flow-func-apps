using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Realizations;

public class UpdateRealizationFunction
{
    private readonly IRealizationService _service;
    public UpdateRealizationFunction(IRealizationService service) => _service = service;

    [Function("UpdateRealization")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}/realizations/{realizationId}")] HttpRequestData req, string tenantId, string companyId, string transactionId, string realizationId)
    {
        var dto = await req.ReadFromJsonAsync<UpdateRealizationRequest>();
        if (dto == null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var updated = await _service.UpdateAsync(tenantId, companyId, transactionId, realizationId, dto);
        if (updated == null) return req.CreateResponse(HttpStatusCode.NotFound);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(updated);
        return res;
    }
}
