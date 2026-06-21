@echo off
set "ROOT=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%scripts\rebuild-desktop-configurator.ps1"
if errorlevel 1 pause & exit /b %errorlevel%
