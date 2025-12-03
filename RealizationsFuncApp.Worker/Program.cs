using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")
                             ?? throw new InvalidOperationException("COSMOS_ENDPOINT missing");
        var cosmosKey = Environment.GetEnvironmentVariable("COSMOS_KEY")
                             ?? throw new InvalidOperationException("COSMOS_KEY missing");
        var db = Environment.GetEnvironmentVariable("COSMOS_DB") ?? "RealizationsDb";
        var container = Environment.GetEnvironmentVariable("COSMOS_CONTAINER") ?? "Realizations";

        services.AddSingleton(sp => new CosmosService(cosmosEndpoint, cosmosKey, db, container));
    })
    .Build();

host.Run();
