# Ontwerpplan

## Keuze

De configurator wordt opgezet als C# WinForms/SolidWorks Add-in basis. De werkplaatslogica staat los van SolidWorks, zodat afkortlijsten, boorlijsten en Mach3 G-code testbaar blijven zonder CAD-sessie.

## Fase 1

Werktafelgenerator:

- invoer via GUI
- materiaalkeuze voorlopig hardcoded met configuratievoorbeelden in JSON
- berekening van profielmaten
- berekening van werkblad
- Mach3 `.tap` voor plaatmateriaal
- CSV afkortlijst
- CSV boorlijst
- SolidWorks exportplan als tussenstap

## G-code afspraken

- `G21` millimeters
- `G90` absolute coordinaten
- `G17` XY vlak
- nulpunt links onder
- `Z0` op bovenzijde plaat
- buitencontour krijgt automatische offset van `toolDiameter / 2`
- gaten kleiner dan ongeveer freesdiameter worden gepeckboord
- grotere gaten worden circulair gefreesd
- tabs alleen op de laatste contourpass

## SolidWorks koppeling

De volgende stap is een echte `SolidWorksExporter`:

1. Open of kopieer template part voor profiel.
2. Vul lengteparameter in.
3. Open of kopieer template part voor plaat.
4. Vul lengte, breedte en dikte in.
5. Maak assembly.
6. Plaats profielen op vaste coordinaten.
7. Exporteer tekeningen of DXF waar gewenst.

## Later

- JSON daadwerkelijk inlezen in de GUI.
- Toollibrary beheren vanuit GUI.
- Zaagoptimalisatie per handelslengte.
- Nesting van plaatdelen.
- Kasten en ladekasten als extra productfamilies.
- Mach3 preview/simulatie en waarschuwingen.
