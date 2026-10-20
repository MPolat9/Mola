using System;
using System.ComponentModel;
using System.IO;

namespace Mola.Core
{
    public enum FreezeResult { Ok, AccessDenied, Gone, Failed }

    /// <summary>
    /// Bir işlemin tüm iş parçacıklarını aynı anda dondurur / çözer.
    /// Oyunun saati, mantığı ve ses iş parçacıkları birlikte durduğu için
    /// devam edince hepsi birbirine göre kaldığı yerden sürer.
    /// Hangi işlemin donduğu diske de yazılır: Mola beklenmedik kapanırsa
    /// bir sonraki açılışta oyun otomatik çözülür, asla donuk kalmaz.
    /// </summary>
    public sealed class ProcessFreezer
    {
        static readonly string StateFile = Path.Combine(AppPaths.DataDir, "donuk-islem.txt");

        IntPtr _handle = IntPtr.Zero;
        long _creationTime;

        public int Pid { get; private set; }
        public string ProcessName { get; private set; }
        public bool IsFrozen => _handle != IntPtr.Zero;

        public FreezeResult Freeze(int pid, string processName)
        {
            if (IsFrozen) return FreezeResult.Failed;

            var h = Native.OpenProcess(Native.PROCESS_SUSPEND_RESUME | Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.SYNCHRONIZE, false, pid);
            if (h == IntPtr.Zero)
            {
                int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Log.Write($"OpenProcess başarısız pid={pid} hata={err}");
                return err == 5 ? FreezeResult.AccessDenied : FreezeResult.Gone;
            }

            Native.GetProcessTimes(h, out long creation, out _, out _, out _);

            // Önce diske yaz: dondurduktan hemen sonra Mola çökse bile kurtarma bilgisi hazır olsun.
            WriteState(pid, creation);

            int status = Native.NtSuspendProcess(h);
            if (status != 0)
            {
                Log.Write($"NtSuspendProcess başarısız pid={pid} durum=0x{status:X8}");
                Native.CloseHandle(h);
                DeleteState();
                return (uint)status == 0xC0000022 ? FreezeResult.AccessDenied : FreezeResult.Failed;
            }

            _handle = h;
            _creationTime = creation;
            Pid = pid;
            ProcessName = processName;
            Log.Write($"Donduruldu: {processName} pid={pid}");
            return FreezeResult.Ok;
        }

        public void Thaw()
        {
            if (!IsFrozen) return;
            int status = Native.NtResumeProcess(_handle);
            Log.Write($"Çözüldü: {ProcessName} pid={Pid} durum=0x{status:X8}");
            Native.CloseHandle(_handle);
            _handle = IntPtr.Zero;
            Pid = 0;
            ProcessName = null;
            DeleteState();
        }

        /// <summary>Donuk işlem bu arada kapandıysa (ör. kullanıcı Görev Yöneticisi'nden kapattı) true döner ve durumu temizler.</summary>
        public bool CheckExited()
        {
            if (!IsFrozen) return false;
            if (Native.WaitForSingleObject(_handle, 0) != Native.WAIT_OBJECT_0) return false;
            Log.Write($"Donuk işlem kapanmış: {ProcessName} pid={Pid}");
            Native.CloseHandle(_handle);
            _handle = IntPtr.Zero;
            Pid = 0;
            ProcessName = null;
            DeleteState();
            return true;
        }

        /// <summary>Önceki oturumdan donuk kalmış bir işlem varsa çözer (çökme/elektrik kesintisi sonrası kurtarma).</summary>
        public static void RecoverLeftover()
        {
            try
            {
                if (!File.Exists(StateFile)) return;
                var parts = File.ReadAllText(StateFile).Trim().Split('|');
                File.Delete(StateFile);
                if (parts.Length != 2 || !int.TryParse(parts[0], out int pid) || !long.TryParse(parts[1], out long creation)) return;

                var h = Native.OpenProcess(Native.PROCESS_SUSPEND_RESUME | Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (h == IntPtr.Zero) return;
                try
                {
                    // Aynı numarayı başka bir işlem almış olabilir: oluşturulma zamanı eşleşmiyorsa dokunma.
                    if (Native.GetProcessTimes(h, out long c, out _, out _, out _) && c == creation)
                    {
                        Native.NtResumeProcess(h);
                        Log.Write($"Önceki oturumdan donuk kalan işlem çözüldü pid={pid}");
                    }
                }
                finally { Native.CloseHandle(h); }
            }
            catch (Exception ex) { Log.Write("Kurtarma hatası: " + ex.Message); }
        }

        static void WriteState(int pid, long creation)
        {
            try { File.WriteAllText(StateFile, pid + "|" + creation); }
            catch (Exception ex) { Log.Write("Durum yazılamadı: " + ex.Message); }
        }

        static void DeleteState()
        {
            try { if (File.Exists(StateFile)) File.Delete(StateFile); }
            catch (Exception ex) { Log.Write("Durum silinemedi: " + ex.Message); }
        }
    }
}
