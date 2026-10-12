using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

/// <summary>
/// Renders chat messages, notices, and Markdown, and supports copying messages from the chat view.
/// </summary>

namespace GeminiWP81
{
    public sealed class ChatMessageRenderer
    {
        private const double MaximumBubbleWidth = 380;
        private readonly StackPanel _chatPanel;
        private readonly ScrollViewer _scrollViewer;
        private bool _scrollPending;

        public ChatMessageRenderer(StackPanel chatPanel, ScrollViewer scrollViewer)
        {
            _chatPanel = chatPanel;
            _scrollViewer = scrollViewer;
        }

        public void Clear()
        {
            _chatPanel.Children.Clear();
        }

        public void Remove(UIElement element)
        {
            _chatPanel.Children.Remove(element);
        }

        public UIElement AddMessage(string text, bool isUser)
        {
            double bubbleWidth = GetChatWidth();
            Border bubble = new Border
            {
                CornerRadius = new CornerRadius(12),
                Width = bubbleWidth,
                Margin = new Thickness(0, 4, 0, 8),
                Padding = new Thickness(15, 12, 15, 12),
                MaxWidth = MaximumBubbleWidth,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = isUser
                    ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215))
                    : new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
                Child = isUser
                    ? (UIElement)CreatePlainTextMessage(text, Math.Max(1, bubbleWidth - 30))
                    : CreateFormattedResponse(text)
            };

            AddCopyOnHold(bubble, text);
            bubble.Loaded += (sender, args) =>
            {
                _scrollViewer.Dispatcher.BeginInvoke(() => SizeBubble(bubble));
            };
            _chatPanel.Children.Add(bubble);
            QueueScrollToBottom();
            return bubble;
        }

        public void AddNotice(string text)
        {
            TextBlock notice = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Width = GetChatWidth(),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)),
                FontSize = 16,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 8, 0, 8)
            };
            _chatPanel.Children.Add(notice);
            QueueScrollToBottom();
        }

        public void OnViewportSizeChanged(SizeChangedEventArgs e)
        {
            double viewportWidth = _scrollViewer.ViewportWidth;
            if (viewportWidth <= 0) viewportWidth = e.NewSize.Width;
            if (viewportWidth <= 0) return;

            _chatPanel.Width = viewportWidth;
            foreach (UIElement child in _chatPanel.Children)
            {
                Border bubble = child as Border;
                if (bubble != null)
                {
                    SizeBubble(bubble, viewportWidth);
                    continue;
                }

                TextBlock textBlock = child as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Width = Math.Min(MaximumBubbleWidth, viewportWidth);
                    textBlock.Height = double.NaN;
                }
            }

            _chatPanel.UpdateLayout();
            QueueScrollToBottom();
        }

        public void QueueScrollToBottom()
        {
            if (_scrollPending) return;
            _scrollPending = true;
            _scrollViewer.Dispatcher.BeginInvoke(() =>
            {
                _scrollPending = false;
                _scrollViewer.UpdateLayout();
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.ScrollableHeight);
            });
        }

        private double GetChatWidth()
        {
            double width = _scrollViewer.ViewportWidth;
            if (width <= 0) width = _scrollViewer.ActualWidth;
            if (width <= 0) width = MaximumBubbleWidth;
            return Math.Max(1, Math.Min(MaximumBubbleWidth, width));
        }

        private void SizeBubble(Border bubble)
        {
            SizeBubble(bubble, GetChatWidth());
        }

        private void SizeBubble(Border bubble, double viewportWidth)
        {
            StackPanel content = bubble.Child as StackPanel;
            if (content == null) return;

            double width = Math.Max(1, Math.Min(MaximumBubbleWidth, viewportWidth));
            double contentWidth = Math.Max(1, width - bubble.Padding.Left - bubble.Padding.Right);
            bubble.Width = width;
            bubble.Height = double.NaN;
            content.Width = contentWidth;
            content.Height = double.NaN;
            foreach (UIElement child in content.Children)
            {
                TextBlock textBlock = child as TextBlock;
                if (textBlock == null) continue;
                textBlock.Width = contentWidth;
                textBlock.Height = double.NaN;
            }

            bubble.UpdateLayout();
            _chatPanel.UpdateLayout();
        }

        private StackPanel CreatePlainTextMessage(string text, double width)
        {
            StackPanel message = new StackPanel
            {
                Width = width,
                VerticalAlignment = VerticalAlignment.Top
            };
            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    message.Children.Add(CreateTextBlock(" ", width));
                    continue;
                }

                int start = 0;
                while (start < line.Length)
                {
                    int length = Math.Min(500, line.Length - start);
                    if (start + length < line.Length)
                    {
                        int split = line.LastIndexOf(' ', start + length - 1, length);
                        if (split > start + length / 2) length = split - start + 1;
                    }

                    message.Children.Add(CreateTextBlock(line.Substring(start, length), width));
                    start += length;
                }
            }

            return message;
        }

        private TextBlock CreateTextBlock(string text, double width)
        {
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 20
            };
        }

        private void AddCopyOnHold(Border bubble, string text)
        {
            DispatcherTimer holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            Point pressPosition = new Point();
            bool copied = false;
            holdTimer.Tick += (sender, args) =>
            {
                holdTimer.Stop();
                Clipboard.SetText(text ?? string.Empty);
                copied = true;
                MessageBox.Show(Gemini.AppText.Get("MessageCopied"));
            };
            bubble.MouseLeftButtonDown += (sender, args) =>
            {
                copied = false;
                pressPosition = args.GetPosition(bubble);
                holdTimer.Start();
            };
            bubble.MouseMove += (sender, args) =>
            {
                if (!holdTimer.IsEnabled) return;
                Point position = args.GetPosition(bubble);
                if (Math.Abs(position.X - pressPosition.X) > 20 || Math.Abs(position.Y - pressPosition.Y) > 20)
                {
                    holdTimer.Stop();
                }
            };
            bubble.MouseLeftButtonUp += (sender, args) =>
            {
                holdTimer.Stop();
                if (copied) args.Handled = true;
            };
            bubble.MouseLeave += (sender, args) => holdTimer.Stop();
        }

        private StackPanel CreateFormattedResponse(string text)
        {
            StackPanel content = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            StringBuilder paragraph = new StringBuilder();
            StringBuilder code = new StringBuilder();
            bool insideCode = false;
            string codeLanguage = string.Empty;
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("```"))
                {
                    if (insideCode)
                    {
                        AddCodeBlock(content, code.ToString(), codeLanguage);
                        code.Clear();
                        codeLanguage = string.Empty;
                        insideCode = false;
                    }
                    else
                    {
                        AddParagraph(content, paragraph.ToString());
                        paragraph.Clear();
                        insideCode = true;
                        codeLanguage = trimmed.Substring(3).Trim();
                    }

                    continue;
                }

                if (insideCode)
                {
                    if (code.Length > 0) code.Append('\n');
                    code.Append(line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    AddParagraph(content, paragraph.ToString());
                    paragraph.Clear();
                    continue;
                }

                string blockText;
                int headingLevel;
                if (TryGetHeading(trimmed, out headingLevel, out blockText))
                {
                    AddParagraph(content, paragraph.ToString());
                    paragraph.Clear();
                    AddTextBlock(content, blockText, headingLevel);
                    continue;
                }

                string listPrefix;
                if (TryGetListItem(trimmed, out blockText, out listPrefix))
                {
                    AddParagraph(content, paragraph.ToString());
                    paragraph.Clear();
                    TextBlock listItem = CreateFormattedTextBlock(string.Empty);
                    listItem.Inlines.Add(new Run { Text = listPrefix });
                    AddInlineMarkdown(listItem.Inlines, blockText);
                    content.Children.Add(listItem);
                    continue;
                }

                if (trimmed.StartsWith(">"))
                {
                    AddParagraph(content, paragraph.ToString());
                    paragraph.Clear();
                    TextBlock quote = CreateFormattedTextBlock(trimmed.Substring(1).TrimStart());
                    quote.Foreground = new SolidColorBrush(Color.FromArgb(255, 205, 205, 205));
                    quote.FontStyle = FontStyles.Italic;
                    content.Children.Add(quote);
                    continue;
                }

                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(trimmed);
            }

            if (insideCode) AddCodeBlock(content, code.ToString(), codeLanguage);
            AddParagraph(content, paragraph.ToString());
            return content;
        }

        private void AddParagraph(StackPanel content, string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) AddTextBlock(content, text, 0);
        }

        private void AddTextBlock(StackPanel content, string text, int headingLevel)
        {
            TextBlock block = CreateFormattedTextBlock(text);
            if (headingLevel > 0)
            {
                block.FontSize = Math.Max(20, 30 - headingLevel * 2);
                block.FontWeight = FontWeights.Bold;
            }

            content.Children.Add(block);
        }

        private TextBlock CreateFormattedTextBlock(string text)
        {
            TextBlock block = CreateTextBlock(string.Empty, double.NaN);
            AddInlineMarkdown(block.Inlines, text);
            return block;
        }

        private void AddCodeBlock(StackPanel content, string code, string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                TextBlock languageBlock = CreateTextBlock(language.ToUpperInvariant(), double.NaN);
                languageBlock.FontSize = 13;
                languageBlock.FontWeight = FontWeights.Bold;
                languageBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 170, 190, 210));
                content.Children.Add(languageBlock);
            }

            TextBlock codeBlock = CreateTextBlock(code, double.NaN);
            codeBlock.FontFamily = new FontFamily("Courier New");
            codeBlock.FontSize = 16;
            content.Children.Add(codeBlock);
        }

        private bool TryGetHeading(string text, out int level, out string content)
        {
            level = 0;
            while (level < text.Length && level < 6 && text[level] == '#') level++;
            if (level > 0 && level < text.Length && text[level] == ' ')
            {
                content = text.Substring(level).Trim();
                return true;
            }

            level = 0;
            content = text;
            return false;
        }

        private bool TryGetListItem(string text, out string content, out string prefix)
        {
            if (text.Length > 2 && (text.StartsWith("- ") || text.StartsWith("* ") || text.StartsWith("+ ")))
            {
                content = text.Substring(2);
                prefix = "• ";
                return true;
            }

            int index = 0;
            while (index < text.Length && char.IsDigit(text[index])) index++;
            if (index > 0 && index + 1 < text.Length && (text[index] == '.' || text[index] == ')') && text[index + 1] == ' ')
            {
                content = text.Substring(index + 2);
                prefix = text.Substring(0, index + 2);
                return true;
            }

            content = text;
            prefix = string.Empty;
            return false;
        }

        private void AddInlineMarkdown(InlineCollection inlines, string text)
        {
            int index = 0;
            while (index < text.Length)
            {
                int markerIndex = text.IndexOfAny(new[] { '*', '_', '`' }, index);
                if (markerIndex < 0)
                {
                    inlines.Add(new Run { Text = text.Substring(index) });
                    break;
                }

                if (markerIndex > index) inlines.Add(new Run { Text = text.Substring(index, markerIndex - index) });
                string marker = text[markerIndex].ToString();
                if (markerIndex + 1 < text.Length && text[markerIndex + 1] == text[markerIndex] && text[markerIndex] != '`')
                {
                    marker += marker;
                }

                int closingIndex = text.IndexOf(marker, markerIndex + marker.Length, StringComparison.Ordinal);
                if (closingIndex <= markerIndex + marker.Length)
                {
                    inlines.Add(new Run { Text = marker });
                    index = markerIndex + marker.Length;
                    continue;
                }

                string formattedText = text.Substring(markerIndex + marker.Length, closingIndex - markerIndex - marker.Length);
                if (marker == "`")
                {
                    inlines.Add(new Run
                    {
                        Text = formattedText,
                        FontFamily = new FontFamily("Courier New"),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 200, 130))
                    });
                }
                else
                {
                    Span style = marker == "**" || marker == "__" ? (Span)new Bold() : new Italic();
                    AddInlineMarkdown(style.Inlines, formattedText);
                    inlines.Add(style);
                }

                index = closingIndex + marker.Length;
            }
        }
    }
}
