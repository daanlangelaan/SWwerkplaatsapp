# Opvouwbare werktafel

## Status

`opvouwbare_werktafel` is een configureerbaar conceptproduct. Klantvoorstel, projectdata en interactief 3D zijn toegestaan. Productie-CAM, native productie-SolidWorks en productievrijgave blijven geblokkeerd totdat de open controles uit de masterdata zijn gesloten.

De aangeleverde foto's zijn uitsluitend gebruikt om de constructie en beweging te reconstrueren; ze zijn geen maat- of leveranciersbron.

## Parametrische opbouw

- invoer: bladlengte, bladbreedte en hoogte bovenzijde blad;
- standaard: 2400 × 1180 × 900 mm;
- plaatmateriaal: `betonplex_18` voor alle zeven plaatdelen;
- realistische materiaalweergave: donkere betonplex-vlakken met de gelaagde multiplexkern op alle buiten- en uitsparingskanten;
- bladoversteek: twee afzonderlijke invoerparameters, één vanaf de lange bladranden en één vanaf de korte bladranden; beide staan voorlopig standaard op 80 mm, zodat de standaard onderstelmaat 2240 × 1020 mm blijft;
- onderstel: twee vaste volledige framepanelen over de lengte en aan iedere korte zijde twee framevormige vouwpanelen;
- geïntegreerde voeten: ieder langspaneel heeft drie vloercontacten onder de linker-, midden- en rechterstijl; ieder kort half vouwpaneel heeft twee vloercontacten onder zijn beide stijlen;
- elk kort half vouwpaneel heeft een stijl aan het langspaneel, een stijl aan de middenas, een brede bovenregel en een brede onderregel; alle stijlen lopen tot de vloer;
- iedere korte vouwzijde heeft een voorste, middelste en achterste verticale scharnieras;
- bladborging: drie nokken per vast langspaneel en twee nokken per kort half vouwpaneel in veertien doorlopende slots, uitsluitend geometrisch en door zwaartekracht;
- blad blijft zonder gereedschap uitneembaar;
- wielen en wielgaten vallen buiten versie 1.

De backend leidt alle technische waarden af uit `config/runtime/masterdata-runtime.json`. De browser ontvangt alleen het geometrie-, render- en bewegingscontract.

De overstekken volgen de zichtbare constructie uit de referentiefoto en besparen plaatmateriaal in alle zes onderstelpanelen. De conceptstandaard is 80 mm per zijde, maar beide richtingen zijn onafhankelijk configureerbaar. De offset vanaf de korte bladranden bepaalt de onderstellengte; de offset vanaf de lange bladranden bepaalt de ondersteldiepte. De masterdataparameters sturen samen de onderstelomtrek, paneellengtes, nokposities en de bijbehorende werkbladslots, zodat visualisatie, nesting en latere CAM dezelfde geometrie gebruiken. De waarden worden pas definitief na stabiliteits-, randbelasting- en proefstukcontrole.

## Scharnierconcept

Het concept gebruikt voorlopig zes normale AXA RVS-kogellagerscharnieren 76 × 76 × 2,5 mm, G Goedkoop-artikel `0000006696` / AXA `1KL227676N`: één per verticale scharnieras. De officiële AXA-tekening voor 1KL227676 bevestigt rondhoek R10, knoop Ø10,8 mm en drie verzonken gaten per blad. De render bestaat uit drie fysieke delen: blad A met twee knoopsegmenten, blad B met het middelste knoopsegment en één pen. Meerdere renderprimitieven die bij hetzelfde blad horen delen één fysiek part-ID en dezelfde plaatgebonden beweging.

De buitenste scharnieren liggen op de binnenvlakken van de framepanelen. De knoop ligt daardoor aan de binnenzijde van de plaatvlakken en niet als afstandhouder tussen de plaatranden; de paneelvoeg is 2 mm totaal (de universele 1-mm conceptspeling per zijde). Iedere as heeft een expliciete voorlopige `SheetHinge`-relatie tussen precies twee platen: voorste langspaneel ↔ voorste korte helft, beide korte helften onderling, en achterste korte helft ↔ achterste langspaneel.

De framepanelen lopen niet als één doorlopende rand over de vloer. Hun buitencontour vormt voeten onder iedere verticale stijl. Bij het standaardconcept zijn de voeten 140 mm breed en ligt de onderrand tussen de voeten 30 mm hoger. Daarmee staan de twee langspanelen ieder op drie voeten en ieder kort half vouwpaneel op twee voeten. Deze waarden zijn fotogereconstrueerde conceptmaten en blijven geblokkeerd tot constructieberekening en proefstuk; 3D, nesting en CAM gebruiken wel dezelfde backendcontour.

AXA publiceert voor dit artikel geen bemaatte gatcoördinaten, gatdiameter, verzinkgeometrie, penmaat of lengten van de knoopsegmenten. Het zichtbare boorbeeld is daarom uit de fabrikanttekening gekalibreerd en blijft `ProvisionalRenderEnvelope`. AXA noemt 4,0 × 40 mm als deurschroef, maar die kan niet zonder validatie als bevestiger voor één 18-mm plaat worden overgenomen. Plaatboringen/CAM, definitieve bevestigers en productievrijgave blijven geblokkeerd tot een fysiek sample is ingemeten en een proefverbinding is belast.

Een fysieke proef bepaalt of één scharnier per as voldoende is of dat twee scharnieren per as nodig zijn.

## Interactieve beweging

De onderstelslider loopt van ingeklapt naar uitgeklapt. De backend levert 21 kinematische keyframes per bewegend onderdeel; de UI interpoleert die zonder constructiekennis. De twee korte zijden knikken naar binnen terwijl het achterste vaste langspaneel naar het voorste beweegt. In gevouwen toestand bedraagt de voorlopige afstand tussen de twee langspanelen vier plaatdiktes plus de vastgelegde scharnierruimte.

De bladslider loopt onafhankelijk van geplaatst naar de vastgelegde zweefhoogte. Alleen het losse werkblad beweegt op deze as.

## Open vrijgavepunten

- scharnier-sample inmeten: zes gatcoördinaten, gat-/verzinkdiameters, pen en knoopsegmenten; daarna een door-en-door of aantoonbaar geschikte bevestiger voor 18-mm betonplex selecteren;
- universele gat-/nokspeling met een CNC-proefstuk bevestigen;
- exacte L-vormige eindslotcontour en dogbones valideren;
- constructieve frame-randbreedte, stijfheid en stabiliteit berekenen en beproeven;
- veilige gelijkmatig verdeelde werkbelasting vaststellen;
- risico op onbedoeld uitlichten van het blad toetsen;
- freesrandafwerking en bestaande 1-mm buitencontourafschuining op een proefstuk beoordelen.
