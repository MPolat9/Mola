using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using Mola.Core;
using Mola.UI;

namespace Mola
{
    /// <summary>
    /// Açılış: tek kopya kontrolü, önceki oturumdan donuk kalan oyunu kurtarma, kısayolu kaydetme.
    /// Mola ne şekilde kapanırsa kapansın (çıkış, Windows kapanışı, beklenmedik hata) önce oyunu çözer.
    /// </summary>
    public partial class App : Application
    {
        const string MutexName = @"Local\Mola.TekKopya";
        const string ShowEventName = @"Local\Mola.PencereyiGoster";

        Mutex _mutex;
        EventWaitHandle _showEvent;
        RegisteredWaitHandle _showWait;

        public Settings Settings { get; private set; }
        public PauseController Controller { get; private set; }
        public HotkeyManager Hotkeys { get; private set; }
        public TrayIcon Tray { get; private set; }
        public TrayMenu TrayMenu { get; private set; }
        public ToastWindow Toast { get; private set; }
        public bool HotkeyActive { get; private set; }
        public bool Exiting { get; private set; }
        public static new App Current => (App)Application.Current;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Loc.Init(e.Args);
            bool background = e.Args.Contains("--arka-plan");
            bool restarted = e.Args.Contains("--yeniden");

            _mutex = new Mutex(false, MutexName);
            bool owned;
            try { owned = _mutex.WaitOne(restarted ? 8000 : 0); }
            catch (AbandonedMutexException) { owned = true; }
            if (!owned)
            {
                // Zaten açık: var olan pencereyi öne getir, ikinci kopyayı kapat.
                try { using (var ev = EventWaitHandle.OpenExisting(ShowEventName)) ev.Set(); } catch { }
                Shutdown();
                return;
            }

            Log.Write("Mola açıldı" + (PauseController.IsElevated ? " (yönetici)" : ""));
            ProcessFreezer.RecoverLeftover();

            DispatcherUnhandledException += (s, ev) =>
            {
                Log.Write("Beklenmedik hata: " + ev.Exception);
                SafeResume();
                ev.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => { Log.Write("Ölümcül hata: " + ev.ExceptionObject); SafeResume(); };
            SessionEnding += (s, ev) => { Exiting = true; SafeResume(); };

            Theme.Init();
            Settings = Settings.Load();
            Mola.Core.Startup.Sync(Settings.RunAtStartup);

            Toast = new ToastWindow();
            Controller = new PauseController(() => Settings);
            Controller.Notice += OnNotice;

            Hotkeys = new HotkeyManager();
            Hotkeys.Pressed += () => Controller.Toggle();
            HotkeyActive = Hotkeys.Register(Settings.Hotkey);

            Tray = new TrayIcon();
            Tray.OpenRequested += ShowMainWindow;
            TrayMenu = new TrayMenu();
            TrayMenu.OpenRequested += ShowMainWindow;
            TrayMenu.ResumeRequested += () => Controller.Resume();
            TrayMenu.ExitRequested += ExitApp;
            Tray.MenuRequested += () => TrayMenu.ShowAtCursor(Controller.IsPaused);
            Controller.StateChanged += () => Tray.SetState(Controller.IsPaused, Settings.Hotkey.ToString());
            Tray.SetState(false, Settings.Hotkey.ToString());

            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            _showWait = ThreadPool.RegisterWaitForSingleObject(_showEvent, (o, t) => Dispatcher.BeginInvoke(new Action(ShowMainWindow)), null, -1, false);

            MainWindow = new MainWindow();
            if (!background) MainWindow.Show();
        }

        /// <summary>Kısayol değişince yeniden kaydeder. Başarısızsa eskisine döner ve false verir.</summary>
        public bool ChangeHotkey(Hotkey hk)
        {
            var old = Settings.Hotkey;
            if (Hotkeys.Register(hk))
            {
                Settings.Hotkey = hk;
                Settings.Save();
                HotkeyActive = true;
                Tray.SetState(Controller.IsPaused, hk.ToString());
                return true;
            }
            HotkeyActive = Hotkeys.Register(old);
            return false;
        }

        void OnNotice(NoticeKind kind, string title, string body)
        {
            var mw = MainWindow as MainWindow;
            mw?.ShowNotice(kind, title, body);
            if (mw == null || !mw.IsActive) Toast.Pop(title, body);
        }

        public void ShowMainWindow()
        {
            var w = MainWindow;
            if (w == null) return;
            if (!w.IsVisible) w.Show();
            if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
            w.Activate();
        }

        /// <summary>Yönetici izniyle yeniden açar (yönetici olarak çalışan oyunları duraklatabilmek için).</summary>
        public void RestartElevated()
        {
            try
            {
                SafeResume();
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo(exe, "--yeniden") { Verb = "runas", UseShellExecute = true });
                ExitApp();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Kullanıcı izin penceresinde "Hayır" dedi: hiçbir şey değişmez.
            }
        }

        void SafeResume()
        {
            try { Controller?.Resume(); } catch (Exception ex) { Log.Write("Çözme hatası: " + ex.Message); }
        }

        public void ExitApp()
        {
            if (Exiting) return;
            Exiting = true;
            SafeResume();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SafeResume();
            try { Controller?.Dispose(); } catch { }
            try { Hotkeys?.Dispose(); } catch { }
            try { Tray?.Dispose(); } catch { }
            _showWait?.Unregister(null);
            _showEvent?.Dispose();
            try { _mutex?.ReleaseMutex(); } catch { }
            Log.Write("Mola kapandı");
            base.OnExit(e);
        }
    }
}
