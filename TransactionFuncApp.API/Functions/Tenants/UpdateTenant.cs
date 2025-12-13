using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Tenants;

public class UpdateTenant
{
    private readonly ITenantService _service;
    public UpdateTenant(ITenantService service) => _service = service;

    [Function("UpdateTenant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "tenants/{tenantId}")] HttpRequestData req, string tenantId)
    {
        var dto = await req.ReadFromJsonAsync<UpdateTenantRequest>();
        if (dto == null) return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        try
        {
            var updated = await _service.UpdateAsync(tenantId, dto);
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
