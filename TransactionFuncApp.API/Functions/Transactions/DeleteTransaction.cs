using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Transactions;

public class DeleteTransactionFunction
{
    private readonly ITransactionService _service;
    public DeleteTransactionFunction(ITransactionService service) => _service = service;

    [Function("DeleteTransaction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}")] HttpRequestData req, string tenantId, string companyId, string transactionId)
    {
        await _service.DeleteAsync(tenantId, companyId, transactionId);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { deleted = true });
        return res;
    }
}
