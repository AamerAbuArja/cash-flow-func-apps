using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Companies;

public class UpdateCompany
{
    private readonly ICompanyService _service;
    public UpdateCompany(ICompanyService service) => _service = service;

    [Function("UpdateCompany")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tenants/{tenantId}/companies/{companyId}")] HttpRequestData req,
        string tenantId, string companyId)
    {
        var dto = await req.ReadFromJsonAsync<UpdateCompanyRequest>();
        if (dto == null) return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        try
        {
            var updated = await _service.UpdateAsync(tenantId, companyId, dto);
            if (updated == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await res.WriteAsJsonAsync(updated);
            return res;
        }
        catch (ValidationException vex)
        {
            var res = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => e.ErrorMessage) });
            return res;
        }
    }
}
