using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        
		var cosmosConn = context.Configuration["COSMOS_CONNECTION"] ?? throw new ArgumentNullException("COSMOS_CONNECTION");
		services.AddSingleton(new CosmosClient(cosmosConn));

        services.AddSingleton(sp => new CosmosService(cosmosConn));
    })
    .Build();

host.Run();
