using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Realizations;

public class DeleteRealizationFunction
{
    private readonly IRealizationService _service;
    public DeleteRealizationFunction(IRealizationService service) => _service = service;

    [Function("DeleteRealization")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}/realizations/{realizationId}")] HttpRequestData req, string tenantId, string companyId, string transactionId, string realizationId)
    {
        await _service.DeleteAsync(tenantId, companyId, transactionId, realizationId);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { deleted = true });
        return res;
    }
}
