using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Mola.Core;

namespace Mola.UI
{
    /// <summary>
    /// Windows 11 açık/koyu renk düzeni. Renkler programın içinde tanımlı; Windows sürümüne göre
    /// değişmez, yalnızca kullanıcının açık/koyu tercihini izler.
    /// </summary>
    public static class Theme
    {
        public static bool IsDark { get; private set; }
        public static event Action Changed;

        public static void Init()
        {
            Apply(ReadSystemDark());
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category != UserPreferenceCategory.General && e.Category != UserPreferenceCategory.Color) return;
                bool dark = ReadSystemDark();
                if (dark == IsDark) return;
                Application.Current?.Dispatcher.BeginInvoke(new Action(() => { Apply(dark); Changed?.Invoke(); }));
            };
        }

        static bool ReadSystemDark()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch { return false; }
        }

        static void Apply(bool dark)
        {
            IsDark = dark;
            var r = Application.Current.Resources;
            void Set(string key, string hex) => r[key] = Freeze(new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)));

            if (dark)
            {
                Set("WindowBg", "#202020");
                Set("CardBg", "#2B2B2B");
                Set("CardStroke", "#1D1D1D");
                Set("TextPrimary", "#FFFFFF");
                Set("TextSecondary", "#C8C8C8");
                Set("TextTertiary", "#9A9A9A");
                Set("Accent", "#4CC2FF");
                Set("AccentHover", "#47B1E8");
                Set("AccentPressed", "#42A1D2");
                Set("OnAccent", "#000000");
                Set("ControlBg", "#2F2F2F");
                Set("ControlHover", "#383838");
                Set("ControlPressed", "#2A2A2A");
                Set("ControlStroke", "#444444");
                Set("KeycapBg", "#3A3A3A");
                Set("KeycapStroke", "#4F4F4F");
                Set("ToggleOffStroke", "#9A9A9A");
                Set("ToggleOffKnob", "#D0D0D0");
                Set("Track", "#6A6A6A");
                Set("Good", "#6CCB5F");
                Set("GoodBg", "#1F3A1D");
                Set("Warn", "#FCE100");
                Set("WarnBg", "#433519");
                Set("InfoBg", "#1E3446");
                Set("MenuBg", "#2C2C2C");
                Set("MenuStroke", "#454545");
                Set("MenuHover", "#3A3A3A");
                Set("MenuPressed", "#333333");
            }
            else
            {
                Set("WindowBg", "#F3F3F3");
                Set("CardBg", "#FBFBFB");
                Set("CardStroke", "#E5E5E5");
                Set("TextPrimary", "#1B1B1B");
                Set("TextSecondary", "#5C5C5C");
                Set("TextTertiary", "#8A8A8A");
                Set("Accent", "#005FB8");
                Set("AccentHover", "#1A6FC1");
                Set("AccentPressed", "#3380CA");
                Set("OnAccent", "#FFFFFF");
                Set("ControlBg", "#FFFFFF");
                Set("ControlHover", "#F7F7F7");
                Set("ControlPressed", "#F0F0F0");
                Set("ControlStroke", "#D5D5D5");
                Set("KeycapBg", "#FFFFFF");
                Set("KeycapStroke", "#CFCFCF");
                Set("ToggleOffStroke", "#858585");
                Set("ToggleOffKnob", "#5C5C5C");
                Set("Track", "#C2C2C2");
                Set("Good", "#0F7B0F");
                Set("GoodBg", "#DFF6DD");
                Set("Warn", "#9D5D00");
                Set("WarnBg", "#FFF4CE");
                Set("InfoBg", "#E6F0FA");
                Set("MenuBg", "#F9F9F9");
                Set("MenuStroke", "#E0E0E0");
                Set("MenuHover", "#EBEBEB");
                Set("MenuPressed", "#E2E2E2");
            }
        }

        static Brush Freeze(Brush b) { b.Freeze(); return b; }

        /// <summary>Pencerenin başlık çubuğunu temaya uydurur (Windows 10 20H1+ / 11).</summary>
        public static void ApplyTitleBar(Window w)
        {
            var h = new WindowInteropHelper(w).Handle;
            if (h == IntPtr.Zero) return;
            int v = IsDark ? 1 : 0;
            try { Native.DwmSetWindowAttribute(h, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int)); } catch { }
        }
    }
}
