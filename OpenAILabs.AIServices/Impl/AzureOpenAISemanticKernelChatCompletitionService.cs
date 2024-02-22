using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;
using OpenAILabs.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.AI.OpenAI;
using OpenAILabs.Common.Model;
using static System.Collections.Specialized.BitVector32;
using SharpToken;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Runtime;

namespace OpenAILabs.AIServices.Impl
{
    public class AzureOpenAISemanticKernelChatCompletitionService : IOpenAIChatCompletitionService
    {
        private readonly ILogger _logger;
        private readonly IChatCompletionService _service = null;
        private readonly IConversationHistoryService _conversationHistoryService = null;
        private AzureOpenAIConfiguration _openAIConfiguration;
        private Kernel _kernel;
        private List<ChatSession> _sessions = new();
        private const string ENCODING_NAME = "cl100k_base";
        private GptEncoding _encoding;

        private readonly string _summarizePrompt = @"
Summarize this prompt in one or two words to use as a label in a button on a web page.
Do not use any punctuation." + Environment.NewLine;

        private readonly string _searchPrompt = @"
DOCUMENT:
{0}

QUESTION:
{1}

INSTRUCTIONS:
Answer the users QUESTION using the DOCUMENT text above.
You must answer in the same language of QUESTION.
Keep your answer ground in the facts of the DOCUMENT.
If the DOCUMENT doesn’t contain the facts to answer the QUESTION return 
- You are an assistant about board games that search inside an internal database.
- The answer to the question is not present and the user have to reformulate the question in a more detailed way.
- Some example of board game that you have knowledge: Res Arcana, Brass Birmingham, Spirit Island, KingDomino, Seasons, Great Western Trail, Tzolkin";

        public AzureOpenAISemanticKernelChatCompletitionService(ILogger<AzureOpenAISemanticKernelChatCompletitionService> logger, IConversationHistoryService conversationHistoryService, IOptionsSnapshot<AzureOpenAIConfiguration> openAIConfiguration)
        {
            _logger = logger;
            _openAIConfiguration = openAIConfiguration.Get("chatcompletition");
            _conversationHistoryService = conversationHistoryService;
            _encoding = GptEncoding.GetEncoding(ENCODING_NAME);

            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(_openAIConfiguration.DeploymentName, _openAIConfiguration.Endpoint, _openAIConfiguration.ApiKey)
                .Build();
            _service = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> CleanTextAsync(string text, string documentName)
        {

            var result = await _service.GetChatMessageContentAsync(String.Format(_openAIConfiguration.Prompt, documentName) + " \r\n \r\n" + text, new OpenAIPromptExecutionSettings()
            {
                MaxTokens = _openAIConfiguration.MaxTokens
            });
            var metadata = result.Metadata;
            if (metadata != null && metadata.ContainsKey("Usage"))
            {
                var usage = (CompletionsUsage)metadata["Usage"];
                _logger.LogInformation($"Token usage. Input tokens: {usage.PromptTokens}; Output tokens: {usage.CompletionTokens}");

            }
            return result.Content;
        }

        public async Task<string> GetChatCompletionWithResultSearchAsync(string? sessionId, string promptText, List<String> resultSearch)
        {
            return await GetChatCompletionWithResultSearchAsync(sessionId, promptText, RoleEnum.User, resultSearch);
        }

        public async Task<string> GetChatCompletionWithResultSearchAsync(string? sessionId, string promptText, RoleEnum role, List<String> resultSearch)
        {
            return await GetChatCompletionAsync(sessionId, promptText, role, resultSearch);
        }

        public async Task<string> GetChatCompletionAsync(string? sessionId, string promptText)
        {
            return await GetChatCompletionAsync(sessionId, promptText, RoleEnum.User, null);
        }
        public async Task<string> GetChatCompletionAsync(string? sessionId, string promptText, RoleEnum role)
        {
            return await GetChatCompletionAsync(sessionId, promptText, role, null);
        }

        /// <summary>
        /// Get a completion from _openAiService
        /// </summary>
        public async Task<string> GetChatCompletionAsync(string? sessionId, string promptText, RoleEnum role, List<string> resultSearch)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            // Refresh Sessions
            _sessions = await _conversationHistoryService.GetSessionsAsync();

            string userMessage = promptText;
            if (resultSearch != null)
            {
                promptText = String.Format(_searchPrompt, String.Join(" ", resultSearch), userMessage);
            } 

            //Create a message object for the User Prompt and calculate token usage
            ChatHistoryItem prompt = CreatePromptMessage(sessionId, promptText, userMessage, role);
            
            //Grab conversation history up to the maximum configured tokens
            string conversation = GetChatSessionConversation(sessionId, prompt);

            //Generate a completion and tokens used from the user prompt and conversation
            var output = await ChatAsync(sessionId, conversation);

            //Create a message object for the completion
            ChatHistoryItem completion = CreateCompletionMessage(sessionId, output.OutputTokens.HasValue ? output.OutputTokens.Value : 0, output.OutputMessage);

            //Update the tokens used in the session
            ChatSession session = UpdateSessionTokens(sessionId, prompt.Tokens, completion.Tokens);

            //Insert/Update all of it in a transaction to Cosmos
            await _conversationHistoryService.UpsertSessionBatchAsync(prompt, completion, session);

            return session.OutputMessage;
        }

        public async Task<ChatSession> ChatAsync(string sessionId, string conversation)
        {
            ChatHistory history = new ChatHistory(_openAIConfiguration.Prompt);
            history.AddUserMessage(conversation);
            return await ChatAsync(history, new ChatSession());
        }

        public async Task<ChatSession> ChatAsync(ChatSession session)
        {
            ChatHistory history = new ChatHistory();
            foreach (var item in session.ChatHistory)
            {
                switch (item.Role)
                {
                    case RoleEnum.System:
                        history.AddSystemMessage(item.PromptText);
                        break;
                    case RoleEnum.User:
                        history.AddUserMessage(item.PromptText);
                        break;
                    case RoleEnum.Assistant:
                        history.AddMessage(AuthorRole.Assistant, item.PromptText);
                        break;
                }
            }
            return await ChatAsync(history, session);
        }

        /// <summary>
        /// Have OpenAI summarize the conversation based upon the prompt and completion text in the session
        /// </summary>
        public async Task<string> SummarizeChatSessionNameAsync(string? sessionId, string conversationText)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            var completition = await SummarizeAsync(conversationText);

            await RenameChatSessionAsync(sessionId, completition.OutputMessage);

            return completition.OutputMessage;
        }

        private async Task<ChatSession> SummarizeAsync(string conversationText)
        {
            ChatHistory history = new ChatHistory(_summarizePrompt);
            history.AddUserMessage(conversationText);
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
            {
                MaxTokens = 200,
                Temperature = 0,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };
            return await ChatAsync(history, new ChatSession(), settings);
        }

        private async Task<ChatSession> ChatAsync(ChatHistory history, ChatSession session)
        {
            return await ChatAsync(history, session, null);
        }

        private async Task<ChatSession> ChatAsync(ChatHistory history, ChatSession session, OpenAIPromptExecutionSettings settings)
        {
            try
            {
                if(settings == null )
                {
                    settings = new OpenAIPromptExecutionSettings()
                    {
                        MaxTokens = _openAIConfiguration.MaxTokens,
                        Temperature = _openAIConfiguration.Temperature,
                        FrequencyPenalty = _openAIConfiguration.FrequencyPenalty,
                        PresencePenalty = _openAIConfiguration.PresencePenalty
                    };
                }
               
                var result = await _service.GetChatMessageContentAsync(history, settings, _kernel);
                var metadata = result.Metadata;
                if (metadata != null && metadata.ContainsKey("Usage"))
                {
                    var usage = (CompletionsUsage)metadata["Usage"];
                    if (usage != null)
                    {
                        session.OutputTokens = usage.CompletionTokens;
                        session.InputTokens = usage.PromptTokens;
                        _logger.LogInformation($"Token usage. Input tokens: {usage.PromptTokens}; Output tokens: {usage.CompletionTokens}");
                    }
                }
                session.OutputMessage = result.Content;
                return session;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during chat");
                throw new Exception("Error during chat", ex);
            }
        }

        public async Task<List<ChatSession>> GetAllChatSessionsAsync()
        {
            return _sessions = await _conversationHistoryService.GetSessionsAsync();
        }

        /// <summary>
        /// Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
        /// </summary>
        public async Task<List<ChatHistoryItem>> GetChatSessionMessagesAsync(string? sessionId)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            List<ChatHistoryItem> chatMessages = new();

            if (_sessions.Count == 0)
            {
                return Enumerable.Empty<ChatHistoryItem>().ToList();
            }

            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            if (_sessions[index].ChatHistory.Count == 0)
            {
                // Messages are not cached, go read from database
                chatMessages = await _conversationHistoryService.GetSessionMessagesAsync(sessionId);

                // Cache results
                _sessions[index].ChatHistory = chatMessages;
            }
            else
            {
                // Load from cache
                chatMessages = _sessions[index].ChatHistory;
            }

            return chatMessages;
        }

        /// <summary>
        /// User creates a new Chat Session.
        /// </summary>
        public async Task CreateNewChatSessionAsync()
        {
            ChatSession session = new();

            _sessions.Add(session);

            await _conversationHistoryService.InsertSessionAsync(session);

        }

        /// <summary>
        /// Rename the Chat Session from "New Chat" to the summary provided by OpenAI
        /// </summary>
        public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            _sessions[index].Name = newChatSessionName;

            await _conversationHistoryService.UpdateSessionAsync(_sessions[index]);
        }

        /// <summary>
        /// User deletes a chat session
        /// </summary>
        public async Task DeleteChatSessionAsync(string? sessionId)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            _sessions.RemoveAt(index);

            await _conversationHistoryService.DeleteSessionAndMessagesAsync(sessionId);
        }

        /// <summary>
        /// Calculate token count for prompt text. Add user prompt to the chat session message list object
        /// </summary>
        private ChatHistoryItem CreatePromptMessage(string sessionId, string promptText, string userMessage, RoleEnum role)
        {
            ChatHistoryItem promptMessage = new(sessionId, nameof(role), default, promptText, userMessage, role);

            //Calculate tokens for the user prompt message. OpenAI calculates tokens for completion so can get those from there 
            promptMessage.Tokens = GetTokens(promptText);

            //Add to the cache
            int index = _sessions.FindIndex(s => s.SessionId == sessionId);
            _sessions[index].AddMessage(promptMessage);

            return promptMessage;

        }

        /// <summary>
        /// Get current conversation, including latest user prompt, from newest to oldest up to max conversation tokens
        /// </summary>
        private string GetChatSessionConversation(string sessionId, ChatHistoryItem prompt)
        {

            int? tokensUsed = prompt.Tokens;

            List<string> conversationBuilder = new List<string>() { prompt.PromptText };

            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            List<ChatHistoryItem> messages = _sessions[index].ChatHistory;


            //Start at the end of the list and work backwards
            //This includes the latest user prompt which is already cached
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                tokensUsed += messages[i].Tokens is null ? 0 : messages[i].Tokens;

                if (tokensUsed > _openAIConfiguration.MaxConversationTokens)
                    break;

                conversationBuilder.Add(messages[i].PromptText);
            }

            //Invert the chat messages to put back into chronological order and output as string.        
            string conversation = string.Join(Environment.NewLine, conversationBuilder.Reverse<string>());

            return conversation;

        }

        /// <summary>
        /// Add completion to the chat session message list object
        /// </summary>
        private ChatHistoryItem CreateCompletionMessage(string sessionId, int completionTokens, string completionText)
        {
            //Create completion message
            ChatHistoryItem completionMessage = new(sessionId, nameof(RoleEnum.Assistant), completionTokens, completionText, completionText, RoleEnum.Assistant);

            //Add to the cache
            int index = _sessions.FindIndex(s => s.SessionId == sessionId);
            _sessions[index].AddMessage(completionMessage);

            return completionMessage;
        }

        /// <summary>
        /// Update session with user prompt and completion tokens and update the cache
        /// </summary>
        private ChatSession UpdateSessionTokens(string sessionId, int? promptTokens, int? completionTokens)
        {

            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            //Update session with user prompt and completion tokens and update the cache
            _sessions[index].InputTokens += promptTokens;
            _sessions[index].OutputTokens += completionTokens;

            return _sessions[index];

        }

        /// <summary>
        /// Calculate the number of tokens from the user prompt
        /// </summary>
        private int GetTokens(string userPrompt)
        {
            //Get count of vectors on user prompt (return)
            return _encoding.Encode(userPrompt).Count;
        }
    }
}
