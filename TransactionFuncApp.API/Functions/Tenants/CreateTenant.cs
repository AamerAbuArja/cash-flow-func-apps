using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Extensions;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Tenants;

public class CreateTenant
{
    private readonly ITenantService _service;

    public CreateTenant(ITenantService service) => _service = service;

    [Function("CreateTenant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenants")] HttpRequestData req)
    {
        var dto = await req.ReadFromJsonAsync<CreateTenantRequest>();
        if (dto == null) return req.CreateJsonResponse(HttpStatusCode.BadRequest, new { error = "Invalid payload" });

        try
        {
            var t = await _service.CreateAsync(dto);
            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(t);
            return res;
        }
        catch (ValidationException vex)
        {
            return req.CreateJsonResponse(HttpStatusCode.BadRequest, new { errors = vex.Errors.Select(e => e.ErrorMessage) });
        }
    }
}
