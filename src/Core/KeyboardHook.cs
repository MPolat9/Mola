using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mola.Core
{
    /// <summary>
    /// Kısayolu Windows'un en alt klavye katmanında yakalar. Bazı oyunlar tam ekranda klavyeyi kendine kilitler
    /// (raw input / DirectInput özel mod); o zaman Windows kısayolları (RegisterHotKey) hiç gelmez. Bu katman
    /// tuşu oyundan önce görür. Tuş Mola'ya ait olduğu için yutulur, oyuna ulaşmaz.
    /// Ayrı bir iş parçacığında çalışır: arayüz meşgulken bile Windows'un katman süre sınırını aşmaz.
    /// </summary>
    public sealed class KeyboardHook : IDisposable
    {
        readonly Native.LowLevelKeyboardProc _proc; // çöp toplayıcı silmesin diye alanda tutulur
        readonly Action _onPressed;                 // katman iş parçacığından çağrılır
        volatile uint _vk;                          // 0 = kapalı
        volatile uint _mods;
        uint _downVk;                               // yuttuğumuz ve henüz bırakılmamış tuş (yalnız katman iş parçacığı)
        uint _threadId;
        IntPtr _hook;

        public KeyboardHook(Action onPressed)
        {
            _onPressed = onPressed;
            _proc = HookProc;
            // Bilerek Dispose edilmez: bekleme süresi dolsa bile iş parçacığı sonradan Set çağırabilir.
            var ready = new ManualResetEventSlim();
            new Thread(() => Run(ready)) { IsBackground = true, Name = "MolaKlavye" }.Start();
            ready.Wait(3000);
        }

        public bool Installed => _hook != IntPtr.Zero;

        public void SetTarget(Hotkey hk) { _mods = hk.Modifiers; _vk = hk.Vk; }
        public void Clear() => _vk = 0;

        void Run(ManualResetEventSlim ready)
        {
            _threadId = Native.GetCurrentThreadId();
            _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero) Log.Write("Klavye katmanı kurulamadı, yalnız Windows kısayolu kullanılacak. hata=" + Marshal.GetLastWin32Error());
            ready.Set();
            if (_hook == IntPtr.Zero) return;

            while (Native.GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }
            Native.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                var k = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
                int msg = wParam.ToInt32();
                bool down = msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN;

                // Bastığımız tuşun bırakılmasını da oyuna gönderme.
                if (!down && _downVk != 0 && k.vkCode == _downVk) { _downVk = 0; return (IntPtr)1; }

                uint vk = _vk;
                if (down && vk != 0 && k.vkCode == vk)
                {
                    if (_downVk == vk) return (IntPtr)1; // basılı tutunca gelen tekrarlar: tek basış sayılır
                    if (CurrentModifiers() == _mods)
                    {
                        _downVk = vk;
                        _onPressed();
                        return (IntPtr)1;
                    }
                }
            }
            return Native.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        /// <summary>Şu an basılı değiştiriciler, Native.MOD_* olarak. Windows kısayolu gibi tam eşleşme aranır.</summary>
        static uint CurrentModifiers()
        {
            uint m = 0;
            if (IsDown(Native.VK_CONTROL)) m |= Native.MOD_CONTROL;
            if (IsDown(Native.VK_MENU)) m |= Native.MOD_ALT;
            if (IsDown(Native.VK_SHIFT)) m |= Native.MOD_SHIFT;
            if (IsDown(Native.VK_LWIN) || IsDown(Native.VK_RWIN)) m |= Native.MOD_WIN;
            return m;
        }

        static bool IsDown(int vk) => (Native.GetAsyncKeyState(vk) & 0x8000) != 0;

        public void Dispose()
        {
            if (_threadId != 0) Native.PostThreadMessage(_threadId, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
