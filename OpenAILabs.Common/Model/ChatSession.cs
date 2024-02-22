using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using System.Xml.Linq;

namespace OpenAILabs.Common.Model
{
    public record ChatSession
    {
        public string Id { get; set; }

        public string Type { get; set; }
        public List<ChatHistoryItem> ChatHistory { get; set; }

        public string SessionId { get; set; }

        public string Name { get; set; }

        public string InputMessage { get; set; }

        public string OutputMessage { get; set; }

        public int? InputTokens { get; set; }

        public int? OutputTokens { get; set; }

        public int? TokensUsed { get { return InputTokens + OutputTokens;  } }

        public ChatSession()
        {
            Id = Guid.NewGuid().ToString();
            Type = nameof(ChatSession);
            SessionId = this.Id;
            InputTokens = 0;
            OutputTokens = 0;
            Name = "New Chat";
            ChatHistory = new List<ChatHistoryItem>();
        }

        public void AddMessage(ChatHistoryItem message)
        {
            ChatHistory.Add(message);
        }

        public void UpdateMessage(ChatHistoryItem message)
        {
            var match = ChatHistory.Single(m => m.Id == message.Id);
            var index = ChatHistory.IndexOf(match);
            ChatHistory[index] = message;
        }


    }
}
