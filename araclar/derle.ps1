# Mola'yı derler; tek exe derleme\Mola.exe olarak çıkar.
# Kullanım: powershell -ExecutionPolicy Bypass -File araclar\derle.ps1 [-Kopyala <hedef-yol>]
#   -Kopyala: derlenen exe ayrıca bu yola kopyalanır (çalışan Mola.exe önce kapatılmalı).
param([string]$Kopyala = "")
$ErrorActionPreference = 'Stop'
$kok = Resolve-Path "$PSScriptRoot\.."
$src = Join-Path $kok 'src'
$cikti = Join-Path $kok 'derleme'

# .NET SDK: önce PATH, sonra DOTNET_ROOT, sonra resmi kurulum betiğinin varsayılan yeri.
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet -and $env:DOTNET_ROOT) { $dotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe' }
if (-not $dotnet) { $dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe" }
if (-not (Test-Path $dotnet)) { throw ".NET SDK bulunamadı. https://dotnet.microsoft.com/download adresinden kurun ya da DOTNET_ROOT ortam değişkenini SDK klasörüne ayarlayın." }

& $dotnet build "$src\Mola.csproj" -c Release -o $cikti --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Derleme başarısız" }

$exe = Join-Path $cikti 'Mola.exe'
$ekler = Get-ChildItem $cikti -File | Where-Object { $_.Name -notin @('Mola.exe', 'Mola.exe.config') }
if ($ekler) { Write-Warning ("Exe dışında dosya üretildi: " + ($ekler.Name -join ', ')) }

if ($Kopyala) {
    Copy-Item $exe $Kopyala -Force
    "Kopyalandı: $Kopyala"
}
"Hazır: $exe  ($([math]::Round((Get-Item $exe).Length / 1KB)) KB)"
