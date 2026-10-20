using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mola.Core;

namespace Mola.UI
{
    /// <summary>
    /// Sağ alttaki simgeye sağ tıklayınca açılan Windows 11 görünümlü menü.
    /// Windows'un eski tip menüsü yerine kendi çizimimiz: yuvarlak köşe, hafif gölge, açık/koyu tema.
    /// </summary>
    public sealed class TrayMenu : Window
    {
        readonly Button _resume;
        IntPtr _hwnd;

        public event Action OpenRequested;
        public event Action ResumeRequested;
        public event Action ExitRequested;

        public TrayMenu()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            Left = -32000; Top = -32000;
            UseLayoutRounding = true;
            FontFamily = (FontFamily)Application.Current.Resources["UiFont"];

            var open = Item(Loc.MenuOpen, () => OpenRequested?.Invoke());
            open.FontWeight = FontWeights.SemiBold;
            _resume = Item(Loc.MenuResume, () => ResumeRequested?.Invoke());
            var exit = Item(Loc.MenuExit, () => ExitRequested?.Invoke());

            var sep = new Border { Height = 1, Margin = new Thickness(0, 4, 0, 4) };
            sep.SetResourceReference(Border.BackgroundProperty, "MenuStroke");

            var stack = new StackPanel { MinWidth = 200 };
            stack.Children.Add(open);
            stack.Children.Add(_resume);
            stack.Children.Add(sep);
            stack.Children.Add(exit);

            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Margin = new Thickness(14),
                Child = stack,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Direction = 270, Opacity = 0.28, Color = Colors.Black },
            };
            card.SetResourceReference(Border.BackgroundProperty, "MenuBg");
            card.SetResourceReference(Border.BorderBrushProperty, "MenuStroke");
            Content = card;

            SourceInitialized += (s, e) =>
            {
                _hwnd = new WindowInteropHelper(this).Handle;
                long ex = Native.GetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE).ToInt64();
                Native.SetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE, new IntPtr(ex | Native.WS_EX_TOOLWINDOW));
            };
            Deactivated += (s, e) => Hide();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Hide(); };
        }

        Button Item(string text, Action onClick)
        {
            var b = new Button { Content = text, Style = (Style)Application.Current.Resources["MenuItemBtn"] };
            b.Click += (s, e) => { Hide(); onClick(); };
            return b;
        }

        /// <summary>Fare imlecinin yanında açar; ekran kenarına taşmaz.</summary>
        public void ShowAtCursor(bool paused)
        {
            _resume.Visibility = paused ? Visibility.Visible : Visibility.Collapsed;
            if (!IsVisible) Show();
            UpdateLayout();

            Native.GetCursorPos(out var pt);
            var mon = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(mon, ref mi);
            var work = mi.rcWork;

            var src = PresentationSource.FromVisual(this);
            double sc = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            int w = (int)Math.Ceiling(ActualWidth * sc), h = (int)Math.Ceiling(ActualHeight * sc);
            int margin = (int)(14 * sc); // gölge payı: görünen kart imlecin hemen yanında dursun

            int x = pt.X - margin;
            int y = pt.Y - h + margin;                          // görev çubuğu altta: menü imlecin üstünde açılır
            if (y < work.Top - margin) y = pt.Y - margin;        // üstte yer yoksa altında
            if (x + w - margin > work.Right) x = work.Right - w + margin;
            if (x < work.Left - margin) x = work.Left - margin;
            if (y + h - margin > work.Bottom) y = work.Bottom - h + margin;

            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, w, h, Native.SWP_SHOWWINDOW);
            Activate();
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
        }
    }
}
