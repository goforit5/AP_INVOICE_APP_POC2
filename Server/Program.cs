using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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

// Get connection strings with debug output
var cosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");
var storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

// Add debug output
Console.WriteLine($"Cosmos DB Connection String exists: {!string.IsNullOrEmpty(cosmosConnectionString)}");
Console.WriteLine($"Storage Connection String exists: {!string.IsNullOrEmpty(storageConnectionString)}");

if (string.IsNullOrEmpty(cosmosConnectionString))
{
    throw new InvalidOperationException("Cosmos DB connection string not found in configuration.");
}

if (string.IsNullOrEmpty(storageConnectionString))
{
    throw new InvalidOperationException("Azure Storage connection string not found in configuration.");
}

// Register services
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton<InvoiceHandler>();
builder.Services.AddControllers();

var app = builder.Build();

// Use CORS
app.UseCors("AllowReactApp");

// Map Controllers
app.MapControllers();

app.Run();