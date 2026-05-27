using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos.Fluent;

var builder = FunctionsApplication.CreateBuilder(args);

//builder.ConfigureFunctionsWebApplication();
builder.Services.AddFunctionsWorkerDefaults();
builder.Services.AddSingleton(s =>
{
    var connectionString = s.GetRequiredService<IConfiguration>()["CosmosDbConnection"];
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Cosmos DB connection string is not configured.");
    }
    return new CosmosClientBuilder(connectionString).Build();
});

builder.Build().Run();
