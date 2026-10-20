using System;
using System.Windows.Interop;

namespace Mola.Core
{
    /// <summary>
    /// Windows'a sistem genelinde bir kısayol kaydeder. Tuş oyun öndeyken de çalışır
    /// ve Windows tuşu kendisi yakaladığı için oyuna ulaşmaz (oyunda başka bir işi tetiklemez).
    /// </summary>
    public sealed class HotkeyManager : IDisposable
    {
        const int Id = 0x4D4F; // "MO"
        readonly HwndSource _source;
        bool _registered;

        public event Action Pressed;

        public HotkeyManager()
        {
            // Görünmez, yalnızca mesaj alan pencere.
            var p = new HwndSourceParameters("MolaKisayol") { Width = 0, Height = 0, WindowStyle = 0, ParentWindow = new IntPtr(-3) /* HWND_MESSAGE */ };
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
        }

        /// <summary>Kaydeder. Başka bir program aynı tuşu tutuyorsa false döner.</summary>
        public bool Register(Hotkey hk)
        {
            Unregister();
            if (hk.IsEmpty) return false;
            _registered = Native.RegisterHotKey(_source.Handle, Id, hk.Modifiers | Native.MOD_NOREPEAT, hk.Vk);
            if (!_registered)
                Log.Write($"Kısayol kaydedilemedi: {hk} hata={System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
            return _registered;
        }

        public void Unregister()
        {
            if (!_registered) return;
            Native.UnregisterHotKey(_source.Handle, Id);
            _registered = false;
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_HOTKEY && wParam.ToInt32() == Id)
            {
                handled = true;
                Pressed?.Invoke();
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            _source.RemoveHook(WndProc);
            _source.Dispose();
        }
    }
}
