# Data-eigenaarschap en UI-grens

Status: **actueel architectuurcontract**.

## Doel

Deze software wordt grotendeels door AI-agents ontwikkeld. Daarom moet iedere
waarde één aangewezen eigenaar, één wijzigingsroute en een automatische controle
hebben. Een visueel correct resultaat is geen bewijs dat technische data op de
juiste plaats staat.

## Beslisboom voor iedere waarde

1. Verandert de waarde wanneer een product, materiaal, leverancier, artikel,
   bewerking of norm verandert? Dan is zij technische masterdata.
2. Is de waarde afgeleid uit technische masterdata en nodig om een model, stap of
   render direct op te bouwen? Dan hoort zij in een getypeerd backendcontract.
3. Verandert alleen de uitstraling of bediening terwijl de fysieke betekenis
   gelijk blijft? Dan is zij presentatie.
4. Is de herkomst niet aantoonbaar? Dan is de waarde ontbrekend. Zij wordt niet
   als fallback toegevoegd.

## Eén eigenaar per categorie

| Categorie | Voorbeelden | Wijzigingsbron | Runtimeconsument |
|---|---|---|---|
| Product- en leveranciersdata | stabiele ID, artikel, materiaal, profielserie, bout, moment, prijs | `config/product-master-data.xlsx` | `config/runtime/masterdata-runtime.json` |
| Technische geometrie | profieldoorsnede, sleufkamer, kernboring, hardware-envelope, gat- en tapwaarden | Excel-record via schema en validator | Domain/Application-services |
| Productcontract | keuzes, defaults, grenzen, blokkades | product- en controleregels in Excel | `/api/catalog` en getypeerde application-contracten |
| Assemblagecontract | trace-ID's, verbindingen, rollen, assen, posities, gereedschap | echte assembly en verbindingsgraaf | assemblageplan en rendercontract |
| Presentatie | kleur, typografie, lijndikte, SVG-layout, camera-fit, animatieduur | `config/ui/presentation-contract.json` | Portal-presentatielaag |
| Operationele data | orders, statussen, vrijgave en exports | SQLite en de afgesproken mirror | repositories en workflows |

Excel is de menselijke technische wijzigingsbron. Applicatiecode leest Excel
niet rechtstreeks. De runtimesnapshot is gegenereerd en wordt nooit handmatig
als tweede waarheid onderhouden.

## Absolute regels voor de presentatielaag

`PortalHtml`, CSS, SVG en browser-JavaScript mogen:

- generieke velden, panelen, interacties en rendermeshes maken;
- getypeerde backendwaarden tonen en direct projecteren;
- presentatieconfig lezen voor layout, kleur, camera en timing.

Zij mogen niet:

- een materiaal-, component-, leverancier-, recept-, artikel- of productregel-ID
  bezitten of op zo'n ID technisch gedrag kiezen;
- technische maten, toleranties, momenten, materiaalkeuzes, defaults of grenzen
  als literal bevatten;
- gaten, sleufassen, kernboringen of verbindingstellingen uit bounding boxes,
  namen of zichtbare vormen reconstrueren wanneer backenddata beschikbaar hoort
  te zijn;
- een ontbrekende backendwaarde vervangen door een waarschijnlijk getal;
- technische data uit een labeltekst terugparsen.

Een technische tekst in de UI is alleen toegestaan wanneer de volledige tekst of
de onderliggende waarden uit het backendcontract komen. Menselijke labels mogen
presentatiecode zijn, maar dragen geen productregel.

## Verplichte AI-werkwijze

Een AI-agent die technische data, een productkeuze of rendergeometrie wijzigt,
voert in deze volgorde uit:

1. Classificeer elk gewijzigd veld met de beslisboom hierboven.
2. Zoek het bestaande stabiele ID en de exacte bron. Koppel nooit op naam,
   omschrijving, afbeelding, rijpositie of een gelijkend artikel.
3. Voeg ontbrekende kolommen en records toe aan
   `config/product-master-data.xlsx` volgens `config/master-data-schema.json`.
   Gebruik hiervoor uitsluitend de verplichte spreadsheet-skill en workspace-
   runtime; geen alternatieve workbooklibrary.
4. Voeg bron, artikelcode, revisie, verificatiestatus en wijzigingslog toe.
5. Valideer Excel en genereer `config/runtime/masterdata-runtime.json`.
6. Lees de snapshot in Domain/Application en bouw één getypeerd contract. Laat de
   contractbouw expliciet falen of een geblokkeerde status leveren bij ontbrekende
   velden.
7. Laat de UI alleen dat contract consumeren. Verwijder het oude literal,
   afleidingspad en iedere fallback in dezelfde wijziging.
8. Voeg een regressietest toe die de technische bron, het backendcontract én de
   afwezigheid van de oude UI-waarde controleert.
9. Voer de minimale controles uit `AGENTS.md` uit en controleer relevante renders
   visueel vanuit de echte assemblydata.

Een agent stopt bij stap 2 of 3 wanneer artikelkeuze, bron of spreadsheet-runtime
ontbreekt. Hij mag wel schema, contract, blokkade en documentatie voorbereiden,
maar noemt de envelope niet exact en maakt haar niet vrij voor productie.

## Voorlopige render-enveloppen

`ProvisionalRenderEnvelope` is uitsluitend een tijdelijke backendpresentatiehulp.
Het contract:

- heeft een niet-lege `OpenData`-lijst met concrete ontbrekende bronvelden;
- is gekoppeld aan de echte stabiele materiaal- en component-ID's;
- is zichtbaar als niet-vrijgegeven in interne controle;
- wordt niet gebruikt door CAM, pricing, inkoop, BOM-beslissingen of vrijgave;
- verdwijnt zodra alle vereiste leveranciersvelden zijn gevalideerd.

Een voorlopige envelope in `PortalHtml`, een anoniem getal in JavaScript of een
fallback achter `||` is verboden.

## Regressiecontract

Minimaal moet geautomatiseerd worden gecontroleerd dat:

- `PortalHtml` geen technische renderfallbacks of voorlopige envelopes bevat;
- cataloguskeuzes, defaults en grenzen via `/api/catalog` worden gebruikt;
- profiel- en hardware-renderobjecten hun waarden via backendservices ontvangen;
- `ProvisionalRenderEnvelope` altijd `OpenData` bevat en niet naar productie-
  contracten doorlekt;
- een vrijgegeven leveranciersrecord met ontbrekende verplichte geometrie de
  build of validatie blokkeert;
- een vervangen legacyconsument en zijn fallback werkelijk zijn verwijderd.

Documentatie alleen is geen eindcontrole. Iedere nieuwe technische migratie voegt
een gerichte, machineleesbare regressie toe.

## Beoordelingschecklist

- Kan één bronwijziging zonder wijziging in `PortalHtml` doorstromen naar de UI?
- Heeft ieder technisch getal een stabiel ID, bron en verificatiestatus?
- Is er exact één afleidingspad naar het backendcontract?
- Is ontbrekende data zichtbaar en blokkerend in plaats van aangevuld?
- Zijn oude literals, fallbacks en legacy-ID's verwijderd?
- Slagen validator, runtimecheck, repositorycheck, build en regressietests?

Alle antwoorden moeten ja zijn voordat een technische migratie gereed is.
