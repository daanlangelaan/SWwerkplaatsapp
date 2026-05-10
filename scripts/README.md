# Scripts

## build-with-csc.ps1

Fallback buildscript voor machines zonder `dotnet` of `msbuild` op PATH.

Uitvoeren vanuit de projectroot:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-with-csc.ps1
```

Voor de echte SolidWorks Add-in ontwikkeling blijft Visual Studio 2022 aanbevolen.
