using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Companies;

public class CreateCompany
{
    private readonly ICompanyService _service;
    public CreateCompany(ICompanyService service) => _service = service;

    [Function("CreateCompany")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenants/{tenantId}/companies")] HttpRequestData req,
        string tenantId)
    {
        var dto = await req.ReadFromJsonAsync<CreateCompanyRequest>();
        if (dto == null) return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        try
        {
            var comp = await _service.CreateAsync(tenantId, dto);
            var res = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await res.WriteAsJsonAsync(comp);
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
