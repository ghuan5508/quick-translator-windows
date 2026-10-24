using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace QuickTranslator
{
    public static class TranslationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static bool _initialized = false;
        
        private static void Initialize()
        {
            if (_initialized) return;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "QuickTranslator/1.0");
            _initialized = true;
        }
        
        public static async Task<string> TranslateAsync(string text, string direction, bool useOnline, string ocrLanguage)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            Initialize();
            
            string source = text.Trim();
            
            // 在线翻译使用免费 MyMemory 服务，不需要 API 密钥。
            if (useOnline && TryGetLanguagePair(direction, ocrLanguage, out var from, out var to))
            {
                try
                {
                    string onlineResult = await TranslateWithMyMemory(source, from, to);
                    if (!string.IsNullOrEmpty(onlineResult) && onlineResult != source)
                    {
                        return onlineResult;
                    }
                }
                catch { }
            }
            catch { }
            
            // 在线翻译失败，使用离线词典
            return TranslateOffline(source);
        }

        private static bool TryGetLanguagePair(string direction, string ocrLanguage, out string from, out string to)
        {
            to = "zh-CN";
            from = direction switch
            {
                "EnToZh" => "en",
                "JaToZh" => "ja",
                "KoToZh" => "ko",
                "ZhToEn" => "zh-CN",
                // MyMemory requires an explicit source language. Use the selected OCR language.
                "AutoToZh" => ocrLanguage switch
                {
                    "en-US" => "en",
                    "ja-JP" => "ja",
                    "ko-KR" => "ko",
                    "zh-Hans-CN" => "zh-CN",
                    _ => ""
                },
                _ => ""
            };

            if (direction == "ZhToEn") to = "en";
            return !string.IsNullOrEmpty(from);
        }
        
        private static async Task<string> TranslateWithMyMemory(string text, string from, string to)
        {
            try
            {
                string encodedText = HttpUtility.UrlEncode(text);
                string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={from}|{to}";
                
                var response = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("responseData", out var responseData))
                {
                    if (responseData.TryGetProperty("translatedText", out var translatedText))
                    {
                        string result = translatedText.GetString() ?? "";
                        // MyMemory 返回全大写表示失败
                        if (result == text.ToUpper() || result.Contains("MYMEMORY WARNING"))
                        {
                            return "";
                        }
                        return result;
                    }
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        private static string TranslateOffline(string text)
        {
            var dict = WordDictionary.EnToZh;
            if (dict == null) return text;
            
            // 尝试匹配整个句子
            if (dict.TryGetValue(text.ToLower(), out var fullTranslation))
            {
                return fullTranslation;
            }
            
            // 逐词翻译
            var result = new StringBuilder();
            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                
                // 保留标点
                var leadingPunct = "";
                var trailingPunct = "";
                var cleanWord = word;
                
                while (cleanWord.Length > 0 && char.IsPunctuation(cleanWord[0]))
                {
                    leadingPunct += cleanWord[0];
                    cleanWord = cleanWord.Substring(1);
                }
                
                while (cleanWord.Length > 0 && char.IsPunctuation(cleanWord[cleanWord.Length - 1]))
                {
                    trailingPunct = cleanWord[cleanWord.Length - 1] + trailingPunct;
                    cleanWord = cleanWord.Substring(0, cleanWord.Length - 1);
                }
                
                if (cleanWord.Length == 0)
                {
                    result.Append(word);
                    continue;
                }
                
                // 单词翻译
                if (dict.TryGetValue(cleanWord.ToLower(), out var wordTrans))
                {
                    result.Append(leadingPunct);
                    result.Append(wordTrans);
                    result.Append(trailingPunct);
                }
                else
                {
                    result.Append(word);
                }
                
                if (i < words.Length - 1)
                {
                    result.Append(" ");
                }
            }
            
            return result.ToString();
        }
    }
}
