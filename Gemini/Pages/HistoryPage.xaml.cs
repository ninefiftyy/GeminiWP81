using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

/// <summary>
/// Displays saved conversations and lets the user open, create, or delete them.
/// </summary>

namespace GeminiWP81
{
    public partial class HistoryPage : PhoneApplicationPage
    {
        public static string ActiveSessionId = string.Empty;

        public HistoryPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            PageTitle.Text = Gemini.AppText.Get("HistoryTitle");
            NewChatButton.Content = Gemini.AppText.Get("NewConversation");
            LoadSessions();
        }

        private void LoadSessions()
        {
            // Forzar la desvinculación para limpiar los ítems renderizados en UI
            HistoryListBox.ItemsSource = null;

            List<ChatSession> sessions = SessionManager.GetSessions();
            HistoryListBox.ItemsSource = sessions;
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChatSession selectedSession = HistoryListBox.SelectedItem as ChatSession;

            // Validación indispensable para evitar colapsos al borrar un ítem seleccionado
            if (selectedSession == null) return;

            ActiveSessionId = selectedSession.Id;
            NavigationService.Navigate(new Uri("/Pages/MainPage.xaml", UriKind.Relative));

            // Resetear la selección para permitir volver a hacer clic en el futuro
            HistoryListBox.SelectedItem = null;
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            ChatSession newSession = SessionManager.CreateNewSession();
            ActiveSessionId = newSession.Id;
            NavigationService.Navigate(new Uri("/Pages/MainPage.xaml", UriKind.Relative));
        }

        private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
        {
            Button deleteBtn = sender as Button;
            if (deleteBtn == null || deleteBtn.Tag == null) return;

            string sessionId = deleteBtn.Tag.ToString();

            MessageBoxResult result = MessageBox.Show(
                Gemini.AppText.Get("DeletePrompt"),
                Gemini.AppText.Get("DeleteChat"),
                MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                // 1. Desactivar selección temporalmente antes de borrar de la lista
                HistoryListBox.SelectedItem = null;

                // 2. Si la conversación activa era la eliminada, resetear la variable global
                if (ActiveSessionId == sessionId)
                {
                    ActiveSessionId = string.Empty;
                }

                // 3. Borrar de IsolatedStorage y actualizar pantalla
                SessionManager.DeleteSession(sessionId);
                LoadSessions();
            }
        }
    }
}