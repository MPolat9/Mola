using System;
using System.Globalization;

namespace Mola.UI
{
    /// <summary>
    /// Ekranda görünen tüm yazılar (Türkçe / İngilizce). Dil, Windows'un görüntüleme diline göre
    /// açılışta bir kez seçilir: Türkçe ise Türkçe, değilse İngilizce.
    /// Yeni yazı eklerken buraya iki dilde birlikte eklenir.
    /// </summary>
    public static class Loc
    {
        public static bool Tr { get; private set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr";

        /// <summary>Deneme için dili zorlar: "--dil=en" veya "--dil=tr" (normal kullanımda gerekmez).</summary>
        public static void Init(string[] args)
        {
            foreach (var a in args)
            {
                if (a.Equals("--dil=en", StringComparison.OrdinalIgnoreCase)) Tr = false;
                else if (a.Equals("--dil=tr", StringComparison.OrdinalIgnoreCase)) Tr = true;
            }
        }

        static string S(string tr, string en) => Tr ? tr : en;

        // --- Ana pencere ---
        public static string Subtitle => S("Oyunu tek tuşla gerçekten duraklatır, kaldığı yerden sürdürür.", "Truly pause any game with one key and resume right where you left off.");
        public static string Resume => S("Devam ettir", "Resume");
        public static string RunAsAdmin => S("Yönetici olarak aç", "Run as administrator");

        public static string SectionShortcut => S("Kısayol", "Shortcut");
        public static string PauseResume => S("Duraklat / devam ettir", "Pause / resume");
        public static string PauseResumeDesc => S("Oyundayken bu tuşa bas. Tekrar basınca oyun kaldığı yerden sürer.", "Press this key in game. Press it again to continue where you left off.");
        public static string PressAKey => S("Bir tuşa bas...", "Press a key...");
        public static string Change => S("Değiştir", "Change");
        public static string Cancel => S("Vazgeç", "Cancel");

        public static string SectionScreen => S("Ekran", "Screen");
        public static string OverlayTitle => S("Ekranı hafifçe karart, ortada \"Duraklatıldı\" yaz", "Dim the screen slightly and show \"Paused\"");
        public static string OverlayDesc => S("Kapalı olsa da oyun durur; yalnızca ekranda bir şey görünmez.", "The game still pauses when this is off; nothing is shown on screen.");
        public static string DimAmount => S("Karartma miktarı", "Dim amount");
        public static string Preview => S("Önizle", "Preview");

        public static string SectionGeneral => S("Genel", "General");
        public static string StartupTitle => S("Windows açılınca arka planda başlat", "Start in the background with Windows");
        public static string StartupDesc => S("Her seferinde açmana gerek kalmaz, kısayol hep hazır olur.", "No need to open it every time; the shortcut is always ready.");
        public static string TrayTitle => S("Pencereyi kapatınca arka planda çalışmaya devam et", "Keep running in the background when the window is closed");
        public static string TrayDesc => S("Mola sağ alttaki simgelerde bekler. Tamamen kapatmak için simgeye sağ tıklayıp Çıkış'ı seç.", "Mola waits in the system tray. To quit completely, right-click its icon and choose Exit.");

        public static string Footer => S("Mola oyunun dosyalarına ve hafızasına dokunmaz; Windows'un kendi dondurma özelliğini kullanır. Çevrim içi ve hile korumalı oyunlarda kullanma.",
                                         "Mola never touches game files or memory; it uses Windows' own suspend feature. Do not use it in online or anti-cheat protected games.");
        public static string Version(string v) => S("Sürüm " + v, "Version " + v);
        public static string Percent(int v) => S("%" + v, v + "%");

        // --- Durum kartı ---
        public static string ReadyTitle(string key) => S("Oyunu duraklatmak için " + key + " tuşuna bas", "Press " + key + " to pause your game");
        public static string PausedTitle => S("Oyun duraklatıldı", "Game paused");
        public static string PausedDesc(string key) => S("Devam etmek için " + key + " tuşuna bas.", "Press " + key + " to resume.");
        public static string HotkeyBrokenTitle => S("Kısayol çalışmıyor", "Shortcut is not working");
        public static string HotkeyBrokenDesc(string key) => S(key + " tuşunu şu an başka bir program kullanıyor. Aşağıdan başka bir tuş seç.", key + " is being used by another program right now. Choose a different key below.");

        // --- Kısayol seçimi ---
        public static string NeedsModifier(string key) => S("\"" + key + "\" tek başına yazı yazarken de kullanılır. Yanına Ctrl, Alt veya Shift ekle ya da F1-F12, Pause gibi bir tuş seç.",
                                                            "\"" + key + "\" is also used for typing. Add Ctrl, Alt or Shift, or pick a key like F1-F12 or Pause.");
        public static string KeyTaken(string key) => S("\"" + key + "\" tuşunu başka bir program kullanıyor. Başka bir tuş seç.", "\"" + key + "\" is used by another program. Choose a different key.");

        // --- Bildirimler ---
        public static string NoGameTitle => S("Önce oyuna geç", "Switch to your game first");
        public static string NoGameBody => S("Kısayol, o an önde açık olan oyunu duraklatır. Oyunun penceresine geçip tekrar dene.", "The shortcut pauses the game in front. Switch to the game window and try again.");
        public static string AdminTitle => S("Bu oyun yönetici olarak çalışıyor", "This game runs as administrator");
        public static string AdminBody => S("Duraklatabilmek için Mola'nın da yönetici olarak açılması gerekiyor.", "To pause it, Mola must also run as administrator.");
        public static string ProtectedTitle => S("Bu oyun duraklatılamıyor", "This game can't be paused");
        public static string ProtectedBody => S("Oyun kendini dış programlara karşı koruyor (genelde çevrim içi oyunlardaki hile koruması). Mola bu oyunda çalışmaz ve zorlamaz.",
                                                "The game protects itself from outside programs (usually anti-cheat in online games). Mola won't work here and won't force it.");
        public static string FailedTitle => S("Duraklatılamadı", "Couldn't pause");
        public static string FailedBody => S("Oyun bu sırada kapanmış ya da yanıt vermiyor olabilir. Tekrar dene.", "The game may have closed or stopped responding. Try again.");
        public static string BackgroundTitle => S("Mola arka planda çalışıyor", "Mola is running in the background");
        public static string BackgroundBody => S("Kısayol hazır. Pencereyi açmak için sağ alttaki Mola simgesine tıkla.", "The shortcut is ready. Click the Mola icon in the system tray to open the window.");

        // --- Ekran katmanı, simge, menü ---
        public static string Paused => S("Duraklatıldı", "Paused");
        public static string TrayPaused => S("Mola - oyun duraklatıldı", "Mola - game paused");
        public static string TrayReady(string key) => S("Mola - hazır (" + key + ")", "Mola - ready (" + key + ")");
        public static string MenuOpen => S("Mola'yı aç", "Open Mola");
        public static string MenuResume => S("Oyunu devam ettir", "Resume game");
        public static string MenuExit => S("Çıkış", "Exit");

        // --- Tuş adları (dile göre değişenler) ---
        public static string KeySpace => S("Boşluk", "Space");
        public static string KeyBackspace => S("Geri", "Backspace");
        public static string KeyLeft => S("Sol ok", "Left arrow");
        public static string KeyUp => S("Yukarı ok", "Up arrow");
        public static string KeyRight => S("Sağ ok", "Right arrow");
        public static string KeyDown => S("Aşağı ok", "Down arrow");
        public static string KeyPlayPause => S("Oynat/Duraklat", "Play/Pause");
        public static string KeyMediaStop => S("Medya Durdur", "Media Stop");
        public static string KeyNextTrack => S("Sonraki Parça", "Next Track");
        public static string KeyPrevTrack => S("Önceki Parça", "Previous Track");
        public static string KeyMute => S("Sesi Kapat", "Mute");
        public static string KeyGeneric(uint vk) => S("Tuş " + vk, "Key " + vk);
    }
}
