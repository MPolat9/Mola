# Verilen başlıklı pencerenin (yoksa tüm ana ekranın) görüntüsünü PNG olarak kaydeder.
param([string]$Cikti, [string]$Baslik = "")
Add-Type -AssemblyName System.Drawing, System.Windows.Forms
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class W {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string t);
  [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int s);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public struct RECT { public int L, T, R, B; }
}
"@
[W]::SetProcessDPIAware() | Out-Null
if ($Baslik) {
    $h = [W]::FindWindow([NullString]::Value, $Baslik)
    if ($h -eq [IntPtr]::Zero) { throw "Pencere yok: $Baslik" }
    $r = New-Object W+RECT
    [W]::DwmGetWindowAttribute($h, 9, [ref]$r, 16) | Out-Null   # DWMWA_EXTENDED_FRAME_BOUNDS
    $x = $r.L; $y = $r.T; $w = $r.R - $r.L; $hh = $r.B - $r.T
} else {
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $x = $b.X; $y = $b.Y; $w = $b.Width; $hh = $b.Height
}
$bmp = New-Object System.Drawing.Bitmap $w, $hh
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
$bmp.Save($Cikti, [System.Drawing.Imaging.ImageFormat]::Png)
"$Cikti ($w x $hh)"
