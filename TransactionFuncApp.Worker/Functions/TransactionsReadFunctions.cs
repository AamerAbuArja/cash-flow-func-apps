using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;
using System;

public class TransactionsReadFunctions
{
    private readonly CosmosRepository _repo;

    public TransactionsReadFunctions(CosmosRepository repo)
    {
        _repo = repo;
    }

    [Function("GetTransactionById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "transactions/{companyId}/{id}")] HttpRequestData req,
        string companyId,
        string id)
    {
        var item = await _repo.GetByIdAsync(id, companyId);
        if (item == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Not found");
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(item);
        return ok;
    }

    [Function("GetTransactions")]
    public async Task<HttpResponseData> GetTransactions(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "transactions/{companyId}")] HttpRequestData req,
        string companyId)
    {
        var items = await _repo.QueryTransactionsAsync(companyId, top: 100);
        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(items);
        return ok;
    }
}
