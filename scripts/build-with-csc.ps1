param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Write-Warning "build-with-csc.ps1 is een compatibiliteitsnaam. De enige ondersteunde buildroute gebruikt nu dotnet/MSBuild."
& (Join-Path $PSScriptRoot "build-configurator.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
