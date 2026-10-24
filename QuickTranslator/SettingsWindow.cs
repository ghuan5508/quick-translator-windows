using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;

namespace QuickTranslator
{
    public class SettingsWindow : Window
    {
        private ComboBox _captureModifierCombo;
        private ComboBox _captureKeyCombo;
        private ComboBox _closeKeyCombo;
        private ComboBox _opacityCombo;
        private ComboBox _autoCloseCombo;
        private ComboBox _delayCombo;
        private ComboBox _ocrLangCombo;
        private ComboBox _translationDirCombo;
        private CheckBox _onlineTranslationCheck;
        private CheckBox _startWithWindowsCheck;
        
        public SettingsWindow()
        {
            Title = "设置";
            Width = 450;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 50));
            
            var mainStack = new StackPanel
            {
                Margin = new Thickness(20)
            };
            
            // 标题
            var titleBlock = new TextBlock
            {
                Text = "⚙️ 设置",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 255, 150)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            mainStack.Children.Add(titleBlock);
            
            // 板块1: 快捷键设置
            mainStack.Children.Add(CreateSectionHeader("快捷键设置"));
            
            var hotkeyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            
            // 主截图快捷键
            hotkeyPanel.Children.Add(CreateLabel("主截图快捷键:"));
            var captureHotkeyPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            _captureModifierCombo = CreateComboBox(new[] { "Alt", "Ctrl", "Shift", "Win" });
            _captureKeyCombo = CreateComboBox(new[] { "Q", "W", "E", "R", "T", "A", "S", "D", "F", "G", "Z", "X", "C", "V", "B", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" });
            captureHotkeyPanel.Children.Add(_captureModifierCombo);
            captureHotkeyPanel.Children.Add(new TextBlock { Text = " + ", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });
            captureHotkeyPanel.Children.Add(_captureKeyCombo);
            hotkeyPanel.Children.Add(captureHotkeyPanel);
            
            // 关闭弹窗快捷键
            hotkeyPanel.Children.Add(CreateLabel("关闭弹窗快捷键:"));
            _closeKeyCombo = CreateComboBox(new[] { "Escape", "F1", "F2", "F3", "F4", "F5", "Space" });
            hotkeyPanel.Children.Add(_closeKeyCombo);
            
            mainStack.Children.Add(hotkeyPanel);
            
            // 板块2: 弹窗与通用设置
            mainStack.Children.Add(CreateSectionHeader("弹窗与通用设置"));
            
            var generalPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            
            // 窗口透明度
            generalPanel.Children.Add(CreateLabel("悬浮窗透明度:"));
            _opacityCombo = CreateComboBox(new[] { "100%", "80%", "60%" });
            generalPanel.Children.Add(_opacityCombo);
            
            // 自动关闭规则
            generalPanel.Children.Add(CreateLabel("自动关闭规则:"));
            _autoCloseCombo = CreateComboBox(new[] { "不自动关闭", "鼠标移出关闭", "延时关闭" });
            generalPanel.Children.Add(_autoCloseCombo);
            
            // 延时关闭时间
            generalPanel.Children.Add(CreateLabel("延时关闭时间(秒):"));
            _delayCombo = CreateComboBox(new[] { "3", "5", "10" });
            generalPanel.Children.Add(_delayCombo);
            
            // 开机自启
            _startWithWindowsCheck = new CheckBox
            {
                Content = "开机自启",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 0)
            };
            generalPanel.Children.Add(_startWithWindowsCheck);
            
            mainStack.Children.Add(generalPanel);
            
            // 板块3: OCR & 翻译设置
            mainStack.Children.Add(CreateSectionHeader("OCR & 翻译设置"));
            
            var ocrPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            
            // OCR语言
            ocrPanel.Children.Add(CreateLabel("默认识别语种:"));
            _ocrLangCombo = CreateComboBox(new[] { "简体中文", "English", "日本語", "한국어" });
            ocrPanel.Children.Add(_ocrLangCombo);
            
            // 翻译方向
            ocrPanel.Children.Add(CreateLabel("默认翻译方向:"));
            _translationDirCombo = CreateComboBox(new[] { "外文→中文", "中文→英文", "英文→中文", "日文→中文", "韩文→中文" });
            ocrPanel.Children.Add(_translationDirCombo);
            
            // 在线翻译开关
            _onlineTranslationCheck = new CheckBox
            {
                Content = "启用联网翻译 (需要网络)",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 0)
            };
            ocrPanel.Children.Add(_onlineTranslationCheck);
            
            mainStack.Children.Add(ocrPanel);
            
            // 板块4: 重置功能
            mainStack.Children.Add(CreateSectionHeader("重置"));
            
            var resetButton = new Button
            {
                Content = "恢复所有默认设置",
                Background = new SolidColorBrush(Color.FromRgb(80, 80, 90)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15)
            };
            resetButton.Click += (s, e) => ResetSettings();
            mainStack.Children.Add(resetButton);
            
            // 保存和取消按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var saveButton = new Button
            {
                Content = "保存",
                Background = new SolidColorBrush(Color.FromRgb(50, 150, 50)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += (s, e) => SaveSettings();
            buttonPanel.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "取消",
                Background = new SolidColorBrush(Color.FromRgb(80, 80, 90)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => Close();
            buttonPanel.Children.Add(cancelButton);
            
            mainStack.Children.Add(buttonPanel);
            
            Content = new ScrollViewer { Content = mainStack };
            
            LoadSettings();
        }
        
        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }
        
        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }
        
        private ComboBox CreateComboBox(string[] items)
        {
            var combo = new ComboBox
            {
                ItemsSource = items,
                SelectedIndex = 0,
                Margin = new Thickness(0, 0, 0, 8),
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            return combo;
        }
        
        private void LoadSettings()
        {
            var config = ConfigManager.Current;
            
            _captureModifierCombo.SelectedItem = config.CaptureHotkeyModifier;
            _captureKeyCombo.SelectedItem = config.CaptureHotkeyKey;
            _closeKeyCombo.SelectedItem = config.CloseHotkeyKey;
            
            _opacityCombo.SelectedIndex = config.WindowOpacity switch
            {
                1.0 => 0,
                0.8 => 1,
                0.6 => 2,
                _ => 0
            };
            
            _autoCloseCombo.SelectedIndex = config.AutoCloseRule switch
            {
                "None" => 0,
                "MouseLeave" => 1,
                "Delay" => 2,
                _ => 0
            };
            
            _delayCombo.SelectedItem = config.AutoCloseDelaySeconds.ToString();
            
            _ocrLangCombo.SelectedIndex = config.OcrLanguage switch
            {
                "zh-Hans-CN" => 0,
                "en-US" => 1,
                "ja-JP" => 2,
                "ko-KR" => 3,
                _ => 0
            };
            
            _translationDirCombo.SelectedIndex = config.TranslationDirection switch
            {
                "AutoToZh" => 0,
                "ZhToEn" => 1,
                "EnToZh" => 2,
                "JaToZh" => 3,
                "KoToZh" => 4,
                _ => 0
            };
            
            _onlineTranslationCheck.IsChecked = config.UseOnlineTranslation;
            _startWithWindowsCheck.IsChecked = config.StartWithWindows;
        }
        
        private void SaveSettings()
        {
            var config = ConfigManager.Current;
            
            config.CaptureHotkeyModifier = _captureModifierCombo.SelectedItem?.ToString() ?? "Alt";
            config.CaptureHotkeyKey = _captureKeyCombo.SelectedItem?.ToString() ?? "Q";
            config.CloseHotkeyKey = _closeKeyCombo.SelectedItem?.ToString() ?? "Escape";
            
            config.WindowOpacity = _opacityCombo.SelectedIndex switch
            {
                0 => 1.0,
                1 => 0.8,
                2 => 0.6,
                _ => 1.0
            };
            
            config.AutoCloseRule = _autoCloseCombo.SelectedIndex switch
            {
                0 => "None",
                1 => "MouseLeave",
                2 => "Delay",
                _ => "None"
            };
            
            if (int.TryParse(_delayCombo.SelectedItem?.ToString(), out int delay))
            {
                config.AutoCloseDelaySeconds = delay;
            }
            
            config.OcrLanguage = _ocrLangCombo.SelectedIndex switch
            {
                0 => "zh-Hans-CN",
                1 => "en-US",
                2 => "ja-JP",
                3 => "ko-KR",
                _ => "zh-Hans-CN"
            };
            
            config.TranslationDirection = _translationDirCombo.SelectedIndex switch
            {
                0 => "AutoToZh",
                1 => "ZhToEn",
                2 => "EnToZh",
                3 => "JaToZh",
                4 => "KoToZh",
                _ => "AutoToZh"
            };
            
            config.UseOnlineTranslation = _onlineTranslationCheck.IsChecked ?? false;
            config.StartWithWindows = _startWithWindowsCheck.IsChecked ?? false;
            
            ConfigManager.Save();
            MessageBox.Show("设置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        
        private void ResetSettings()
        {
            if (MessageBox.Show("确定要恢复所有默认设置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ConfigManager.Reset();
                LoadSettings();
                MessageBox.Show("已恢复默认设置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
