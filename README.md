# SW Werkplaats Portal

Lokale configurator en werkplaatsportal voor SW Werkplaats. De app rekent kast- en werkplaatsproducten door, maakt nesting, controlebestanden en Mach3 `.tap` bestanden voor de portaalfrees.

## Wat zit erin

- Klantconfigurator via lokale webportal op `http://localhost:8088`.
- Cabinet/kast generator met lades, legplanken, achterwand, groeven en montagegaten.
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

## Build check

Handmatig bouwen:

```powershell
dotnet build src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj
```

GitHub Actions draait dezelfde build op Windows bij push en pull request.

Snelle portal-check na wijzigingen:

```powershell
$html = (Invoke-WebRequest -Uri http://localhost:8088/ -UseBasicParsing).Content
$match = [regex]::Match($html, '<script>([\s\S]*)</script>')
$tmp = Join-Path $env:TEMP 'sw-portal-check.js'
Set-Content -LiteralPath $tmp -Value $match.Groups[1].Value -Encoding UTF8
node --check $tmp
```

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

De portal schrijft orders, freeswachtrij en gegenereerde bestanden naar:

```text
src\SWWerkplaats.Configurator\bin\Debug\PortalData
```

Deze map wordt niet naar GitHub gepusht. Ook lokale machine-instellingen zoals `config/app-settings.json` blijven buiten git.

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
  SolidWorks/     Voorbereiding voor SolidWorks-export
  UI/             WinForms shell
config/           Voorbeeldconfiguratie en lokale instellingen
docs/             Ontwerpnotities
scripts/          Start/build scripts
```

## Git workflow

Werk vanaf een feature branch, commit kleine stappen, en push naar GitHub:

```powershell
git status
git add .
git commit -m "Beschrijf je wijziging"
git push
```

Niet alles hoeft in git. Lokale orderdata, gegenereerde freesbestanden en losse referentiebestanden blijven lokaal tenzij je ze bewust toevoegt.

## Huidige technische afspraken

- De vakjeskast is actief in de portal.
- Bij vakjeskast tellen `Vakken breedte` en `Vakken hoogte` het aantal open vakken, niet het aantal losse kamplaten.
- Interne kamdelen worden berekend als `vakken breedte - 1` staander-kammen en `vakken hoogte - 1` ligger-kammen.
- Kam-uitsparingen zijn door-en-door bewerkingen in nesting/G-code en worden in 3D als echte open uitsparingen weergegeven.
- Achterwand wordt waar mogelijk als een deel gemaakt; alleen als hij niet op een plaat past wordt hij gesegmenteerd.
