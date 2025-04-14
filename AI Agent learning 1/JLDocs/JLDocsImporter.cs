using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.SemanticKernel.Memory;
using System.Collections.Generic;

#pragma warning disable SKEXP0001
namespace RetrievalAugmentedGeneration.JLDocs
{
    public class JLDocsImporterFromUrls
    {
        private readonly ISemanticTextMemory memory;
        private readonly List<string> urls;

        public JLDocsImporterFromUrls(ISemanticTextMemory memory, List<string> urls)
        {
            this.memory = memory;
            this.urls = urls;
        }

        public async Task Import()
        {
            foreach (var url in urls)
            {
                var content = await FetchPageContent(url);

                if (content != null)
                {
                    await ExtractContentAndSave(url, content);
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

        private async Task ExtractContentAndSave(string url, string htmlContent)
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
                    await WriteMemoryIfNeeded(sectionId, contentText, description);
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

        private async Task<bool> WriteMemoryIfNeeded(string contentId, string content, string contentDescription, int retryCount = 0)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                try
                {
                    Console.WriteLine($"Saving: {contentId}");
                    await memory.SaveInformationAsync("JLDocs", id: contentId, text: content, description: contentDescription, additionalMetadata: contentId);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error saving {contentId}: {e.Message}");
                    await Task.Delay(30000);
                    if (retryCount < 3)
                    {
                        await WriteMemoryIfNeeded(contentId, content, contentDescription, retryCount + 1);
                    }
                }

                return true;
            }

            return false;
        }
    }
}