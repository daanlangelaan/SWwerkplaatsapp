param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,
    [string]$Executable,
    [string]$OutputFolder,
    [int]$TimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Executable)) {
    $Executable = Join-Path $repositoryRoot 'src\SWWerkplaats.Configurator\bin\Debug\net48\win-x64\SWWerkplaats.Configurator.exe'
}
if ([string]::IsNullOrWhiteSpace($OutputFolder)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputFolder = Join-Path $repositoryRoot ("tmp\solidworks-audited-assembly-" + $stamp)
}

$resolvedExecutable = [System.IO.Path]::GetFullPath($Executable)
$resolvedInputPath = [System.IO.Path]::GetFullPath($InputPath)
$resolvedOutputFolder = [System.IO.Path]::GetFullPath($OutputFolder)
if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) { throw "Configurator-uitvoer ontbreekt: $resolvedExecutable" }
if (-not (Test-Path -LiteralPath $resolvedInputPath -PathType Leaf)) { throw "PortalQuoteRequest JSON ontbreekt: $resolvedInputPath" }
New-Item -ItemType Directory -Path $resolvedOutputFolder -Force | Out-Null

$resultPath = Join-Path $resolvedOutputFolder 'SolidWorksAuditedAssemblyResult.json'
$arguments = @('--solidworks-audited-assembly-worker', $resolvedInputPath, $resultPath)
$process = Start-Process -FilePath $resolvedExecutable -ArgumentList $arguments -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    try { $process.Kill() } catch { }
    @{
        ContractVersion = 1
        Ok = $false
        GeometryAuditPassed = $false
        ReleaseEligible = $false
        Status = 'TimedOut'
        FailureStage = 'ExternalProcessTimeout'
        Error = "De geaudite SolidWorks-assembly reageerde niet binnen $TimeoutSeconds seconden."
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resultPath -Encoding UTF8
}
if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "De geaudite SolidWorks-worker stopte zonder resultaatbestand." }

$result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
$result | ConvertTo-Json -Depth 10
if (-not $result.Ok) { exit 2 }
