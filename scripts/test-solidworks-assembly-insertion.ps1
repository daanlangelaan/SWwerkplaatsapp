param(
    [string]$Executable,
    [string]$OutputFolder,
    [int]$TimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Executable)) {
    $Executable = Join-Path $repositoryRoot 'src\SWWerkplaats.Configurator\bin\Debug\net48\win-x64\SWWerkplaats.Configurator.exe'
}
if ([string]::IsNullOrWhiteSpace($OutputFolder)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputFolder = Join-Path $repositoryRoot ("tmp\solidworks-assembly-probe-" + $stamp)
}

$resolvedExecutable = [System.IO.Path]::GetFullPath($Executable)
$resolvedOutputFolder = [System.IO.Path]::GetFullPath($OutputFolder)
if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    throw "Configurator-uitvoer ontbreekt: $resolvedExecutable"
}

New-Item -ItemType Directory -Path $resolvedOutputFolder -Force | Out-Null
$resultPath = Join-Path $resolvedOutputFolder 'SolidWorksAssemblyProbeResult.json'
$eventsPath = Join-Path $resolvedOutputFolder 'WindowsApplicationControlEvents.json'
$started = Get-Date
$arguments = @('--solidworks-assembly-probe', $resultPath, (Join-Path $resolvedOutputFolder 'cad'))
$process = Start-Process -FilePath $resolvedExecutable -ArgumentList $arguments -PassThru -WindowStyle Hidden

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    try { $process.Kill() } catch { }
    @{
        ContractVersion = 1
        Ok = $false
        AssemblyInsertionAvailable = $false
        Status = 'TimedOut'
        FailureStage = 'ExternalProcessTimeout'
        Error = "De SolidWorks-assemblyproef reageerde niet binnen $TimeoutSeconds seconden. Controleer SolidWorks op een beleids- of bevestigingsdialoog."
        ProbeFolder = $resolvedOutputFolder
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resultPath -Encoding UTF8
}

$eventLogs = @(
    'Microsoft-Windows-CodeIntegrity/Operational',
    'Microsoft-Windows-AppLocker/EXE and DLL',
    'Microsoft-Windows-AppLocker/MSI and Script'
)
$events = foreach ($logName in $eventLogs) {
    try {
        Get-WinEvent -FilterHashtable @{ LogName = $logName; StartTime = $started } -ErrorAction Stop |
            Select-Object TimeCreated, Id, LevelDisplayName, ProviderName, LogName, Message
    }
    catch {
        [pscustomobject]@{
            TimeCreated = Get-Date
            Id = $null
            LevelDisplayName = 'Unavailable'
            ProviderName = $null
            LogName = $logName
            Message = $_.Exception.Message
        }
    }
}
$events | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $eventsPath -Encoding UTF8

if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    throw "De SolidWorks-assemblyproef stopte zonder resultaatbestand. Zie $eventsPath"
}

$result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
$result | ConvertTo-Json -Depth 10
if (-not $result.Ok) { exit 2 }
