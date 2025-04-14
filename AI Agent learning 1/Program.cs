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

ISemanticTextMemory semanticTextMemory = new MemoryBuilder()
    .WithTextEmbeddingGeneration(kernel.GetRequiredService<ITextEmbeddingGenerationService>())
    .WithMemoryStore(new AzureCosmosDBNoSQLMemoryStore(cosmosDbConnectionString, "jldocs", 1536, VectorDataType.Float32, VectorIndexType.DiskANN))
    .Build();

//NEW
const bool importData = true;
const string vectorStoreCollection = "jldocs";
if (importData)
{
    string filePath = @"C:\Urls\urls.txt";
    string jsonContent = File.ReadAllText(filePath);
    List<string> urls = JsonSerializer.Deserialize<List<string>>(jsonContent);
    await new JLDocsImporterFromUrls(semanticTextMemory,urls).Import();
}

//NEW
kernel.ImportPluginFromObject(new JLDocsPlugin(semanticTextMemory, vectorStoreCollection));


var agent = new ChatCompletionAgent
{
    Name = "TestAgent",
    Kernel = kernel,
    Instructions = """
                   You are a Joblogic Agent that can exchange pleasantries and can answer questions about how to use Joblogic app via its documentation.
                   Please only used information from memory plugins but please ask all of them for data
                   Please include all 'More info links' used at the bottom of the answer
                   Please only answer questions about Joblogic. If you are ask about anything else please say 'I can only answer questions about Joblogic' and if you do not know answer 'I do not know 😔'
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
    history.AddUserMessage(question);

    try
    {
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
    //NEW
    history.RemoveToolCalls();
    await agent.ReduceAsync(history);

    Console.WriteLine();
    Console.WriteLine("*********************");
    Console.WriteLine();
}