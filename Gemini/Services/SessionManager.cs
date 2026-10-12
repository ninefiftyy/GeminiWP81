using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;

/// <summary>
/// Loads, creates, updates, and deletes chat sessions stored in isolated storage.
/// </summary>

namespace GeminiWP81
{
    public static class SessionManager
    {
        private const string SessionsKey = "SavedChatSessions";

        public static List<ChatSession> GetSessions()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(SessionsKey))
            {
                return IsolatedStorageSettings.ApplicationSettings[SessionsKey] as List<ChatSession> ?? new List<ChatSession>();
            }
            return new List<ChatSession>();
        }

        public static ChatSession GetSessionById(string id)
        {
            List<ChatSession> sessions = GetSessions();
            foreach (ChatSession session in sessions)
            {
                if (session.Id == id) return session;
            }
            return null;
        }

        public static ChatSession CreateNewSession()
        {
            List<ChatSession> sessions = GetSessions();
            ChatSession newSession = new ChatSession();
            sessions.Insert(0, newSession);
            SaveSessions(sessions);
            return newSession;
        }

        public static void SaveSession(ChatSession updatedSession)
        {
            List<ChatSession> sessions = GetSessions();
            bool sessionFound = false;
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].Id == updatedSession.Id)
                {
                    sessions[i] = updatedSession;
                    sessionFound = true;
                    break;
                }
            }

            if (!sessionFound)
            {
                sessions.Insert(0, updatedSession);
            }

            SaveSessions(sessions);
        }

        private static void SaveSessions(List<ChatSession> sessions)
        {
            IsolatedStorageSettings.ApplicationSettings[SessionsKey] = sessions;
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void DeleteSession(string sessionId)
        {
            List<ChatSession> sessions = GetSessions();

            // Remover usando la coincidencia de ID de forma explícita
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                if (sessions[i].Id == sessionId)
                {
                    sessions.RemoveAt(i);
                    break;
                }
            }

            SaveSessions(sessions);
        }
    }
}