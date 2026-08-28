param(
    [ValidateSet("Start", "Stop", "Rebuild")]
    [string]$Action = "Start",
    [switch]$NoOpen
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\SWWerkplaats.Configurator"
$outDir = Join-Path $project "bin\Debug\net48\win-x64"
$exe = Join-Path $outDir "SWWerkplaats.Configurator.exe"
$url = "http://localhost:8088/"
. (Join-Path $PSScriptRoot "configurator-process-control.ps1")

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
    Stop-ConfiguratorProcesses -ExecutablePath $exe -PortalOnly
}

function Build-CurrentPortal {
    & (Join-Path $PSScriptRoot "build-configurator.ps1") -Configuration Debug
    if ($LASTEXITCODE -ne 0) {
        throw "Build mislukt. Controleer de foutmelding in dit venster."
    }
}

function Start-Portal {
    $existingPid = Get-PortalProcessId
    if ($existingPid) {
        if (Test-ProcessUsesConfiguratorExecutable -ProcessId $existingPid -ExecutablePath $exe) { return }
        throw "Poort 8088 is al bezet door een ander proces (PID $existingPid)."
    }
    if (-not (Test-Path $exe)) {
        throw "De portal is nog niet gebouwd. Gebruik eerst Web configurator rebuild.cmd."
    }

    $started = Start-Process -FilePath $exe -ArgumentList "--portal-only" -WorkingDirectory $outDir -WindowStyle Hidden -PassThru
    Write-Host "Webconfigurator gestart, PID $($started.Id)."

    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 150
        if (Get-PortalProcessId) { return }
    }

    throw "Portal startte niet op poort 8088."
}

function Wait-PortalReady {
    for ($i = 0; $i -lt 30; $i++) {
        if (Test-PortalHealth) { return }

        Start-Sleep -Milliseconds 200
    }

    throw "Portal luistert op poort 8088, maar de health-check geeft nog geen antwoord."
}

function Test-PortalHealth {
    $client = $null
    try {
        $client = New-Object Net.Sockets.TcpClient
        $client.ReceiveTimeout = 1000
        $client.SendTimeout = 1000
        $client.Connect("127.0.0.1", 8088)
        $stream = $client.GetStream()
        $request = [Text.Encoding]::ASCII.GetBytes("GET /api/health HTTP/1.1`r`nHost: localhost`r`nConnection: close`r`n`r`n")
        $stream.Write($request, 0, $request.Length)
        $buffer = New-Object byte[] 256
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) { return $false }

        $response = [Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        return $response.StartsWith("HTTP/1.1 200", [StringComparison]::OrdinalIgnoreCase)
    } catch {
        return $false
    } finally {
        if ($client) { $client.Close() }
    }
}

try {
    if ($Action -eq "Stop") {
        Stop-ExistingPortal
        exit 0
    }

    if ($Action -eq "Rebuild") {
        Stop-ConfiguratorProcesses -ExecutablePath $exe
        Wait-ConfiguratorExecutableUnlocked -ExecutablePath $exe
        Build-CurrentPortal
    }

    Start-Portal
    Wait-PortalReady
    if (-not $NoOpen) {
        Start-Process $url
    }
} catch {
    Show-PortalError $_.Exception.Message
    exit 1
}
