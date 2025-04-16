using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.Extensions.VectorData;
using System.ComponentModel;

namespace RetrievalAugmentedGeneration.JLDocs;

#pragma warning disable SKEXP0001


//THIS COULD BE RETREIVE DATA/CALL FUNCTION
public class JLDocsPlugin(ITextEmbeddingGenerationService? embeddingGenerationService, IVectorStoreRecordCollection<string, JLDocsVectorEntity> collection)
{
    private readonly ITextEmbeddingGenerationService? _embeddingGenerationService = embeddingGenerationService;
    private readonly IVectorStoreRecordCollection<string, JLDocsVectorEntity> _collection = collection;

    [KernelFunction("search_joblogic_documentation")]
    [Description("Searches for relevant Joblogic documentation using RAG")]
    public async Task<string[]> SearchDocsAsync(
        [Description("The input query to search for in Joblogic documentation")] string input)
    {
        if (_embeddingGenerationService == null)
        {
            throw new InvalidOperationException("Embedding generation service is not initialized");
        }

        List<string> searchResults = new List<string>();
        ReadOnlyMemory<float> searchVector = await _embeddingGenerationService.GenerateEmbeddingAsync(input);
        var searchResult = await _collection.VectorizedSearchAsync(searchVector, new()
        {
            Top = 3
        });

        await foreach (var record in searchResult.Results.Where(x => x.Score > 0.8))
        {
            searchResults.Add(record.Record.Description);
        }

        return searchResults.ToArray();
    }

    [KernelFunction("suggest_call_joblogic_support_team_with_question")]
    public void CallSupportTeam(string question)
    {
        Console.WriteLine("Supported suggest called with question: " + question);
        Console.WriteLine("******************************************************");
    }
}