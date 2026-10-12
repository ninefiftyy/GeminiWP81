using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;

/// <summary>
/// Lets the user configure the Gemini API key, model, and application language.
/// </summary>

namespace GeminiWP81
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private bool _loadingLanguage;
        private bool _loadingModel;
        private string _configuredModel;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateLocalizedUi();

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            _configuredModel = GeminiApiClient.GetSelectedModel();

            string language = "en";
            if (settings.Contains("AppLanguage"))
            {
                language = settings["AppLanguage"].ToString();
            }
            _loadingLanguage = true;
            EnglishLanguageOption.IsChecked = language != "es";
            SpanishLanguageOption.IsChecked = language == "es";
            _loadingLanguage = false;

            // 1. Cargar la API Key guardada previamente
            if (IsolatedStorageSettings.ApplicationSettings.Contains("GeminiApiKey"))
            {
                ApiKeyInput.Text = IsolatedStorageSettings.ApplicationSettings["GeminiApiKey"].ToString();
            }

            // 2. Seleccionar en el ListBox el modelo que se había guardado
            _loadingModel = true;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("GeminiModel"))
            {
                string savedModel = IsolatedStorageSettings.ApplicationSettings["GeminiModel"].ToString();

                foreach (var item in ModelListBox.Items)
                {
                    ListBoxItem lbi = item as ListBoxItem;
                    if (lbi != null && lbi.Content != null && lbi.Content.ToString() == savedModel)
                    {
                        ModelListBox.SelectedItem = lbi;
                        break;
                    }
                }
            }
            else
            {
                foreach (var item in ModelListBox.Items)
                {
                    ListBoxItem listItem = item as ListBoxItem;
                    if (listItem != null && listItem.Content != null && listItem.Content.ToString() == MainPage.DefaultGeminiModel)
                    {
                        ModelListBox.SelectedItem = listItem;
                        break;
                    }
                }
            }
            _loadingModel = false;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            SaveSettings();
            base.OnNavigatedFrom(e);
        }

        private void UpdateLocalizedUi()
        {
            PageTitle.Text = Gemini.AppText.Get("Settings");
            ApiKeyLabel.Text = Gemini.AppText.Get("ApiKeyLabel");
            ModelLabel.Text = Gemini.AppText.Get("ModelLabel");
            LanguageLabel.Text = Gemini.AppText.Get("LanguageLabel");
            AboutLabel.Text = Gemini.AppText.Get("About");
        }

        private void LanguageOption_Checked(object sender, RoutedEventArgs e)
        {
            if (_loadingLanguage) return;

            string language = SpanishLanguageOption.IsChecked == true ? "es" : "en";
            Gemini.AppText.SetLanguage(language);
            IsolatedStorageSettings.ApplicationSettings["AppLanguage"] = language;
            IsolatedStorageSettings.ApplicationSettings.Save();
            UpdateLocalizedUi();
        }

        private void ModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingModel)
            {
                return;
            }

            ListBoxItem selectedLbi = ModelListBox.SelectedItem as ListBoxItem;
            if (selectedLbi == null || selectedLbi.Content == null)
            {
                return;
            }

            string selectedModel = selectedLbi.Content.ToString();
            if (string.Equals(_configuredModel, selectedModel, StringComparison.Ordinal))
            {
                return;
            }

            GeminiApiClient.CancelPendingRequest();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["GeminiModel"] = selectedModel;
            settings.Save();
            AddModelChangeNotice(selectedModel);
            _configuredModel = selectedModel;
        }

        private void SaveSettings()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string apiKey = ApiKeyInput.Text.Trim();
            if (!string.IsNullOrEmpty(apiKey))
            {
                settings["GeminiApiKey"] = apiKey;
            }

            // Guardar el modelo seleccionado del ListBox de forma segura en Silverlight 8.1
            ListBoxItem selectedLbi = ModelListBox.SelectedItem as ListBoxItem;
            if (selectedLbi != null && selectedLbi.Content != null)
            {
                string selectedModel = selectedLbi.Content.ToString();
                settings["GeminiModel"] = selectedModel;
            }

            settings.Save();
        }

        private void AddModelChangeNotice(string model)
        {
            string activeSessionId = HistoryPage.ActiveSessionId;
            ChatSession activeSession = string.IsNullOrEmpty(activeSessionId)
                ? null
                : SessionManager.GetSessionById(activeSessionId);

            if (activeSession == null && IsolatedStorageSettings.ApplicationSettings.Contains("CurrentSession"))
            {
                activeSession = IsolatedStorageSettings.ApplicationSettings["CurrentSession"] as ChatSession;
            }

            if (activeSession == null)
            {
                return;
            }

            if (activeSession.Messages == null)
            {
                activeSession.Messages = new System.Collections.Generic.List<ChatMessage>();
            }

            activeSession.Messages.Add(new ChatMessage
            {
                Role = "notice",
                Text = Gemini.AppText.Get("ModelChangedTo") + model
            });
            activeSession.LastUpdated = DateTime.Now;
            SessionManager.SaveSession(activeSession);
        }
    }
}