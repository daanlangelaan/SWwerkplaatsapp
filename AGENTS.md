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
- Genereer na iedere Excel- of afbeeldingscataloguswijziging `config/runtime/masterdata-runtime.json` met `python scripts/generate-masterdata-runtime.py`. Applicatiecode leest Excel niet rechtstreeks.
- Pricing, leveranciersvoorkeur en CAM lezen de runtime-snapshot. Overige JSON- of hardcoded catalogusconsumenten mogen pas verdwijnen nadat ze zijn omgezet en productregressies slagen.
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
python .\scripts\generate-masterdata-runtime.py --check
python .\scripts\check-repository.py
.\scripts\build-configurator.ps1
dotnet run --project .\tests\GCodeMonitoringMarkers.SmokeTests\GCodeMonitoringMarkers.SmokeTests.csproj
dotnet run --project .\tests\ProductContracts.RegressionTests\ProductContracts.RegressionTests.csproj
dotnet run --project .\tests\OrderStorage.IntegrationTests\OrderStorage.IntegrationTests.csproj
```

De webportal is de standaardinterface; rails en dragers worden via `/library` beheerd. WinForms is alleen compatibiliteit en krijgt geen nieuwe functies. De `--solidworks-worker`-modus is een actieve procesisolatie voor de SolidWorks-export en mag niet als ongebruikte UI worden verwijderd. Nieuwe ordermetadata gebruikt standaard SQLite; bestanden blijven export- en herstelmirror.
