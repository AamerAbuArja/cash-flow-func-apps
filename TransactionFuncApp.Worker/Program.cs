using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using System;

var host = new HostBuilder()
	.ConfigureFunctionsWorkerDefaults()
	.ConfigureServices((context, services) =>
	{
		// Register Cosmos client
		var cosmosConn = context.Configuration["Cosmos__ConnectionString"] ?? throw new ArgumentNullException("Cosmos__ConnectionString");
		services.AddSingleton(new CosmosClient(cosmosConn));

		// Register repo
		services.AddSingleton<CosmosRepository>();

		// Polly retry policy for DB ops (exponential)
		AsyncRetryPolicy retryPolicy = Policy.Handle<Exception>()
			.WaitAndRetryAsync(new[] {
				TimeSpan.FromSeconds(2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			});
		services.AddSingleton(retryPolicy);

		services.AddScoped<IValidator<TransactionDto>, TransactionDtoValidator>();
	})
	.Build();

host.Run();
