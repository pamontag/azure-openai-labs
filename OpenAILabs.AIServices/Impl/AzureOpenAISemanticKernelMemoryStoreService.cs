using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using OpenAILabs.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0052 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// TODO: Add HttpClient to be injested with retry policy
namespace OpenAILabs.AIServices.Impl
{
    public class AzureOpenAISemanticKernelMemoryStoreService : IMemoryStoreService
    {
        private readonly ILogger _logger;
        private ISemanticTextMemory _kernel = null;
        private AzureOpenAIConfiguration _openAIConfiguration;
        private MemoryConfiguration _memoryConfiguration;

        public AzureOpenAISemanticKernelMemoryStoreService(ILogger<AzureOpenAISemanticKernelMemoryStoreService> logger, IOptionsSnapshot<AzureOpenAIConfiguration> openAIConfiguration, IOptions<MemoryConfiguration> memoryConfiguration)
        {
            _logger = logger;
            _openAIConfiguration = openAIConfiguration.Get("embedding");
            _memoryConfiguration = memoryConfiguration.Value;

            _kernel = new MemoryBuilder()
                .WithAzureOpenAITextEmbeddingGeneration(_openAIConfiguration.DeploymentName, _openAIConfiguration.Endpoint, _openAIConfiguration.ApiKey)
                .WithMemoryStore(ChooseMemoryStore())
                .Build();
        }
        private IMemoryStore ChooseMemoryStore()
        {
            switch (_memoryConfiguration.MemoryStoreType)
            {
                case MemoryStoreTypeEnum.Volatile:
                    return new VolatileMemoryStore();
                case MemoryStoreTypeEnum.AzureAISearch:
                    return new AzureAISearchMemoryStore(_memoryConfiguration.Endpoint, _memoryConfiguration.ApiKey);
                case MemoryStoreTypeEnum.CosmosDB:
                    return null;
                case MemoryStoreTypeEnum.Postgres:
                    return null;
                default:
                    return new VolatileMemoryStore();
            }
        }

        public async Task<string> StoreMemoryAsync(string text, Dictionary<string, object> metadata)
        {
            try
            {
                string additionalMetadata = null;
                if(metadata != null)
                    additionalMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);
                return await _kernel.SaveInformationAsync(_memoryConfiguration.CollectionName, text, Guid.NewGuid().ToString(), String.Empty, additionalMetadata);

            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error storing memory");
                throw new Exception("Error storing memory", ex);
            }
        }
        public async Task<List<string>> SearchMemoryAsync(string query)
        {
            var response = _kernel.SearchAsync(_memoryConfiguration.CollectionName, query, _memoryConfiguration.Limit, _memoryConfiguration.MinRelevanceScore);
            List<string> result = new List<string>();
            _logger.LogInformation($"Get Informations from memory for: {query}");
            await foreach(var memory in response)
            {
                _logger.LogInformation($"Found: Text:{memory.Metadata.Text} - Id:{memory.Metadata.Id} Relevance:{memory.Relevance}");
                result.Add(memory.Metadata.Text);
            }
            return result;
        }
    }
}
