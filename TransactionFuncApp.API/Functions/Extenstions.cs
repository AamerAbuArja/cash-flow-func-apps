using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace TransactionFuncApp.API.Extensions;

public static class HttpRequestDataExtensions
{
    public static HttpResponseData CreateJsonResponse(this HttpRequestData req, HttpStatusCode statusCode, object? body = null)
    {
        var res = req.CreateResponse(statusCode);
        if (body != null)
        {
            res.Headers.Add("Content-Type", "application/json");
            res.WriteString(JsonSerializer.Serialize(body));
        }
        return res;
    }
}
