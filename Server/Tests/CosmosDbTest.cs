using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Server.Tests;

public class CosmosDbTest
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbTest> _logger;
    private readonly Container _container;

    public CosmosDbTest(CosmosClient cosmosClient, ILogger<CosmosDbTest> logger, string databaseName, string containerName)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
        
        // Get existing database and container
        Database database = _cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(containerName);
        
        _logger.LogInformation("Connected to database: {DatabaseName} and container: {ContainerName}", 
            databaseName, containerName);
    }

    public async Task RunConnectionTests()
    {
        _logger.LogInformation("Starting Cosmos DB Connection Tests");
        _logger.LogInformation($"Connecting to endpoint: {_cosmosClient.Endpoint}");
        _logger.LogInformation("Connection mode: {0}", _cosmosClient.ClientOptions.ConnectionMode);
        _logger.LogInformation("Request timeout: {0}s", _cosmosClient.ClientOptions.RequestTimeout.TotalSeconds);

        try
        {
            // Test 1: Basic Connection
            _logger.LogInformation("Test 1: Testing basic connection");
            var dbResponse = await _cosmosClient.GetDatabase(_container.Database.Id).ReadAsync();
            _logger.LogInformation("Test 1: Successfully connected to database {DatabaseId}", dbResponse.Resource.Id);

            // Test 2: Container Access
            _logger.LogInformation("Test 2: Testing container access");
            var containerResponse = await _container.ReadContainerAsync();
            _logger.LogInformation("Test 2: Successfully accessed container {ContainerId}", containerResponse.Resource.Id);

            // Test 3: Simple Write Operation
            _logger.LogInformation("Test 3: Testing write operation");
            var testDoc = new { id = $"test_{Guid.NewGuid()}", message = "test" };
            var createResponse = await _container.CreateItemAsync(testDoc);
            _logger.LogInformation("Test 3: Successfully wrote test document with id {DocumentId}", testDoc.id);

            // Test 4: Simple Read Operation
            _logger.LogInformation("Test 4: Testing read operation");
            var readResponse = await _container.ReadItemAsync<dynamic>(testDoc.id, new PartitionKey(testDoc.id));
            _logger.LogInformation("Test 4: Successfully read test document");

            // Test 5: Simple Delete Operation
            _logger.LogInformation("Test 5: Testing delete operation");
            await _container.DeleteItemAsync<dynamic>(testDoc.id, new PartitionKey(testDoc.id));
            _logger.LogInformation("Test 5: Successfully deleted test document");
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB Error: {Message}. Status: {Status}, Sub-status: {SubStatus}", 
                ex.Message, ex.StatusCode, ex.SubStatusCode);
            _logger.LogError("Diagnostics: {Diagnostics}", ex.Diagnostics?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during connection tests");
        }
    }
}
