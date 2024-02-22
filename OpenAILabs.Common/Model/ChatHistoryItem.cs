using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.Common.Model
{
    public record ChatHistoryItem
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string PromptText { get; set; }

        public string UserMessage { get; set; }
        public RoleEnum Role { get; set; }

        public string Sender { get; set; }
        public int? Tokens { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        public string SessionId { get; set; }

        public DateTime TimeStamp { get; set; }

        public ChatHistoryItem(string text, RoleEnum role)
        {
            new ChatHistoryItem(Guid.NewGuid().ToString(), nameof(role), default, text, text, role);
        }
        public ChatHistoryItem(string sessionId, string sender, int tokens, string text, string userMessage, RoleEnum role)
        {
            Id = Guid.NewGuid().ToString();
            Type = nameof(ChatHistoryItem);
            SessionId = sessionId;
            Sender = sender;
            Tokens = tokens;
            TimeStamp = DateTime.UtcNow;
            PromptText = text;
            UserMessage = userMessage;
            Role = role;
        }
        
    }
    public enum RoleEnum
    {
        System,
        User,
        Assistant
    }
}
