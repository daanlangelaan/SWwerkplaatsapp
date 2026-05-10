# SW Werkplaats Configurator

Eerste basis voor een SolidWorks werkplaats-configurator die parametrische werktafels kan genereren met productie-output voor de werkplaats.

## Doel

- CAD-generatie voorbereiden voor SolidWorks Design 2026.
- Mach3 `.tap` G-code genereren voor plaatmateriaal op een portaalfrees.
- Afkortlijsten en boorlijsten maken voor profielen/balken.
- Materiaal-, machine- en tooldata buiten de code beheren.

## Eerste prototype

Het eerste producttype is een werktafel:

- aluminium frame, standaard 40x40
- 4 poten
- bovenframe rondom
- optioneel onderframe
- werkblad als plaatdeel
- Mach3 G-code voor het werkblad
- CSV afkortlijst
- CSV boorlijst

## Machineprofiel

De standaard machine is vastgelegd als:

- portaalfrees
- max plaatmaat 3020 x 1520 mm
- Mach3 `.tap`
- nulpunt links onder
- standaard tool 6 mm frees
- tabs automatisch voor kleine onderdelen

## Projectindeling

```text
src/SWWerkplaats.Configurator/
  Domain/        Product-, materiaal-, tool- en machine-modellen
  Engine/        Werktafelberekening
  Manufacturing/ Mach3 G-code, afkortlijst en boorlijst
  SolidWorks/    Adapterlaag voor toekomstige SolidWorks API-koppeling
  UI/            WinForms GUI skeleton
config/          Voorbeeldmateriaal, tools en machineprofiel
examples/        Voorbeeld werktafelconfiguratie
docs/            Ontwerpnotities
```

## Volgende technische stap

Installeer Visual Studio 2022 met .NET desktop development en SolidWorks API SDK/COM references. Daarna kan dit project als .NET Framework WinForms applicatie of SolidWorks Add-in worden geopend en verder gekoppeld aan SolidWorks.
