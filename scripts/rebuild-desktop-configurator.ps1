param(
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $root "src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj"
$sourceDir = Join-Path $root "src\SWWerkplaats.Configurator\bin\Debug"
$targetDir = Join-Path $root "bin"
$exe = Join-Path $targetDir "SWWerkplaats.Configurator.exe"

Get-Process SWWerkplaats.Configurator -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase) } |
    Stop-Process -Force

dotnet build $projectFile
if ($LASTEXITCODE -ne 0) {
    throw "Desktop build mislukt."
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -LiteralPath (Join-Path $sourceDir "SWWerkplaats.Configurator.exe") -Destination $exe -Force

$pdb = Join-Path $sourceDir "SWWerkplaats.Configurator.pdb"
if (Test-Path $pdb) {
    Copy-Item -LiteralPath $pdb -Destination (Join-Path $targetDir "SWWerkplaats.Configurator.pdb") -Force
}

$assets = Join-Path $sourceDir "PortalAssets"
if (Test-Path $assets) {
    Copy-Item -Path $assets -Destination $targetDir -Recurse -Force
}

Write-Host "Desktop configurator bijgewerkt: $exe"

if (-not $NoStart) {
    Start-Process -FilePath $exe -WorkingDirectory $targetDir
}
