using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Transactions;

public class GetTransactionFunction
{
    private readonly ITransactionService _service;
    public GetTransactionFunction(ITransactionService service) => _service = service;

    [Function("GetTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}")] HttpRequestData req, string tenantId, string companyId, string transactionId)
    {
        var trx = await _service.GetAsync(tenantId, companyId, transactionId);
        if (trx == null) return req.CreateResponse(HttpStatusCode.NotFound);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(trx);
        return res;
    }
}
