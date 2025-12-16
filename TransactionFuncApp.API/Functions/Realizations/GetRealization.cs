using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Realizations;

public class GetRealizationFunction
{
    private readonly IRealizationService _service;
    public GetRealizationFunction(IRealizationService service) => _service = service;

    [Function("GetRealization")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}/realizations/{realizationId}")] HttpRequestData req, string tenantId, string companyId, string transactionId, string realizationId)
    {
        var r = await _service.GetAsync(tenantId, companyId, transactionId, realizationId);
        if (r == null) return req.CreateResponse(HttpStatusCode.NotFound);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(r);
        return res;
    }
}
