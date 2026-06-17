# Lokale server

De portal kan zonder codewijziging op een andere poort of datafolder starten.

## Standaard

Zonder extra configuratie:

- URL: `http://localhost:8088/`
- Datafolder: `bin/PortalData`

## Klikbare startbestanden

Voor lokaal ontwikkelen staan in de projectmap drie klikbare bestanden:

- `Web configurator starten.cmd`: start de laatst gebouwde portal en opent de browser.
- `Web configurator stoppen.cmd`: stopt de portal op poort `8088`.
- `Web configurator rebuild.cmd`: stopt de portal, bouwt de actuele code opnieuw, start de portal en opent de browser.

Gebruik `rebuild` na codewijzigingen. Gebruik `stop` als je twijfelt of er nog een oude portal draait.

## Tijdelijk starten via argumenten

```powershell
.\bin\SWWerkplaats.Configurator.exe --portal-only --portal-port=8090 --portal-root=C:\SWWerkplaats\PortalData
```

## Configbestand

Kopieer `config/portal-runtime.example.json` naar `config/portal-runtime.json` en pas waarden aan.

Belangrijke velden:

- `RootFolder`: locatie voor orders, freeswachtrij en notificaties.
- `Prefix`: URL-prefix voor de portal.
- `Port`: poort wanneer geen expliciete prefixpoort is gezet.
- `PortalOnly`: start alleen de webportal zonder WinForms.

## Omgevingsvariabelen

Deze waarden kunnen ook via de omgeving worden gezet:

- `SW_PORTAL_ROOT`
- `SW_PORTAL_PORT`
- `SW_PORTAL_PREFIX`

## Controle

Gebruik deze endpoints om een lokale server snel te controleren:

- `GET /api/health`
- `GET /api/catalog`
- `GET /api/workflow`
