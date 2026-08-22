# SW Werkplaats Portal

Lokale configurator en werkplaatsportal voor SW Werkplaats. De app rekent kast- en werkplaatsproducten door, maakt nesting, controlebestanden en Mach3 `.tap` bestanden voor de portaalfrees.

## Wat zit erin

- Klantconfigurator via lokale webportal op `http://localhost:8088`.
- Cabinet/kast generator met lades, legplanken, achterwand, groeven en montagegaten.
- Werkbank met kastonderbouw: doorlopende bodem, deurparen met T-stijlen, stelpootgaten en losse voorzetplint.
- Profielproducten: machinebasis, robotcel en modulaire materiaal-/gereedschapswagen met centrale TechXXL-leveranciersselectie.
- Materiaalwagen: 600–1800 mm breed, 450–1000 mm diep, 700–1200 mm bovenbladhoogte, 2–4 legbordlagen, configureerbare duwbeugel en twee D100-wielopstellingen.
- Nesting per plaatmateriaal met SVG-preview en CSV-controle.
- Mach3 G-code voor geneste platen, inclusief toolwissel-stop (`M0`) en plaatwisseltekst.
- Controle-output voor railgaten, tekencontracten, BOM, prijs en assemblage.
- Start/stop/rebuild scripts voor Windows.

## Snel starten op een laptop

Vereist:

- Windows
- .NET SDK of Visual Studio 2022 / Build Tools met .NET desktop development
- Git
- Optioneel: Node.js voor de portal-JS syntaxcheck

Clone de repo:

```powershell
git clone https://github.com/daanlangelaan/SWwerkplaatsapp.git
cd SWwerkplaatsapp
```

Start de webportal:

```powershell
.\Web configurator starten.cmd
```

Start of rebuild de tijdelijke desktop configurator:

```powershell
.\SW configurator rebuild.cmd
```

Rebuild na codewijzigingen:

```powershell
.\Web configurator rebuild.cmd
```

Stop de portal:

```powershell
.\Web configurator stoppen.cmd
```

De app draait lokaal op:

```text
http://localhost:8088
```

### Doorwerken op een tweede laptop

1. Haal eerst de laatste versie op:

```powershell
git pull
```

2. Start of rebuild de portal met de klikbare bestanden in de projectmap:

```powershell
.\Web configurator rebuild.cmd
```

3. Controleer of de server draait:

```powershell
Invoke-RestMethod http://localhost:8088/api/health
```

4. Open daarna `http://localhost:8088`.

Gebruik `.\Web configurator stoppen.cmd` als de app oud lijkt of als een build faalt omdat `SWWerkplaats.Configurator.exe` nog draait. Gebruik daarna `rebuild`.
De webportal is de standaardinterface. De rail-/dragereditor staat op `http://localhost:8088/library`. De desktopstart blijft alleen als tijdelijke compatibiliteitsschil; alle routes gebruiken dezelfde `dotnet`/MSBuild-build.

## Build check

Handmatig bouwen via de canonieke route:

```powershell
.\scripts\build-configurator.ps1
```

GitHub Actions draait dezelfde build op Windows bij push en pull request.

Het numerieke bouwcontract van de materiaalwagen staat in `config/material-cart-assembly-manifest.json`. Wijzig profielopbouw of parameters alleen samen met `MaterialCartEngine`, de masterdata en `ProductContracts.RegressionTests`, zodat portal, BOM, prijs en 3D-assembly gelijk blijven.

Voer vóór een uitrol of overdracht de volledige lokale controle uit:

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

Snelle portal-check na wijzigingen:

```powershell
$html = (Invoke-WebRequest -Uri http://localhost:8088/ -UseBasicParsing).Content
$match = [regex]::Match($html, '<script>([\s\S]*)</script>')
$tmp = Join-Path $env:TEMP 'sw-portal-check.js'
Set-Content -LiteralPath $tmp -Value $match.Groups[1].Value -Encoding UTF8
node --check $tmp
```

Na het genereren van een actuele configuratie staat **Exporteer projectpakket** klaar. Iedere export krijgt één map onder `<PortalData>\Projecten` (op de huidige ontwikkelinstallatie `C:\SWWerkplaats\PortalData\Projecten`), met een vast contract: `01_CAM` voor productie- en freesbestanden, `02_SolidWorks` uitsluitend voor `.SLDPRT`, `.SLDASM` en `.SLDDRW`, `03_Klantvoorstel` voor PDF, PowerPoint, GLB, HTML, aanzichten en render-assets, `04_3D-print` voor printdelen en `05_Projectdata` voor configuratie, BOM, prijzen, validatie- en generatiegegevens. Niet-relevante mappen worden niet aangemaakt. Het zelfstandige HTML-bestand bevat het volledige GLB-model en de viewer. De klantpresentatiemodule wijst automatisch materiaalachtige appearances toe (onder andere betonplex, OSB, multiplex, aluminium en beslag) en bewaart de benodigde materiaalassets onder het klantvoorstel.

De exporter ondersteunt zowel de standaard COM-registratie `SldWorks.Application` als de afwijkende 3DEXPERIENCE ROT-registratie `SolidWorks_PID_*`. Als nog geen sessie actief is, start hij SOLIDWORKS Design via de bureaubladsnelkoppeling of `CATSTART.exe` voor tenant `R1132104190977` en wacht hij maximaal vijf minuten op een eventuele handmatige login.

Snelle vakjeskast-check:

```powershell
$body = @{
  product='vakjeskast'
  cubbyCellWidthMm=100
  cubbyCellDepthMm=90
  cubbyCellHeightMm=100
  cubbyColumnCount=3
  cubbyRowCount=3
  sheetMaterialId='betonplex_18'
  backMaterialId='multiplex_15'
  includeBackPanel=$true
  cubbyGridInsetMm=20
} | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:8088/api/quote -Method Post -ContentType 'application/json' -Body $body
```

## Lokale data

De portal schrijft orders, freeswachtrij en gegenereerde bestanden standaard naar de externe operationele map. Ordermetadata staat in SQLite; JSON-orderbestanden blijven als export- en herstelmirror bestaan:

```text
C:\SWWerkplaats\PortalData
```

Deze map staat bewust buiten buildoutput en wordt niet naar GitHub gepusht. Ook lokale machine-instellingen zoals `config/app-settings.json` en `config/portal-runtime.json` blijven buiten git.

## Belangrijke scripts

- `Web configurator starten.cmd`: start bestaande build.
- `Web configurator stoppen.cmd`: stopt de portal op poort `8088`.
- `Web configurator rebuild.cmd`: stopt, bouwt opnieuw en start.
- `scripts/start-web-configurator.ps1`: onderliggend PowerShell script.

## Projectindeling

```text
src/SWWerkplaats.Configurator/
  Application/    Use-cases, orderflow en productregistry
  Domain/         Product-, materiaal-, rail-, tool- en machine-modellen
  Drawing/        Algemene en productspecifieke tekenregels
  Engine/         Productberekeningen voor kast/werkbank
  Manufacturing/ Nesting, G-code en productie-export
  Portal/         Lokale webportal, visualisaties en outputservice
  SolidWorks/     Export, klantoutput en geïsoleerde SolidWorks-worker
  UI/             WinForms shell
config/
  product-master-data.xlsx        Menselijke beheerbron
  master-data-schema.json         Tabel-, sleutel- en relatiecontract
  catalog-images/                 Versiebeheerde leveranciersafbeeldingen
  runtime/masterdata-runtime.json Gegenereerde applicatiesnapshot
  portal-runtime.example.json     Installatievoorbeeld; lokale override blijft buiten Git
docs/             Actuele contracten, productafspraken, toekomst en archief
scripts/          Start/build scripts
.codex/skills/    Projectskills; geen gegenereerde Python-caches
tests/            Uitvoerbare rook-, productcontract- en opslagtests
```

Begin voor beheer- of architectuurwerk bij `AGENTS.md` en `docs/README.md`. Oude iteratieverslagen onder `docs/archive/` zijn context en geen actueel runtimecontract.

## Git workflow

Werk vanaf een feature branch, commit kleine stappen, en push naar GitHub:

```powershell
git status
git add .
git commit -m "Beschrijf je wijziging"
git push
```

Niet alles hoeft in git. Lokale orderdata, gegenereerde freesbestanden en losse referentiebestanden blijven lokaal tenzij je ze bewust toevoegt.

## Library-data

Het leidende beheerregister voor productregels, materialen, componenten, verbindingsrecepten, prijzen en inkoopgegevens staat in:

```text
config\product-master-data.xlsx
```

Excel is de menselijke beheerbron. `python scripts\generate-masterdata-runtime.py` compileert de werkmap en afbeeldingscatalogus naar `config\runtime\masterdata-runtime.json`; pricing, leveranciersvoorkeuren en CAM lezen deze snapshot. De kolom `Interne-ID` koppelt een BOM-regel aan aanbieding, leverancier, artikelcode, bestel-URL, afbeelding en prijsstatus.

De binaire catalogusafbeeldingen staan in `config\catalog-images\<leverancier>\` en hun stabiele koppelingen in `config\catalog-images\image-catalog.json`. Excel bevat alleen previews. Voeg geen losse leveranciers- of cataloguswerkbladen toe; de vaste indeling en wijzigingsprocedure staan in `docs\Masterdata-beheer.md`.

Rails en legplankdragers staan in:

```text
config\hardware-library.json
```

In de portal kun je deze aanpassen via `/library`. `Kast posities ;` en `Lade posities ;` zijn puntkomma-gescheiden X-posities in mm, bijvoorbeeld `34;98;226;354;418`. Als deze posities leeg zijn gebruikt de app `1e gat + gatpas * aantal`.

## Uitrollen

De huidige ondersteunde uitrolvorm is één volledige repository-checkout op een Windows-server of werkstation. Bouw en start vanuit die map. Kopieer **niet alleen** `SWWerkplaats.Configurator.exe`: de app verwacht ook de versiebeheerde `config/runtime/masterdata-runtime.json`, catalogusassets en overige configuratie in de repositorystructuur.

Operationele orderdata hoort niet in Git en staat per installatie onder `C:\SWWerkplaats\PortalData`. Maak op een nieuwe installatie `config\portal-runtime.json` vanuit het voorbeeldbestand en houd dit lokale bestand buiten versiebeheer.

Voor meerdere gebruikers draait één portalproces met SQLite/WAL. Publiceer de ingebouwde listener niet rechtstreeks op internet; gebruik bij externe toegang een HTTPS-reverse-proxy met authenticatie en logging. De volledige installatie-, update-, back-up- en rollbackprocedure staat in `docs\deployment\Lokale-server.md`.

## Huidige technische afspraken

- De vakjeskast is actief in de portal.
- Bij vakjeskast tellen `Vakken breedte` en `Vakken hoogte` het aantal open vakken, niet het aantal losse kamplaten.
- Interne kamdelen worden berekend als `vakken breedte - 1` staander-kammen en `vakken hoogte - 1` ligger-kammen.
- Kam-uitsparingen zijn door-en-door bewerkingen in nesting/G-code en worden in 3D als echte open uitsparingen weergegeven.
- Achterwand wordt waar mogelijk als een deel gemaakt; alleen als hij niet op een plaat past wordt hij gesegmenteerd.
