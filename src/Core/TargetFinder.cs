using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mola.Core
{
    public sealed class Target
    {
        public int Pid;
        public string ExeName;     // ör. h1_sp64_ship.exe
        public string Title;       // pencere başlığı
        public IntPtr Window;
        public string DisplayName => string.IsNullOrWhiteSpace(Title) ? ExeName : Title;
    }

    /// <summary>Kısayola basıldığı anda öndeki pencerenin (oyunun) işlemini bulur.</summary>
    public static class TargetFinder
    {
        // Windows'un kendi parçaları: bunları dondurmak masaüstünü kilitler, asla dokunulmaz.
        static readonly HashSet<string> Protected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer.exe", "dwm.exe", "csrss.exe", "winlogon.exe", "svchost.exe", "lsass.exe", "services.exe",
            "taskmgr.exe", "shellexperiencehost.exe", "startmenuexperiencehost.exe", "searchhost.exe",
            "searchapp.exe", "lockapp.exe", "applicationframehost.exe", "textinputhost.exe", "sihost.exe",
            "ctfmon.exe", "systemsettings.exe", "logonui.exe", "consent.exe", "fontdrvhost.exe",
            "gamebar.exe", "gamebarftserver.exe", "widgets.exe", "phoneexperiencehost.exe",
        };

        public static Target FromForeground()
        {
            IntPtr hwnd = Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            IntPtr root = Native.GetAncestor(hwnd, Native.GA_ROOTOWNER);
            if (root != IntPtr.Zero) hwnd = root;

            Native.GetWindowThreadProcessId(hwnd, out int pid);
            if (pid <= 4) return null;

            var title = new StringBuilder(256);
            Native.GetWindowText(hwnd, title, title.Capacity);
            return new Target { Pid = pid, ExeName = ExeNameOf(pid), Title = title.ToString(), Window = hwnd };
        }

        public static bool IsSelf(Target t) => t != null && t.Pid == Process.GetCurrentProcess().Id;

        public static bool IsProtected(Target t) => t == null || string.IsNullOrEmpty(t.ExeName) || Protected.Contains(t.ExeName);

        public static string ExeNameOf(int pid)
        {
            IntPtr h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                var sb = new StringBuilder(1024);
                int size = sb.Capacity;
                return Native.QueryFullProcessImageName(h, 0, sb, ref size) ? Path.GetFileName(sb.ToString()) : null;
            }
            finally { Native.CloseHandle(h); }
        }
    }
}
