using OpenAILabs.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace OpenAILabs.AIServices
{
    public interface IConversationHistoryService
    {
        Task<List<ChatHistoryItem>> GetSessionMessagesAsync(string sessionId);
        Task<List<ChatSession>> GetSessionsAsync();
        Task<ChatSession> InsertSessionAsync(ChatSession session);
        Task<ChatHistoryItem> InsertMessageAsync(ChatHistoryItem message);
        Task<ChatSession> UpdateSessionAsync(ChatSession session);
        Task UpsertSessionBatchAsync(params dynamic[] messages);
        Task DeleteSessionAndMessagesAsync(string sessionId);


    }
}
