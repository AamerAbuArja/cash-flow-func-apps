using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using FluentValidation;
using TransactionFuncApp.API.Repositories;
using TransactionFuncApp.API.Services;
using TransactionFuncApp.API.Validators;
using TransactionFuncApp.API.Models;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Add Application Insights
        // services.AddApplicationInsightsTelemetryWorkerService();
        // services.ConfigureFunctionsApplicationInsights();

        // Get environment variables
        var connectionString = Environment.GetEnvironmentVariable("Cosmos__ConnectionString");
        var databaseId = Environment.GetEnvironmentVariable("Cosmos__DatabaseId");
        var tenantsContainerId = Environment.GetEnvironmentVariable("Cosmos__TenantsContainerId") ?? "Tenants";
        var companiesContainerId = Environment.GetEnvironmentVariable("Cosmos__CompaniesContainerId") ?? "Companies";
        var transactionsContainerId = Environment.GetEnvironmentVariable("Cosmos__TransactionsContainerId") ?? "Transactions";
        var realizationsContainerId = Environment.GetEnvironmentVariable("Cosmos__RealizationsContainerId") ?? "Realizations";

        // Validate required environment variables
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Cosmos__ConnectionString is missing from environment variables.");
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new InvalidOperationException("Cosmos__DatabaseId is missing from environment variables.");

        // Register CosmosClient as singleton
        services.AddSingleton<CosmosClient>(_ => new CosmosClient(connectionString));

        // Register specific repository instances for each entity type with factory pattern
        services.AddScoped<ICosmosRepository<Tenant>>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            return new CosmosRepository<Tenant>(client, databaseId, tenantsContainerId);
        });

        services.AddScoped<ICosmosRepository<Company>>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            return new CosmosRepository<Company>(client, databaseId, companiesContainerId);
        });

        services.AddScoped<ICosmosRepository<Transaction>>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            return new CosmosRepository<Transaction>(client, databaseId, transactionsContainerId);
        });

        services.AddScoped<ICosmosRepository<Realization>>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            return new CosmosRepository<Realization>(client, databaseId, realizationsContainerId);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<TenantValidator>();

        // Register services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRealizationService, RealizationService>();
    })
    .Build();

host.Run();