using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Mola.Core;

namespace Mola.UI
{
    /// <summary>
    /// Oyun öndeyken kısa bilgi göstermek için küçük bildirim (ör. "bu oyun duraklatılamıyor").
    /// Windows bildirimleri tam ekran oyunda gizlendiği için kendi bildirimimizi kullanırız.
    /// Odak almaz, fareyi engellemez, birkaç saniye sonra kendiliğinden kaybolur.
    /// </summary>
    public sealed class ToastWindow : Window
    {
        const long WS_EX_TRANSPARENT = 0x00000020;
        const long WS_EX_LAYERED = 0x00080000;

        readonly TextBlock _title, _body;
        readonly DispatcherTimer _timer;
        IntPtr _hwnd;

        public ToastWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            Left = -32000; Top = -32000;

            var font = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _title = new TextBlock { FontFamily = font, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
            _body = new TextBlock { FontFamily = font, FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) };
            var stack = new StackPanel { MaxWidth = 380 };
            stack.Children.Add(_title);
            stack.Children.Add(_body);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xEB, 0x1F, 0x1F, 0x1F)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 11, 16, 12),
                Margin = new Thickness(16),
                Child = stack,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 3, Opacity = 0.35 },
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
            _timer.Tick += (s, e) => { _timer.Stop(); Hide(); };

            SourceInitialized += (s, e) =>
            {
                _hwnd = new WindowInteropHelper(this).Handle;
                long ex = Native.GetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE).ToInt64();
                ex |= Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_LAYERED;
                Native.SetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE, new IntPtr(ex));
            };
        }

        /// <summary>Öndeki pencerenin bulunduğu ekranın üst ortasında gösterir.</summary>
        public void Pop(string title, string body)
        {
            _title.Text = title;
            _body.Text = body;
            if (!IsVisible) Show();
            UpdateLayout();

            var fg = Native.GetForegroundWindow();
            var mon = Native.MonitorFromWindow(fg, Native.MONITOR_DEFAULTTONEAREST);
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(mon, ref mi);

            var src = PresentationSource.FromVisual(this);
            double sx = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            int w = (int)Math.Ceiling(ActualWidth * sx), h = (int)Math.Ceiling(ActualHeight * sx);
            int x = mi.rcMonitor.Left + (mi.rcMonitor.Right - mi.rcMonitor.Left - w) / 2;
            int y = mi.rcMonitor.Top + (int)(40 * sx);
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, w, h, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
            _timer.Stop();
            _timer.Start();
        }
    }
}
