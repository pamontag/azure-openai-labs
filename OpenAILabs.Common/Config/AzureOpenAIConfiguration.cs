namespace OpenAILabs.Common.Config
{
    public class AzureOpenAIConfiguration
    {
        public string Endpoint { get; set; }
        public string DeploymentName { get; set; }
        public string ApiKey { get; set; }

        public int MaxTokens { get; set; }

        public int MaxConversationTokens { get; set; }

        public string Prompt { get; set; }  

        public double Temperature { get; set; }  

        public double FrequencyPenalty { get; set; } 

        public double PresencePenalty { get; set; }


    }
}
