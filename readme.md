# Joblogic AI Assistant

This project implements a Retrieval-Augmented Generation (RAG) system using Microsoft Semantic Kernel to create an AI assistant that can answer questions about Joblogic documentation.

## Features

- Conversational AI agent using Azure OpenAI
- Vector-based document search using Azure Cosmos DB
- Retrieval-Augmented Generation (RAG) for accurate responses
- Support for importing documentation from URLs
- Real-time streaming responses

## Prerequisites

- .NET 7.0 or later
- Azure OpenAI Service
- Azure Cosmos DB account
- Required NuGet packages:
  - Microsoft.SemanticKernel
  - Microsoft.SemanticKernel.Connectors.AzureOpenAI
  - Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL
  - Microsoft.Azure.Cosmos

## Configuration

Create an `appsettings.json` file with the following configuration:

```json
{
  "Portal": {
    "AzureOpenAiEndPoint": "your-azure-openai-endpoint",
    "AzureOpenAiApiKey": "your-azure-openai-api-key",
    "ModelDeploymentName": "your-model-deployment-name",
    "CosmosDbConnectionString": "your-cosmos-db-connection-string"
  }
}
```

## Setup

1. Clone the repository
2. Install required NuGet packages
3. Configure your `appsettings.json` with the necessary credentials
4. (Optional) Import documentation by setting `importData = true` in Program.cs and providing a file with URLs

## Usage

Run the application and interact with the AI agent through the console. The agent will:
- Answer questions about Joblogic documentation
- Provide relevant links to documentation
- Handle general pleasantries
- Direct users to support for unknown questions

## Architecture

The project uses:
- Azure OpenAI for text generation and embeddings
- Azure Cosmos DB for vector storage
- Semantic Kernel for AI orchestration
- RAG pattern for accurate, context-aware responses

## Limitations

- The agent only answers questions about Joblogic
- Requires internet connection for Azure services
- Vector search threshold set to 0.8 for relevance

## Security Notes

- API keys and connection strings should be stored securely
- Never commit sensitive credentials to version control
- Use environment variables or secure configuration management in production
