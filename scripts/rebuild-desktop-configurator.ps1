param(
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $root "src\SWWerkplaats.Configurator\bin\Debug\net48\win-x64"
$exe = Join-Path $targetDir "SWWerkplaats.Configurator.exe"
$buildScript = Join-Path $PSScriptRoot "build-configurator.ps1"
. (Join-Path $PSScriptRoot "configurator-process-control.ps1")

Stop-ConfiguratorProcesses -ExecutablePath $exe
Wait-ConfiguratorExecutableUnlocked -ExecutablePath $exe

& $buildScript -Configuration Debug
if ($LASTEXITCODE -ne 0) { throw "Desktop build mislukt." }

Write-Host "Desktop configurator bijgewerkt: $exe"

if (-not $NoStart) {
    Start-Process -FilePath $exe -WorkingDirectory $targetDir
}
