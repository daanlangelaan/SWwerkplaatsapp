# Vakjeskast productafspraak

Deze productfamilie staat als code-skelet klaar, maar is nog niet actief in de portal.

## Klantparameters

De klant kiest:

- plaatmateriaal en dikte;
- vakdiepte;
- vakbreedte;
- vakhoogte;
- aantal kolommen;
- aantal rijen;
- achterwand ja/nee.

De vakmaten plus de plaatdikte bepalen de buitenmaat van de kast. De klant stuurt dus primair op bruikbare binnenvakken, niet op totale kastmaat.

Voor gelijke vakken:

- buitenbreedte = `kolommen * vakbreedte + (kolommen + 1) * plaatdikte`;
- buitenhoogte = `rijen * vakhoogte + (rijen + 1) * plaatdikte`;
- buitendiepte = `vakdiepte`.

## Constructie

De vakjes worden als kamconstructie opgebouwd:

- verticale kamdelen en horizontale kamdelen schuiven in elkaar;
- sleuven worden op halve diepte gefreesd, met een kleine passing/clearance;
- voorbeeld: 4 rijen en 3 kolommen bestaat uit verticale kamdelen plus horizontale kamdelen die in elkaar haken;
- de vakjesconstructie ligt verdiept ten opzichte van top, bodem en zijkanten van de buitenkast.

## Achterwand

De achterwand krijgt:

- verdiepte sleuven op de buitencontour, zodat top, bodem en zijplaten netjes in de achterwand vallen;
- gaten verdeeld langs zijkanten en boven/onderkant om vanuit de achterwand in de kopse kanten van top, bodem en zijplaten te schroeven;
- gaten op de lijnen waar de interne schotten/liggers tegen de achterwand komen, zodat de vakjesconstructie aan de achterwand gemonteerd kan worden.

Assemblage-afspraak:

- voorboren gebeurt alleen in de achterwand;
- in de kopse kanten van top, bodem, zijkanten en schotten wordt bij assemblage direct geschroefd.

## Tekencontract aandachtspunten

- Achterwand is `SheetVerticalX`.
- Top/bodem zijn `SheetHorizontal`.
- Zijkanten en verticale kamdelen zijn `SheetVerticalZ`.
- Horizontale kamdelen zijn `SheetHorizontal` of product-specifiek te bepalen op basis van de freesstrategie.
- Alle sleuven/pockets moeten een expliciete fysieke zijde krijgen.
- Schroefgaten in de achterwand worden lokale sheetcoordinaten op de achterwand.

## Open keuzes

Deze gegevens zijn nog nodig voordat de engine veilig gebouwd wordt:

- hoeveel mm verdiept de vakjesconstructie ligt ten opzichte van de voorkant/top/bodem/zijkanten;
- gewenste passing van kam-sleuven, bijvoorbeeld `plaatdikte + 0.3 mm`;
- sleufdiepte voor kamverbindingen, meestal halve plaatdikte;
- diepte van de achterwand-sleuven;
- maximale schroefafstand langs buitencontour;
- maximale schroefafstand op interne schotlijnen;
- diameter voor voorboorgaten in de achterwand;
- minimale randafstand voor die gaten.

## Eerste implementatievoorstel

- Vakken zijn exact gelijk verdeeld.
- Buitenmaat wordt afgeleid uit vakmaat, aantal rijen/kolommen en plaatdikte.
- Kamdelen schuiven in elkaar met halve-dikte sleuven.
- Achterwand heeft pockets/sleuven plus voorboorgaten.
- Geen deuren/lades/bakken in versie 1.
- Alle gaten en pockets lopen door de bestaande tekencontractvalidatie.
