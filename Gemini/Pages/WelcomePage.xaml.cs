using System.ComponentModel;
using System.Windows;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;

/// <summary>
/// Collects the initial API key and language choice before opening the chat.
/// </summary>

namespace GeminiWP81
{
    public partial class WelcomePage : PhoneApplicationPage
    {
        private bool _loadingLanguage;

        public WelcomePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string language = "en";
            if (IsolatedStorageSettings.ApplicationSettings.Contains("AppLanguage"))
            {
                language = IsolatedStorageSettings.ApplicationSettings["AppLanguage"].ToString();
            }

            _loadingLanguage = true;
            EnglishLanguageOption.IsChecked = language != "es";
            SpanishLanguageOption.IsChecked = language == "es";
            _loadingLanguage = false;
            UpdateLocalizedUi();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void UpdateLocalizedUi()
        {
            WelcomeTitle.Text = Gemini.AppText.Get("WelcomeTitle");
            WelcomeInstructions.Text = Gemini.AppText.Get("WelcomeInstructions");
            LanguageLabel.Text = Gemini.AppText.Get("LanguageLabel");
            ApiKeyLabel.Text = Gemini.AppText.Get("ApiKeyLabel");
            ApiKeyHelpLink.Content = Gemini.AppText.Get("ApiKeyHelpLink");
            ContinueButton.Content = Gemini.AppText.Get("Continue");
        }

        private void ApiKeyHelpLink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new System.Uri("/Pages/ApiKeyHelpPage.xaml", System.UriKind.Relative));
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

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyInput.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show(Gemini.AppText.Get("InvalidApiKey"), Gemini.AppText.Get("Notice"), MessageBoxButton.OK);
                return;
            }

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["GeminiApiKey"] = apiKey;
            settings["SetupComplete"] = true;
            settings.Save();

            NavigationService.Navigate(new System.Uri("/Pages/MainPage.xaml", System.UriKind.Relative));
        }
    }
}