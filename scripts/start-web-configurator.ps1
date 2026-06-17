$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\SWWerkplaats.Configurator"
$outDir = Join-Path $project "bin\Debug"
$exe = Join-Path $outDir "SWWerkplaats.Configurator.exe"
$url = "http://localhost:8088/"

function Show-PortalError($message) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($message, "SW Werkplaats Portal") | Out-Null
}

function Get-PortalProcessId {
    try {
        $connection = Get-NetTCPConnection -LocalPort 8088 -State Listen -ErrorAction Stop | Select-Object -First 1
        if ($connection) { return $connection.OwningProcess }
    } catch {
        return $null
    }

    return $null
}

function Stop-ExistingPortal {
    $portalPid = Get-PortalProcessId
    if (-not $portalPid) { return }

    try {
        Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8088/api/shutdown" -TimeoutSec 2 | Out-Null
    } catch {
    }

    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 150
        if (-not (Get-PortalProcessId)) { return }
    }

    try {
        Stop-Process -Id $portalPid -Force -ErrorAction Stop
    } catch {
    }
}

function Build-CurrentPortal {
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
        /reference:System.Web.Extensions.dll `
        /reference:System.Windows.Forms.dll `
        /reference:System.Xml.dll `
        $sources

    if ($LASTEXITCODE -ne 0) {
        throw "Build mislukt. Controleer de foutmelding in dit venster."
    }

    $portalAssets = Join-Path $outDir "PortalAssets"
    $portalVendor = Join-Path $project "Portal\vendor"
    $portalImages = Join-Path $project "Portal\images"

    if (Test-Path $portalVendor) {
        New-Item -ItemType Directory -Force -Path (Join-Path $portalAssets "vendor") | Out-Null
        Copy-Item -Path (Join-Path $portalVendor "*") -Destination (Join-Path $portalAssets "vendor") -Recurse -Force
    }

    if (Test-Path $portalImages) {
        New-Item -ItemType Directory -Force -Path (Join-Path $portalAssets "images") | Out-Null
        Copy-Item -Path (Join-Path $portalImages "*") -Destination (Join-Path $portalAssets "images") -Recurse -Force
    }
}

function Start-Portal {
    Start-Process -FilePath $exe -ArgumentList "--portal-only" -WorkingDirectory $outDir -WindowStyle Hidden | Out-Null

    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 150
        if (Get-PortalProcessId) { return }
    }

    throw "Portal startte niet op poort 8088."
}

try {
    Stop-ExistingPortal
    Build-CurrentPortal
    Start-Portal
    Start-Process $url
} catch {
    Show-PortalError $_.Exception.Message
    exit 1
}
