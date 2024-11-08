using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Load .env file from the current directory
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Configure CosmosDB settings
var cosmosDbConfig = builder.Configuration.GetSection("Azure:CosmosDb").Get<CosmosDbConfig>();
if (cosmosDbConfig == null)
{
    throw new InvalidOperationException("CosmosDB configuration is missing or invalid.");
}

// Configure Azure Storage
var storageConnectionString = builder.Configuration.GetValue<string>("Azure:BlobStorage:ConnectionString");
if (string.IsNullOrEmpty(storageConnectionString))
{
    throw new InvalidOperationException("Azure Storage connection string not found in configuration.");
}

// Configure Polly retry policy
var retryPolicy = Policy<HttpResponseMessage>
    .Handle<CosmosException>(ex => ex.StatusCode == HttpStatusCode.ServiceUnavailable)
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            builder.Services.BuildServiceProvider()
                .GetService<ILogger<InvoiceHandler>>()?
                .LogWarning("Attempt {RetryCount} to connect to Cosmos DB failed. Retrying in {TimeSpan}...",
                    retryCount, timeSpan);
        });

// Register services
builder.Services.AddSingleton(cosmosDbConfig);
builder.Services.AddSingleton(sp => 
{
    var clientOptions = new CosmosClientOptions
    {
        MaxRetryAttemptsOnRateLimitedRequests = 3,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60)
    };
    return new CosmosClient(cosmosDbConfig.ConnectionString, clientOptions);
});
builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton<InvoiceHandler>();
builder.Services.AddHttpClient(); // Add HttpClient factory
builder.Services.AddControllers();

// Add Cosmos DB Test
builder.Services.AddSingleton<CosmosDbTest>(sp => 
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosDbTest>>();
    return new CosmosDbTest(cosmosClient, logger, 
        cosmosDbConfig.DatabaseName, 
        cosmosDbConfig.InvoiceContainer);
});

var app = builder.Build();

// Use CORS
app.UseCors("AllowReactApp");

// Map Controllers
app.MapControllers();

// Run Cosmos DB tests
var cosmosTest = app.Services.GetRequiredService<CosmosDbTest>();
await cosmosTest.RunConnectionTests();

app.Run();
