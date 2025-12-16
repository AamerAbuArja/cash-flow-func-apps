using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Services;

namespace TransactionFuncApp.API.Functions.Realizations;

public class CreateRealizationFunction
{
    private readonly IRealizationService _service;
    public CreateRealizationFunction(IRealizationService service) => _service = service;

    [Function("CreateRealization")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenants/{tenantId}/companies/{companyId}/transactions/{transactionId}/realizations")] HttpRequestData req,
        string tenantId, string companyId, string transactionId)
    {
        var dto = await req.ReadFromJsonAsync<CreateRealizationRequest>();
        if (dto == null) return req.CreateResponse(HttpStatusCode.BadRequest);

        try
        {
            var r = await _service.CreateAsync(tenantId, companyId, transactionId, dto);
            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(r);
            return res;
        }
        catch (ValidationException vex)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => e.ErrorMessage) });
            return res;
        }
        catch (Exception ex)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteAsJsonAsync(new { error = ex.Message });
            return res;
        }
    }
}
