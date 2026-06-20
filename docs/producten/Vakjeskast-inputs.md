# Vakjeskast productafspraak

Deze productfamilie is actief in de portal. De huidige implementatie rekent de buitenmaat uit op basis van vakmaten, plaatdikte, aantal vakken en de gekozen verdiepte positie van het raster.

## Klantparameters

De klant kiest:

- plaatmateriaal en dikte;
- vakdiepte;
- vakbreedte;
- vakhoogte;
- vakken breedte;
- vakken hoogte;
- vakjes verdiept vanaf de voorkant;
- achterwand ja/nee.

De vakmaten plus de plaatdikte bepalen de buitenmaat van de kast. De klant stuurt dus primair op bruikbare binnenvakken, niet op totale kastmaat.

Voor gelijke vakken:

- buitenbreedte = `vakkenBreedte * vakbreedte + (vakkenBreedte + 1) * plaatdikte`;
- buitenhoogte = `vakkenHoogte * vakhoogte + (vakkenHoogte + 1) * plaatdikte`;
- buitendiepte = `vakdiepte + vakjesVerdiept + plaatdikte`.

`Vakjes verdiept mm` is de afstand vanaf de voorkant van de buitenkast tot de voorkant van het interne vakjesraster. Het is dus geen offset vanaf de achterwand. De ingevulde `vakdiepte` blijft de bruikbare diepte van het vak.

Let op: `Vakken breedte` en `Vakken hoogte` tellen open vakken, niet het aantal losse kamplaten.

Voorbeeld:

- `3` vakken breedte geeft `2` interne staander-kammen;
- `3` vakken hoogte geeft `2` interne ligger-kammen;
- linker/rechter zijwand, bovenplaat en bodemplaat zijn aparte kastdelen.

## Constructie

De vakjes worden als kamconstructie opgebouwd:

- verticale staander-kammen en horizontale ligger-kammen schuiven in elkaar;
- kamuitsparingen worden door-en-door gefreesd en in de 3D-view als open uitsparingen weergegeven;
- voorbeeld: 4 vakken hoog en 3 vakken breed bestaat uit 2 interne staander-kammen en 3 interne ligger-kammen, plus de buitenkast;
- de vakjesconstructie ligt verdiept ten opzichte van de voorkant van top, bodem en zijkanten van de buitenkast.

## Achterwand

De achterwand krijgt:

- verdiepte sleuven op de buitencontour, zodat top, bodem en zijplaten netjes in de achterwand vallen;
- gaten verdeeld langs zijkanten en boven/onderkant om vanuit de achterwand in de kopse kanten van top, bodem en zijplaten te schroeven;
- gaten op de lijnen waar de interne schotten/liggers tegen de achterwand komen, zodat de vakjesconstructie aan de achterwand gemonteerd kan worden.

Assemblage-afspraak:

- voorboren gebeurt alleen in de achterwand;
- in de kopse kanten van top, bodem, zijkanten en schotten wordt bij assemblage direct geschroefd.
- achterwand wordt als een deel gemaakt als hij op de gekozen plaat past; anders wordt hij per segment gedeeld.

## Tekencontract aandachtspunten

- Achterwand is `SheetVerticalX`.
- Top/bodem zijn `SheetHorizontal`.
- Zijkanten en verticale kamdelen zijn `SheetVerticalZ`.
- Horizontale ligger-kammen zijn `SheetHorizontal`.
- Alle sleuven/pockets moeten een expliciete fysieke zijde krijgen.
- Schroefgaten in de achterwand worden lokale sheetcoordinaten op de achterwand.
- Door-en-door kamuitsparingen gebruiken `OperationDepthMode.Through` en worden in de portal gemarkeerd als `IsThroughCutout`, zodat 3D geen dubbele pocketlijnen tekent.

## Huidige defaults en open keuzes

Huidige basis:

- vakjesconstructie krijgt een instelbare frontverdieping via `Vakjes verdiept mm`;
- kamuitsparingen krijgen clearance via productregels;
- achterwandgroeven en montagegaten worden door de engine gezet;
- nesting toont door-en-door kamuitsparingen als witte/cutout vlakken.

Nog te bepalen voordat dit product naar echte productie mag:

- definitieve passing per materiaalsoort;
- definitieve achterwandgroefdiepte per materiaal;
- bevestigingsgatdiameter en schroefafstand per productnorm;
- of de UI ook een modus moet krijgen die direct aantal kam-schotten invoert in plaats van aantal open vakken.

## Eerste implementatievoorstel

De eerste implementatie is aanwezig:

- vakken zijn exact gelijk verdeeld;
- buitenmaat wordt afgeleid uit vakmaat, aantal vakken en plaatdikte;
- kamdelen schuiven in elkaar met door-en-door kamuitsparingen;
- achterwand heeft pockets/sleuven plus voorboorgaten;
- geen deuren/lades/bakken in versie 1;
- alle gaten en pockets lopen door de bestaande tekencontractvalidatie.

Snelle controle:

- `3 x 3` vakken geeft `2` staander-kammen en `2` ligger-kammen;
- dezelfde test geeft `8` door-en-door kamuitsparingen;
- er mogen geen halfhout-kamuitsparingen als gewone 3D-pocket worden gerenderd.
