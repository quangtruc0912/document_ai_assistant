using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

namespace RetrievalAugmentedGeneration.JLDocs;

#pragma warning disable SKEXP0001


//THIS COULD BE RETREIVE DATA/CALL FUNCTION
public class JLDocsPlugin(ITextEmbeddingGenerationService? embeddingGenerationService, string collection)
{
    // [KernelFunction("get_informantion_about_joblogic")]
    // public async Task<string[]> Get(string question)
    // {
    //     Console.ForegroundColor = ConsoleColor.Green;
    //     Console.WriteLine("Plugin Called with question: " + question);
    //     Console.ForegroundColor = ConsoleColor.White;

    //     var memories = textMemory.SearchAsync(collection, question, limit: 3, minRelevanceScore: 0.75);
    //     var list = new List<string>();
    //     await foreach (var memory in memories)
    //     {
    //         list.Add(memory.Metadata.Text + $" [More Info link: {memory.Metadata.AdditionalMetadata}]");
    //     }

    //     return list.ToArray();
    // }

    [KernelFunction("suggest_call_joblogic_support_team_with_question")]
    public void CallSupportTeam(string question)
    {
        Console.WriteLine("Supported suggest called with question: " + question);
        Console.WriteLine("******************************************************");
    }
}