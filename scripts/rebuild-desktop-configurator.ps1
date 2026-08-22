param(
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $root "bin"
$exe = Join-Path $targetDir "SWWerkplaats.Configurator.exe"
$buildScript = Join-Path $PSScriptRoot "build-with-csc.ps1"

Get-Process SWWerkplaats.Configurator -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase) } |
    Stop-Process -Force

& $buildScript
if ($LASTEXITCODE -ne 0) { throw "Desktop build mislukt." }

Write-Host "Desktop configurator bijgewerkt: $exe"

if (-not $NoStart) {
    Start-Process -FilePath $exe -WorkingDirectory $targetDir
}
