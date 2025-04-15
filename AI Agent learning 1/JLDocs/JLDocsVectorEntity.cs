using Microsoft.Extensions.VectorData;

namespace RetrievalAugmentedGeneration.JLDocs;
#pragma warning disable SKEXP0001

public class JLDocsVectorEntity
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string Url { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    //Needed for Cosmos DB
    // [VectorStoreRecordVector(Dimensions: 1536)]
    // public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}