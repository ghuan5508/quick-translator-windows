using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace QuickTranslator
{
    public class ImageTranslateWindow : Window
    {
        private Canvas _mainCanvas;
        private System.Windows.Controls.Image _backgroundImage;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private byte[] _originalImage;
        
        public ImageTranslateWindow(byte[] originalImage, List<OcrResultItem> ocrItems, Dictionary<string, string> translations)
        {
            _originalImage = originalImage;
            
            Title = "翻译结果 - 按 Esc 关闭";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Cursor = System.Windows.Input.Cursors.Arrow;
            
            // 加载原图获取实际尺寸
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(originalImage))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();
            
            double originalWidth = bitmapImage.PixelWidth;
            double originalHeight = bitmapImage.PixelHeight;
            
            double maxWidth = SystemParameters.PrimaryScreenWidth * 0.85;
            double maxHeight = SystemParameters.PrimaryScreenHeight * 0.85;
            
            double displayWidth = originalWidth;
            double displayHeight = originalHeight;
            
            _scaleX = 1.0;
            _scaleY = 1.0;
            
            if (displayWidth > maxWidth || displayHeight > maxHeight)
            {
                double scaleX = maxWidth / displayWidth;
                double scaleY = maxHeight / displayHeight;
                double scale = Math.Min(scaleX, scaleY);
                _scaleX = scale;
                _scaleY = scale;
                displayWidth = originalWidth * scale;
                displayHeight = originalHeight * scale;
            }
            
            Width = displayWidth + 20;
            Height = displayHeight + 70;
            
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
            
            var mainStack = new StackPanel();
            
            // 标题栏
            var titleBar = new DockPanel
            {
                Margin = new Thickness(10, 8, 10, 5)
            };
            titleBar.MouseLeftButtonDown += (s, e) => DragMove();
            
            var titleLabel = new TextBlock
            {
                Text = "📖 图片翻译结果",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 255, 150)),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleLabel, Dock.Left);
            titleBar.Children.Add(titleLabel);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var saveButton = CreateButton("💾 保存", () => SaveImage());
            var copyButton = CreateButton("📋 复制", () => CopyImage());
            var closeButton = CreateButton("✕", () => Close());
            
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);
            
            DockPanel.SetDock(buttonPanel, Dock.Right);
            titleBar.Children.Add(buttonPanel);
            
            mainStack.Children.Add(titleBar);
            
            // 图片区域
            var imageContainer = new Border
            {
                Margin = new Thickness(10, 0, 10, 10),
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 30)),
                CornerRadius = new CornerRadius(4)
            };
            
            _mainCanvas = new Canvas
            {
                Width = displayWidth,
                Height = displayHeight
            };
            
            // 背景图
            _backgroundImage = new System.Windows.Controls.Image
            {
                Source = bitmapImage,
                Width = displayWidth,
                Height = displayHeight,
                Stretch = Stretch.Fill
            };
            Canvas.SetLeft(_backgroundImage, 0);
            Canvas.SetTop(_backgroundImage, 0);
            _mainCanvas.Children.Add(_backgroundImage);
            
            // 按行分组 OCR 项
            var lines = GroupByLines(ocrItems);
            
            // 在原图上覆盖翻译
            foreach (var line in lines)
            {
                // 翻译整行
                string lineText = string.Join(" ", line.ConvertAll(i => i.Text));
                if (!translations.TryGetValue(lineText, out var translatedLine))
                {
                    // 如果整行没有翻译，尝试逐词翻译并拼接
                    var translatedWords = new List<string>();
                    foreach (var item in line)
                    {
                        if (translations.TryGetValue(item.Text, out var wordTrans) && !string.IsNullOrEmpty(wordTrans))
                        {
                            translatedWords.Add(wordTrans);
                        }
                        else
                        {
                            translatedWords.Add(item.Text);
                        }
                    }
                    translatedLine = string.Join("", translatedWords);
                }
                
                if (string.IsNullOrEmpty(translatedLine) || translatedLine == lineText) continue;
                
                // 计算整行的边界框
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                
                foreach (var item in line)
                {
                    minX = Math.Min(minX, item.BoundingBox.X);
                    minY = Math.Min(minY, item.BoundingBox.Y);
                    maxX = Math.Max(maxX, item.BoundingBox.X + item.BoundingBox.Width);
                    maxY = Math.Max(maxY, item.BoundingBox.Y + item.BoundingBox.Height);
                }
                
                // 缩放坐标
                double x = minX * _scaleX;
                double y = minY * _scaleY;
                double w = (maxX - minX) * _scaleX;
                double h = (maxY - minY) * _scaleY;
                
                // 确保位置在画布范围内
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + w > displayWidth) w = displayWidth - x;
                if (y + h > displayHeight) h = displayHeight - y;
                
                // 白色背景覆盖原文字
                var cover = new System.Windows.Shapes.Rectangle
                {
                    Width = Math.Max(w + 6, 20),
                    Height = Math.Max(h + 4, 16),
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    StrokeThickness = 0.5,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(cover, x - 3);
                Canvas.SetTop(cover, y - 2);
                _mainCanvas.Children.Add(cover);
                
                // 计算合适的字体大小
                double fontSize = Math.Max(10, Math.Min(h * 0.7, 24));
                
                // 中文翻译文字
                var textBlock = new TextBlock
                {
                    Text = translatedLine,
                    FontSize = fontSize,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Width = Math.Max(w + 6, 20),
                    Height = Math.Max(h + 4, 16),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(textBlock, x - 3);
                Canvas.SetTop(textBlock, y - 2);
                _mainCanvas.Children.Add(textBlock);
            }
            
            imageContainer.Child = _mainCanvas;
            mainStack.Children.Add(imageContainer);
            
            mainBorder.Child = mainStack;
            Content = mainBorder;
            
            // 按 Esc 关闭
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
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
        
        private Button CreateButton(string content, Action onClick)
        {
            var button = new Button
            {
                Content = content,
                Height = 24,
                MinWidth = 40,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 70)),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4, 0, 4, 0),
                FontSize = 11,
                Padding = new Thickness(8, 0, 8, 0)
            };
            button.Click += (s, e) => onClick();
            return button;
        }
        
        private void SaveImage()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG 图片|*.png|JPEG 图片|*.jpg",
                DefaultExt = ".png",
                FileName = "translated_image"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dialog.FileName, _originalImage);
                    MessageBox.Show("原图已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void CopyImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(_originalImage))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }
                Clipboard.SetImage(bitmap);
                MessageBox.Show("原图已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
