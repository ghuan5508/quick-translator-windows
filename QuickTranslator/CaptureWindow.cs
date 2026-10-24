using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using Size = System.Drawing.Size;
using Rectangle = System.Windows.Shapes.Rectangle;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Brushes = System.Windows.Media.Brushes;

namespace QuickTranslator
{
    public class CaptureWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting;
        private Canvas _overlayCanvas;
        private Rectangle _selectionVisual;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        public event Action<byte[]?, Rect>? CaptureCompleted;

        public CaptureWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 0));
            Topmost = true;
            Cursor = Cursors.Cross;
            AllowsTransparency = true;
            ShowInTaskbar = false;
            
            _overlayCanvas = new Canvas();
            Content = _overlayCanvas;

            _selectionVisual = new Rectangle
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 255, 0)),
                Visibility = Visibility.Collapsed
            };
            Canvas.SetLeft(_selectionVisual, 0);
            Canvas.SetTop(_selectionVisual, 0);
            _overlayCanvas.Children.Add(_selectionVisual);

            PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            PreviewMouseMove += OnMouseMove;
            PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewMouseRightButtonDown += (s, e) => CloseWindow(null);
            
            SourceInitialized += (s, e) =>
            {
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    _dpiScaleX = source.CompositionTarget.TransformFromDevice.M11;
                    _dpiScaleY = source.CompositionTarget.TransformFromDevice.M22;
                }
            };
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isSelecting = true;
            _selectionVisual.Width = 0;
            _selectionVisual.Height = 0;
            _selectionVisual.Visibility = Visibility.Visible;
            _selectionVisual.SetValue(Canvas.LeftProperty, _startPoint.X);
            _selectionVisual.SetValue(Canvas.TopProperty, _startPoint.Y);
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPos = e.GetPosition(this);
            double x = Math.Min(currentPos.X, _startPoint.X);
            double y = Math.Min(currentPos.Y, _startPoint.Y);
            double width = Math.Abs(currentPos.X - _startPoint.X);
            double height = Math.Abs(currentPos.Y - _startPoint.Y);

            _selectionVisual.SetValue(Canvas.LeftProperty, x);
            _selectionVisual.SetValue(Canvas.TopProperty, y);
            _selectionVisual.Width = width;
            _selectionVisual.Height = height;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;
            _isSelecting = false;
            ReleaseMouseCapture();

            var currentPos = e.GetPosition(this);
            double x = Math.Min(currentPos.X, _startPoint.X);
            double y = Math.Min(currentPos.Y, _startPoint.Y);
            double width = Math.Abs(currentPos.X - _startPoint.X);
            double height = Math.Abs(currentPos.Y - _startPoint.Y);

            if (width < 10 || height < 10)
            {
                CloseWindow(null);
                return;
            }

            // 转换为屏幕物理像素坐标
            // 对于 Maximized 窗口，Left=0, Top=0，直接使用窗口坐标
            // 需要考虑 DPI 缩放
            var screenX = (int)(x / _dpiScaleX);
            var screenY = (int)(y / _dpiScaleY);
            var screenWidth = (int)(width / _dpiScaleX);
            var screenHeight = (int)(height / _dpiScaleY);
            
            var screenRect = new Rect(screenX, screenY, screenWidth, screenHeight);
            
            Hide();
            
            byte[]? imageBytes = CaptureScreenRegion(screenRect);
            CaptureCompleted?.Invoke(imageBytes, screenRect);
            
            Close();
        }

        private byte[]? CaptureScreenRegion(Rect rect)
        {
            try
            {
                int x = (int)rect.X;
                int y = (int)rect.Y;
                int width = (int)rect.Width;
                int height = (int)rect.Height;

                if (width <= 0 || height <= 0) return null;

                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseWindow(null);
            }
        }

        private void CloseWindow(byte[]? result)
        {
            CaptureCompleted?.Invoke(result, Rect.Empty);
            Close();
        }
    }
}
