using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Realizations;

public class ListRealizationsByTransaction
{
    private readonly IRealizationService _service;
    public ListRealizationsByTransaction(IRealizationService service) => _service = service;

    [Function("ListRealizationsByTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}/realizations")] HttpRequestData req, string tenantId, string companyId, string transactionId)
    {
        var list = await _service.ListByTransactionAsync(tenantId, transactionId);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(list);
        return res;
    }
}
