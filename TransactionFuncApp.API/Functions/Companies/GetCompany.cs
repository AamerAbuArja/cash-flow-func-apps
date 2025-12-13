using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Companies;

public class GetCompany
{
    private readonly ICompanyService _service;
    public GetCompany(ICompanyService service) => _service = service;

    [Function("GetCompany")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenants/{tenantId}/companies/{companyId}")] HttpRequestData req,
        string tenantId, string companyId)
    {
        var c = await _service.GetAsync(tenantId, companyId);
        if (c == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await res.WriteAsJsonAsync(c);
        return res;
    }
}
