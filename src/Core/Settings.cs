using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mola.Core
{
    /// <summary>Kullanıcı ayarları: %APPDATA%\Mola\ayarlar.ini (düz, okunabilir metin).</summary>
    public sealed class Settings
    {
        static readonly string FilePath = Path.Combine(AppPaths.DataDir, "ayarlar.ini");

        public Hotkey Hotkey = Hotkey.Default;
        public bool ShowOverlay = true;
        public int DimPercent = 25;          // ekran karartma oranı (0-60)
        public bool RunAtStartup = false;
        public bool KeepRunningOnClose = true;
        public bool TrayHintShown = false;

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
                if (d.TryGetValue("TusKodu", out var vk) && uint.TryParse(vk, out var v) && v != 0)
                    s.Hotkey = new Hotkey(d.TryGetValue("Degistirici", out var m) && uint.TryParse(m, out var mm) ? mm : 0, v);
                if (d.TryGetValue("KarartmaGoster", out var o)) s.ShowOverlay = o == "1";
                if (d.TryGetValue("KarartmaYuzde", out var p) && int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pp))
                    s.DimPercent = Math.Max(0, Math.Min(60, pp));
                if (d.TryGetValue("WindowsIleBaslat", out var r)) s.RunAtStartup = r == "1";
                if (d.TryGetValue("KapatincaArkaPlanda", out var k)) s.KeepRunningOnClose = k == "1";
                if (d.TryGetValue("TepsiIpucuGosterildi", out var t)) s.TrayHintShown = t == "1";
            }
            catch (Exception ex) { Log.Write("Ayarlar okunamadı, varsayılanlar kullanılıyor: " + ex.Message); }
            return s;
        }

        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("; Mola ayarları");
                sb.AppendLine("TusKodu=" + Hotkey.Vk);
                sb.AppendLine("Degistirici=" + Hotkey.Modifiers);
                sb.AppendLine("KarartmaGoster=" + (ShowOverlay ? 1 : 0));
                sb.AppendLine("KarartmaYuzde=" + DimPercent.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("WindowsIleBaslat=" + (RunAtStartup ? 1 : 0));
                sb.AppendLine("KapatincaArkaPlanda=" + (KeepRunningOnClose ? 1 : 0));
                sb.AppendLine("TepsiIpucuGosterildi=" + (TrayHintShown ? 1 : 0));
                // Yarım kalmış yazma ayarları bozmasın: önce geçici dosyaya yaz, sonra yer değiştir.
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
                else File.Move(tmp, FilePath);
            }
            catch (Exception ex) { Log.Write("Ayarlar kaydedilemedi: " + ex.Message); }
        }
    }
}
