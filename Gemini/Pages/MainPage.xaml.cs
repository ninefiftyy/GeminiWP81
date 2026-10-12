using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Collections.Generic;

/// <summary>
/// Coordinates the active conversation, chat interface, memory, and Gemini requests.
/// </summary>

namespace GeminiWP81
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ChatSession _currentSession;
        private ChatMessageRenderer _messageRenderer;
        public const string DefaultGeminiModel = "gemini-3.1-flash-lite";
        private readonly GeminiApiClient _apiClient = new GeminiApiClient();
        private readonly ConversationMemoryService _memoryService;

        public MainPage()
        {
            InitializeComponent();
            _messageRenderer = new ChatMessageRenderer(ChatPanel, ChatScrollViewer);
            _memoryService = new ConversationMemoryService(_apiClient);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            bool setupComplete = settings.Contains("SetupComplete") && settings["SetupComplete"] is bool && (bool)settings["SetupComplete"];
            if (!setupComplete && settings.Contains("GeminiApiKey") && !string.IsNullOrWhiteSpace(settings["GeminiApiKey"] as string))
            {
                settings["SetupComplete"] = true;
                settings.Save();
                setupComplete = true;
            }

            if (!setupComplete)
            {
                NavigationService.Navigate(new Uri("/Pages/WelcomePage.xaml", UriKind.Relative));
                return;
            }

            UpdateLocalizedUi();

            string activeSessionId = HistoryPage.ActiveSessionId;
            if (!string.IsNullOrEmpty(activeSessionId))
            {
                _currentSession = SessionManager.GetSessionById(activeSessionId);
            }
            else if (settings.Contains("CurrentSession"))
            {
                _currentSession = settings["CurrentSession"] as ChatSession;
            }

            if (_currentSession == null)
            {
                _currentSession = SessionManager.CreateNewSession();
            }

            if (_currentSession.Messages == null)
            {
                _currentSession.Messages = new List<ChatMessage>();
            }

            HistoryPage.ActiveSessionId = _currentSession.Id;
            LoadChatUI();
        }

        private void UpdateLocalizedUi()
        {
            SendButton.Content = Gemini.AppText.Get("Send");
            Microsoft.Phone.Shell.ApplicationBarIconButton historyButton = ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;
            Microsoft.Phone.Shell.ApplicationBarIconButton settingsButton = ApplicationBar.Buttons[1] as Microsoft.Phone.Shell.ApplicationBarIconButton;
            historyButton.Text = Gemini.AppText.Get("Chats");
            settingsButton.Text = Gemini.AppText.Get("Settings");
        }

        private void LoadChatUI()
        {
            _messageRenderer.Clear();

            if (_currentSession != null && _currentSession.Messages != null)
            {
                foreach (var msg in _currentSession.Messages)
                {
                    if (msg.Role == "notice")
                    {
                        _messageRenderer.AddNotice(msg.Text);
                        continue;
                    }

                    bool isUser = msg.Role == "user";
                    _messageRenderer.AddMessage(msg.Text, isUser);
                }
            }
        }

        private void ChatScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _messageRenderer.OnViewportSizeChanged(e);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputText.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            string modelToUse = GeminiApiClient.GetSelectedModel();
            int requestGeneration = GeminiApiClient.BeginRequest();
            bool isFirstMessage = _memoryService.GetConversationMessages(_currentSession).Count == 0;
            if (isFirstMessage)
            {
                _currentSession.Title = CreateConversationTitle(userInput);
            }

            InputText.Text = string.Empty;
            SendButton.IsEnabled = false;

            ChatMessage pendingUserMessage = new ChatMessage { Role = "user", Text = userInput };
            UIElement userBubble = _messageRenderer.AddMessage(userInput, true);
            if (_currentSession.Messages == null)
            {
                _currentSession.Messages = new List<ChatMessage>();
            }

            _currentSession.Messages.Add(pendingUserMessage);
            SaveCurrentSession();

            UIElement typingBubble = _messageRenderer.AddMessage("...", false);
            try
            {
                await _memoryService.UpdateAsync(_currentSession, modelToUse, requestGeneration);
                GeminiApiClient.EnsureRequestCurrent(requestGeneration);
                List<ChatMessage> conversationContext = _memoryService.GetContext(_currentSession);
                GeminiApiClient.GeminiApiResult result = await _apiClient.GenerateContentAsync(conversationContext, modelToUse, requestGeneration);
                GeminiApiClient.EnsureRequestCurrent(requestGeneration);

                _messageRenderer.Remove(typingBubble);
                _messageRenderer.AddMessage(result.Text, false);
                if (string.Equals(result.FinishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
                {
                    _messageRenderer.AddNotice(Gemini.AppText.Get("ResponseReachedTokenLimit"));
                }

                if (_currentSession.Messages != null)
                {
                    _currentSession.Messages.Add(new ChatMessage { Role = "model", Text = result.Text });
                }

                SaveCurrentSession();
            }
            catch (OperationCanceledException)
            {
                RemoveFailedUserMessage(pendingUserMessage, userBubble, typingBubble);
                _currentSession.Messages.Add(new ChatMessage
                {
                    Role = "notice",
                    Text = Gemini.AppText.Get("RequestCancelled")
                });
                SaveCurrentSession();
                _messageRenderer.AddNotice(Gemini.AppText.Get("RequestCancelled"));
            }
            catch (Exception ex)
            {
                RemoveFailedUserMessage(pendingUserMessage, userBubble, typingBubble);
                _messageRenderer.AddMessage(ex.Message, false);
            }
            finally
            {
                SendButton.IsEnabled = true;
            }
        }

        private void RemoveFailedUserMessage(ChatMessage message, UIElement userBubble, UIElement typingBubble)
        {
            if (typingBubble != null)
            {
                _messageRenderer.Remove(typingBubble);
            }

            if (userBubble != null)
            {
                _messageRenderer.Remove(userBubble);
            }

            if (_currentSession.Messages != null)
            {
                _currentSession.Messages.Remove(message);
            }

            if (_memoryService.GetConversationMessages(_currentSession).Count == 0)
            {
                _currentSession.Title = Gemini.AppText.Get("ConversationDefaultTitle");
            }

            SaveCurrentSession();
        }

        private string CreateConversationTitle(string firstMessage)
        {
            string title = firstMessage.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return title.Length <= 32 ? title : title.Substring(0, 32);
        }

        private void SaveCurrentSession()
        {
            try
            {
                _currentSession.LastUpdated = DateTime.Now;
                IsolatedStorageSettings.ApplicationSettings["CurrentSession"] = _currentSession;
                SessionManager.SaveSession(_currentSession);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar sesión: " + ex.Message);
            }
        }

        private void history_button_click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/HistoryPage.xaml", UriKind.Relative));
        }

        private void settingsbutton_click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.Relative));
        }
    }

}
