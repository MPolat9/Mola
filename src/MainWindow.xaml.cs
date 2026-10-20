using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mola.Core;
using Mola.UI;

namespace Mola
{
    public partial class MainWindow : Window
    {
        App AppRef => App.Current;
        bool _loading = true;
        bool _recording;
        NoticeKind? _notice;
        string _noticeTitle, _noticeBody;

        public MainWindow()
        {
            InitializeComponent();
            var s = AppRef.Settings;
            OverlaySwitch.IsChecked = s.ShowOverlay;
            DimSlider.Value = s.DimPercent;
            StartupSwitch.IsChecked = s.RunAtStartup;
            TraySwitch.IsChecked = s.KeepRunningOnClose;
            VersionText.Text = Loc.Version(typeof(App).Assembly.GetName().Version.ToString(3));
            UpdateDimUi();
            UpdateHotkeyUi();
            UpdateStatus();
            _loading = false;

            MaxHeight = SystemParameters.WorkArea.Height - 40;
            SourceInitialized += (o, e) => Theme.ApplyTitleBar(this);
            Theme.Changed += () => Theme.ApplyTitleBar(this);
            AppRef.Controller.StateChanged += () => { _notice = null; UpdateStatus(); };
            Deactivated += (o, e) => { if (_recording) StopRecording(); };
        }

        // ---------- Durum kartı ----------

        public void ShowNotice(NoticeKind kind, string title, string body)
        {
            _notice = kind; _noticeTitle = title; _noticeBody = body;
            UpdateStatus();
        }

        void UpdateStatus()
        {
            var c = AppRef.Controller;
            string key = AppRef.Settings.Hotkey.ToString();
            ResumeButton.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;

            if (c.IsPaused)
            {
                SetLook(warn: false);
                StatusTitle.Text = Loc.PausedTitle;
                StatusDesc.Text = Loc.PausedDesc(key);
                ResumeButton.Visibility = Visibility.Visible;
            }
            else if (!AppRef.HotkeyActive)
            {
                SetLook(warn: true);
                StatusTitle.Text = Loc.HotkeyBrokenTitle;
                StatusDesc.Text = Loc.HotkeyBrokenDesc(key);
            }
            else if (_notice.HasValue && _notice != NoticeKind.Info)
            {
                SetLook(warn: true);
                StatusTitle.Text = _noticeTitle;
                StatusDesc.Text = _noticeBody;
                if (_notice == NoticeKind.NeedsAdmin) AdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SetLook(warn: false);
                StatusTitle.Text = Loc.ReadyTitle(key);
                StatusDesc.Text = "";
            }
            StatusDesc.Visibility = StatusDesc.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Normalde yalnızca büyük düz yazı; uyarıda sarı simge + daha küçük başlık.</summary>
        void SetLook(bool warn)
        {
            StatusWarn.Visibility = warn ? Visibility.Visible : Visibility.Collapsed;
            StatusTitle.FontSize = warn ? 15 : 20;
            StatusTitle.SetResourceReference(System.Windows.Controls.TextBlock.FontFamilyProperty, warn ? "UiFont" : "UiDisplayFont");
        }
        void ResumeButton_Click(object sender, RoutedEventArgs e) => AppRef.Controller.Resume();
        void AdminButton_Click(object sender, RoutedEventArgs e) => AppRef.RestartElevated();

        // ---------- Kısayol ----------

        void UpdateHotkeyUi()
        {
            KeyCaps.ItemsSource = AppRef.Settings.Hotkey.Parts;
        }

        void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_recording) { StopRecording(); return; }
            _recording = true;
            AppRef.Hotkeys.Unregister();
            KeyCaps.Visibility = Visibility.Collapsed;
            RecordingText.Visibility = Visibility.Visible;
            ChangeHotkeyButton.Content = Loc.Cancel;
            HideHotkeyMessage();
            Keyboard.Focus(this);
        }

        void StopRecording()
        {
            _recording = false;
            KeyCaps.Visibility = Visibility.Visible;
            RecordingText.Visibility = Visibility.Collapsed;
            ChangeHotkeyButton.Content = Loc.Change;
            // Kayıt sırasında bırakılan eski kısayolu geri al.
            AppRef.ChangeHotkey(AppRef.Settings.Hotkey);
            UpdateHotkeyUi();
            UpdateStatus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!_recording) { base.OnPreviewKeyDown(e); return; }
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None) { StopRecording(); return; }

            var hk = Hotkey.FromKeyEvent(e);
            if (hk == null) return; // yalnızca Ctrl/Alt/Shift basılı: asıl tuşu bekle

            var h = hk.Value;
            if (h.NeedsModifier)
            {
                ShowHotkeyMessage(Loc.NeedsModifier(h.ToString()));
                return;
            }

            if (AppRef.ChangeHotkey(h))
            {
                _recording = false;
                KeyCaps.Visibility = Visibility.Visible;
                RecordingText.Visibility = Visibility.Collapsed;
                ChangeHotkeyButton.Content = Loc.Change;
                HideHotkeyMessage();
                UpdateHotkeyUi();
                UpdateStatus();
            }
            else
            {
                AppRef.Hotkeys.Unregister(); // kayıt modunda kal
                ShowHotkeyMessage(Loc.KeyTaken(h.ToString()));
            }
        }

        void ShowHotkeyMessage(string text)
        {
            HotkeyMessage.Text = text;
            HotkeyMessageBox.Visibility = Visibility.Visible;
        }

        void HideHotkeyMessage() => HotkeyMessageBox.Visibility = Visibility.Collapsed;

        // ---------- Ekran ----------

        void OverlaySwitch_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDimUi();
            if (_loading) return;
            AppRef.Settings.ShowOverlay = OverlaySwitch.IsChecked == true;
            AppRef.Settings.Save();
        }

        void DimSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDimUi();
            if (_loading) return;
            AppRef.Settings.DimPercent = (int)Math.Round(DimSlider.Value);
            AppRef.Settings.Save();
        }

        void UpdateDimUi()
        {
            if (DimValue == null) return;
            DimValue.Text = Loc.Percent((int)Math.Round(DimSlider.Value));
            DimCard.IsEnabled = OverlaySwitch.IsChecked == true;
            DimCard.Opacity = DimCard.IsEnabled ? 1 : 0.5;
        }

        void PreviewButton_Click(object sender, RoutedEventArgs e) => AppRef.Controller.Preview();

        // ---------- Genel ----------

        void StartupSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            AppRef.Settings.RunAtStartup = StartupSwitch.IsChecked == true;
            AppRef.Settings.Save();
            Mola.Core.Startup.Apply(AppRef.Settings.RunAtStartup);
        }

        void TraySwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            AppRef.Settings.KeepRunningOnClose = TraySwitch.IsChecked == true;
            AppRef.Settings.Save();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (AppRef.Exiting) { base.OnClosing(e); return; }
            if (_recording) StopRecording();
            if (AppRef.Settings.KeepRunningOnClose)
            {
                e.Cancel = true;
                Hide();
                if (!AppRef.Settings.TrayHintShown)
                {
                    AppRef.Settings.TrayHintShown = true;
                    AppRef.Settings.Save();
                    AppRef.Tray.Balloon(Loc.BackgroundTitle, Loc.BackgroundBody);
                }
                return;
            }
            AppRef.ExitApp();
        }
    }
}
