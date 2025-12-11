using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionFuncApp.API.Models;
using TransactionFuncApp.API.Repositories;
using TransactionFuncApp.API.Services;
using TransactionFuncApp.API.Validators;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((ctx, cfg) => { /* nothing extra */ })
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        // Cosmos client
        string cosmosConn = config["Cosmos__ConnectionString"]
            ?? throw new InvalidOperationException("Cosmos__ConnectionString missing");
        string databaseId = config["Cosmos__DatabaseId"] ?? "MyDatabase";
        string tenantsContainer = config["Cosmos__TenantsContainerId"] ?? "Tenants";
        string companiesContainer = config["Cosmos__CompaniesContainerId"] ?? "Companies";

        var cosmosClient = new CosmosClient(cosmosConn, new CosmosClientOptions { ApplicationName = "MyFuncApp" });
        services.AddSingleton(cosmosClient);
        services.AddSingleton(sp =>
        {
            var db = cosmosClient.GetDatabase(databaseId);
            var container = db.GetContainer(tenantsContainer);
            return container;
        });
        services.AddSingleton(sp =>
        {
            var db = cosmosClient.GetDatabase(databaseId);
            var container = db.GetContainer(companiesContainer);
            return container;
        });

        // Repositories
        services.AddSingleton<ICosmosRepository<Tenant>>(sp =>
            new CosmosRepository<Tenant>(sp.GetRequiredService<CosmosClient>(), databaseId, tenantsContainer));
        services.AddSingleton<ICosmosRepository<Company>>(sp =>
            new CosmosRepository<Company>(sp.GetRequiredService<CosmosClient>(), databaseId, companiesContainer));

        // Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICompanyService, CompanyService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<TenantValidator>();
        services.AddFluentValidationAutoValidation();

    })
    .Build();

host.Run();
