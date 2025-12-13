using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Tenants;

public class DeleteTenant
{
    private readonly ITenantService _service;
    public DeleteTenant(ITenantService service) => _service = service;

    [Function("DeleteTenant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tenants/{tenantId}")] HttpRequestData req, string tenantId)
    {
        await _service.DeleteAsync(tenantId);
        var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { deleted = true });
        return res;
    }
}
