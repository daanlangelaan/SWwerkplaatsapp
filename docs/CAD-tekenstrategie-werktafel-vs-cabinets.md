# CAD tekenstrategie: werktafel versus cabinets

Doel van dit document: snel kunnen terugvinden waarom de SolidWorks macro voor de werktafel stabiel werkt, welke tekenafspraken daaruit volgen, en waar de cabinet-route daarvan afwijkt of nog risico loopt.

## Kernprincipe uit de werktafel

De werktafel werkt omdat onderdelen in hun eigen part al in de eindstand worden getekend. De assembly hoeft de onderdelen daarna alleen op coordinaten te plaatsen. Er is geen correctie met rotatie- of spiegelmatrices nodig.

Praktisch betekent dit:

- Het part-coordinate system is bewust gelijk aan de assembly-richting.
- De hoofdschets ligt op een basisvlak dat past bij de uiteindelijke wereldrichting.
- De lengte-as van profielen en platen wordt expliciet uit de onderdeelnaam/type afgeleid.
- Gaten worden in dezelfde lokale coordinaten beschreven als het vlak waarop ze worden getekend.
- De assembly plaatst onderdelen met `AddPart` of `AddPartOriented` zonder orientation-code.

Dit is robuust omdat SolidWorks niet achteraf hoeft te raden of een vlak, sketch of cut na een transform nog de juiste zijde raakt.

## Werktafel: assenafspraak

### Profielen

De werktafelprofielen krijgen hun maatvoering uit `ProfileX`, `ProfileY`, `ProfileZ` en `ProfileAxis` in `SolidWorksMacroExporter`.

| Onderdeel | Part X | Part Y | Part Z | Lengte-as |
| --- | --- | --- | --- | --- |
| `Poot` | profielbreedte | pootlengte | profielhoogte | `Y` |
| `Bovenframe/Onderframe/Tussenframe voor/achter` | liggerlengte | profielhoogte | profielhoogte | `X` |
| `Bovenframe/Onderframe/Tussenframe links/rechts` | profielbreedte | profielhoogte | liggerlengte | `Z` |

Belangrijk detail: de poot wordt dus niet als generiek balkje getekend en later gedraaid. De poot is lokaal al verticaal omdat zijn lengte over `Y` loopt.

### Werktafelplaten

Werktafelplaten zijn horizontaal:

- basisschets op `Top Plane`;
- lengte over `X`;
- breedte/diepte over `Z`;
- dikte/extrude over `Y`;
- gaten als `SheetHole.Xmm/Ymm`, daarna gecentreerd via `HoleData`.

`HoleData` rekent plaatcoordinaten vanaf linksvoor om naar schetscoordinaten rond het midden:

```text
hx = hole.Xmm - sheet.LengthMm / 2
hy = hole.Ymm - sheet.WidthMm / 2
```

Voor horizontale platen betekent dat: `hx` wordt wereld/part `X`, `hy` wordt wereld/part `Z`.

## Strategie die cabinets moeten volgen

De cabinet-onderdelen moeten dezelfde regel volgen: teken elk part in eindstand en plaats het zonder assembly-transform.

| Cabinet orientatie | Bedoeld vlak | Part X | Part Y | Part Z | Gebruik |
| --- | --- | --- | --- | --- | --- |
| `SheetHorizontal` | `Top Plane` | lengte/breedte kast over `X` | dikte | diepte over `Z` | werkblad, bodems, legplanken, ladebodems |
| `SheetVerticalX` | `Front Plane` | breedte over `X` | hoogte over `Y` | dikte over `Z` | fronten, achterwand, plint voor |
| `SheetVerticalZ` | `Right Plane` | dikte over `X` | hoogte over `Y` | diepte over `Z` | zijwanden, tussenschotten, ladezijden |

Voor `SheetVerticalZ` is de juiste cabinet-route dus:

1. Selecteer `Right Plane`.
2. Teken de buitencontour in 2D sketchruimte: sketch-X = `-kast-Z`/diepte, sketch-Y = hoogte.
3. Zet de plintuitsparing als onderdeel van de buitencontour in dezelfde eerste sketch.
4. Extrudeer de plaatdikte mid-plane over `X`.
5. Teken gewone railgaten opnieuw op hetzelfde `Right Plane` middenvlak en cut tweezijdig door de dikte.
6. Plaats het part in de assembly op `X/Y/Z` zonder transform.

Dit sluit aan op de laatste afspraak in `docs/SolidWorksMacroStrategieLog.md`: niet meer terug naar assembly-transforms of losse face-plane cuts voor de eerste stabiele versie.

## Wat de huidige cabinet-code al goed doet

`CabinetEngine` maakt voor zijwanden en tussenschotten `SheetVerticalZ` placements:

- `Zijwand links`: `X = -kastbreedte / 2 + dikte / 2`
- `Zijwand rechts`: `X = kastbreedte / 2 - dikte / 2`
- `Tussenschot i`: `X = -kastbreedte / 2 + unitWidth * i`
- `Y = bodyHeight / 2`
- `Z = 0`

De dimensies van een side panel zijn ook logisch voor `VerticalZ`:

- `LengthMm = DepthMm`
- `WidthMm = bodyHeight`

De railgaten worden op het paneelvlak als `Xmm = dieptepositie vanaf voorkant` en `Ymm = hoogte` vastgelegd. Na `HoleData` worden die gecentreerd. In de `VerticalZ` gatenschets worden ze op de gespiegelde `Right Plane` diepte-as gezet:

```text
sketch point = (X=-holeDepthCentered, Y=holeYCentered, Z=0)
```

Dat is conceptueel de juiste mapping.

## Waar het fout of kwetsbaar gaat

### 1. Oude strategieen staan nog in de macro

De macro bevat nog functies voor:

- `SelectVerticalZFacePlane`
- `CreateToeKickCut`
- `CreateVerticalZCountersinkFeatures`
- modelpunt-naar-sketch transform helpers
- `CutSelectedSketchThroughThickness`

Volgens het strategie-log zijn deze routes eerder onbetrouwbaar gebleken voor cabinet `VERTICAL_Z`. De huidige `CreateSheetPart` gebruikt voor `isVerticalZ` alleen nog `CreateVerticalZBaseSketch` en `CreateVerticalZThroughHoleFeatures` op het middenvlak. De oude face-plane helpers blijven nog wel in de macro aanwezig voor historische/fallback-codepaden. Dat maakt debugging verwarrend: je ziet oude oplossingsrichtingen in de code terwijl de gewenste route het middenvlak is.

Risico: een latere kleine wijziging kan per ongeluk weer door deze oude face-plane/cut-route lopen.

### 2. `VerticalZ` hangt aan een correcte basiscontour en middenvlak-cut

Voor `VERTICAL_Z` wordt de plintuitsparing in `CreateVerticalZBaseSketch` gezet. Railgaten worden daarna op hetzelfde `Right Plane` middenvlak getekend en tweezijdig door de dikte gecut. Als SolidWorks de plintcontour niet als geldige buitencontour herkent, ontstaat een fout bij extruderen. Als de gatenschets niet op het middenvlak ligt, komen de gaten verkeerd of niet door het materiaal.

Daarom moet deze route zo simpel mogelijk blijven:

- een gesloten buitencontour;
- geen dubbele of overlappende lijnen;
- plintuitsparing als deel van de buitencontour, niet als losse cut achteraf.
- cirkels volledig binnen de buitencontour wanneer ze later als gatenschets worden getekend.

### 3. Linker/rechter zijde en tussenschotten hebben nu geen zijde-semantiek

`AddRailHolesForPanel` voegt voor een boundary beide aangrenzende units toe:

```text
boundaryIndex
boundaryIndex + 1
```

Voor doorlopende gaten is dat acceptabel. Voor schroefgaten, kopkamers of niet-doorgaande bewerkingen is het onvoldoende, omdat de code niet vastlegt of de bewerking op de linker- of rechterzijde van het paneel hoort.

Dit is waarschijnlijk de bron van "orientatie van de zij- en tussenschotten" zodra gaten niet meer alleen simpele doorboringen zijn. De werktafel had dit probleem niet: daar waren plaatgaten vanaf een eenduidig plaatvlak en profielboringen met een expliciete `Side` zoals `X-zijde centrum` of `Z-zijde centrum`.

Cabinets missen dus nog een equivalent van:

```text
welke fysieke zijde van het paneel krijgt deze bewerking?
```

### 4. Assembly-orientatie wordt bewust niet doorgegeven

`AppendGenericAssemblyCalls` roept nu aan:

```text
AddComponentOriented(..., "")
```

Dat is correct als alle parts in eindstand zijn getekend. Het betekent ook dat `OrientationCode` en de transform-matrices in `AddPartOriented` voor cabinets geen reddingsnet meer zijn. Als een `VerticalZ` part verkeerd staat, moet de fout in de part-tekenroute worden opgelost, niet in de assembly.

## Aanbevolen volgende stap

Niet opnieuw beginnen met transforms of face-plane cuts. De werktafelstrategie zegt:

1. Houd `SheetVerticalZ` in eindstand: `Right Plane`, `Y/Z` basisschets, dikte over `X`.
2. Houd railgaten op hetzelfde `Right Plane` middenvlak en cut ze tweezijdig door de dikte.
3. Voeg aan cabinet railgaten een zijde/face-betekenis toe voordat er kopkamers, verzinkingen of niet-doorgaande gaten komen.
4. Maak een kleine gegenereerde testmacro met een enkel `VerticalZ` paneel: buitencontour, plintuitsparing en 2 gaten. Pas daarna dezelfde route toe op alle zijwanden en tussenschotten.

Kort gezegd: de cabinet-code zit qua assen al dicht bij de werktafelstrategie. De grootste afwijking is niet de plaatsing, maar het ontbreken van expliciete zijde-semantiek voor bewerkingen op verticale panelen, plus de aanwezigheid van oude macro-routes die volgens het log al onbetrouwbaar waren.
