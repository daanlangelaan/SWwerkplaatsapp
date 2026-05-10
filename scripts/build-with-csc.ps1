$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\SWWerkplaats.Configurator"
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

$sources = Get-ChildItem -Path $project -Recurse -Filter *.cs | Select-Object -ExpandProperty FullName

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
    /reference:System.Windows.Forms.dll `
    /reference:System.Xml.dll `
    $sources

Write-Host "Build klaar: $exe"
