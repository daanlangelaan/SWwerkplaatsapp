# Lokale server

De portal kan zonder codewijziging op een andere poort of datafolder starten.

## Standaard

Zonder extra configuratie:

- URL: `http://localhost:8088/`
- Datafolder: `bin/PortalData`

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
