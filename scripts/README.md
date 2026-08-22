# Scripts

`build-configurator.ps1` is de enige ondersteunde buildroute voor portal, desktopcompatibiliteit en SolidWorks-worker. Het script gebruikt `dotnet build`/MSBuild en schrijft naar `src/SWWerkplaats.Configurator/bin/<Configuration>/net48/win-x64`.

`build-with-csc.ps1` blijft alleen als tijdelijke compatibiliteitsnaam bestaan en roept dezelfde buildroute aan; er is geen tweede compilerpad meer.

```powershell
.\scripts\build-configurator.ps1 -Configuration Debug
.\scripts\start-web-configurator.ps1 -Action Rebuild
```

Voor uitrol wordt `-Configuration Release` gebruikt. De buildmap alleen is nog geen zelfstandige release: start vanuit een volledige repository-checkout zodat `config/runtime/masterdata-runtime.json` en de catalogusassets gevonden worden. Voer vóór uitrol de validatie- en regressiecommando's uit `README.md` en `docs/deployment/Lokale-server.md` uit.
