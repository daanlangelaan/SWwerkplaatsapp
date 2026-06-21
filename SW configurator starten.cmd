@echo off
set "ROOT=%~dp0"
set "EXE=%ROOT%bin\SWWerkplaats.Configurator.exe"

if not exist "%EXE%" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('Desktop configurator is nog niet gebouwd. Gebruik eerst SW configurator rebuild.cmd.', 'SW Werkplaats Configurator') | Out-Null"
  exit /b 1
)

start "" "%EXE%"
