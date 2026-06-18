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

Clone de repo:

```powershell
git clone <github-url>
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

## Build check

Handmatig bouwen:

```powershell
dotnet build src\SWWerkplaats.Configurator\SWWerkplaats.Configurator.csproj
```

GitHub Actions draait dezelfde build op Windows bij push en pull request.

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
