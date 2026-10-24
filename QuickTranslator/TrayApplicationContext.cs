using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace QuickTranslator
{
    public class TrayApplicationContext : IDisposable
    {
        private TaskbarIcon _trayIcon;
        private CaptureWindow? _captureWindow;
        private ResultWindow? _resultWindow;
        private ImageTranslateWindow? _imageTranslateWindow;
        private SettingsWindow? _settingsWindow;
        private IntPtr _mainWindowHandle;
        
        public TrayApplicationContext()
        {
            SetupTrayIcon();
            RegisterHotKey();
        }
        
        private void SetupTrayIcon()
        {
            _trayIcon = new TaskbarIcon();
            
            // 使用应用程序图标
            try
            {
                // 尝试加载嵌入的图标
                var assembly = Assembly.GetExecutingAssembly();
                var iconStream = assembly.GetManifestResourceStream("QuickTranslator.app.ico");
                if (iconStream != null)
                {
                    _trayIcon.Icon = new Icon(iconStream);
                }
                else
                {
                    // 使用默认图标
                    _trayIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
            
            _trayIcon.ToolTipText = "截图翻译工具";
            
            // 创建右键菜单
            var contextMenu = new ContextMenu();
            
            var captureItem = new MenuItem { Header = "开始截图翻译" };
            captureItem.Click += (s, e) => StartCapture();
            contextMenu.Items.Add(captureItem);
            
            contextMenu.Items.Add(new Separator());
            
            var settingsItem = new MenuItem { Header = "打开设置面板" };
            settingsItem.Click += (s, e) => OpenSettings();
            contextMenu.Items.Add(settingsItem);
            
            var autoStartItem = new MenuItem { Header = "开机自启" };
            autoStartItem.IsChecked = ConfigManager.Current.StartWithWindows;
            autoStartItem.Click += (s, e) =>
            {
                ConfigManager.Current.StartWithWindows = !ConfigManager.Current.StartWithWindows;
                ConfigManager.Save();
                autoStartItem.IsChecked = ConfigManager.Current.StartWithWindows;
                UpdateAutoStart();
            };
            contextMenu.Items.Add(autoStartItem);
            
            contextMenu.Items.Add(new Separator());
            
            var exitItem = new MenuItem { Header = "退出程序" };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);
            
            _trayIcon.ContextMenu = contextMenu;
            
            // 双击托盘图标开始截图
            _trayIcon.TrayMouseDoubleClick += (s, e) => StartCapture();
            
            // 创建隐藏窗口用于接收消息
            CreateMessageWindow();
        }
        
        private void CreateMessageWindow()
        {
            // 创建一个隐藏的窗口来接收全局热键消息
            var window = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowState = WindowState.Minimized,
                Topmost = false,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };
            
            window.SourceInitialized += (s, e) =>
            {
                _mainWindowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                HotKeyManager.SetupHook(_mainWindowHandle);
                RegisterHotKey();
            };
            
            window.Show();
            window.Hide();
        }
        
        private void RegisterHotKey()
        {
            if (_mainWindowHandle == IntPtr.Zero) return;
            
            HotKeyManager.HotKeyPressed -= StartCapture;
            HotKeyManager.HotKeyPressed += StartCapture;
            
            bool success = HotKeyManager.Register(
                _mainWindowHandle,
                ConfigManager.Current.CaptureHotkeyModifier,
                ConfigManager.Current.CaptureHotkeyKey
            );
            
            if (!success)
            {
                _trayIcon.ShowBalloonTip("提示", "快捷键注册失败，可能与其他程序冲突", BalloonIcon.Warning);
            }
        }
        
        private void UnregisterHotKey()
        {
            HotKeyManager.Unregister();
            HotKeyManager.HotKeyPressed -= StartCapture;
        }
        
        private void StartCapture()
        {
            if (_captureWindow != null) return;
            
            _captureWindow = new CaptureWindow();
            _captureWindow.CaptureCompleted += OnCaptureCompleted;
            _captureWindow.Show();
        }
        
        private async void OnCaptureCompleted(byte[]? imageBytes, Rect rect)
        {
            _captureWindow = null;
            
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _trayIcon.ShowBalloonTip("提示", "截图取消", BalloonIcon.Info);
                return;
            }
            
            try
            {
                // OCR识别，获取文字和位置
                var ocrResult = await OcrEngineHelper.RecognizeWithPositionAsync(imageBytes, ConfigManager.Current.OcrLanguage);
                
                if (ocrResult.Items.Count == 0)
                {
                    _trayIcon.ShowBalloonTip("提示", "未识别到文字", BalloonIcon.Info);
                    return;
                }
                
                // 按行分组
                var lines = GroupByLines(ocrResult.Items);
                
                // 翻译每行
                var translations = new Dictionary<string, string>();
                foreach (var line in lines)
                {
                    string lineText = string.Join(" ", line.ConvertAll(i => i.Text));
                    if (!translations.ContainsKey(lineText))
                    {
                        string translated = await TranslationService.TranslateAsync(
                            lineText, 
                            ConfigManager.Current.TranslationDirection,
                            ConfigManager.Current.UseOnlineTranslation,
                            ConfigManager.Current.OcrLanguage
                        );
                        translations[lineText] = translated;
                    }
                }
                
                // 显示图片翻译结果
                _imageTranslateWindow?.Close();
                _imageTranslateWindow = new ImageTranslateWindow(imageBytes, ocrResult.Items, translations);
                _imageTranslateWindow.Show();
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip("错误", $"处理失败: {ex.Message}", BalloonIcon.Error);
            }
        }
        
        private List<List<OcrResultItem>> GroupByLines(List<OcrResultItem> items)
        {
            var result = new List<List<OcrResultItem>>();
            if (items.Count == 0) return result;
            
            var currentLine = new List<OcrResultItem>();
            double currentY = items[0].BoundingBox.Y;
            double lineHeight = items[0].BoundingBox.Height;
            
            foreach (var item in items)
            {
                if (Math.Abs(item.BoundingBox.Y - currentY) < lineHeight * 0.5)
                {
                    currentLine.Add(item);
                }
                else
                {
                    if (currentLine.Count > 0)
                    {
                        result.Add(currentLine);
                    }
                    currentLine = new List<OcrResultItem> { item };
                    currentY = item.BoundingBox.Y;
                    lineHeight = item.BoundingBox.Height;
                }
            }
            
            if (currentLine.Count > 0)
            {
                result.Add(currentLine);
            }
            
            return result;
        }
        
        private string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "EnToZh";
            
            // 简单的语言检测逻辑
            bool hasChinese = false;
            bool hasJapanese = false;
            bool hasKorean = false;
            
            foreach (char c in text)
            {
                if (c >= 0x4E00 && c <= 0x9FFF) // CJK统一汉字
                {
                    hasChinese = true;
                }
                else if (c >= 0x3040 && c <= 0x309F || c >= 0x30A0 && c <= 0x30FF) // 平假名和片假名
                {
                    hasJapanese = true;
                }
                else if (c >= 0xAC00 && c <= 0xD7AF) // 韩文
                {
                    hasKorean = true;
                }
            }
            
            // 如果主要是中文，翻译成英文
            if (hasChinese && !hasJapanese && !hasKorean)
            {
                return "ZhToEn";
            }
            
            // 如果有日文，翻译成中文
            if (hasJapanese)
            {
                return "JaToZh";
            }
            
            // 如果有韩文，翻译成中文
            if (hasKorean)
            {
                return "KoToZh";
            }
            
            // 默认英文翻译成中文
            return "EnToZh";
        }
        
        private void OpenSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }
            
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        
        private void UpdateAutoStart()
        {
            try
            {
                string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(appPath)) return;
                
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;
                
                if (ConfigManager.Current.StartWithWindows)
                {
                    key.SetValue("QuickTranslator", $"\"{appPath}\"");
                }
                else
                {
                    key.DeleteValue("QuickTranslator", false);
                }
            }
            catch
            {
                // 忽略错误
            }
        }
        
        private void ExitApplication()
        {
            UnregisterHotKey();
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        }
        
        public void Dispose()
        {
            _trayIcon?.Dispose();
        }
    }
}
