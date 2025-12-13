using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Tenants;

public class GetTenant
{
    private readonly ITenantService _service;
    public GetTenant(ITenantService service) => _service = service;

    [Function("GetTenant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenants/{tenantId}")] HttpRequestData req, string tenantId)
    {
        var t = await _service.GetAsync(tenantId);
        if (t == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await res.WriteAsJsonAsync(t);
        return res;
    }
}
