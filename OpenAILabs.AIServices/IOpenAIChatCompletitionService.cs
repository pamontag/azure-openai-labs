using OpenAILabs.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices
{
    public interface IOpenAIChatCompletitionService
    {
        Task<string> CleanTextAsync(string text, string documentName);
        Task<ChatSession> ChatAsync(ChatSession session);
        Task<string> GetChatCompletionAsync(string? sessionId, string promptText);
        Task<string> GetChatCompletionAsync(string? sessionId, string promptText, RoleEnum role);
        Task<string> GetChatCompletionWithResultSearchAsync(string? sessionId, string promptText, List<string> resultSearch);
        Task<string> GetChatCompletionWithResultSearchAsync(string? sessionId, string promptText, RoleEnum role, List<string> resultSearch);
        Task<ChatSession> ChatAsync(string sessionId, string conversation);
        Task<string> SummarizeChatSessionNameAsync(string? sessionId, string conversationText);
        Task<List<ChatSession>> GetAllChatSessionsAsync();
        Task<List<ChatHistoryItem>> GetChatSessionMessagesAsync(string? sessionId);
        Task CreateNewChatSessionAsync();
        Task RenameChatSessionAsync(string? sessionId, string newChatSessionName);
        Task DeleteChatSessionAsync(string? sessionId);
    }
}
