using System;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Mola.UI
{
    /// <summary>Sağ alttaki simge: sol tık pencereyi açar, sağ tık Windows 11 görünümlü menüyü (TrayMenu) açar.</summary>
    public sealed class TrayIcon : IDisposable
    {
        readonly Forms.NotifyIcon _icon;

        public event Action OpenRequested;
        public event Action MenuRequested;

        public TrayIcon()
        {
            System.Drawing.Icon ico;
            var res = Application.GetResourceStream(new Uri("pack://application:,,,/Mola.ico"));
            using (var s = res.Stream) ico = new System.Drawing.Icon(s, Forms.SystemInformation.SmallIconSize);

            _icon = new Forms.NotifyIcon { Icon = ico, Text = "Mola", Visible = true };
            _icon.MouseUp += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left) OpenRequested?.Invoke();
                else if (e.Button == Forms.MouseButtons.Right) MenuRequested?.Invoke();
            };
        }

        public void SetState(bool paused, string hotkey)
        {
            var text = paused ? Loc.TrayPaused : Loc.TrayReady(hotkey);
            _icon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        public void Balloon(string title, string text)
        {
            _icon.ShowBalloonTip(4000, title, text, Forms.ToolTipIcon.None);
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
