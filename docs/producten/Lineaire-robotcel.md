# Lineaire robotcel

Status: **actueel productcontract, concept / niet productievrijgegeven**.

## Productidentiteit

- Productnaam: `Lineaire robotcel`.
- Stabiele ID: `lineaire_robotcel`.
- Categorie: `profielconstructie`.
- Functionele overlap: `robotcel` voor het robotwerkstation en `machinebasis` voor het gesloten profiel- en afschermconcept. Het nieuwe product heeft een eigen builder omdat de centrale bewegende robotas, twee werkzonevarianten en veiligheidslogica niet gelijk zijn aan een van beide voorbeelden.

## Configureerbare parameters

- lengte langs de robotas;
- werkbladdiepte per zijde;
- werkbladhoogte;
- één werkbladzijde of twee werkbladzijden;
- hoogte vanaf werkblad tot bovenzijde topframe;
- maximale afstand tussen berekende steunvakken.

De standaardwaarden en grenzen staan uitsluitend in `config/product-master-data.xlsx` en worden via `config/runtime/masterdata-runtime.json` aan backend en portal geleverd.

## Constructieconcept

De lineaire as loopt in lengterichting. De robotadapterplaat rust op vier railwagens, twee per rail. De motoradapterplaat wordt als afzonderlijk functioneel deel aan de robotadapterplaat bevestigd. Een tandheugel loopt langs één raildrager. De lineaire rails worden niet op het HPL als dragende precisie-interface gemonteerd: twee 80×80-raildragers rusten rechtstreeks op de dwarsliggers van het werkbladframe.

De geselecteerde robot is de **FAIRINO FR5**. Officiële leveranciersdata: zes assen, 5 kg nominale payload (7 kg maximum), 922 mm bereik, herhaalnauwkeurigheid ±0,02 mm, typische TCP-snelheid 1 m/s, circa 22 kg, beschermingsgraad IP54 (IP65 optioneel) en montage in iedere oriëntatie. De voet is Ø149 mm met vier montagegaten Ø9 op een steekcirkel Ø132 mm en een positioneergat Ø8 H7, 10 mm diep. Bronnen: officiële FAIRINO FR5-productpagina en `FR5 Drawings.zip` uit het FAIRINO Download Center, gecontroleerd op 2026-08-26 (SHA-256 `26DEDB01518855A40BF33EC8DE1618EB326D25FE7AA3424DF247D6BDAB175E1D`).

De geselecteerde lineaire geleiding bestaat uit **twee HIWIN HGR20-rails en vier HGH20CA-wagens**. Volgens HIWIN G99TE24-2410 is de rail 20 mm breed en 17,5 mm hoog; de wagen is 77,5 mm lang en 44 mm breed en de totale montagehoogte is 30 mm. Het wagenpatroon is 32×36 mm met vier M5-gaten, 6 mm diep. De rail heeft gaten op 60 mm steek en gebruikt M5×16. De basis dynamische draaggetalwaarde C is 27,1 kN en de basis statische waarde C0 is 36,68 kN per wagen; deze cataloguswaarden zijn geen vrijgave van de complete dynamische robotas. De raillengte volgt parametrisch uit de cellengte. De volledige HIWIN-bestelcode (voorspanning, nauwkeurigheid, afdichting), rail-eindafstanden en uitvoering uit één stuk of gedeeld blijven nog te kiezen. Bron: officiële HIWIN Linear Guideway Catalog, gecontroleerd op 2026-08-26 (SHA-256 `B9445D5ADA1B19EC0D3FD1E702A78F45A77B638024A7EED786A6E4B256EE50FC`).

Voor de conceptconstructie geldt:

- primaire staanders: 80×80;
- onder- en werkbladframe: 40×80 staand, met berekende tussenstaanders en dwarsliggers;
- raildragers: twee doorlopende 80×80-profielen;
- bovenframe voor de afscherming: 40×40;
- werkblad: HPL naast de railzone, één- of tweezijdig;
- achter- en kopse afscherming: helder gegoten acrylaat;
- lange operatorzijde: veiligheidslichtscherm; bij twee werkbladzijden hebben beide lange zijden een lichtscherm.

Alle dragende profielrollen en standaardtellingen staan in `config/linear-robot-cell-assembly-manifest.json`. De standaardconfiguratie van 3000 mm gebruikt vijf steunspanten, dus tien primaire staanders. Dit is een parametrisch concept, geen bewijs van voldoende dynamische stijfheid.

## Afgesproken SolidWorks-workflow

De configurator bouwt de parametrische machine-/robotbasis. De gebruiker bepaalt eerst in een eigen SolidWorks-hoofdassembly de benodigde werkzone en neemt dezelfde hoofdafmetingen over in de configurator. De geëxporteerde basisassembly wordt daarna als subassembly toegevoegd aan die handmatige hoofdassembly.

De FAIRINO-robotbody, grijper, pneumatiek, motor/reductor, sensoren, kabelrups en overige toepassingsdelen worden bewust later handmatig in SolidWorks toegevoegd. De robot staat met zijn montagehart in het midden van de robotadapterplaat. Het ontbreken van de robotbody in de configuratorassembly is daarom geen fout.

Zolang de vrijgaveblokkade openstaat, mag uitsluitend de in masterdata vastgelegde conceptexport worden gemaakt: native SolidWorks plus projectdata. Iedere export bevat een opvallend statusbestand en de actuele openstaande releasepunten. CAM, 3D-print, klantvrijgave, inkoopvrijgave en productievrijgave blijven geblokkeerd.

Voor conceptgeneratie zijn dynamische berekening, vloerberekening en definitieve veiligheidsselectie geen invoervoorwaarde. Het overslaan daarvan is geen technische goedkeuring: deze onderwerpen blijven zichtbaar als open releasepunt wanneer de samengestelde machine later daadwerkelijk gebouwd of vrijgegeven wordt.

## Adapterplaten

De robotadapterplaat en motoradapterplaat blijven twee aparte manifestleden. Voor iedere plaat moeten vóór vrijgave minimaal worden vastgelegd:

- exacte gekoppelde contactvlakken;
- materiaal, dikte, contour en maakproces;
- ieder gat of iedere sleuf met functionele eigenaar;
- FAIRINO FR5-voetpatroon (bekend) en definitieve boutkeuze, positionering en montagetoegang;
- HIWIN HGH20CA-wagenpatronen (bekend) en definitieve boutlengtes, randafstanden en gereedschapstoegang;
- motor-/reductorpatroon, reactiekrachten en pignonuitlijning;
- sterkte-, stijfheids- en vermoeiingscontrole;
- fysieke proefpassing.

De huidige plaatmaten zijn uitsluitend `ProvisionalRenderEnvelope` uit backendmasterdata. Ze mogen niet naar CAM, inkoop of productievrijgave.

Gebruikersbesluit voor de volgende plaatrevisie: gegoten aluminium bewerkingsplaat, nominaal 20 mm dik, met circa 20 mm extra materiaal rondom de functionele bevestigingsgroepen. De FR5-zijde gebruikt het bekende gecentreerde voetpatroon en normale M8-bevestigers; de HGH20CA-zijde blijft 4× M5 per wagen volgens het leverancierspatroon en mag niet naar M8 worden omgezet. Verzonken kopkamers aan beide zijden, boutlengtes, exacte aluminiumsoort, contour, randafstanden en toegankelijke montagevolgorde worden in het uiteindelijke plaatcontract vastgelegd. Tot die revisie blijft de rechthoekige 400×320×20-mm plaat een voorlopige render-envelop.

De motoradapterplaat wordt door de gebruiker later in de SolidWorks-hoofdassembly toegevoegd. De voorlopige envelop mag in de conceptweergave zichtbaar blijven, maar is geen maakdeel.

## Veiligheidsconcept

Een lichtscherm vervangt niet automatisch een fysieke afscherming. De definitieve selectie volgt pas uit de risicobeoordeling en vereist onder meer veilige afstand, resolutie, beschermhoogte, bereik, responstijd van de volledige stopketen, herstartblokkering, resetpositie, eventuele muting en het vereiste Performance Level of SIL. Acrylaatdikte, bevestigingsafstand en slagvastheid moeten eveneens tegen het werkelijke risico worden gecontroleerd.

## Productieblokkades

Productie- en SolidWorks-export blijven geblokkeerd totdat minimaal bekend en gecontroleerd zijn:

- dynamisch toelaatbare FR5-basisbelasting, massamiddelpunt en gekozen bewegingsprofiel van robot plus lineaire as;
- volledige HIWIN-bestelcode voor voorspanning, nauwkeurigheid en afdichting, plus HGR20-raillengte, eindgaten en eventuele raildeling;
- motor, reductor, rem, tandheugelmodule, pignon, snelheid en versnelling;
- beide adapterplaten inclusief bevestigers en berekening;
- kabelrups, eindstops, harde aanslagen en referentiesensoren;
- vloerverankering, vloersterkte, nivellering en dynamische framecontrole;
- volledige machineveiligheidsvalidatie.

De genoemde blokkades verhinderen niet de gemarkeerde concept-SolidWorks-export. Zij verhinderen wel CAM, inkoopvrijgave en iedere verklaring dat de complete machine productierijp of veilig is.
