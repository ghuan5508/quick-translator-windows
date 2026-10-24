using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;

namespace QuickTranslator
{
    public class ResultWindow : Window
    {
        private TextBlock _originalTextBlock;
        private TextBlock _translatedTextBlock;
        private DispatcherTimer? _autoCloseTimer;
        
        public ResultWindow(string originalText, string translatedText)
        {
            Title = "翻译结果";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 420;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // 根据配置设置透明度
            var opacity = ConfigManager.Current.WindowOpacity;
            Opacity = opacity;
            
            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(35, 35, 45)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 80)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 15,
                    ShadowDepth = 2,
                    Opacity = 0.6
                }
            };
            
            var mainStack = new StackPanel
            {
                Margin = new Thickness(16)
            };
            
            // 标题栏（可拖拽）
            var titleBar = new DockPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            titleBar.MouseLeftButtonDown += (s, e) => DragMove();
            
            var titleLabel = new TextBlock
            {
                Text = "📋 翻译结果",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 255, 150)),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleLabel, Dock.Left);
            titleBar.Children.Add(titleLabel);
            
            var closeButton = new Button
            {
                Content = "✕",
                Width = 24,
                Height = 24,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 70)),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (s, e) => Close();
            DockPanel.SetDock(closeButton, Dock.Right);
            titleBar.Children.Add(closeButton);
            
            mainStack.Children.Add(titleBar);
            
            // 原文区域
            var originalLabel = new TextBlock
            {
                Text = "识别原文:",
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(originalLabel);
            
            _originalTextBlock = new TextBlock
            {
                Text = originalText,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 55)),
                MaxHeight = 100
            };
            _originalTextBlock.MouseLeftButtonDown += (s, e) => CopyToClipboard(_originalTextBlock.Text, "原文");
            mainStack.Children.Add(_originalTextBlock);
            
            // 翻译结果区域
            var translatedLabel = new TextBlock
            {
                Text = "翻译结果:",
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(translatedLabel);
            
            _translatedTextBlock = new TextBlock
            {
                Text = translatedText,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 220, 100)),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 55)),
                MaxHeight = 100
            };
            _translatedTextBlock.MouseLeftButtonDown += (s, e) => CopyToClipboard(_translatedTextBlock.Text, "译文");
            mainStack.Children.Add(_translatedTextBlock);
            
            // 按钮区域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var copyOriginalBtn = CreateButton("复制原文", () => CopyToClipboard(_originalTextBlock.Text, "原文"));
            var copyTranslatedBtn = CreateButton("复制译文", () => CopyToClipboard(_translatedTextBlock.Text, "译文"));
            var closeBtn = CreateButton("关闭", () => Close());
            
            buttonPanel.Children.Add(copyOriginalBtn);
            buttonPanel.Children.Add(copyTranslatedBtn);
            buttonPanel.Children.Add(closeBtn);
            
            mainStack.Children.Add(buttonPanel);
            
            mainBorder.Child = mainStack;
            Content = mainBorder;
            
            // 自动关闭逻辑
            if (ConfigManager.Current.AutoCloseRule == "MouseLeave")
            {
                MouseLeave += (s, e) => Close();
            }
            else if (ConfigManager.Current.AutoCloseRule == "Delay")
            {
                _autoCloseTimer = new DispatcherTimer();
                _autoCloseTimer.Interval = TimeSpan.FromSeconds(ConfigManager.Current.AutoCloseDelaySeconds);
                _autoCloseTimer.Tick += (s, e) =>
                {
                    _autoCloseTimer.Stop();
                    Close();
                };
                _autoCloseTimer.Start();
            }
        }
        
        private Button CreateButton(string text, Action onClick)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(4, 0, 4, 0),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 70)),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12
            };
            button.Click += (s, e) => onClick();
            return button;
        }
        
        private void CopyToClipboard(string text, string label)
        {
            try
            {
                Clipboard.SetText(text);
                // 可以显示一个短暂的提示
                var originalTitle = Title;
                Title = $"已复制{label}到剪贴板";
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1.5);
                timer.Tick += (s, e) =>
                {
                    Title = originalTitle;
                    timer.Stop();
                };
                timer.Start();
            }
            catch
            {
                MessageBox.Show($"复制{label}失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _autoCloseTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
