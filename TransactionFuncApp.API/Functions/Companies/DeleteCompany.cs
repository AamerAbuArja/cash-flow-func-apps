using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Companies;

public class DeleteCompany
{
    private readonly ICompanyService _service;
    public DeleteCompany(ICompanyService service) => _service = service;

    [Function("DeleteCompany")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tenants/{tenantId}/companies/{companyId}")] HttpRequestData req,
        string tenantId, string companyId)
    {
        await _service.DeleteAsync(tenantId, companyId);
        var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { deleted = true });
        return res;
    }
}
