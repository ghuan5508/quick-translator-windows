using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickTranslator
{
    public static class HotKeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private static IntPtr _hWnd;
        private static HwndSource? _hwndSource;

        public static event Action? HotKeyPressed;

        public static bool Register(IntPtr hWnd, string modifierStr, string keyStr)
        {
            Unregister();

            _hWnd = hWnd;
            uint modifiers = GetModifiers(modifierStr);
            uint vk = GetVirtualKey(keyStr);

            if (vk == 0) return false;

            return RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, vk);
        }

        public static void Unregister()
        {
            if (_hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
            }
        }

        private static uint GetModifiers(string modifierStr)
        {
            uint modifiers = MOD_NONE;
            if (string.IsNullOrEmpty(modifierStr)) return modifiers;

            var parts = modifierStr.Split('+', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var clean = part.Trim().ToLower();
                if (clean == "alt") modifiers |= MOD_ALT;
                else if (clean == "ctrl" || clean == "control") modifiers |= MOD_CONTROL;
                else if (clean == "shift") modifiers |= MOD_SHIFT;
                else if (clean == "win" || clean == "windows") modifiers |= MOD_WIN;
            }
            return modifiers;
        }

        private static uint GetVirtualKey(string keyStr)
        {
            if (string.IsNullOrEmpty(keyStr)) return 0;
            if (Enum.TryParse<Key>(keyStr, true, out Key key))
            {
                return (uint)KeyInterop.VirtualKeyFromKey(key);
            }
            return 0;
        }

        public static void SetupHook(IntPtr hWnd)
        {
            _hwndSource = HwndSource.FromHwnd(hWnd);
            _hwndSource?.AddHook(HwndHook);
        }

        public static void RemoveHook()
        {
            _hwndSource?.RemoveHook(HwndHook);
            _hwndSource = null;
        }

        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotKeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
