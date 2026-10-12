using System;
using System.Collections.Generic;

/// <summary>
/// This .cs file creates a conversation (ChatSession object) that stores messages, the last updated timestamp, and a memory summary. 
/// </summary>

namespace GeminiWP81
{
    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "model"
        public string Text { get; set; }
    }

    public class ChatSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public string MemorySummary { get; set; }
        public int MemorySummaryMessageCount { get; set; }

        public ChatSession()
        {
            Id = Guid.NewGuid().ToString();
            Title = Gemini.AppText.Get("ConversationDefaultTitle");
            LastUpdated = DateTime.Now;
            Messages = new List<ChatMessage>();
            MemorySummary = string.Empty;
            MemorySummaryMessageCount = 0;
        }
    }
}
