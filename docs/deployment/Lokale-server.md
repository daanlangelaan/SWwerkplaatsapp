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
