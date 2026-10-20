# Uçtan uca test: sahte "oyun" penceresi aç, F8 gönder, oyunun gerçekten durup durmadığını ve devam ettiğini ölç.
# Çalışan Mola.exe gerektirir (kısayol F8 varsayılan). Kullanım: powershell -File test-dondur.ps1 <calisma-klasoru> [duraklatilmis-ekran.png]
param([string]$Dir = $env:TEMP, [string]$Ekran = "")
$ErrorActionPreference = 'Stop'
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class K {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out int pid);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string t);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  public static void Tap(byte vk) { keybd_event(vk, 0, 0, UIntPtr.Zero); keybd_event(vk, 0, 2, UIntPtr.Zero); }
}
"@
$sayac = Join-Path $Dir 'sayac.txt'
Remove-Item $sayac -ErrorAction SilentlyContinue
$oyun = Join-Path $Dir 'sahte-oyun.ps1'
@"
Add-Type -AssemblyName System.Windows.Forms
`$f = New-Object Windows.Forms.Form; `$f.Text = 'Sahte Oyun'; `$f.Width = 500; `$f.Height = 300; `$f.TopMost = `$true
`$n = 0; `$t = New-Object Windows.Forms.Timer; `$t.Interval = 50
`$t.Add_Tick({ `$script:n++; [IO.File]::WriteAllText('$sayac', "`$script:n|" + [DateTime]::Now.Ticks); `$f.Text = "Sahte Oyun `$script:n" })
`$t.Start(); `$f.Add_Shown({ `$f.Activate() }); [Windows.Forms.Application]::Run(`$f)
"@ | Set-Content $oyun -Encoding UTF8

$p = Start-Process powershell -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $oyun -PassThru
Start-Sleep 3
# Sahte oyunu öne getir (Windows, odağı başka uygulamaya kaptırmamak için Alt tuşu hilesini ister).
$p.Refresh(); $h = $p.MainWindowHandle
[K]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [K]::SetForegroundWindow($h) | Out-Null; [K]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 400
function Oku { $s = (Get-Content $sayac -Raw).Split('|'); [int]$s[0] }
function OnPid { $h = [K]::GetForegroundWindow(); $id = 0; [K]::GetWindowThreadProcessId($h, [ref]$id) | Out-Null; $id }

if ((OnPid) -ne $p.Id) { "TEST GEÇERSİZ: sahte oyun öne getirilemedi (odak başka pencerede)"; Stop-Process -Id $p.Id -Force; exit 2 }
"Öndeki pencere sahte oyun: evet"
$a = Oku; Start-Sleep 1; $b = Oku
"Duraklatmadan önce 1 sn'de sayaç artışı: $($b - $a)"

[K]::Tap(0x77)   # F8: duraklat
Start-Sleep -Milliseconds 400
$c = Oku; Start-Sleep 2; $d = Oku
$ov = [K]::FindWindow([NullString]::Value, 'Mola - Duraklatıldı')
"Duraklatılmışken 2 sn'de sayaç artışı (0 olmalı): $($d - $c)"
"Karartma katmanı görünüyor mu: $($ov -ne [IntPtr]::Zero -and [K]::IsWindowVisible($ov))"
if ($Ekran) { powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\ekran-goruntusu.ps1" $Ekran | Out-Null }
$global:durakSonu = $d

[K]::Tap(0x77)   # F8: devam
Start-Sleep 1
$e = Oku
"Devam ettikten sonra 1 sn'de sayaç artışı (>0 olmalı): $($e - $d)"
"Katman kapandı mı: $(-not [K]::IsWindowVisible($ov))"
Stop-Process -Id $p.Id -Force
