using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;
using TransactionFuncApp.API.Services;
using TransactionFuncApp.API.Validators;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        /*
         * CosmosClient
         * Configuration is resolved lazily to ensure local.settings.json
         * environment variables are available.
         */
        services.AddSingleton<CosmosClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            string cosmosConn = config["Cosmos__ConnectionString"]
                ?? throw new InvalidOperationException("Cosmos__ConnectionString missing");

            return new CosmosClient(
                cosmosConn,
                new CosmosClientOptions
                {
                    ApplicationName = "TransactionFuncApp"
                });
        });

        /*
         * Repositories
         * Each repository resolves configuration at runtime.
         */
        services.AddSingleton<ICosmosRepository<Tenant>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<CosmosClient>();

            return new CosmosRepository<Tenant>(
                client,
                config["Cosmos__DatabaseId"] ?? "MyDatabase",
                config["Cosmos__TenantsContainerId"] ?? "Tenants");
        });

        services.AddSingleton<ICosmosRepository<Company>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<CosmosClient>();

            return new CosmosRepository<Company>(
                client,
                config["Cosmos__DatabaseId"] ?? "MyDatabase",
                config["Cosmos__CompaniesContainerId"] ?? "Companies");
        });

        services.AddSingleton<ICosmosRepository<Transaction>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<CosmosClient>();

            return new CosmosRepository<Transaction>(
                client,
                config["Cosmos__DatabaseId"] ?? "MyDatabase",
                config["Cosmos__TransactionsContainerId"] ?? "Transactions");
        });

        services.AddSingleton<ICosmosRepository<Realization>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<CosmosClient>();

            return new CosmosRepository<Realization>(
                client,
                config["Cosmos__DatabaseId"] ?? "MyDatabase",
                config["Cosmos__RealizationsContainerId"] ?? "Realizations");
        });

        /*
         * Domain Services
         */
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRealizationService, RealizationService>();

        /*
         * FluentValidation
         */
        services.AddValidatorsFromAssemblyContaining<TenantValidator>();
        services.AddFluentValidationAutoValidation();
    })
    .Build();

host.Run();
