using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace QuickTranslator
{
    public class OcrResultItem
    {
        public string Text { get; set; } = "";
        public Rect BoundingBox { get; set; }
    }

    public class OcrFullResult
    {
        public List<OcrResultItem> Items { get; set; } = new List<OcrResultItem>();
        public string FullText { get; set; } = "";
    }

    public static class OcrEngineHelper
    {
        public static async Task<OcrFullResult> RecognizeWithPositionAsync(byte[] imageBytes, string languageCode)
        {
            var result = new OcrFullResult();
            
            try
            {
                // 预处理图片
                byte[] processedImage = EnhanceImageForOcr(imageBytes);
                
                var randomAccessStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(processedImage);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }

                var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                
                var language = new Language(languageCode);
                
                OcrEngine? ocrEngine = null;
                if (OcrEngine.IsLanguageSupported(language))
                {
                    ocrEngine = OcrEngine.TryCreateFromLanguage(language);
                }

                if (ocrEngine == null)
                {
                    ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                }

                if (ocrEngine == null)
                {
                    result.FullText = "提示: 您的系统未安装此语言的 OCR 语言包";
                    return result;
                }
                
                var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                
                if (ocrResult.Lines == null || ocrResult.Lines.Count == 0)
                {
                    // 如果第一次识别失败，尝试用原图
                    if (processedImage != imageBytes)
                    {
                        return await RecognizeWithOriginalImage(imageBytes, ocrEngine);
                    }
                    return result;
                }

                var fullTextSb = new System.Text.StringBuilder();
                
                foreach (var line in ocrResult.Lines)
                {
                    fullTextSb.AppendLine(line.Text);
                    
                    foreach (var word in line.Words)
                    {
                        var rect = word.BoundingRect;
                        result.Items.Add(new OcrResultItem
                        {
                            Text = word.Text,
                            BoundingBox = new Rect(rect.X, rect.Y, rect.Width, rect.Height)
                        });
                    }
                }
                
                result.FullText = fullTextSb.ToString().Trim();
                return result;
            }
            catch (Exception ex)
            {
                result.FullText = $"OCR 识别出错: {ex.Message}";
                return result;
            }
        }
        
        private static async Task<OcrFullResult> RecognizeWithOriginalImage(byte[] imageBytes, OcrEngine ocrEngine)
        {
            var result = new OcrFullResult();
            
            var randomAccessStream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(imageBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }

            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            
            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
            
            if (ocrResult.Lines != null)
            {
                var fullTextSb = new System.Text.StringBuilder();
                
                foreach (var line in ocrResult.Lines)
                {
                    fullTextSb.AppendLine(line.Text);
                    
                    foreach (var word in line.Words)
                    {
                        var rect = word.BoundingRect;
                        result.Items.Add(new OcrResultItem
                        {
                            Text = word.Text,
                            BoundingBox = new Rect(rect.X, rect.Y, rect.Width, rect.Height)
                        });
                    }
                }
                
                result.FullText = fullTextSb.ToString().Trim();
            }
            
            return result;
        }
        
        private static byte[] EnhanceImageForOcr(byte[] imageBytes)
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var original = new Bitmap(ms);
                
                int width = original.Width;
                int height = original.Height;
                
                // 如果图片太小，放大到至少 1000px 宽
                if (width < 1000)
                {
                    double scale = 1000.0 / width;
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }
                
                using var processed = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(processed))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(original, 0, 0, width, height);
                }
                
                // 轻度增强：提高对比度，保留灰度
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = processed.GetPixel(x, y);
                        
                        // 转换为灰度
                        int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                        
                        // 轻度提高对比度（1.2倍）
                        gray = (int)((gray - 128) * 1.2 + 128);
                        gray = Math.Max(0, Math.Min(255, gray));
                        
                        processed.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                    }
                }
                
                using var outMs = new MemoryStream();
                processed.Save(outMs, ImageFormat.Png);
                return outMs.ToArray();
            }
            catch
            {
                return imageBytes;
            }
        }
    }
}
