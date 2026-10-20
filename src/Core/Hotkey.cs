using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Mola.UI;

namespace Mola.Core
{
    /// <summary>Kısayol: değiştirici tuşlar (Ctrl/Alt/Shift/Win) + bir tuş.</summary>
    public struct Hotkey
    {
        public uint Modifiers;   // Native.MOD_* bayrakları
        public uint Vk;          // sanal tuş kodu

        public Hotkey(uint modifiers, uint vk) { Modifiers = modifiers; Vk = vk; }

        public static readonly Hotkey Default = new Hotkey(0, 0x77); // F8

        public bool IsEmpty => Vk == 0;

        /// <summary>Tek başına seçildiğinde yazı yazmayı bozacak tuşlar (harf, rakam, boşluk, Enter...). Bunlara değiştirici şart.</summary>
        public bool NeedsModifier
        {
            get
            {
                if (Modifiers != 0) return false;
                if (Vk >= 0x70 && Vk <= 0x87) return false;                  // F1-F24
                if (Vk == 0x13 || Vk == 0x91 || Vk == 0x2D || Vk == 0x24 || Vk == 0x23 || Vk == 0x21 || Vk == 0x22) return false; // Pause, Scroll Lock, Insert, Home, End, PgUp, PgDn
                if (Vk >= 0x60 && Vk <= 0x6F) return false;                  // sayı tuş takımı
                if (Vk >= 0xA6 && Vk <= 0xB7) return false;                  // medya / tarayıcı tuşları
                return true;
            }
        }

        public string[] Parts
        {
            get
            {
                var list = new List<string>();
                if ((Modifiers & Native.MOD_CONTROL) != 0) list.Add("Ctrl");
                if ((Modifiers & Native.MOD_ALT) != 0) list.Add("Alt");
                if ((Modifiers & Native.MOD_SHIFT) != 0) list.Add("Shift");
                if ((Modifiers & Native.MOD_WIN) != 0) list.Add("Win");
                list.Add(KeyName(Vk));
                return list.ToArray();
            }
        }

        public override string ToString() => string.Join(" + ", Parts);

        public static string KeyName(uint vk)
        {
            switch (vk)
            {
                case 0x13: return "Pause";
                case 0x91: return "Scroll Lock";
                case 0x20: return Loc.KeySpace;
                case 0x0D: return "Enter";
                case 0x1B: return "Esc";
                case 0x09: return "Tab";
                case 0x08: return Loc.KeyBackspace;
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0x24: return "Home";
                case 0x23: return "End";
                case 0x21: return "Page Up";
                case 0x22: return "Page Down";
                case 0x25: return Loc.KeyLeft;
                case 0x26: return Loc.KeyUp;
                case 0x27: return Loc.KeyRight;
                case 0x28: return Loc.KeyDown;
                case 0x6A: return "Num *";
                case 0x6B: return "Num +";
                case 0x6D: return "Num -";
                case 0x6E: return Loc.Tr ? "Num ," : "Num .";
                case 0x6F: return "Num /";
                case 0xB3: return Loc.KeyPlayPause;
                case 0xB2: return Loc.KeyMediaStop;
                case 0xB0: return Loc.KeyNextTrack;
                case 0xB1: return Loc.KeyPrevTrack;
                case 0xAD: return Loc.KeyMute;
            }
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
            if (vk >= 0x60 && vk <= 0x69) return "Num " + (vk - 0x60);
            if ((vk >= 0x30 && vk <= 0x39) || (vk >= 0x41 && vk <= 0x5A)) return ((char)vk).ToString();

            // Diğerleri (ör. Türkçe klavyedeki Ö, Ç, nokta): Windows'un kendi klavye düzenine göre adı.
            uint scan = Native.MapVirtualKey(vk, 0);
            if (scan != 0)
            {
                var sb = new StringBuilder(64);
                if (Native.GetKeyNameText((int)(scan << 16), sb, sb.Capacity) > 0)
                {
                    var s = sb.ToString();
                    return s.Length == 1 ? s.ToUpper(System.Globalization.CultureInfo.CurrentCulture) : s;
                }
            }
            return Loc.KeyGeneric(vk);
        }

        /// <summary>WPF klavye olayından kısayol üretir. Yalnızca değiştirici basılıysa null döner.</summary>
        public static Hotkey? FromKeyEvent(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin || key == Key.None)
                return null;

            uint mods = 0;
            var m = Keyboard.Modifiers;
            if ((m & ModifierKeys.Control) != 0) mods |= Native.MOD_CONTROL;
            if ((m & ModifierKeys.Alt) != 0) mods |= Native.MOD_ALT;
            if ((m & ModifierKeys.Shift) != 0) mods |= Native.MOD_SHIFT;
            if ((m & ModifierKeys.Windows) != 0) mods |= Native.MOD_WIN;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return null;
            return new Hotkey(mods, vk);
        }
    }
}
