$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\SWWerkplaats.Configurator"
$solidWorksInterop = "C:\Program Files\Dassault Systemes\SOLIDWORKS 3DEXPERIENCE R2026x\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll"
$outDir = Join-Path $root "bin"
$exe = Join-Path $outDir "SWWerkplaats.Configurator.exe"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    throw "csc.exe niet gevonden. Installeer Visual Studio 2022 of Visual Studio Build Tools."
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sources = Get-ChildItem -Path $project -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    Select-Object -ExpandProperty FullName

& $csc `
    /nologo `
    /target:winexe `
    /out:$exe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Data.dll `
    /reference:System.Drawing.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /reference:Microsoft.CSharp.dll `
    /reference:System.Web.Extensions.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Xml.dll `
    /reference:$solidWorksInterop `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build mislukt. Controleer de compilerfouten hierboven."
}

Copy-Item -LiteralPath $solidWorksInterop -Destination (Join-Path $outDir "SolidWorks.Interop.sldworks.dll") -Force

$portalVendor = Join-Path $project "Portal\vendor"
$portalAssets = Join-Path $outDir "PortalAssets\vendor"
if (Test-Path $portalVendor) {
    New-Item -ItemType Directory -Force -Path $portalAssets | Out-Null
    Copy-Item -Path (Join-Path $portalVendor "*") -Destination $portalAssets -Recurse -Force
}

$portalImages = Join-Path $project "Portal\images"
$portalImageAssets = Join-Path $outDir "PortalAssets\images"
if (Test-Path $portalImages) {
    New-Item -ItemType Directory -Force -Path $portalImageAssets | Out-Null
    Copy-Item -Path (Join-Path $portalImages "*") -Destination $portalImageAssets -Recurse -Force
}

$solidWorksAssets = Join-Path $project "SolidWorks\Assets"
$solidWorksOutputAssets = Join-Path $outDir "SolidWorksAssets"
if (Test-Path $solidWorksAssets) {
    New-Item -ItemType Directory -Force -Path $solidWorksOutputAssets | Out-Null
    Copy-Item -Path (Join-Path $solidWorksAssets "*") -Destination $solidWorksOutputAssets -Recurse -Force
}

Write-Host "Build klaar: $exe"
