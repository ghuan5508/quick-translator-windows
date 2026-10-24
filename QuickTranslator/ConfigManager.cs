using System;
using System.IO;
using System.Text.Json;

namespace QuickTranslator
{
    public class AppConfig
    {
        public string CaptureHotkeyModifier { get; set; } = "Alt";
        public string CaptureHotkeyKey { get; set; } = "Q";
        public string CloseHotkeyKey { get; set; } = "Escape";
        
        public double WindowOpacity { get; set; } = 1.0; // 1.0, 0.8, 0.6
        public string AutoCloseRule { get; set; } = "None"; // None, MouseLeave, Delay
        public int AutoCloseDelaySeconds { get; set; } = 5; // 3, 5, 10
        
        public string OcrLanguage { get; set; } = "en-US"; // zh-Hans-CN, en-US, ja-JP, ko-KR
        public string TranslationDirection { get; set; } = "EnToZh"; // AutoToZh, EnToZh, JaToZh, KoToZh, ZhToEn
        public bool UseOnlineTranslation { get; set; } = true;
        
        public bool StartWithWindows { get; set; } = false;
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public static AppConfig Current { get; private set; } = new AppConfig();

        static ConfigManager()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    Current = new AppConfig();
                }
            }
            catch
            {
                Current = new AppConfig();
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Ignore
            }
        }

        public static void Reset()
        {
            Current = new AppConfig();
            Save();
        }
    }
}
