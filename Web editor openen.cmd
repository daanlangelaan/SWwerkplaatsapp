@echo off
set "ROOT=%~dp0"
set "EXE=%ROOT%bin\SWWerkplaats.Configurator.exe"
set "URL=http://localhost:8088/"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$exe = $env:EXE; $url = $env:URL; if (-not (Test-Path $exe)) { Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('Kan de web editor niet vinden:' + [Environment]::NewLine + $exe, 'SW Werkplaats Portal') | Out-Null; exit 1 }; $client = New-Object Net.Sockets.TcpClient; $running = $false; try { $client.Connect('127.0.0.1', 8088); $running = $true } catch { $running = $false } finally { if ($client) { $client.Close() } }; if (-not $running) { Start-Process -FilePath $exe -ArgumentList '--portal-only'; Start-Sleep -Seconds 1 }; Start-Process $url"
