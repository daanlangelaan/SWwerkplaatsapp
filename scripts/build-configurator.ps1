param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $root "src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK ontbreekt. Installeer de .NET SDK die het .NET Framework 4.8-project kan bouwen."
}

& dotnet build $projectFile --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Configuratorbuild mislukt." }

$exe = Join-Path (Split-Path -Parent $projectFile) ("bin\" + $Configuration + "\net48\win-x64\SWWerkplaats.Configurator.exe")
if (-not (Test-Path -LiteralPath $exe)) { throw "Gebouwde configurator ontbreekt: $exe" }
Write-Host "Build klaar: $exe"
