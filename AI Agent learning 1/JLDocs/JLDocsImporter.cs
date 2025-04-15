using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.SemanticKernel.Memory;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001
namespace RetrievalAugmentedGeneration.JLDocs
{
    public class JLDocsImporterFromUrls
    {
        private readonly IVectorStoreRecordCollection<string, JLDocsVectorEntity> vector;
        private readonly ITextEmbeddingGenerationService? embeddingGenerationService;
        private readonly List<string> urls;

        public JLDocsImporterFromUrls(ITextEmbeddingGenerationService? embeddingGenerationService, IVectorStoreRecordCollection<string, JLDocsVectorEntity> vector, List<string> urls)
        {
            this.embeddingGenerationService = embeddingGenerationService;
            this.vector = vector;
            this.urls = urls;
        }

        public async Task Import()
        {
            for (int i = 0; i < urls.Count; i++)
            {
                var url = urls[i];
                Console.WriteLine($"Processing URL {i + 1}/{urls.Count}: {url}");

                var content = await FetchPageContent(url);

                if (content != null)
                {
                    await ExtractContentAndSave(url, content, i + 1);
                }
            }
        }

        private async Task<string?> FetchPageContent(string url)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch content from {url}: {ex.Message}");
                return null;
            }
        }

        private async Task ExtractContentAndSave(string url, string htmlContent, int index)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Find the content inside the block with class 'doc_content_block'
            var docContentBlock = doc.DocumentNode.SelectSingleNode("//div[@class='content_container_text_sec_in']");
            if (docContentBlock != null)
            {
                string contentText = docContentBlock.InnerText.Trim();
                Console.WriteLine($"Fetched content from {contentText}");
                if (!string.IsNullOrWhiteSpace(contentText))
                {
                    string sectionId = $"{url}";
                    string[] parts = url.Split('/');
                    string description = "Content extracted from content_container " + parts[parts.Length - 1]; // Optional description

                    // Save to memory
                    await WriteMemoryIfNeeded(sectionId, contentText, description, index);
                }
                else
                {
                    Console.WriteLine($"No content found inside 'doc_content_block' for URL: {url}");
                }
            }
            else
            {
                Console.WriteLine($"'doc_content_block' not found in URL: {url}");
            }
        }

        private async Task<bool> WriteMemoryIfNeeded(string contentId, string contentText, string contentDescription, int index, int retryCount = 0)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                try
                {
                    Console.WriteLine($"Saving: {contentId}");
                    var vectorEntity = await embeddingGenerationService.GenerateEmbeddingAsync(contentText.ToString());
  
                    await vector.UpsertAsync(new JLDocsVectorEntity
                    {
                        Id = index.ToString(),
                        Url = contentId,
                        Description = contentDescription,
                        DescriptionEmbedding = vectorEntity,
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error saving {contentId}: {e.Message}");
                    await Task.Delay(30000);
                    if (retryCount < 3)
                    {
                        var vectorEntity = await embeddingGenerationService.GenerateEmbeddingAsync(contentText.ToString());
                        await vector.UpsertAsync(new JLDocsVectorEntity
                        {
                            Id = index.ToString(),
                            Url = contentId,
                            Description = contentDescription,
                            DescriptionEmbedding = vectorEntity,
                        });
                    }
                }

                return true;
            }

            return false;
        }
    }
}