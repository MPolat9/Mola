using System;
using System.IO;

namespace Mola.Core
{
    /// <summary>Ayar ve günlük dosyalarının yeri: %APPDATA%\Mola (program klasörüne hiçbir şey yazılmaz).</summary>
    public static class AppPaths
    {
        public static readonly string DataDir;

        static AppPaths()
        {
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mola");
            try { Directory.CreateDirectory(DataDir); } catch { }
        }
    }

    /// <summary>Kısa, kendini sınırlayan günlük: sorun bildirimi için %APPDATA%\Mola\mola.log.</summary>
    public static class Log
    {
        static readonly object Gate = new object();
        static readonly string FilePath = Path.Combine(AppPaths.DataDir, "mola.log");

        public static void Write(string line)
        {
            lock (Gate)
            {
                try
                {
                    var fi = new FileInfo(FilePath);
                    if (fi.Exists && fi.Length > 512 * 1024)
                    {
                        var old = FilePath + ".1";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(FilePath, old);
                    }
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + line + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
