using Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from .env if available
builder.Configuration.AddEnvironmentVariables();

// Register CosmosClient and BlobServiceClient with connection strings from configuration
builder.Services.AddSingleton(new CosmosClient(builder.Configuration["COSMOS_DB_CONNECTION_STRING"]));
builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"]));

// Register services
builder.Services.AddSingleton<InvoiceHandler>();
builder.Services.AddControllers();

var app = builder.Build();

// Map Controllers
app.MapControllers();

app.Run();
