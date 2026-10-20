# Mola: geliştirici notları

Kısayola basınca öndeki oyunu **tamamen** durdurur (görüntü, oyun mantığı, ses iş parçacıkları aynı anda),
tekrar basınca kaldığı yerden sürdürür. Amaç: sinematik ve oyun içi altyazıları rahat okuyabilmek.

- Çıktı: tek dosya `Mola.exe` (~125 KB, .NET Framework 4.8; Windows 10/11'de hazır gelir, kurulum yok).
- Ayarlar ve günlük: `%APPDATA%\Mola\` (`ayarlar.ini`, `mola.log`, duraklatma sırasında `donuk-islem.txt`).
- Program klasörüne hiçbir şey yazmaz.

## Nasıl çalışır

1. Kısayol Windows'a sistem genelinde kaydedilir (`RegisterHotKey`). Tuş oyuna ulaşmaz, oyunda başka işi tetiklemez.
2. Basılınca o an öndeki pencerenin işlemi bulunur (`Core/TargetFinder.cs`), Windows parçaları (explorer, dwm vb.) korunur.
3. İşlem Windows'un kendi dondurma çağrısıyla (`NtSuspendProcess`, Process Explorer'daki "Suspend" ile aynı) dondurulur.
   Cheat Engine'in hız 0'ından farkı: saat sahte değildir, ses iş parçacıkları da birlikte durur; bu yüzden devam edince
   ses ve görüntü birbirine göre kaymaz. Oyunun dosyasına/belleğine dokunulmaz, oyuna kod sokulmaz.
4. Oyunun ekranı üstüne odak almayan, tıklamaları tutan şeffaf bir katman çıkar: hafif karartma + ortada düz "Duraklatıldı".
   Kullanıcı Alt+Tab yaparsa katman çekilir, oyuna dönünce geri gelir.
5. Güvenlik ağı: donuk işlem diske yazılır. Mola nasıl kapanırsa kapansın (çıkış, Windows kapanışı, hata) önce oyunu çözer;
   zorla öldürülse bile bir sonraki açılışta donuk kalan oyun otomatik çözülür (işlem numarası + oluşturulma zamanı eşleşmeli).

## Dosyalar

| Dosya | Görev |
|---|---|
| `src/App.xaml(.cs)` | Açılış, tek kopya, kurtarma, kapanışta oyunu çözme |
| `src/MainWindow.xaml(.cs)` | Ayar penceresi (durum, kısayol, karartma, genel) |
| `src/Core/ProcessFreezer.cs` | Dondur / çöz / önceki oturumdan kurtarma |
| `src/Core/PauseController.cs` | Kısayol → dondur + katman; oyun kapanırsa / Alt+Tab izleme |
| `src/Core/HotkeyManager.cs`, `Hotkey.cs` | Kısayol kaydı ve tuş adları |
| `src/Core/TargetFinder.cs` | Öndeki oyunu bulma, korunan Windows işlemleri listesi |
| `src/Core/Settings.cs`, `Startup.cs`, `AppPaths.cs`, `Native.cs` | Ayarlar, Windows ile başlat, günlük, Windows API |
| `src/UI/OverlayWindow.cs` | "Duraklatıldı" katmanı |
| `src/UI/ToastWindow.cs` | Oyun öndeyken kısa uyarı (tam ekranda Windows bildirimleri gizlendiği için) |
| `src/UI/TrayIcon.cs`, `TrayMenu.cs` | Sağ alttaki simge ve Windows 11 görünümlü sağ tık menüsü |
| `src/UI/Loc.cs` | Ekrandaki tüm yazılar, Türkçe + İngilizce. Dil Windows görüntüleme diline göre seçilir (tr → Türkçe, diğerleri → İngilizce). Deneme için `Mola.exe --dil=en` / `--dil=tr` |
| `src/UI/Theme.cs`, `Styles.xaml` | Açık/koyu tema renkleri, Windows 11 görünümlü kontroller |
| `araclar/derle.ps1` | Derler, exe'yi `derleme\Mola.exe` olarak çıkarır (`-Kopyala <yol>` ile ayrıca başka yere kopyalar) |
| `araclar/simge-uret.ps1` | Logoyu (Mola.ico) koddan üretir: Windows 11 logosu tarzı mavi kare + keskin iki beyaz çubuk |
| `araclar/test-dondur.ps1` | Uçtan uca test: sahte oyun aç, F8 gönder, gerçekten durup devam ettiğini ölç |
| `araclar/ekran-goruntusu.ps1` | Pencere / ekran görüntüsü |

## Derleme

.NET SDK (6 veya üstü) gerekir. Derleme betiği SDK'yı önce PATH'te, sonra `DOTNET_ROOT` ortam değişkeninde,
sonra resmi kurulum betiğinin varsayılan yerinde (`%USERPROFILE%\.dotnet`) arar.

```powershell
powershell -ExecutionPolicy Bypass -File araclar\derle.ps1
```

Çalışan Mola.exe'nin üzerine kopyalanacaksa önce Mola kapatılmalı.

Derleme tekrarlanabilir: aynı kayıttan her bilgisayarda birebir aynı exe çıkar. Bunu iki ayar sağlar, bozulmamalı:
`.gitattributes` tüm metin dosyalarını LF satır sonuyla açar, `Mola.csproj` sürüm bilgisine git kayıt numarası eklemez
(`IncludeSourceRevisionInInformationalVersion`). Yayımlanan exe'nin SHA-256 değeri sürüm notlarına yazılır.

## Windows 10 uyumluluğu

Kullanılan her şey Windows 10'da da var: .NET Framework 4.8 (Windows 10 Mayıs 2019 güncellemesinden beri hazır gelir),
`NtSuspendProcess`, `RegisterHotKey`, ekran başına DPI ayarı. Windows 10'da görünüm farkları:

- "Segoe UI Variable" yazı tipi yalnız Windows 11'de var; Windows 10'da klasik "Segoe UI" kullanılır.
- Pencere köşeleri Windows 10'da yuvarlak değil düz olur (bunu Windows çizer).
- Koyu başlık çubuğu Windows 10 sürüm 2004 ve sonrasında çalışır; daha eskilerde başlık çubuğu açık renk kalır.

32 bit Windows desteklenmez: `Native.cs` içindeki `GetWindowLongPtrW` / `SetWindowLongPtrW` 32 bit user32.dll'de yoktur.

## Doğrulananlar (14.10.2026, Windows 11)

- Sahte oyun testi: duraklatınca 2 sn'de 0 adım, devamda normal hız; katman açılıp kapanıyor.
- Karartma ölçümü: %20 ayarında beyaz 255 → 204 (tam %20).
- Tek kopya: ikinci açılış var olan pencereyi öne getiriyor.
- Arayüz: koyu tema ekran görüntüsüyle kontrol edildi; İngilizce (`--dil=en`) ve otomatik Türkçe (sistem tr-TR) ana pencere görüntüyle kontrol edildi.

## Gerçek oyun testi (14.10.2026, Call of Duty: Modern Warfare Remastered, tek oyunculu)

- Oyundaki tüm duraklat/devam işlemleri başarılı.
- Bazen devamda ses kısa kesilip geri geliyor; ilk sinematiğin başında oldu, ilerleyince olmadı. Mola tarafında hata yok.
  Büyük ihtimalle önceden kaydedilmiş video sahnesinin oynatıcısı, duran süreyi fark edip sesini yeniden hizalıyor.
  Kökten çözümü oyunun içine kod sokmayı gerektirir (oyuna dokunmama ilkesine aykırı, riskli). Bilinçli olarak yapılmadı.

20.10.2026, Crysis Remastered: Çerçevesiz modda duraklat/devam çalışıyor, katman görünüyor (`docs/images/crysis-remastered.jpg`).
Tam ekran modunda çalışmıyor (aşağıda).

## Bilinen sınırlar / dikkat

- Devam edince oyun, durduğu süreyi "geçmiş zaman" olarak görebilir; çoğu oyun motoru bunu tek kare sayıp yutar.
  Bir oyunda devam anında sıçrama/kayma görülürse not alınmalı.
- **Özel tam ekranda (exclusive fullscreen) çalışmıyor; oyun Çerçevesiz ya da Pencere modunda olmalı.**
  20.10.2026 Crysis Remastered testi: Tam ekranda F8 "çalışmıyor". Günlükte her basışta dondurma ve çözme başarılı
  (yarım saniye arayla art arda basılmış); yani sorun dondurmada değil, ekranda görünen sonuçta. Tam olarak ne görüldüğü
  (görüntü donmadı mı, ekran mı karardı, oyun mu küçüldü) henüz bilinmiyor. Katmanı özel tam ekranın üstüne çizmenin
  tek yolu oyunun içine kod sokmak (Steam/Discord katmanı gibi); bu, oyuna dokunmama ilkesine aykırı ve hile
  korumalarını tetikler. Olası iyileştirme: özel tam ekran algılanınca (`SHQueryUserNotificationState` =
  `QUNS_RUNNING_D3D_FULL_SCREEN`) katmanı hiç göstermemek. Önce tam ekranda ne görüldüğü belirlenmeli.
- Yönetici olarak çalışan oyunlar için Mola'nın da yönetici olması gerekir (durum kartında düğme çıkar).
- Çevrim içi / hile korumalı oyunlarda kullanılmamalı. Koruma erişimi reddederse Mola zorlamaz, uyarı verir.
- Duraklatılmışken Mola Görev Yöneticisi'nden zorla kapatılırsa oyun, Mola tekrar açılana kadar donuk kalır
  (açılınca otomatik çözülür).

## Sonraki adım

Oyunu hızlandırma. Cheat Engine'deki sorun (ses hızlanmıyor, sesler karışıyor) bilinçli olarak ele alınacak.
