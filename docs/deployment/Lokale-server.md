# Lokale server

Status: **actueel beheercontract**.

De portal kan zonder codewijziging op een andere poort of datafolder starten.

## Standaard

Zonder extra configuratie:

- URL: `http://localhost:8088/`
- Datafolder: `C:\SWWerkplaats\PortalData`
- Orderdatabase: `C:\SWWerkplaats\PortalData\portal-orders.sqlite`

De lokale override staat in `config/portal-runtime.json`. Dit bestand blijft buiten Git,
zodat iedere installatie een eigen absolute runtime-locatie kan gebruiken. Gegenereerde
orders, exports en freeswachtrijen horen niet in de repository.

## Klikbare startbestanden

Voor lokaal ontwikkelen staan in de projectmap drie klikbare bestanden:

- `Web configurator starten.cmd`: start de laatst gebouwde portal en opent de browser.
- `Web configurator stoppen.cmd`: stopt de portal op poort `8088`.
- `Web configurator rebuild.cmd`: stopt de portal, bouwt de actuele code opnieuw, start de portal en opent de browser.

Gebruik `rebuild` na codewijzigingen. Gebruik `stop` als je twijfelt of er nog een oude portal draait.

## Ondersteunde uitrolvorm

De huidige productie-eenheid is één volledige repository-checkout op een Windows-server of werkstation. De executable zoekt de gevalideerde masterdatasnapshot omhoog in de omliggende repositorystructuur. Een losse kopie van `bin/.../SWWerkplaats.Configurator.exe` is daarom geen complete release.

De volgende versiebeheerde onderdelen moeten samen worden uitgerold:

- de Release-build onder `src/SWWerkplaats.Configurator/bin/Release/net48/win-x64`;
- `config/runtime/masterdata-runtime.json`;
- `config/catalog-images/` en `config/catalog-images/image-catalog.json`;
- overige versieerbare configuratie onder `config/`;
- portal-, SolidWorks- en presentatiemiddelen die door het projectbestand naar de buildoutput worden gekopieerd.

`config/product-master-data.xlsx` blijft in de checkout aanwezig als menselijke beheer- en auditbron, maar de actieve applicatie leest voor leveranciers, pricing en CAM de gegenereerde runtime-snapshot. Een zelfstandige, automatisch samengestelde releasebundle is nog geen ondersteunde route; totdat die bestaat wordt altijd vanuit de volledige checkout gebouwd en gestart.

De klikbare startbestanden gebruiken voor lokaal ontwikkelen de Debug-build. Start na een productiepreflight de Release-build expliciet:

```powershell
$releaseDir = Resolve-Path .\src\SWWerkplaats.Configurator\bin\Release\net48\win-x64
Start-Process -FilePath (Join-Path $releaseDir 'SWWerkplaats.Configurator.exe') -ArgumentList '--portal-only' -WorkingDirectory $releaseDir -WindowStyle Hidden
Invoke-RestMethod http://localhost:8088/api/health
```

Automatische installatie als Windows-service is nog niet ingericht. Gebruik tot die tijd een beheerde Windows-login of een afzonderlijk, gecontroleerd taak/servicecontract; registreer nooit twee portalprocessen tegen dezelfde SQLite-database.

## Preflight voor uitrol

Voer vanuit de repositoryroot uit:

```powershell
.\Web configurator stoppen.cmd
python .\scripts\validate-master-data.py
python .\scripts\generate-masterdata-runtime.py --check
python .\scripts\check-repository.py
.\scripts\build-configurator.ps1 -Configuration Release
dotnet run --configuration Release --project .\tests\GCodeMonitoringMarkers.SmokeTests\GCodeMonitoringMarkers.SmokeTests.csproj
dotnet run --configuration Release --project .\tests\ProductContracts.RegressionTests\ProductContracts.RegressionTests.csproj
dotnet run --configuration Release --project .\tests\OrderStorage.IntegrationTests\OrderStorage.IntegrationTests.csproj
```

Een uitrol gaat niet door wanneer de runtime-snapshot verouderd is, een afbeelding of verwijzing ontbreekt, de build faalt of een regressietest rood is.

Wanneer `dotnet build` faalt met een melding dat `SWWerkplaats.Configurator.exe` in gebruik is, draait de portal nog. Gebruik dan:

```powershell
.\Web configurator stoppen.cmd
dotnet build src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj
.\Web configurator starten.cmd
```

Of korter:

```powershell
.\Web configurator rebuild.cmd
```

## Tijdelijk starten via argumenten

```powershell
.\src\SWWerkplaats.Configurator\bin\Debug\net48\win-x64\SWWerkplaats.Configurator.exe --portal-only --portal-port=8090 --portal-root=C:\SWWerkplaats\PortalData
```

## Configbestand

Kopieer op een nieuwe installatie `config/portal-runtime.example.json` naar
`config/portal-runtime.json` en pas de waarden aan. De huidige ontwikkelinstallatie
gebruikt `C:\SWWerkplaats\PortalData`.

Belangrijke velden:

- `RootFolder`: locatie voor orders, freeswachtrij en notificaties.
- `Prefix`: URL-prefix voor de portal.
- `Port`: poort wanneer geen expliciete prefixpoort is gezet.
- `PortalOnly`: start alleen de webportal zonder WinForms.
- `OrderStorageProvider`: `sqlite` (standaard) of tijdelijk `files` voor herstel/compatibiliteit.
- `DatabasePath`: absoluut pad naar de SQLite-database.

## Omgevingsvariabelen

Deze waarden kunnen ook via de omgeving worden gezet:

- `SW_PORTAL_ROOT`
- `SW_PORTAL_PORT`
- `SW_PORTAL_PREFIX`
- `SW_ORDER_STORAGE`
- `SW_ORDER_DATABASE`

## Controle

Gebruik deze endpoints om een lokale server snel te controleren:

- `GET /api/health`
- `GET /api/catalog`
- `GET /api/workflow`
- `GET /api/library`

## Meerdere gebruikers en online gebruik

SQLite gebruikt WAL, korte transacties en een verbinding per repositorybewerking. Daarmee kunnen meerdere browsergebruikers veilig via **één draaiende portalserver** werken. Plaats het `.sqlite`-bestand niet op een netwerkshare en start niet meerdere portalservers tegen hetzelfde bestand. Voor meerdere serverinstanties wordt `IOrderRepository` later met PostgreSQL of SQL Server geïmplementeerd.

De ingebouwde listener biedt zelf geen TLS, gebruikersaccounts of internetbeveiliging. Publiceer hem daarom niet rechtstreeks op internet; gebruik voor online gebruik een HTTPS-reverse-proxy met authenticatie, toegangslogging en back-upbeleid.

Voor doorwerken op een andere laptop:

```powershell
git clone https://github.com/daanlangelaan/SWwerkplaatsapp.git
cd SWwerkplaatsapp
.\Web configurator rebuild.cmd
Invoke-RestMethod http://localhost:8088/api/health
```

## Update en rollback

1. Maak vóór een update een consistente back-up van `PortalData` en noteer de actieve Git-commit.
2. Stop de portal.
3. Haal de gecontroleerde commit op en voer de volledige preflight uit.
4. Start de portal en controleer minimaal `/api/health`, `/api/catalog`, `/api/workflow` en `/api/library`.
5. Controleer daarna één standaardofferte en één testorder zonder productie vrij te geven.

Bij rollback: stop de portal, herstel de vorige gecontroleerde Git-commit/build en herstel alleen wanneer nodig de bijbehorende consistente PortalData-back-up. Kopieer tijdens actieve WAL-opslag nooit uitsluitend `portal-orders.sqlite` zonder de SQLite-back-upregels uit `architecture/Operationele-opslag.md` te volgen.
