using System;
using System.Security.Principal;
using System.Windows.Threading;
using Mola.UI;

namespace Mola.Core
{
    public enum NoticeKind { Info, Warning, NeedsAdmin }

    /// <summary>
    /// Kısayola basılınca ne olacağını yönetir: öndeki oyunu dondur + katmanı göster,
    /// tekrar basılınca katmanı kaldır + oyunu çöz. Oyun kapanırsa veya kullanıcı
    /// başka pencereye geçerse katmanı kendiliğinden yönetir.
    /// </summary>
    public sealed class PauseController : IDisposable
    {
        readonly ProcessFreezer _freezer = new ProcessFreezer();
        readonly OverlayWindow _overlay = new OverlayWindow();
        readonly DispatcherTimer _watch;
        readonly Func<Settings> _settings;
        IntPtr _gameWindow;

        public event Action StateChanged;
        public event Action<NoticeKind, string, string> Notice;

        public bool IsPaused => _freezer.IsFrozen;
        public string PausedName { get; private set; }

        public static bool IsElevated
        {
            get
            {
                using (var id = WindowsIdentity.GetCurrent())
                    return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public PauseController(Func<Settings> settings)
        {
            _settings = settings;
            _watch = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(200) };
            _watch.Tick += (s, e) => Watch();
        }

        public void Toggle()
        {
            if (IsPaused) Resume();
            else PauseForeground();
        }

        void PauseForeground()
        {
            var t = TargetFinder.FromForeground();
            if (t == null || TargetFinder.IsSelf(t) || (t.ExeName != null && TargetFinder.IsProtected(t)))
            {
                Notice?.Invoke(NoticeKind.Info, Loc.NoGameTitle, Loc.NoGameBody);
                return;
            }

            var result = t.ExeName == null ? FreezeResult.AccessDenied : _freezer.Freeze(t.Pid, t.ExeName);
            switch (result)
            {
                case FreezeResult.Ok:
                    _gameWindow = t.Window;
                    PausedName = t.DisplayName;
                    ShowOverlay();
                    _watch.Start();
                    StateChanged?.Invoke();
                    break;

                case FreezeResult.AccessDenied:
                    if (!IsElevated)
                        Notice?.Invoke(NoticeKind.NeedsAdmin, Loc.AdminTitle, Loc.AdminBody);
                    else
                        Notice?.Invoke(NoticeKind.Warning, Loc.ProtectedTitle, Loc.ProtectedBody);
                    break;

                default:
                    Notice?.Invoke(NoticeKind.Warning, Loc.FailedTitle, Loc.FailedBody);
                    break;
            }
        }

        public void Resume()
        {
            if (!IsPaused) return;
            _watch.Stop();
            _overlay.HideNow();
            _freezer.Thaw();
            _gameWindow = IntPtr.Zero;
            PausedName = null;
            StateChanged?.Invoke();
        }

        void ShowOverlay()
        {
            var s = _settings();
            if (!s.ShowOverlay) return;
            _overlay.ShowOver(_gameWindow, s.DimPercent);
        }

        /// <summary>Ayarlardaki görünümü denemek için katmanı birkaç saniye gösterir (oyun dondurulmaz).</summary>
        public void Preview()
        {
            if (IsPaused) return;
            var s = _settings();
            _overlay.ShowOver(IntPtr.Zero, s.DimPercent);
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            t.Tick += (o, e) => { t.Stop(); if (!IsPaused) _overlay.HideNow(); };
            t.Start();
        }

        void Watch()
        {
            if (_freezer.CheckExited())
            {
                _watch.Stop();
                _overlay.HideNow();
                PausedName = null;
                StateChanged?.Invoke();
                return;
            }

            if (!_settings().ShowOverlay) { _overlay.HideNow(); return; }

            // Kullanıcı duraklatmışken başka pencereye geçerse (Alt+Tab) katman çekilir,
            // oyuna dönünce geri gelir. Böylece masaüstünü rahatça kullanabilir.
            IntPtr fg = Native.GetForegroundWindow();
            bool gameInFront = false;
            if (fg != IntPtr.Zero)
            {
                Native.GetWindowThreadProcessId(fg, out int pid);
                gameInFront = pid == _freezer.Pid || fg == _overlay.Handle;
            }
            if (gameInFront && !_overlay.IsVisible) ShowOverlay();
            else if (!gameInFront && _overlay.IsVisible) _overlay.HideNow();
        }

        public void Dispose()
        {
            Resume();
            _overlay.Close();
        }
    }
}
