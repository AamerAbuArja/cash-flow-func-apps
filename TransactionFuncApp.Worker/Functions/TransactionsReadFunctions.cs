using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

public class TransactionsReadFunctions
{
    private readonly CosmosRepository _repo;

    public TransactionsReadFunctions(CosmosRepository repo)
    {
        _repo = repo;
    }

    // -----------------------------------------------------
    // GET BY ID
    // -----------------------------------------------------
    // Route now includes BOTH tenantId and companyId
    [Function("GetTransactionById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Function, "get",
         Route = "transactions/{tenantId}/{companyId}/{id}")]
        HttpRequestData req,
        string tenantId,
        string companyId,
        string id)
    {
        var item = await _repo.GetByIdAsync(id, tenantId, companyId);

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

    // -----------------------------------------------------
    // GET LIST
    // -----------------------------------------------------
    [Function("GetTransactions")]
    public async Task<HttpResponseData> GetTransactions(
        [HttpTrigger(AuthorizationLevel.Function, "get",
         Route = "transactions/{tenantId}/{companyId}")]
        HttpRequestData req,
        string tenantId,
        string companyId)
    {
        var items = await _repo.QueryTransactionsAsync(tenantId, companyId, top: 100);

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(items);
        return ok;
    }
}
