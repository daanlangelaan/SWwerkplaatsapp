# Profiel-CNC- en stickeroutput

Status: actueel contract

## Eén projectbron

Elk gegenereerd project bevat precies één canoniek profielbestand: `05_Projectdata/Profielconfiguratie.json`. Dit bestand bestaat ook wanneer CAM bij de projectexport is uitgeschakeld. Het bevat per fysiek profielstuk het stabiele trace-ID, materiaal en doorsnede, vrijgegeven profiel-/sleufgeometrie, lengte, productievolgorde, assemblypositie, stickerplaatsing, D0-D3-machineframe, sleufassen en alle zaag-, boor- en tapbewerkingen. Het bevat daarnaast de gebruikte CNC-masterinstellingen en alle standaardverbindingen inclusief fysieke profielstuk-ID's, kernboring K1..Kn, verbinder/bout en de status van het afzonderlijke sleuteltoegangsgat.

De generator bouwt deze configuratie eenmaal uit het gevalideerde productmodel, schrijft haar naar het project, leest dezelfde JSON vervolgens opnieuw in en maakt **uitsluitend daaruit** de afkortlijst, boorlijst, bewerkingslijst, stickerlijst, taplijst, visuele controle en het CNC-operatorprogramma. Geen exporter mag daarna opnieuw sticker-, tap-, sleuf- of CNC-geometrie uit namen, vrije tekst, rijvolgorde of het oorspronkelijke model afleiden. `config/product-master-data.xlsx` blijft de bibliotheekbron voor materiaal- en machinecontracten; `Profielconfiguratie.json` is binnen het concrete project de enige bron voor fysieke profielproductie.

`ProductionReleased` staat alleen op `true` wanneer alle productiegeometrie exact is. Iedere ontbrekende sticker, onvolledig D0-D3-frame, boring zonder vlak/sleuf of standaardverbinder zonder exact sleuteltoegangsvlak en sleufnummer staat als concrete regel in `ProductionBlockers`. `Profielconfiguratie-validatie.txt` toont dezelfde vrijgavestatus leesbaar voor de werkplaats. Een visualisatie of totaalgetal mag deze blokkade nooit omzeilen.

Bij `ProductionReleased = false` wordt `ProfielCNC-Operatorprogramma.tap` bewust als niet-uitvoerbaar productieprogramma opgebouwd: alleen een blokkademelding, `M5` en `M30`; geen `G0`, `G1`, `M3` of boorcoördinaat. Zo kan een voorlopige visualisatie nooit per ongeluk als vrijgegeven CNC-bestand worden gebruikt.

## Eén productievolgorde

De profielbewerking, het CNC-operatorprogramma en de stickerlijst gebruiken exact dezelfde uit `Profielconfiguratie.json` teruggelezen productievolgorde. Er mag niet per exporter opnieuw worden gesorteerd of genummerd.

De volgorde is deterministisch:

1. grootste doorsnedemaat aflopend;
2. tweede doorsnedemaat aflopend, zodat bijvoorbeeld 80×80 vóór 40×80 komt;
3. binnen dezelfde profielmaat: lengte aflopend;
4. als stabiele laatste sleutel: profielstuk-ID oplopend.

Iedere fysieke profielstaaf komt exact één keer voor. Het profielstuk-ID is hetzelfde trace-ID dat CAM, sticker, 3D-assemblage en handleiding gebruiken.

## Stickerpositie

De stickerpositie komt uitsluitend uit `ProfileStickerPlacement`. De fysieke ankerkop van die plaatsing wordt bij opspannen altijd de **stickerzijde en machine-X=0**. De sticker zelf ligt nadrukkelijk niet op X=0: normaal wordt de vastgelegde standaardafstand vanaf deze kop gebruikt. Die afstand wordt op hele centimeters afgerond. Alleen wanneer de obstructiecontrole de voorkeurspositie afkeurt, mag de stickerplaatsingsservice een andere afstand kiezen. De tegenoverliggende kop heet voor de operator `X=L · eindzijde`. Fysieke Kop A/B blijft alleen interne geometrie en mag niet als wisselende operatorreferentie worden gebruikt.

`ProfileStickerPlacement` bewaart uitsluitend de bestaande assemblywaarheid: lokaal vlak, lokale positie, normaalrichting, ankerkop en afstand. `CrossSectionFace`, `FaceSpanMm` en D1–D3 worden daar niet meer in opgeslagen. `ProfileMachiningFrameService` leidt ze bij iedere productie-export opnieuw af uit die stickerplaatsing, de echte assemblyplaatsing en de vrijgegeven profielgeometrie. Zo kunnen productie en assembly nooit twee onafhankelijk wijzigbare stickervlakken krijgen. Voor een staand 40×80-profiel met de 80-mm-profielmaat verticaal is het gemonteerde bovenvlak bijvoorbeeld assemblagevlak `+Y`, afgeleid doorsnedevlak `+W` en **40 mm breed**. Ontbreekt een van de bronnen, dan blokkeert sticker- en CNC-output; een generieke `+Y`-fallback is verboden.

De operator brengt de sticker aan bij het begin van de profielbewerking. De **afgekorte kop waar de sticker bij ligt** wordt fysiek tegen de vaste X=0-aanslag gezet; de sticker zelf ligt op haar bestaande afgeronde afstand van die kop. D0 betekent uitsluitend: het door de assembly gekozen fysieke stickervlak. Bij de eerste setup ligt het afgeleide tegenovervlak D2 op het machinebed en is D0 zichtbaar boven. De operator keert het profiel nooit in de lengte om. Zonder complete 3D-koppeling wordt geen standaardpositie verzonnen en blijft productie-output geblokkeerd.

### Taal voor de medewerker

D0–D3, S1–Sn, machine-X/Y/Z, lokale vlaknamen en profielassen zijn uitsluitend interne reken- en diagnosecodes. Ze mogen nooit in een `M0`-melding of andere primaire werkinstructie voor de medewerker staan. Iedere stop vertaalt dezelfde geometrie naar direct herkenbare kenmerken:

- welke afgezaagde kop tegen de vaste aanslag blijft;
- korte, lange of vierkante kant boven, inclusief maat;
- stickerafstand in hele centimeters vanaf de vaste aanslag en dwars in het midden;
- bij een zijdewisseling: kijk vanaf de vaste aanslag, exact aantal kwartslagen rechtsom, nieuwe kant boven, dezelfde kop tegen de aanslag, klemmen en starten.

Klemmen en sticker plakken krijgen ieder een eigen korte werkstop. Een rolhandeling krijgt één korte draaistop en één korte controle-/klemstop. Iedere `M0`-regel blijft maximaal 96 tekens lang, zodat de tekst niet uit een smal G-codevenster valt. Technische D0-/sleufinformatie blijft beschikbaar als gestructureerde `SWW_CLAMP`, `SWW_SETUP` en `SWW_HOLE`-markers voor softwarecontrole, maar is geen opdracht aan de medewerker.

## Vast CNC-vlakken- en omdraaicontract

- Kijkrichting voor alle rolstappen: vanaf de vaste X=0-aanslag in de richting van machine `+X`.
- D0 is exact het bestaande stickervlak; D2 is het tegenoverliggende vlak.
- D1 is het vlak dat boven komt na één kwartslag rechtsom in die vaste kijkrichting; D2 en D3 volgen na twee en drie kwartslagen.
- De stickerankerkop blijft tijdens iedere setup tegen X=0. Alleen rollen om de lengteas is toegestaan.
- Sleuven heten per bovenvlak `S1..Sn`, geteld vanaf de operator-linkerrand in diezelfde kijkrichting. De machine-Y is de vrijgegeven sleufas (bij de 40-mm-serie bijvoorbeeld Y20/Y60/Y100/Y140).
- Een boring bevat verplicht D0–D3 en een sleufnummer of exact overeenkomende vrijgegeven sleufas. Vrije tekst zoals `bovenkant` of `zijkant` is alleen beschrijving en bepaalt nooit de geometrie.
- `PositionFromEndAMm` wordt naar machine-X getransformeerd vanuit de echte stickerankerkop. Ligt de sticker bij Kop B, dan geldt `machineX = profiellengte - positieVanafKopA`.
- Voor iedere setup worden profielhoogte, veilige Z, X, Y, eind-Z en de rolstop uit hetzelfde machineframe berekend. Ontbrekende of tegenstrijdige gegevens blokkeren de G-code vóórdat een bestand wordt vrijgegeven.

## Uitvoerbestanden

- `05_Projectdata/Profielconfiguratie.json`: enige projectbron voor alle fysieke profielstukken, stickers, profiel-/machinegeometrie, CNC-instellingen, verbindingen en bewerkingen.
- `05_Projectdata/Validatie/Profielconfiguratie-validatie.txt`: leesbare vrijgavestatus en exacte blokkades uit hetzelfde manifest.
- `Profielstickers-freesvolgorde.xlsx`: één rij per fysiek profielstuk, inclusief freesvolgorde, trace-ID, profieltype, lengte, klemwijze, stickervlak, ankerkop, afstand, oriëntatie en bewerkingssamenvatting.
- `ProfielCNC-Operatorprogramma.tap`: Mach3-boorprogramma met klem- en stickerstop, echte X/Y/Z-boorbewegingen en zo nodig exacte rechtsom-kwartslagstops naar D1–D3. Profielen zonder expliciete boring houden alleen de veilige operatorinformatie.
- `Profielstickers.csv`: bestaande geometrische stickerexport voor compatibiliteit en controle.
- `Profielbewerkingen.xlsx`: bewerkingslijst met fysiek CNC-vlak, sleufnummer, sleufas-Y en diepte naast de leesbare legacyzijde.
- `Profieltappen-werkplaatslijst.xlsx`: compacte aparte lijst in dezelfde productievolgorde. Alleen profielen, koppen en kernboringen met een expliciete tapbewerking staan erin. De lijst gebruikt `X=0 · stickerzijde` en `X=L · eindzijde`; stickerafstand, overbodige gatcoördinaten en nee-regels blijven weg omdat de sticker al tijdens het frezen wordt aangebracht.
- `Profielbewerkingen-visuele-controle.svg`: minimaal visueel tapcontroleblad en aparte portalweergave. Alleen daadwerkelijk te tappen profielen worden getoond. Per tegel staat één herkenbaar profiel met de sleufassen van het werkelijk gekozen stickervlak, de sticker dwars gecentreerd op dat vlak en de lengte als maat eronder; daarnaast alleen de te tappen kopvlakken met K1…Kn. Stickerafstand, X=0/X=L-labels, kopnamen, tapzinnen, gatcoördinaten en dubbele metadata blijven uit deze visualisatie.

Het operatorprogramma leest alle machinewaarden uitsluitend uit `CAM-parameters` in `config/product-master-data.xlsx` via de gegenereerde runtime-snapshot. `ProfileCncMachineSettings` bevat geen alternatieve standaardwaarden. De actuele masterrecords `CAM-PAR-012..024` leggen de uit de oude profielapp afkomstige boorstrategie vast: `S6000`, na iedere `M3` automatisch `G4 P20` om 20 seconden op toerental te komen, veilige parkpositie `Z85/Y300`, 15 mm vrije Z boven het profiel, eerste 3 mm op `F50`, vervolg op `F150` en bij doorboren tot `Z-1`. Dezelfde records leggen ook de X0- en rolconventie plus de fysiek vrijgegeven profielmaten vast. De gebruiker heeft alleen 20×20 en 20×40 als fysiek beproefde profielmaten bevestigd. Voor 40×40, 40×80, 80×80, 40×160 en andere maten blokkeert een echte boring totdat proefstuk, opspanning, gereedschap, voeding en diepte expliciet zijn gevalideerd. De geometrische D0–D3- en sleufascontrole mag al wel worden weergegeven.

## Sleufasgeometrie van de gebruikte 40-mm serie

Voor expliciet vrijgegeven aluminium systeemprofielen uit de 40-mm rasterserie geldt rondom:

- eerste en laatste sleufas liggen 20 mm uit de profielrand;
- volgende sleufassen liggen steeds 40 mm verder;
- groef 8 en groef 10 gebruiken dezelfde aslogica; de sleufbreedte blijft afzonderlijke masterdata;
- 40×40 heeft 4 sleuven rondom;
- 40×80 heeft 6 sleuven rondom;
- 80×80 heeft 8 sleuven rondom;
- 40×160 heeft 10 sleuven rondom. Op de 160-mm vlakken liggen de assen op 20, 60, 100 en 140 mm; op elk 40-mm vlak op 20 mm.

De applicatie berekent de assen uit doorsnedemaat, randafstand en raster en vergelijkt het resultaat met het opgeslagen contractaantal. Een verschil blokkeert de regressietest. Deze regel mag niet op basis van alleen de buitenmaat worden overgenomen voor 20×20, 20×40 of voor 30-, 50- en 100-mm series; die blijven geblokkeerd totdat hun eigen geometrie is gevalideerd.

De kopse kernboringen liggen op de kruisingen van dezelfde assen. Daarmee heeft 40×40 één, 40×80 twee, 80×80 vier en 40×160 vier kernboringen per kop. Deze gaten zijn geschikt als uitgang voor M8, maar **aanwezigheid betekent niet dat ze getapt moeten worden**. `Tappen = JA` mag uitsluitend ontstaan uit een concrete standaardverbinding met een expliciete kernboring K1..Kn. Bij tweezijdige bewerking worden Kop A en Kop B afzonderlijk weergegeven en gecontroleerd.

Voor de machinebasis is `WorkbenchModel.AssemblyConnections` de bron van die expliciete tapbewerking. Iedere `StandardConnector` wijst via `TappedMemberId`, `TappedEnd` en `CoreHoleIndex` exact één fysieke kernboring aan. Een 40×40-kop heeft daardoor één verbinding op K1; een 40×80-kop heeft twee afzonderlijke verbindingen op K1 en K2, op 20 en 60 mm van de rand — nooit één verzonnen middenverbinder. Een groepsvlag, profielnaam, aanwezige kernboring of vrije tekst als `Kop A/B` mag nooit zelfstandig tappen activeren.

Iedere standaardverbinder vereist daarnaast één apart sleuteltoegangsgat in een **niet-kops zijvlak** van het ontvangende profiel. Dat toegangsgat is een boorbewerking en wordt nooit als M8-tapgat geteld. Tapgat, verbinder, M8-bout en sleuteltoegangsgat vormen samen één verbindingspunt en worden uit dezelfde verbinding afgeleid. Zolang ontvangend D0-D3-vlak en sleufas nog niet exact geometrisch zijn vrijgegeven, blijft echte CNC-G-code voor het toegangsgat geblokkeerd; de assembly mag geen middenpositie verzinnen.

## Printerneutraliteit

De Excel-uitvoer blijft voorlopig printerneutraal. `Stickertekst` is het stabiele trace-ID en `Printerstatus` meldt dat handmatig markeren is toegestaan en de printer nog gekozen moet worden. Een toekomstige printeradapter leest deze tabel; hij mag geen eigen volgorde of nieuw sticker-ID genereren.

## Regressiecontract

De productcontracttests bewaken:

- een leesbare JSON-roundtrip zonder verlies van fysieke profielen, trace-ID's, stickers, D0-D3-vlakken, sleufassen, verbindingen of bewerkingen;
- dat ieder onvolledig sleuteltoegangsgat als productieblokkade in dezelfde projectconfiguratie staat;
- één productieregel per fysiek profielstuk;
- aaneengesloten volgordenummers vanaf 1;
- sortering groot naar klein en lang naar kort;
- dezelfde klem- en stickertekst in sequence, Excel en CNC-operatorprogramma;
- exact één korte klemstop en één korte stickerstop per profielstuk, plus per vlakwisseling één draaistop en één controle-/klemstop;
- geen D0–D3-, S1–Sn-, X0- of Y-codes in `M0`-medewerkersteksten; wel herkenbare kant en maat, hele stickercentimeters, draairichting en `START`;
- maximaal 96 tekens per `M0`-regel;
- een geldig OOXML-stickerwerkboek;
- een geldig compact OOXML-tapwerkboek met uitsluitend expliciete tapbewerkingen, de machine-X=0/stickerreferentie en geen impliciete tapbewerking vanuit alleen kernboringsgeometrie;
- voor de machinebasis een exacte setvergelijking tussen iedere `StandardConnector`-koppeling (`TappedMemberId` + `TappedEnd` + `CoreHoleIndex`) en ieder M8-tapgat in de tapwerkplaatslijst;
- een vast machinebasiscontract van 25 tapprofielen, 50 te tappen koppen en 64 M8-tapgaten/standaardverbinders; de extra 14 ontstaan doordat zeven 40×80-profielen per kop K1 én K2 hebben;
- dezelfde 64 verbindingen leveren 64 afzonderlijke sleuteltoegangsgaten in niet-kopse zijvlakken; die tellen niet mee als tapgat en blijven voor echte CNC geblokkeerd zolang vlak/sleufas voorlopig is;
- een minimaal visueel SVG-tapcontroleblad met profielnamen, profielsleuven, sticker, lengtemaat en gatnummers, zonder stickerafstand, X-labels, kopnamen, tapzinnen, `NIET TAPPEN`, gatcoördinaten of dubbele bewerkingsinformatie;
- echte boorbanen met veilige Z vóór iedere X/Y-verplaatsing;
- machinewaarden, vrijgegeven profielmaten, X0-regel en rolrichting die aantoonbaar uit de actieve CAM-masterrecords komen;
- blokkade voor een boring zonder D0–D3, zonder bestaande sleufas of voor een nog niet fysiek gevalideerde profielmaat;
- vaste X=0-ankerkop, correcte omzetting vanaf zowel Kop A als Kop B en een deterministische rechtsom D0→D1→D2→D3-volgorde.
