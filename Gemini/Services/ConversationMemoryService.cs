using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Builds conversation context and updates the compact memory summary for each session.
/// </summary>

namespace GeminiWP81
{
    public sealed class ConversationMemoryService
    {
        private const int RecentMemoryMessageCount = 12;
        private const int MemorySummaryBatchMessageCount = 8;
        private readonly GeminiApiClient _apiClient;

        public ConversationMemoryService(GeminiApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task UpdateAsync(ChatSession session, string model, int generation)
        {
            List<ChatMessage> messages = GetConversationMessages(session);
            int summarizeThrough = ((messages.Count - RecentMemoryMessageCount) / 2) * 2;
            int summarizedCount = Math.Max(0, Math.Min(session.MemorySummaryMessageCount, messages.Count));
            summarizedCount -= summarizedCount % 2;
            if (summarizeThrough - summarizedCount < MemorySummaryBatchMessageCount) return;

            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine("Update the compact memory for this conversation. Keep only useful facts, user preferences, decisions, and unresolved tasks that will help answer future messages. Be concise, preserve important names and details, and do not answer the conversation.");
            if (!string.IsNullOrWhiteSpace(session.MemorySummary))
            {
                prompt.AppendLine();
                prompt.AppendLine("Existing memory:");
                prompt.AppendLine(session.MemorySummary);
            }

            prompt.AppendLine();
            prompt.AppendLine("New conversation messages:");
            for (int i = summarizedCount; i < summarizeThrough; i++)
            {
                ChatMessage message = messages[i];
                prompt.Append(message.Role == "model" ? "Assistant: " : "User: ");
                prompt.AppendLine(message.Text);
            }

            GeminiApiClient.GeminiApiResult result = await _apiClient.GenerateContentAsync(
                new List<ChatMessage> { new ChatMessage { Role = "user", Text = prompt.ToString() } },
                model,
                generation);
            GeminiApiClient.EnsureRequestCurrent(generation);
            session.MemorySummary = result.Text;
            session.MemorySummaryMessageCount = summarizeThrough;
        }

        public List<ChatMessage> GetContext(ChatSession session)
        {
            List<ChatMessage> messages = GetConversationMessages(session);
            int summarizedCount = Math.Max(0, Math.Min(session.MemorySummaryMessageCount, messages.Count));
            summarizedCount -= summarizedCount % 2;
            List<ChatMessage> context = new List<ChatMessage>();
            bool memoryAdded = false;
            for (int i = summarizedCount; i < messages.Count; i++)
            {
                ChatMessage message = messages[i];
                string text = message.Text;
                if (!memoryAdded && !string.IsNullOrWhiteSpace(session.MemorySummary) && message.Role == "user")
                {
                    text = "Earlier conversation memory:\n" + session.MemorySummary + "\n\n" + text;
                    memoryAdded = true;
                }

                context.Add(new ChatMessage { Role = message.Role, Text = text });
            }

            return context;
        }

        public List<ChatMessage> GetConversationMessages(ChatSession session)
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            if (session == null || session.Messages == null) return messages;
            foreach (ChatMessage message in session.Messages)
            {
                if (message.Role != "notice") messages.Add(message);
            }

            return messages;
        }
    }
}
