# Projectinstructies

Lees eerst `docs/README.md`. Gebruik daarna de instructie die bij de wijziging hoort.

## Verplichte bronnen

- Producten, materialen, componenten, leveranciers, prijzen en catalogusafbeeldingen: `docs/Masterdata-beheer.md`.
- Architectuur en mapverantwoordelijkheid: `docs/architecture/App-structuur.md` en `docs/architecture/Repository-structuur.md`.
- Profielconstructies reconstrueren of controleren: `.codex/skills/detect-profile-assembly/SKILL.md`.

## Masterdata

- `config/product-master-data.xlsx` is de menselijke beheer- en wijzigingsbron.
- Koppel uitsluitend met stabiele ID's volgens `config/master-data-schema.json`; nooit op naam, omschrijving of rijpositie.
- Voeg geen aparte leveranciers-, voorkeurs- of cataloguswerkbladen toe. Leveranciers en voorkeuren horen in `Leveranciers`; aanbiedingen en afbeeldingsverwijzingen in `Prijs & inkoop`.
- Afbeeldingsbestanden horen in `config/catalog-images/<supplier>/` en worden geregistreerd in `config/catalog-images/image-catalog.json`. Excel bevat alleen een preview.
- De runtime-migratie naar één gegenereerde masterdatasnapshot is nog niet voltooid. Verwijder JSON/CSV-fallbacks of hardcoded defaults pas nadat alle codeconsumenten zijn omgezet en regressietests slagen.
- Een nieuw product is pas compleet nadat productregister, productregels, bibliotheekreferenties, prijsaanbiedingen, controles en wijzigingslog zijn bijgewerkt en gevalideerd.

## Repositoryhygiëne

- `bin/`, `obj/`, `.codex-artifacts/`, `artifacts/`, `output/`, `outputs/` en `tmp/` zijn gegenereerd en nooit brondata.
- Operationele PortalData staat buiten de repository in `C:\SWWerkplaats\PortalData`.
- Houd de projectskill onder `.codex/skills/detect-profile-assembly` gelijk aan de geïnstalleerde lokale kopie wanneer de skill wordt gewijzigd. Commit nooit `__pycache__` of `.pyc`.
- Behoud geen legacy-tab of legacy-ID nadat alle verwijzingen aantoonbaar zijn gemigreerd.

## Minimale controle

Voer bij relevante wijzigingen minimaal uit:

```powershell
python .\scripts\validate-master-data.py
dotnet build .\src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj
dotnet run --project .\tests\GCodeMonitoringMarkers.SmokeTests\GCodeMonitoringMarkers.SmokeTests.csproj
```

De webportal is de voorkeursinterface. De WinForms-interface blijft tijdelijk aanwezig omdat de rail-/dragereditor nog niet naar de portal is gemigreerd. De `--solidworks-worker`-modus is een actieve procesisolatie voor de SolidWorks-export en mag niet als ongebruikte UI worden verwijderd.
