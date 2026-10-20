using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Mola.Core;

namespace Mola.UI
{
    /// <summary>
    /// Oyunun bulunduğu ekranın üstüne çıkan şeffaf katman: ekranı hafifçe karartır,
    /// ortada düz "Duraklatıldı" yazısı gösterir. Odak almaz (oyun öndeki pencere
    /// olarak kalır), fare tıklamalarını kendisi tutar ki donuk oyuna gitmesin.
    /// </summary>
    public sealed class OverlayWindow : Window
    {
        readonly Rectangle _dim;
        IntPtr _hwnd;

        public OverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Focusable = false;
            Cursor = Cursors.None;
            Title = "Mola - Duraklatıldı";
            Left = -32000; Top = -32000; Width = 1; Height = 1;

            _dim = new Rectangle { Fill = Brushes.Black, Opacity = 0.25 };

            // Yalnızca düz yazı. Hafif gölge, açık renkli sahnelerde de okunmasını sağlar.
            var text = new TextBlock
            {
                Text = Loc.Paused,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7, Color = Colors.Black },
            };

            var grid = new Grid();
            grid.Children.Add(_dim);
            grid.Children.Add(text);
            Content = grid;
            SourceInitialized += (s, e) =>
            {
                _hwnd = new WindowInteropHelper(this).Handle;
                long ex = Native.GetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE).ToInt64();
                ex |= Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST;
                Native.SetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE, new IntPtr(ex));
            };
        }

        public IntPtr Handle => _hwnd;

        /// <summary>Verilen pencerenin bulunduğu ekranı kaplayarak gösterir.</summary>
        public void ShowOver(IntPtr gameWindow, int dimPercent)
        {
            _dim.Opacity = Math.Max(0, Math.Min(60, dimPercent)) / 100.0;

            IntPtr mon;
            if (gameWindow != IntPtr.Zero) mon = Native.MonitorFromWindow(gameWindow, Native.MONITOR_DEFAULTTONEAREST);
            else { Native.GetCursorPos(out var pt); mon = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST); }
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(mon, ref mi);
            var r = mi.rcMonitor;

            if (!IsVisible) Show();
            // Konum ve boyut fiziksel piksel olarak verilir: ölçekleme (%125, %150) ne olursa olsun ekranı tam kaplar.
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top,
                Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, fade);
        }

        public void HideNow()
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 0;
            if (IsVisible) Hide();
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) { e.Handled = true; }
    }
}
