using System;
using Microsoft.Win32;

namespace Mola.Core
{
    /// <summary>"Windows açılınca başlat": yalnızca bu kullanıcının Run kaydı, yönetici izni gerekmez.</summary>
    public static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "Mola";

        static string Command => "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" --arka-plan";

        public static void Apply(bool enabled)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (enabled) k.SetValue(Name, Command);
                    else if (k.GetValue(Name) != null) k.DeleteValue(Name, false);
                }
            }
            catch (Exception ex) { Log.Write("Başlangıç kaydı değiştirilemedi: " + ex.Message); }
        }

        /// <summary>Exe taşındıysa kayıttaki yolu günceller; kapalıysa kayıt bırakmaz.</summary>
        public static void Sync(bool enabled)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    var cur = k?.GetValue(Name) as string;
                    if (enabled && cur == Command) return;
                    if (!enabled && cur == null) return;
                }
                Apply(enabled);
            }
            catch (Exception ex) { Log.Write("Başlangıç kaydı okunamadı: " + ex.Message); }
        }
    }
}
