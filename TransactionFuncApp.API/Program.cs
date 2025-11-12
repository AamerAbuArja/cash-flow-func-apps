using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("worker")
            .ConfigureHttpClient(c =>
            {
                var baseUrl = context.Configuration["WORKER_BASE_URL"] ?? "";
                if (!string.IsNullOrEmpty(baseUrl))
                    c.BaseAddress = new Uri(baseUrl);
            });

        services.AddScoped<IValidator<TransactionDto>, TransactionDtoValidator>();
    })
    .Build();

host.Run();
