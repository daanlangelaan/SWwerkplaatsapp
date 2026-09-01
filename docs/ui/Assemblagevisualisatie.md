# Assemblagevisualisatie

Status: **actueel contract**.

## Semantische rollen

De renderlaag ontvangt rollen en kiest geen onderdelen op naam:

- `active`: in deze stap te plaatsen of te bewerken;
- `receiver`: ontvangt het actieve onderdeel;
- `target`: eindpositie van een beweging;
- `context`: uitsluitend oriëntatie, visueel teruggenomen;
- `hardware`: uitsluitend hardware die in de huidige stap wordt toegevoegd;
- `tool-axis`: gereedschapsas uit het verbindingspunt.

De kleuren van deze rollen staan in `config/ui/presentation-contract.json`. De
betekenis van een rol staat hier en mag niet door kleur alleen worden overgebracht.

## Rendercontract

Voor iedere profielplaatsing levert de backend minimaal:

- stabiele trace-ID en lokale oriëntatie;
- profielafmetingen;
- sleufassen en zichtbare sleufopeningen;
- kernboringen per kop;
- stickerpositie en herkenningskop.

Voor iedere gebruikte verbinding levert de backend:

- verbindings-ID;
- actief en ontvangend trace-ID;
- kop A/B en kernboringindex;
- lokale en wereldpositie;
- toegangsgat, vlak, richting en diameter;
- bout-, verbinder- en gereedschapsrenderdata;
- status en ontbrekende brondata.

De UI rendert deze objecten rechtstreeks. Zij telt geen gaten, kiest geen
sleufbaan en maakt geen boutmaat uit profielafmetingen.

De vrije 360-view en de orthografische voor-, zij- en onderaanzichten gebruiken
dezelfde Three.js-meshbouwer. Een aanzicht verandert uitsluitend camera en fit;
het bouwt geen tweede 2D-silhouet, componentvorm of materiaalweergave. Maattekst,
kaders en labels mogen als presentatie-overlay boven die gedeelde scene staan.

De concrete backendcontracten zijn:

- `PortalAssemblyPart.ProfileRender` voor langsas, sleufassen, sleufmond,
  sleufkamer, buitenradius en kernboringdiameter;
- `AssemblyInstructionConnectionPoint.HardwareRender` voor verbinder-envelop,
  klemveren, boutschacht, bolkop, inbus en invoerafstand;
- `AssemblyInstructionConnectionPoint` zelf voor de fysieke K1..Kn-node en het
  toegangsgat.

Een rendercontract met status `ProvisionalRenderEnvelope` is uitsluitend een
migratievoorziening voor beeldopbouw. Het bijbehorende `OpenData` moet gevuld
zijn en de waarden mogen niet naar CAM, inkoop of productievrijgave doorlekken.
Zodra leveranciersmaten in masterdata zijn vrijgegeven vervangt de backend deze
envelop; de portalcode verandert daarbij niet.

Voor standaardverbinders is het profielkopvlak de vaste nulreferentie. Positieve
plaat- en kophartafstanden lopen naar buiten; de positieve schachthartafstand en
de insteekweg lopen naar binnen. De portal past uitsluitend deze door het
backendcontract vastgelegde richting toe.

## Stapgedrag

- Voorbereiden toont alleen de profielen en nieuwe hardware van die stap.
- Inschuiven gebruikt de door de backend geleverde bewegingsas en eindpositie.
- Vastzetten hergebruikt het verbindingspunt maar introduceert geen hardware opnieuw.
- Een gegroepeerde stap mag alleen gelijke profielen met gelijke oriëntatie en
  installatie bevatten.
- Ontbrekende renderdata blokkeert de technische visualisatie en geeft een duidelijke
  interne foutmelding; er bestaat geen geometrische fallback.

## Visuele regressiecontrole

Minimaal worden vastgelegd:

- overzicht en detail gebruiken dezelfde trace-ID's en verbindingsnodes;
- ieder bedoeld verbindingspunt is zichtbaar en telbaar;
- geen hardware uit eerdere stappen wordt herhaald;
- gat, gereedschapsas en camera kijken naar hetzelfde backendpunt;
- mobiel, normaal desktop en volledig scherm behouden dezelfde betekenis.
