using Gemini.Resources;

/// <summary>
/// Provides localized application text and exposes resources for XAML bindings.
/// </summary>


namespace Gemini
{
    
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }

    public static class AppText
    {
        private static string _language = "en";

        public static string Get(string key)
        {
            string[] values;
            if (!Strings.TryGetValue(key, out values)) return key;
            return values[_language == "es" ? 1 : 0];
        }

        public static void SetLanguage(string language)
        {
            _language = language == "es" ? "es" : "en";
        }

        private static readonly System.Collections.Generic.Dictionary<string, string[]> Strings =
            new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "ConversationTitle", new[] { "conversation", "conversación" } },
                { "Send", new[] { "Send", "Enviar" } },
                { "Chats", new[] { "chats", "chats" } },
                { "Settings", new[] { "settings", "ajustes" } },
                { "HistoryTitle", new[] { "conversations", "conversaciones" } },
                { "NewConversation", new[] { "New Conversation", "Nueva Conversación" } },
                { "ConversationDefaultTitle", new[] { "New conversation", "Nueva conversación" } },
                { "WelcomeTitle", new[] { "Welcome", "Bienvenido" } },
                { "WelcomeInstructions", new[] { "Choose your language and enter your Gemini API key to continue.", "Elige el idioma e ingresa tu API Key de Gemini para continuar." } },
                { "Continue", new[] { "Continue", "Continuar" } },
                { "About", new[] { "About", "Acerca de" } },
                { "ApiKeyHelpLink", new[] { "What is / Where can I get an API key?", "¿Qué es / Dónde puedo obtener una API key?" } },
                { "ApiKeyHelpTitle", new[] { "API key help", "Ayuda para la API key" } },
                { "ApiKeyHelpNotAvailable", new[] { "The help page has not been added yet.", "La página de ayuda todavía no está disponible." } },
                { "DeletePrompt", new[] { "Are you sure you want to delete this conversation?", "¿Estás seguro de que deseas eliminar esta conversación?" } },
                { "DeleteChat", new[] { "Delete chat", "Eliminar chat" } },
                { "ApiKeyLabel", new[] { "Gemini API key", "API Key de Gemini" } },
                { "ModelLabel", new[] { "Gemini model", "Modelo de Gemini" } },
                { "ModelChangedTo", new[] { "Model changed to: ", "Modelo cambiado a: " } },
                { "RequestCancelled", new[] { "Request cancelled because the model was changed.", "Solicitud cancelada porque se cambió el modelo." } },
                { "MessageCopied", new[] { "Message copied to clipboard.", "Mensaje copiado al portapapeles." } },
                { "LanguageLabel", new[] { "Application language", "Idioma de la aplicación" } },
                { "SaveSettings", new[] { "Save Settings", "Guardar Ajustes" } },
                { "InvalidApiKey", new[] { "Please enter a valid API key.", "Por favor, ingresa una API Key válida." } },
                { "Notice", new[] { "Notice", "Aviso" } },
                { "Saved", new[] { "Settings saved successfully!", "¡Ajustes guardados correctamente!" } },
                { "Success", new[] { "Success", "Éxito" } },
                { "Error", new[] { "Error: ", "Error: " } },
                { "NetworkError", new[] { "Network error: ", "Error de red: " } },
                { "InternalError", new[] { "Internal error: ", "Error interno: " } },
                { "MissingApiKey", new[] { "No API key is configured. Go to Settings.", "No hay API Key configurada. Ve a Ajustes." } },
                { "NoResponse", new[] { "Could not extract the response.", "No se pudo extraer la respuesta." } }
                ,{ "ResponseReachedTokenLimit", new[] { "Diagnostic: Gemini stopped this response at its token limit (MAX_TOKENS).", "Diagnóstico: Gemini detuvo esta respuesta al alcanzar su límite de tokens (MAX_TOKENS)." } }
            };
    }
}