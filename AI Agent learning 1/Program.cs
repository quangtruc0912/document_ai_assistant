using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using LocalPlugin;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.Azure.Cosmos;
using RetrievalAugmentedGeneration.JLDocs;
using RetrievalAugmentedGeneration.Extensions;
using System.Text.Json;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0020

var builder = WebApplication.CreateBuilder(args);
var azureOpenAiEndpoint = builder.Configuration["Portal:AzureOpenAiEndPoint"];
var azureOpenAiApiKey = builder.Configuration["Portal:AzureOpenAiApiKey"];
var modelDeploymentName = builder.Configuration["Portal:ModelDeploymentName"];
var cosmosDbConnectionString = builder.Configuration["Portal:CosmosDbConnectionString"];


//Settings
const VectorStoreToUse vectorStoreToUse = VectorStoreToUse.CosmosDb;

IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter, AutoInvocationFilter>();

kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: modelDeploymentName,
    endpoint: azureOpenAiEndpoint,
    apiKey: azureOpenAiApiKey
);

kernelBuilder.AddAzureOpenAITextEmbeddingGeneration("text-embedding-ada-002", azureOpenAiEndpoint, azureOpenAiApiKey);

var myFirstPlugin = new MyFirstPlugin();
Kernel kernel = kernelBuilder.Build();
kernel.ImportPluginFromObject(myFirstPlugin);


var embeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
var collection = GetCollection();


// ISemanticTextMemory semanticTextMemory = new MemoryBuilder()
//     .WithTextEmbeddingGeneration(kernel.GetRequiredService<ITextEmbeddingGenerationService>())
//     .WithMemoryStore(new AzureCosmosDBNoSQLMemoryStore(cosmosDbConnectionString, "jldocs", 1536, VectorDataType.Float32, VectorIndexType.DiskANN))
//     .Build();

//NEW
const bool importData = false;
const string vectorStoreCollection = "jldocs";
if (importData)
{
    string filePath = @"C:\Urls\urls.txt";
    string jsonContent = File.ReadAllText(filePath);
    List<string> urls = JsonSerializer.Deserialize<List<string>>(jsonContent);
    await new JLDocsImporterFromUrls(embeddingGenerationService, collection, urls).Import();
}

// Import the JLDocs plugin
kernel.ImportPluginFromObject(new JLDocsPlugin(embeddingGenerationService, collection));

var agent = new ChatCompletionAgent
{
    Name = "TestAgent",
    Kernel = kernel,
    Instructions = """
                   You are a Joblogic Agent that can exchange pleasantries and can answer questions about how to use Joblogic app via its documentation.
                   Please only used information from memory plugins but please ask all of them for data
                   Please include all 'More info links' used at the bottom of the answer
                   Please only answer questions about Joblogic. If you are ask about anything else please say 'I can only answer questions about Joblogic'
                   If dont know the answer please say 'I dont know the answer to that question' and call the support team with the question
                   """,

    HistoryReducer = new ChatHistoryTruncationReducer(1),
    Arguments = new KernelArguments
    (
        new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        }
    )
};


var history = new ChatHistory();

Console.OutputEncoding = Encoding.UTF8;
while (true)
{
    Console.Write("> Input:");
    var question = Console.ReadLine() ?? "";


    try
    {
        var arguments = new KernelArguments
        {
            ["input"] = question
        };
        string[] searchResultData = await kernel.InvokeAsync<string[]>("JLDocsPlugin", "search_joblogic_documentation", arguments);

        history.AddUserMessage($"Info that match : {string.Join($"{Environment.NewLine}***{Environment.NewLine}", searchResultData)}");
        history.AddUserMessage(question);
        await foreach (var response in agent.InvokeStreamingAsync(history))
        {
            foreach (var content in response.Content ?? "")
            {
                Console.Write(content);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("Exception: " + e.Message);
    }

    Console.WriteLine();
    Console.WriteLine("*********************");
    Console.WriteLine();
}

IVectorStoreRecordCollection<string, JLDocsVectorEntity> GetCollection()
{
    IVectorStoreRecordCollection<string, JLDocsVectorEntity> vectorStoreRecordCollection;
    switch (vectorStoreToUse)
    {
        // case VectorStoreToUse.InMemory:
        //     InMemoryVectorStore inMemoryVectorStore = new InMemoryVectorStore();
        //     vectorStoreRecordCollection = inMemoryVectorStore.GetCollection<string, JLDocsVectorEntity>("jldocs");
        //     break;
        // case VectorStoreToUse.AzureAiSearch:
        //     var azureAiSearchVectorStore = new AzureAISearchVectorStore(new SearchIndexClient(new Uri(azureSearchEndpoint), new AzureKeyCredential(azureSearchKey)));
        //     vectorStoreRecordCollection = azureAiSearchVectorStore.GetCollection<string, JLDocsVectorEntity>("heroes");
        //     break;
        case VectorStoreToUse.CosmosDb:
            var cosmosClient = new CosmosClient(cosmosDbConnectionString, new CosmosClientOptions()
            {
                // When initializing CosmosClient manually, setting this property is required 
                // due to limitations in default serializer. 
                UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
            });
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
            var database = cosmosClient.GetDatabase("jldocs");
            vectorStoreRecordCollection = new AzureCosmosDBNoSQLVectorStoreRecordCollection<JLDocsVectorEntity>(database, "jldocs", new()
            {
                PartitionKeyPropertyName = "Id",
                JsonSerializerOptions = jsonSerializerOptions
            });
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    return vectorStoreRecordCollection;
}

async Task<string[]> RagSearch(string input)
{
    List<string> searchResults = new List<string>();
    ReadOnlyMemory<float> searchVector = await embeddingGenerationService.GenerateEmbeddingAsync(input);
    var searchResult = await collection.VectorizedSearchAsync(searchVector, new()
    {
        Top = 3
    });

    await foreach (var record in searchResult.Results.Where(x => x.Score > 0.8))
    {
        searchResults.Add(record.Record.Description);
    }

    return searchResults.ToArray();
}