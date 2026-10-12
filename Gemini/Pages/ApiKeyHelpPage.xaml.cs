using System.IO;
using System.Windows;
using Microsoft.Phone.Controls;

/// <summary>
/// Displays the embedded API key guide, including its embedded images.
/// </summary>

namespace GeminiWP81
{
    public partial class ApiKeyHelpPage : PhoneApplicationPage
    {
        public ApiKeyHelpPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            using (Stream stream = GetType().Assembly.GetManifestResourceStream("Gemini.Assets.ApiKeyHelp.ApiKeyHelp.html"))
            {
                if (stream == null)
                {
                    MessageBox.Show(Gemini.AppText.Get("ApiKeyHelpNotAvailable"), Gemini.AppText.Get("ApiKeyHelpTitle"), MessageBoxButton.OK);
                    if (NavigationService.CanGoBack) NavigationService.GoBack();
                    return;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    html = InlineImage(html, "CopyAPIKeyHelp.png");
                    html = InlineImage(html, "CreateAPIKeyHelp.png");
                    html = InlineImage(html, "GetStartedButtonHelp.png");
                    html = InlineImage(html, "KeyIconHelp.png");
                    HelpBrowser.NavigateToString(html);
                }
            }
        }

        private string InlineImage(string html, string imageName)
        {
            string resourceName = "Gemini.Assets.ApiKeyHelp." + imageName;
            using (Stream imageStream = GetType().Assembly.GetManifestResourceStream(resourceName))
            {
                if (imageStream == null) return html;

                using (MemoryStream imageData = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = imageStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        imageData.Write(buffer, 0, bytesRead);
                    }

                    string dataUri = "data:image/png;base64," + System.Convert.ToBase64String(imageData.ToArray());
                    return html.Replace("src=\"" + imageName + "\"", "src=\"" + dataUri + "\"");
                }
            }
        }
    }
}