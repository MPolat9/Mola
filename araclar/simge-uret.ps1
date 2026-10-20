# Mola simgesi (Windows 11 logosu tarzı): çok az yuvarlak köşeli mavi kare, ortada keskin köşeli iki beyaz duraklat çubuğu.
# Bilerek detaysız: güven veren, her boyutta net okunan tek bir işaret.
# Kullanım: powershell -ExecutionPolicy Bypass -File simge-uret.ps1 <cikti.ico> [onizleme.png]
param([string]$Cikti = "$PSScriptRoot\..\src\Mola.ico", [string]$Onizleme = "")
Add-Type -AssemblyName System.Drawing

function Renk([string]$hex, [int]$a = 255) {
    $c = [System.Drawing.ColorTranslator]::FromHtml($hex); [System.Drawing.Color]::FromArgb($a, $c.R, $c.G, $c.B)
}

# Apple'ın "sürekli köşe" görünümüne yakın süper elips
function Squircle([float]$x, [float]$y, [float]$w, [float]$n) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pts = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
    $cx = $x + $w / 2; $cy = $y + $w / 2; $r = $w / 2
    for ($i = 0; $i -lt 720; $i++) {
        $t = $i * [Math]::PI / 360
        $ct = [Math]::Cos($t); $st = [Math]::Sin($t)
        $px = $cx + $r * [Math]::Sign($ct) * [Math]::Pow([Math]::Abs($ct), 2 / $n)
        $py = $cy + $r * [Math]::Sign($st) * [Math]::Pow([Math]::Abs($st), 2 / $n)
        $pts.Add((New-Object System.Drawing.PointF ([float]$px), ([float]$py)))
    }
    $p.AddPolygon($pts.ToArray()); return $p
}

function YuvarlakDikdortgen([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [Math]::Min($r * 2, [Math]::Min($w, $h))
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

# Not: PowerShell değişken adlarında büyük/küçük harf ayırmaz ($bh ile $bH aynıdır); adlar bilerek farklı seçildi.
function Ciz([int]$boy) {
    # Büyük çizip küçültmek kenarları her boyutta pürüzsüz yapar.
    $S = $boy * 4
    $big = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.CompositingQuality = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Windows 11 Başlat logosu gibi: kenara yakın gövde, çok az yuvarlatılmış köşeler, gölge yok.
    $pay = if ($boy -ge 48) { 0.06 } elseif ($boy -ge 24) { 0.04 } else { 0.0 }
    $gx = $S * $pay; $gw = $S - 2 * $gx
    $kose = [Math]::Max($S * 0.03, $gw * 0.06)

    $govde = YuvarlakDikdortgen $gx $gx $gw $gw $kose
    $rect = New-Object System.Drawing.RectangleF ($gx - 1), ($gx - 1), ($gw + 2), ($gw + 2)
    $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, (Renk '#3AA6FF'), (Renk '#0A5ED4'), 45
    $g.FillPath($br, $govde)

    # Duraklat işareti: iki keskin köşeli beyaz çubuk
    $cubukG = $S * 0.145; $cubukY = $S * 0.47; $ara = $S * 0.115
    if ($boy -le 24) { $cubukG = $S * 0.185; $cubukY = $S * 0.56; $ara = $S * 0.13 }
    $ust = ($S - $cubukY) / 2
    $sol1 = $S / 2 - $ara / 2 - $cubukG; $sol2 = $S / 2 + $ara / 2
    $beyaz = New-Object System.Drawing.SolidBrush (Renk '#FFFFFF')
    foreach ($lx in @($sol1, $sol2)) { $g.FillRectangle($beyaz, [float]$lx, [float]$ust, [float]$cubukG, [float]$cubukY) }
    $g.Dispose()

    $bmp = New-Object System.Drawing.Bitmap $boy, $boy, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g2 = [System.Drawing.Graphics]::FromImage($bmp)
    $g2.InterpolationMode = 'HighQualityBicubic'; $g2.PixelOffsetMode = 'HighQuality'; $g2.CompositingQuality = 'HighQuality'
    $g2.DrawImage($big, 0, 0, $boy, $boy)
    $g2.Dispose(); $big.Dispose()
    return $bmp
}

function Png($bmp) { $ms = New-Object System.IO.MemoryStream; $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); return , $ms.ToArray() }

$boyutlar = 16, 20, 24, 32, 40, 48, 64, 128, 256
$resimler = @(); $bitmapler = @{}
foreach ($b in $boyutlar) { $bm = Ciz $b; $bitmapler[$b] = $bm; $resimler += , (Png $bm) }

$tam = [System.IO.Path]::GetFullPath($Cikti)
$fs = [System.IO.File]::Create($tam)
$wr = New-Object System.IO.BinaryWriter $fs
$wr.Write([UInt16]0); $wr.Write([UInt16]1); $wr.Write([UInt16]$boyutlar.Count)
$ofs = 6 + 16 * $boyutlar.Count
for ($i = 0; $i -lt $boyutlar.Count; $i++) {
    $b = $boyutlar[$i]; $v = if ($b -ge 256) { 0 } else { $b }
    $wr.Write([byte]$v); $wr.Write([byte]$v); $wr.Write([byte]0); $wr.Write([byte]0)
    $wr.Write([UInt16]1); $wr.Write([UInt16]32)
    $wr.Write([UInt32]$resimler[$i].Length); $wr.Write([UInt32]$ofs)
    $ofs += $resimler[$i].Length
}
foreach ($r in $resimler) { $wr.Write($r) }
$wr.Close()
"Simge yazildi: $tam"

if ($Onizleme) {
    # Açık ve koyu zeminde tüm boyutlar yan yana
    $W = 760; $H = 330
    $on = New-Object System.Drawing.Bitmap $W, $H
    $g = [System.Drawing.Graphics]::FromImage($on)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush (Renk '#202020')), 0, 0, $W, $H / 2)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush (Renk '#F3F3F3')), 0, $H / 2, $W, $H / 2)
    foreach ($satir in 0, 1) {
        $px = 12; $y0 = $satir * $H / 2
        foreach ($b in 256, 128, 64, 48, 32, 24, 16) {
            $ciz = if ($b -eq 256) { 140 } else { $b }
            $g.DrawImage($bitmapler[$b], $px, [int]($y0 + ($H / 2 - $ciz) / 2), $ciz, $ciz)
            $px += $ciz + 22
        }
    }
    $on.Save([System.IO.Path]::GetFullPath($Onizleme), [System.Drawing.Imaging.ImageFormat]::Png)
    "Onizleme: $Onizleme"
}
