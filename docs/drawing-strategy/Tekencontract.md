# Tekencontract

Dit document is leidend voor nieuwe productfamilies en wijzigingen aan plaatdelen, gaten, pockets, SolidWorks export en 3D preview.

## Kernregel

Een onderdeel wordt in zijn part al in eindstand getekend. De assembly plaatst het onderdeel op coordinaten. Assembly-transforms zijn geen correctiemiddel voor verkeerd getekende parts.

## Wereldassen

| As | Betekenis |
| --- | --- |
| X | breedte links/rechts |
| Y | hoogte |
| Z | diepte voor/achter |

## Plaatorientaties

| Orientatie | SolidWorks vlak | Part X | Part Y | Part Z | Voorbeelden |
| --- | --- | --- | --- | --- | --- |
| `SheetHorizontal` | Top Plane | lengte/breedte | dikte | diepte | werkblad, bodem, legplank, ladebodem |
| `SheetVerticalX` | Front Plane | breedte | hoogte | dikte | front, achterwand, plint |
| `SheetVerticalZ` | Right Plane | dikte | hoogte | diepte | zijwand, tussenschot, ladezijde |

## Sheet-coordinaten

`SheetPart.LengthMm` en `SheetPart.WidthMm` beschrijven de 2D plaatmaat waarop gaten en pockets worden vastgelegd.

| Orientatie | Sheet Xmm | Sheet Ymm |
| --- | --- | --- |
| `SheetHorizontal` | part/world X | part/world Z |
| `SheetVerticalX` | part/world X | part/world Y |
| `SheetVerticalZ` | diepte vanaf voorkant | part/world Y |

Voor `SheetVerticalZ` wordt de diepte-as in SolidWorks gespiegeld in de Right Plane sketch: sketch-X = negatieve dieptecoordinaten. Deze afspraak niet vervangen door assembly-rotatie.

## Gaten en pockets

Elke bewerking moet uiteindelijk deze betekenis hebben:

- lokale plaatcoordinaten;
- fysieke zijde of middenvlak;
- doorlopend of blind;
- support/beslagsoort;
- productie- en visualisatiebetekenis.

Doorlopende gaten mogen voorlopig zonder fysieke zijde werken. Zodra een gat blind is, een kopkamer heeft, of alleen aan een zijde hoort, moet de zijde expliciet zijn.

## Fysieke zijden

Gebruik deze taal in code en documentatie:

| Zijde | Betekenis |
| --- | --- |
| `CenterPlane` | middenvlak, doorlopend of symmetrisch |
| `PositiveX` / `NegativeX` | rechter/linker diktezijde |
| `PositiveY` / `NegativeY` | boven/onder |
| `PositiveZ` / `NegativeZ` | achter/voor |

Voor cabinet-zijwanden is dit belangrijk: linker zijwand, rechter zijwand en tussenschotten kunnen aan verschillende fysieke zijden railgaten of blinde gaten krijgen.

Cabinet-bodemgroeven:

- Alleen de buitenste zijwanden krijgen een gefreesde `Bodem positioneergroef`.
- Tussenschotten krijgen geen bodemplaat-groef. Een tussenschot zou bij meerdere units anders aan twee kanten gefreesd moeten worden, en tweezijdig frezen is geen toegestane productieroute.
- Middenbodems worden uitgelijnd via de achterwandgroef en rusten op de plintconstructie; montagegaten in tussenschotten mogen wel blijven.
- Bodemplaten worden per unit op maat gezet: alleen aan een buitenwandzijde steekt de plaat in de groef, aan tussenschotzijden blijft de bodemplaat binnen de vrije opening.
- De voorplint krijgt aan de achterzijde positioneergroeven voor linker zijwand, tussenschotten en rechter zijwand. De achterwand lijnt achter uit; de plint lijnt de voorkant uit.

## 3D preview contract

De preview gebruikt dezelfde productiedata:

- `AssemblyPlacement` bepaalt positie en orientatie;
- `SheetHole` bepaalt gaten;
- `SheetPocket` bepaalt pockets;
- pockets worden als echte meshverdieping getekend, niet als losse overlay.

Als de viewer moet afwijken van productie-output, moet dat expliciet als UI-only helper worden gemarkeerd.

## Nieuwe productfamilie checklist

Voor een nieuw product, zoals een vakjeskast:

1. Definieer productconfiguratie en defaults.
2. Bouw een model met dezelfde `WorkbenchModel` output.
3. Gebruik alleen bekende `AssemblyOrientation` waarden.
4. Leg voor elk plaatdeel vast welke orientatie en lokale sheet-assen gelden.
5. Geef gaten en pockets lokale coordinaten volgens dit contract.
6. Genereer quote, nesting, controlebestand en 3D preview.
7. Voeg minimaal een standaardconfiguratie-test toe.

## Rode vlaggen

- Een onderdeel staat alleen goed na assembly-rotatie.
- Een bewerking gebruikt wereldcoordinaten terwijl de sheet lokale coordinaten verwacht.
- Een blind gat heeft geen fysieke zijde.
- De preview tekent een pocket als los transparant blokje.
- Productdefaults staan alleen in HTML.
