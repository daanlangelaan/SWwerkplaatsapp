function Get-ConfiguratorProcessRecords {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,
        [switch]$PortalOnly
    )

    $expectedPath = [IO.Path]::GetFullPath($ExecutablePath)
    $records = @(Get-CimInstance Win32_Process -Filter "Name = 'SWWerkplaats.Configurator.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ExecutablePath -and
            [string]::Equals([IO.Path]::GetFullPath($_.ExecutablePath), $expectedPath, [StringComparison]::OrdinalIgnoreCase) -and
            (-not $PortalOnly -or ($_.CommandLine -and $_.CommandLine.IndexOf("--portal-only", [StringComparison]::OrdinalIgnoreCase) -ge 0))
        })

    return $records
}

function Stop-ConfiguratorProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,
        [switch]$PortalOnly,
        [int]$TimeoutSeconds = 15
    )

    $records = @(Get-ConfiguratorProcessRecords -ExecutablePath $ExecutablePath -PortalOnly:$PortalOnly)
    if ($records.Count -eq 0) {
        if ($PortalOnly) {
            Write-Host "Geen actieve webconfigurator gevonden."
        } else {
            Write-Host "Geen actieve configuratorprocessen gevonden."
        }
        return
    }

    $ids = @($records | ForEach-Object { [int]$_.ProcessId } | Sort-Object -Unique)
    Write-Host ("Configurator stoppen, PID: " + ($ids -join ", "))
    foreach ($processId in $ids) {
        Stop-Process -Id $processId -Force -ErrorAction Stop
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $remaining = @($ids | Where-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
        if ($remaining.Count -eq 0) {
            Write-Host "Configuratorproces volledig gestopt."
            return
        }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Configuratorproces bleef actief na $TimeoutSeconds seconden, PID: $($remaining -join ', ')."
}

function Wait-ConfiguratorExecutableUnlocked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,
        [int]$TimeoutSeconds = 15
    )

    if (-not (Test-Path -LiteralPath $ExecutablePath)) { return }
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $stream = $null
        try {
            $stream = [IO.File]::Open($ExecutablePath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            Write-Host "EXE-lock is vrij."
            return
        } catch {
            Start-Sleep -Milliseconds 100
        } finally {
            if ($stream) { $stream.Dispose() }
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    $records = @(Get-ConfiguratorProcessRecords -ExecutablePath $ExecutablePath)
    $processText = if ($records.Count -gt 0) {
        " Actieve PID(s): " + (($records | ForEach-Object { $_.ProcessId }) -join ", ") + "."
    } else { "" }
    throw "De configurator-EXE bleef langer dan $TimeoutSeconds seconden vergrendeld.$processText"
}

function Test-ProcessUsesConfiguratorExecutable {
    param(
        [Parameter(Mandatory = $true)]
        [int]$ProcessId,
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath
    )

    $record = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
    if (-not $record -or -not $record.ExecutablePath) { return $false }
    return [string]::Equals(
        [IO.Path]::GetFullPath($record.ExecutablePath),
        [IO.Path]::GetFullPath($ExecutablePath),
        [StringComparison]::OrdinalIgnoreCase)
}
