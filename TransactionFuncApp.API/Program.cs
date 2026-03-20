using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using TransactionFuncApp.API.Repositories;
using TransactionFuncApp.API.Services;
using TransactionFuncApp.API.Validators;
using TransactionFuncApp.API.Models;
using Azure.Core.Serialization;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // Configure JSON serialization globally for the worker
        worker.Serializer = new JsonObjectSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,

            // IMPORTANT:
            // - Accept enum strings ("Income")
            // - Reject numeric enum values (0,1,...)
            Converters =
            {
                new JsonStringEnumConverter(
                    JsonNamingPolicy.CamelCase,
                    allowIntegerValues: false
                )
            }
        });
    })
    .ConfigureServices(services =>
    {
        // -------------------------------------------------
        // Environment Variables
        // -------------------------------------------------
        var connectionString = Environment.GetEnvironmentVariable("Cosmos__ConnectionString");
        var databaseId = Environment.GetEnvironmentVariable("Cosmos__DatabaseId");
        var tenantsContainerId = Environment.GetEnvironmentVariable("Cosmos__TenantsContainerId") ?? "Tenant";
        var companiesContainerId = Environment.GetEnvironmentVariable("Cosmos__CompaniesContainerId") ?? "Company";
        var transactionsContainerId = Environment.GetEnvironmentVariable("Cosmos__TransactionsContainerId") ?? "Transaction";
        var realizationsContainerId = Environment.GetEnvironmentVariable("Cosmos__RealizationsContainerId") ?? "Realization";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Cosmos__ConnectionString is missing from environment variables.");

        if (string.IsNullOrWhiteSpace(databaseId))
            throw new InvalidOperationException("Cosmos__DatabaseId is missing from environment variables.");

        // -------------------------------------------------
        // Cosmos Client (Singleton)
        // -------------------------------------------------
        services.AddSingleton(_ =>
        {
            return new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        });

        // -------------------------------------------------
        // Generic Repository Registrations
        // -------------------------------------------------
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

        // -------------------------------------------------
        // FluentValidation
        // -------------------------------------------------
        services.AddValidatorsFromAssemblyContaining<TenantValidator>();

        // -------------------------------------------------
        // Domain Services
        // -------------------------------------------------
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRealizationService, RealizationService>();
    })
    .Build();

host.Run();
