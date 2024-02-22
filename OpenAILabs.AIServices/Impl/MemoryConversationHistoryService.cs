using Microsoft.Extensions.Logging;
using OpenAILabs.Common.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace OpenAILabs.AIServices.Impl
{
    public class MemoryConversationHistoryService : IConversationHistoryService
    {
        private readonly ILogger _logger;
        private ConcurrentDictionary<string, ChatSession> _sessions = new();
        public MemoryConversationHistoryService(ILogger<MemoryConversationHistoryService> logger)
        {
            _logger = logger;
        }
        public async Task DeleteSessionAndMessagesAsync(string sessionId)
        {
            var result = _sessions.TryRemove(sessionId, out _);
            if(!result)
            {
                _logger.LogWarning($"Session {sessionId} not found for deletion.");
            }
            await Task.CompletedTask;            
        }

        public async Task<List<ChatHistoryItem>> GetSessionMessagesAsync(string sessionId)
        {
            var result = _sessions.TryGetValue(sessionId, out var session) ? session.ChatHistory : new List<ChatHistoryItem>();
            return result;

        }

        public async Task<List<ChatSession>> GetSessionsAsync()
        {
            var result = _sessions.Values.ToList();
            return result;
        }

        public async Task<ChatHistoryItem> InsertMessageAsync(ChatHistoryItem message)
        {
            var currentSession = _sessions.TryGetValue(message.SessionId, out var session) ? session : await InsertSessionAsync(new ChatSession { SessionId = message.SessionId, ChatHistory = new List<ChatHistoryItem> { message } });
            currentSession.ChatHistory.Add(message);
            return message;

        }

        public async Task<ChatSession> InsertSessionAsync(ChatSession session)
        {
            _sessions.TryGetValue(session.SessionId, out var existingSession);
            if(existingSession != null)
            {
                _logger.LogWarning($"Session {session.SessionId} already exists.");
                return existingSession;
            }   
            _sessions.TryAdd(session.SessionId, session);
            return session;

        }

        public async Task<ChatSession> UpdateSessionAsync(ChatSession session)
        {
            _sessions.TryGetValue(session.SessionId, out var existingSession);
            if(existingSession == null)
            {
                _logger.LogWarning($"Session {session.SessionId} not found for update.");
                return null;
            }
            _sessions.TryUpdate(session.SessionId, session, existingSession);
            return session;
        }

        public async Task UpsertSessionBatchAsync(params dynamic[] messages)
        {
            
            if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
            {
                throw new ArgumentException("All items must have the same partition key.");
            }
            var sessionId = (string)messages.First().SessionId;
            _sessions.TryGetValue(sessionId, out var existingSession);
            if(existingSession == null)
            {
                var session = new ChatSession { SessionId = sessionId, ChatHistory = new List<ChatHistoryItem>() };
                foreach (var message in messages)
                {
                    if(message is ChatHistoryItem)
                    {
                        session.ChatHistory.Add(message);
                    }
                }
                _sessions.TryAdd(sessionId, session);
            }
            else
            {
                foreach (var message in messages)
                {
                    if (message is ChatHistoryItem)
                    {
                        if(existingSession.ChatHistory.Count(m => m.Id == message.Id) > 0)
                        {
                            _logger.LogWarning($"Message {message.Id} already exists in session {sessionId}.");
                            continue;
                        }
                        existingSession.ChatHistory.Add(message);
                    }
                }
                _sessions.TryUpdate(sessionId, existingSession, existingSession);
            }   

        }
    }
}
