using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // register validator and publisher via configuration
        services.AddSingleton<RealizationValidator>();

        var sbConn = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION")
                     ?? throw new InvalidOperationException("SERVICEBUS_CONNECTION missing");
        var sbEntity = Environment.GetEnvironmentVariable("SERVICEBUS_ENTITY") ?? "realizations-commands";

        services.AddSingleton(sp => new ServiceBusPublisher(sbConn, sbEntity));
    })
    .Build();

host.Run();
