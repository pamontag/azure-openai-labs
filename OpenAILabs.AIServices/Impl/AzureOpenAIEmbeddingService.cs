
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAILabs.Common.Config;
using System.Net;

namespace OpenAILabs.AIServices.Impl
{
    public class AzureOpenAIEmbeddingService : IEmbeddingService
    {
        private readonly ILogger _logger;
        private OpenAIClient _openAIClient = null;
        private AzureOpenAIConfiguration _configuration;
        public AzureOpenAIEmbeddingService(IOptionsSnapshot<AzureOpenAIConfiguration> configuration, ILogger<AzureOpenAIEmbeddingService> logger)
        {
            _configuration = configuration.Get("embedding");
            _openAIClient = new(new Uri(_configuration.Endpoint), new AzureKeyCredential(_configuration.ApiKey));            
            _logger = logger;
        }
        public float[] GetEmbeddings(string text)
        {
            EmbeddingsOptions options = new()
            {
                DeploymentName = _configuration.DeploymentName,
                Input = { text },
            };
            _logger.LogInformation($"Get Embeddings for : {text}");
            var returnValue = _openAIClient.GetEmbeddings(options);
            var vectors = returnValue.Value.Data[0].Embedding.ToArray();
            _logger.LogDebug($"Vectors {string.Join(",", vectors)}");
            _logger.LogInformation($"Usage - Prompt Tokens: {returnValue.Value.Usage.PromptTokens} - Total Tokens: {returnValue.Value.Usage.TotalTokens}");

            return vectors;
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            EmbeddingsOptions options = new()
            {
                DeploymentName = _configuration.DeploymentName,
                Input = { text },
            };
            _logger.LogInformation($"Get Embeddings for : {text}");
            var returnValue = await _openAIClient.GetEmbeddingsAsync(options);
            var vectors = returnValue.Value.Data[0].Embedding.ToArray();
            _logger.LogDebug($"Vectors {string.Join(",", vectors)}");
            _logger.LogInformation($"Usage - Prompt Tokens: {returnValue.Value.Usage.PromptTokens} - Total Tokens: {returnValue.Value.Usage.TotalTokens}");

            return vectors;
        }
    }
}
