# Assemblagehandleiding voor systeemprofielen

Status: **eerste runtimebasis / exacte masterdata nog open**.

Dit document legt de bevestigde montagekennis, de eerste softwarebasis, de ontbrekende masterdata en de gewenste beeldtaal vast. De portal kan al een voorlopige beeldreeks genereren. Productie-export mag pas op deze logica vertrouwen nadat profiel- en verbinderdata machineleesbaar zijn gemaakt en de open verbindingen als bevestigd zijn vrijgegeven.

## Besluit

De handleidingmodule wordt geen verzameling handgeschreven teksten. Zij wordt een planner bovenop een gerichte verbindingsgraaf:

- elk fysiek profiel heeft een stabiele ID, as, doorsnede-oriëntatie, twee benoemde koppen en benoemde groefbanen;
- elke verbinding benoemt zonder perspectiefafhankelijke termen het **getapte kopprofiel** en het doorlopende **sleufprofiel met toegangsgat**;
- hardware wordt uitsluitend gekozen wanneer profielserie, sleuf/groef, raster, systeemtype, kerngat, draad en connectorvariant compatibel zijn;
- de planner bewaakt invoeg-/schuifruimte, vrije sleuteltoegang, paneelgroeven, uitlijning en het moment waarop definitief mag worden aangedraaid;
- de portal toont hoofdzakelijk beelden: één betekenisvolle handeling per stap, nieuwe delen in accentkleur en bestaand werk gedempt.

### Procesgrens

De assemblagehandleiding begint bij een compleet, machinaal voorbereid bouwpakket. Afkorten, zagen, boren en tappen zijn interne profielbewerkingen en worden niet als assemblagestap aan een klant of assemblagemonteur getoond. De bevestigde fysieke voorbereiding blijft wel brondata voor werkvoorbereiding, machine-uitvoer en kwaliteitscontrole.

Ieder fysiek profielstuk krijgt bij werkvoorbereiding één onveranderlijk trace-ID. Datzelfde nummer staat op het profielregister, de zaag-/inkoopregel, iedere boor- of tapbewerking, het 3D-object, de assemblagepositie en de handleiding. Een intern gebouwd product en een extern geleverd bouwpakket gebruiken dezelfde assemblagelogica; alleen de uitvoerende partij en zichtbaarheid van de voorafgaande profielbewerkingen verschillen.

### Vaste profieloriëntatie: kop A, kop B en datumvlak D0

Voor ieder profiel geldt één lokaal, perspectiefonafhankelijk referentiestelsel:

- **kop A** is het nulpunt van de lengte-as (`L=0`); **kop B** ligt op `L=profiellengte`;
- het profiel krijgt precies één volledig trace-ID, niet hetzelfde nummer op beide uiteinden;
- de trace-ID-sticker komt in een vaste vrije labelzone vlak bij kop A en altijd op hetzelfde benoemde **datumvlak D0**; de exacte stickerafstand wordt pas vastgezet met het label- en opspanproces;
- de sticker bevat een pijl naar kop A. Daarmee legt één sticker zowel A/B als de verdraaiing om de lengte-as vast;
- de overige langsvlakken heten, gezien vanaf kop A, `D90`, `D180` en `D270` met één vastgelegde draairichting;
- een gatbewerking wordt opgeslagen als `positie vanaf kop A + datumvlak D0/D90/D180/D270 + baan/offset`, nooit alleen als een wereldrichting of “boven/links”;
- bij een volgende machine-opspanning blijft kop A aan dezelfde lengtestop. Het profiel wordt uitsluitend om zijn lengte-as naar de gevraagde datumhoek gedraaid;
- als een machinegang toch omkeren van A en B vereist, wordt dat expliciet `B aan stop` genoemd en rekent de software de lengtecoördinaat om. Het mag niet onder de algemene instructie “draaien” vallen.

Alleen een sticker “aan de A-zijde” is onvoldoende: die borgt de lengterichting, maar zonder vast sticker-/datumvlak kan een asymmetrisch geboord profiel nog 90° of 180° verkeerd worden gemonteerd. In de assemblageviewer staat daarom één profielnummer bij het profiel. Eén compacte callout `Kop A`, met een dunne leaderlijn naar het werkelijke kopvlak, legt de oriëntatie vast; een extra zichtbaar label voor kop B voegt geen informatie toe en wordt niet getoond.

LEGO is het beste voorbeeld voor stapgranulariteit en visuele verschillen. IKEA is het beste voorbeeld voor fysieke oriëntatie, waarschuwingen en menselijke handelingen. Voor SWWerkplaats is een combinatie beter dan het letterlijk kopiëren van één van beide.

## Bevestigde werking van de standaardverbinder

### Bewijs

De volgende feiten zijn bevestigd door de aangeleverde foto's en toelichting van de gebruiker op 2026-08-22:

1. Tijdens de interne profielbewerking wordt het kerngat in de kopse kant van het kopprofiel geboord/getapt.
2. De standaardverbinder wordt met een bolkopschroef los op die profielkop voorgemonteerd.
3. Het kopprofiel wordt langs/in de groef van het ontvangstprofiel naar de verbindingspositie geschoven.
4. De boutas wordt met het montagegat in het ontvangstprofiel uitgelijnd.
5. Een inbussleutel gaat door dat montagegat en draait de bout vast.

De foto met `ISB standaard verbinder set sleuf 5` bevestigt de vormfamilie, maar **niet** dat sleuf 5 bij de huidige producten hoort. De foto is daarom geen artikel- of compatibiliteitsbewijs voor een concreet project.

De leveranciersinformatie bevestigt hetzelfde principe voor de serie-8 centreerplaat `S208ZP / TIN 100342`: één profiel krijgt M8-draad in het kerngat; het andere profiel krijgt een Ø7-montagegat. Dit artikel is alleen compatibel met serie 8, raster 40, I-type en mag niet als serie-10 onderdeel worden toegewezen. Zie [TechXXL TIN 100342](https://www.techxxl.nl/part-100342.html).

De leveranciersmaat en het gecontroleerde STEP-model leggen daarnaast de volledige renderenvelop vast. De nominale plaat is 2,2 mm dik. De twee klauwen steken vanaf het profielkopvlak 8,0 mm naar binnen, hebben elk een afzonderlijke envelop van 13×8 mm en liggen 22 mm hart-op-hart. Met het profielkopvlak als nulpunt ligt het plaatcentrum 1,1 mm naar buiten. In combinatie met de vrijgegeven M8×25-bout liggen het schachtcentrum 10,3 mm naar binnen en het kopcentrum 4,2 mm naar buiten. Deze richtingsbetekenis hoort bij het backendcontract en wordt niet opnieuw in de portal afgeleid.

De oorspronkelijke item-variant gebruikt eveneens een getapte profielkop, een montagegat in het tweede profiel en een inbussleutel door dat gat. De variant bepaalt onder meer hoeveel groeven de centreerplaat blokkeert. Zie [item Standard-Fastening Set 8, one-sided](https://www.item24.com/en-nl/standard-fastening-set-8-one-sided-67299).

### Eén verbinding als toestandsreeks

| Toestand | Kopprofiel | Sleufprofiel | Verbinder | Toegestane vervolgstap |
|---|---|---|---|---|
| voorbereid | kop op maat; kerngat en draad gereed | montagegat op boutas gereed | juiste set aanwezig | voormonteren |
| voorgemonteerd | connector aan juiste kop, bout nog los | vrij en bereikbaar | centreerneuzen correct gericht | inschuiven |
| gepositioneerd | eindvlak op bedoeld aansluitvlak | juiste groefbaan bezet | boutas achter montagegat | licht aantrekken |
| uitgelijnd | buitenvlakken/haaksheid correct | sleutelpad nog vrij | verbinding licht geklemd | eindcontrole |
| vastgezet | positie bevroren | montagegat gecontroleerd | voorgeschreven moment bereikt | markeren/afwerken |

`Voorgemonteerd`, `gepositioneerd` en `vastgezet` zijn verschillende statussen. Alleen “verbonden” opslaan is onvoldoende om een montagevolgorde te plannen.

## Waarom de volgorde constructief belangrijk is

Een stap is pas uitvoerbaar wanneer alle volgende voorwaarden waar zijn:

1. **Invoegpad vrij:** het kopprofiel kan langs zijn geplande invoegvector bewegen zonder een bestaand profiel, plaat of beslag te raken.
2. **Juiste groef vrij:** de gekozen groefbaan van het ontvangstprofiel is nog niet geblokkeerd door een paneel, afdekstrip, andere verbinder of eindkap.
3. **Sleutelpad vrij:** een rechte gereedschapsbaan vanaf het montagegat naar de boutkop blijft beschikbaar tot het definitieve moment is bereikt.
4. **Sluitstuk gekozen:** een gesloten frame heeft bewust één of meer laat te plaatsen leden. De planner mag geen lus sluiten waardoor een resterend onderdeel niet meer kan worden ingeschoven.
5. **Paneelvolgorde klopt:** een paneel dat in een groef opgesloten wordt, gaat vóór het sluitprofiel in het open U-frame. Een eenzijdige/K-variant kan nodig zijn om de paneelgroef vrij te houden.
6. **Uitlijnen vóór eindmoment:** herhaalverbindingen worden eerst licht aangetrokken; vlakheid, maat en diagonalen worden gecontroleerd voordat de momentgroep definitief wordt vastgezet.
7. **Afwerking als laatste:** eindkappen en afdekprofielen worden pas geplaatst nadat toegang, moment en visuele controle klaar zijn.

item beschrijft voor een profieldeur exact het patroon `U-vorm bouwen → paneel inschuiven → frame sluiten`; de eenzijdige antitorsieplaat moet van het paneel af wijzen. Zie [Swing door 8 40x40 – Notes on Use and Installation](https://cdn.item24.com/product-assets/DOK_MONT_Schwenktuer-8-40x40_%23SEN_%23AIN_%23V1.pdf). Een tweede item-handleiding adviseert connectoren vooraf te positioneren, daarna uit te lijnen en pas als laatste alles vast te zetten. Zie [Access Door – Notes on Use and Installation](https://cdn.item24.com/product-assets/DOK_MONT_Durchgangstuer-8_%23SALL_%23AIN_%23V3.pdf).

## Verplicht profiel- en hardwarecontract

Afmetingen alleen zijn niet genoeg. Twee profielen van 40x40 mm kunnen verschillende groeven, rasters, kerngaten en compatibele verbindingsfamilies hebben.

### Profielsysteemrecord

Minimaal vereist:

```json
{
  "profile_system_id": "techxxl-series-10-b-grid40",
  "supplier_id": "SUP-TECHXXL",
  "series": "10",
  "slot_nominal_mm": 10,
  "grid_mm": 40,
  "system_type": "B",
  "core_hole_mm": null,
  "allowed_end_threads": ["M8"],
  "grooves": [
    { "face": "Y-", "lane": 0, "state": "open" }
  ],
  "verification_status": "supplier-data"
}
```

`grooves` moet per werkelijk profielartikel worden gevuld. Een gesloten vlak, 1N/2N-profiel of afwijkende moduulbaan verandert de toegestane hardware en montagekant.

### Verbinderrecord

Minimaal vereist:

```json
{
  "connector_id": "supplier-stable-id",
  "fastener_standard_id": "standard-profile-connector-m8",
  "connection_kind": "standard-end-to-slot-90",
  "compatible_profile_system_ids": ["exact-system-id"],
  "tapped_end_thread": "M8",
  "slot_profile_nominal_mm": 10,
  "anti_rotation_noses": "both",
  "blocked_receiver_lanes": [0],
  "bolt_component_id": "stable-bolt-id",
  "hex_key_across_flats_mm": 5,
  "tool_passage_clearance_mm": 0.5,
  "drill_increment_mm": 1,
  "access_hole_calculation": "ceil((SW/cos(30deg))+clearance, drill_increment)",
  "torque_nm": null,
  "source_url": "supplier-page",
  "verification_status": "open"
}
```

Een onbekend veld dat de passing, sterkte of montage bepaalt blijft `null/open` en blokkeert automatische toewijzing. Het krijgt geen gegokte default.

### Gericht verbindingsrecord

Minimaal vereist:

```json
{
  "connection_id": "J-001",
  "workflow_id": "standard-connector-v1",
  "tapped_profile_id": "P-012",
  "tapped_end": "B",
  "slot_profile_id": "P-004",
  "slot_face": "X+",
  "slot_lane": 0,
  "connector_id": "supplier-stable-id",
  "insertion_vector": [0, -1, 0],
  "access_origin_mm": [120, 640, 0],
  "access_axis": [1, 0, 0],
  "tightening_group": "frame-square-01",
  "state": "planned"
}
```

De gerichte rollen mogen niet uit perspectiefwoorden als `bron`, `ontvanger` of naamconventies als `voor/achter` worden geraden. Het kopprofiel eindigt op de verbinding en krijgt de getapte kop. Het sleufprofiel loopt door, bevat de T-sleuf en krijgt het toegangsgat. Beide worden uit expliciete contactvlakken en profielassen opgebouwd en tegen de assemblygeometrie gecontroleerd.

De volgorde `inbuskop → standaardverbinder in T-sleuf → bout → getapte kop van kopprofiel` en de montageacties zijn universeel voor deze verbindingsfamilie. De gatdiameter is dat niet: eerst wordt de concrete bout gekozen (standaard M8, tenzij een expliciete bevestigingsstandaard afwijkt), daarna volgt de bijbehorende inbusmaat en pas daaruit de afgeronde gereedschapsdoorgang. Met de voorlopige standaard `M8 / SW5 / 0,5 mm speling / boorstap 1 mm` resulteert dit in Ø7; SW6 resulteert volgens dezelfde regel in Ø8.

## Plannerlogica

De eerste versie kan deterministisch blijven en heeft geen AI nodig.

1. Valideer alle profielartikelen en los per verbinding exact één compatibele verbinderset op.
2. Bouw een contactgraaf uit `AssemblyPlacements`, maar gebruik expliciete verbindingsrecords als waarheid voor getapte kop, sleufvlak en groefbaan.
3. Maak voorbereidende stappen voor zagen, kopbewerking, montagegat en hardware-kitting.
4. Maak per verbinding de acties `preassemble`, `insert/slide`, `align`, `snug`, `torque` en `inspect`.
5. Voeg afhankelijkheden toe voor botsingsvrij invoegen, vrije groeven, vrije sleutelbanen, panelen en sluitprofielen.
6. Kies per gesloten lus een sluitprofiel waarvoor alle resterende invoeg- en sleutelbanen haalbaar blijven.
7. Groepeer identieke, gelijktijdig bereikbare herhalingen alleen als de afbeelding ondubbelzinnig `xN` kan tonen.
8. Plan maat-/diagonaalcontrole vóór de eerste definitieve momentgroep.
9. Plan eindkappen en afdekkingen na moment- en inspectiestappen.
10. Weiger een plan wanneer een verbinding geen botsingsvrij invoegpad of vrij sleutelpad heeft; genereer nooit stilzwijgend een onuitvoerbare volgorde.

Een topologische sortering van deze afhankelijkheden levert de stapvolgorde. Bij meerdere geldige keuzes wint in deze volgorde: stabiele basis eerst, minst hanteren van zware subassemblies, minste camerawissels, identieke handelingen groeperen en zo laat mogelijk een frame sluiten.

## Gewenste UI/UX: LEGO x IKEA voor de werkplaats

### Technische focusviewer

De interface toont geen rij losse kaarten. Iedere stap gebruikt één rustige, vaste compositie die als een technische bouwplaat voelt:

- boven: fase en voortgang, bijvoorbeeld `Frame · 07/24`;
- midden: één groot technisch beeld of vergrendelde 3D-camera, ongeveer 70% van het scherm;
- linksboven in het beeld: alleen de nieuwe onderdelen en aantallen;
- in het beeld: invoegpijl, boutas, montagegat en een vergrotingsdetail wanneer de verbinding klein is;
- ernaast of eronder: een vaste feitenkolom voor onderdelen, gereedschap en maat/moment;
- onder: grote knoppen `Vorige` en `Volgende` met een zichtbare voortgangslijn;
- optioneel achter `Details`: artikelnummer, toleranties, bron en tekstalternatief.

De hoogwaardige uitstraling komt uit technische precisie, niet uit decoratie: veel witruimte, dunne maatlijnen, consistente isometrie, echte profielcontouren en maximaal één accentkleur voor de actieve hardware. Schaduwen zijn subtiel en functioneel; verlopen, grote marketingkaarten en decoratieve pictogrammen worden vermeden. Op telefoon krijgt het technische beeld een eigen passend camerakader in plaats van een verkleinde desktopplaat.

### Visuele grammatica

| Betekenis | Weergave |
|---|---|
| al gemonteerd | lichtgrijs, volledige contour |
| nu te plaatsen | vaste accentkleur plus dikke contour |
| bewegingsrichting | brede pijl met begin- en eindpositie |
| ontvangstgroef | contrasterende contour/arcering; niet alleen kleur |
| sleutelpad | gestippelde rechte as van gereedschap naar bout |
| nog niet vastzetten | open momentsymbool/handvast-pictogram |
| definitief moment | sleutelboog met expliciete Nm-waarde |
| verboden oriëntatie | klein doorgestreept alternatief naast de juiste situatie |
| controle | meetlint/winkelhaak/diagonaalpijl met doelmaat |

LEGO-handleidingen tonen per stap de toe te voegen delen apart en gebruiken pijlen om de verandering zichtbaar te maken; zie een [officiële LEGO-bouwhandleiding](https://www.lego.com/cdn/product-assets/product.bi.core.pdf/6541113.pdf). Onderzoek naar picturale handleidingen waarschuwt juist voor cognitieve overbelasting, onnauwkeurige kleuren en complexe grafische syntaxis. Daarom krijgt één stap maximaal één precisiehandeling of één exact identieke herhaalgroep. Zie [Martin, Toward More Usable Pictorial Assembly Instructions](https://journals.sagepub.com/doi/10.1177/154193120705101706).

IKEA-achtige beelden zijn sterk in handoriëntatie, omdraaien, gereedschapsgebruik en fout/goed-vergelijkingen. De onderliggende software moet echter geen losse illustraties opslaan: onderzoek naar IKEA-handleidingen modelleert de stappen als een boom van onderdelen en verbindingen. Dat sluit aan op de voorgestelde verbindingsgraaf. Zie [IKEA-Manual: Seeing Shape Assembly Step by Step](https://cs.stanford.edu/~rcwang/projects/ikea_manual/).

item combineert dit met de industriële beeldtaal die hier inhoudelijk het dichtst bij ligt: grote lijnillustraties, een exploded view voor de verbindingslagen, afzonderlijke maat-/boorbeelden en het gereedschap met het aanhaalmoment direct bij de bout. De SWWerkplaatsviewer neemt die informatiehiërarchie over, maar gebruikt eigen gegenereerde geometrie en illustraties. Zie [item Standard-Fastening Set 8](https://www.item24.com/en-gb/standard-fastening-set-8-bright-zinc-plated-2607) en de [item Base Cart MiR250 montagehandleiding](https://cdn.item24.com/product-assets/DOK_MONT_Base-Cart-MIR_%23SEN_%23AIN_%23V1.pdf).

### Minimale tekst, niet tekstloos

Een goede stap kan meestal met één werkwoordregel:

`Schuif P12 tot de bout achter gat J-04 staat.`

De rest wordt als labels getoond: `P12`, `sleuf 10`, `SW5`, `20 Nm`, `x2`. Tekst blijft verplicht voor veiligheidswaarschuwingen, onbekende/open verificatiestatus en als toegankelijk alternatief voor de afbeelding.

## Eerste softwarebasis (augustus 2026)

De eerste herbruikbare doorsnede is aanwezig:

- `WorkbenchModel.AssemblyConnections` legt gerichte kop-/sleufrollen en stabiele verbinding-ID's vast;
- de machinebasis registreert de hoofdframe- en tussenliggerverbindingen met getapte kop A/B en benoemd sleufprofiel;
- `AssemblyInstructionPlanningService` groepeert deze verbindingen deterministisch in vijf assemblagebeelden; de voorafgaande boor-/tapbewerking blijft buiten de assemblagehandleiding;
- herhaling is een optionele plannerkeuze en wordt alleen toegepast op equivalente profielen: getapt en ontvangend profiel hebben dezelfde numerieke buitenmaten, langsas en doorsnede-oriëntatie, terwijl kop A/B, montagerichting, sleufvlak/-baan, toegangsvlak, verbinder, boutstandaard, gereedschap, gatmaat/-afstand en moment eveneens gelijk zijn; bij ontbrekende plaatsingsgeometrie wordt nooit gegroepeerd;
- dezelfde planner kan met groepering uitgeschakeld één verbinding per vijfdelige handelingenreeks leveren; hiermee kan de klantweergave later wisselen tussen een compacte herhaalstap en individuele profielstappen zonder een tweede instructiemodel;
- ieder fysiek profielstuk krijgt centraal een productspecifiek trace-ID (`MB-Pnnn`, `RC-Pnnn`, enzovoort) dat in profielregister, bewerkingsregels, 3D-plaatsing en assemblagestap wordt hergebruikt; meervoudige orders krijgen bovendien een unitcode;
- de offerte-API levert het plan mee en de webportal toont voor klanten standaard een vast 3D-montageoverzicht plus een sterk ingezoomde technische detailviewer met één handeling per beeld, herhaalaantallen, compacte profielnummers, onderdelen, gereedschap, echte controlewaarden en stapnavigatie;
- het montageoverzicht markeert alle bedoelde verbindingen genummerd, onderschept op telefoon geen scrollbeweging en wordt alleen via `Open 3D` roteerbaar en zoombaar; de bedieningshint wordt eenmaal getoond;
- het montageoverzicht blijft tijdens stapwissels in de door de gebruiker gekozen toestand; alleen de expliciete knop `Inklappen` of `Toon positie` wijzigt die toestand;
- de assemblage-assistent heeft een eigen volledig-schermmodus die de bestaande overzichts- en detailviewer hergebruikt, bij stapwissels actief blijft en zonder verlies van stapstatus kan worden gesloten;
- een voorbereidingsstap toont in het montageoverzicht uitsluitend de fysiek te pakken en voor te monteren profielen; ontvangende profielen en eindposities verschijnen pas vanaf de positioneer- of montagestap;
- ieder fysiek profiel dat in de actuele handeling wordt geplaatst of ontvangt krijgt precies één trace-ID en één `Kop A`-callout; een transparante eindpositie-kopie krijgt geen dubbel label;
- de visuele grammatica is vastgelegd als blauw met donkere contour voor het te plaatsen profiel, grijs voor ontvangende profielen, koperrood voor bout/verbinder, blauw voor beweging en een zwarte stippellijn voor de gereedschapsas;
- alle zichtbare delen van de standaardverbinder, inclusief clip/plaat, boutschacht en bolkop, gebruiken dezelfde koperrode hardwarekleur; hun stand volgt de lokale profielas, sleufas en geselecteerde sleufbaan;
- de 3D-profielweergave verdeelt sleufbanen op het 40-mm moduulraster van het werkelijke buitenformaat: 40 mm geeft één middenbaan, 80 mm `-20/+20`, 120 mm `-40/0/+40` en 160 mm `-60/-20/+20/+60`; dit wordt op alle vier langsvlakken volgens de numerieke profielas toegepast;
- zichtbare profielkoppen krijgen per 40×40 module een kerngat/celindicatie en sleufopeningen aan de bijbehorende buitenranden; dit is een generieke systeemprofielweergave en geen vrijgave van de nog ontbrekende exacte serie-10 binnencontour;
- toegangsgaten worden als donkere vlakke boring in het profielvlak weergegeven en nooit meer als koperkleurige torus; koperrood blijft uitsluitend gereserveerd voor de verbinder en bout;
- voormontage, inschuiven en aandraaien gebruiken verschillende detailcomposities: de eerste toont alleen profielkop plus exploded hardware, terwijl positie- en momentstappen het ontvangende profiel, gat en de exact uitgelijnde gereedschapsas tonen;
- klantinstructies tonen geen ontbrekende productiegegevens of interne reviewstatus; zolang vrijgave ontbreekt verschijnt uitsluitend de compacte badge `Concept`;
- voorlopige sleuf-, artikel-, gereedschap-, gat-as- en momentdata blijft zichtbaar in `MissingData` en houdt `CanReleaseForProduction` op `false`;
- regressietests bewaken richting, bestaande profielen, unieke ID's, de fysieke laagvolgorde, de afleiding SW5→Ø7 en SW6→Ø8, en de stapvolgorde.

Dit is nog geen productiehandleiding. De bestaande zwarte toegangsgaten in de 3D-machinebasis staan nog op de liggergeometrie en moeten na exacte profiel-/sleufkoppeling naar het werkelijke ontvangstprofiel en de juiste gereedschapsas worden gemigreerd. Ook kast- en frontbeschermingsverbindingen zijn nog niet als gerichte records opgenomen.

### Scheiding klant en intern

De huidige portal implementeert alleen de klantinstructie. Een latere interne assemblageweergave hergebruikt dezelfde globale structuur (`AssemblyInstructionPlan`, stappen, trace-ID's, verbinding-ID's en 3D-plaatsingen), maar krijgt een eigen presentatie en autorisatie. Interne ontbrekende productiegegevens blijven daarom wel in het plan beschikbaar, maar worden niet via een klant-/internschakelaar in de klantinterface ontsloten. De inhoudelijke interne workflow, vrijgavebediening en registratie van controles worden in een aparte iteratie ontworpen.

Kleur mag nooit de enige informatiedrager zijn. Controls krijgen minimaal 24x24 CSS-pixels volgens WCAG 2.2 AA; voor deze sequentiële werkplaatsinterface wordt 44x44 als ontwerpminimum gebruikt. Zie [WCAG 2.2](https://www.w3.org/TR/WCAG22/) en [Target Size (Enhanced)](https://www.w3.org/WAI/WCAG22/Understanding/target-size-enhanced).

### Gedrag op telefoon, tablet en print

- Telefoon: één stap per scherm, vaste onderbalk, automatisch passend camerastandpunt.
- Tablet: beeld links, onderdelen/gereedschap rechts, zonder extra procesinformatie.
- Desktop: zelfde stapkaart met optionele 3D-rotatie en verbindingsinspectie.
- Print/PDF: twee tot vier stapkaarten per pagina, dezelfde stap-ID's en QR-link naar de interactieve stap.
- Bij 3D-rotatie blijft een knop `Herstel aanzicht` altijd zichtbaar; de standaardcamera toont de werkelijke benaderingszijde van de monteur.

## Vergelijking met de huidige code

### Bruikbare basis

- `WorkbenchModel.AssemblyPlacements` bevat de eindgeometrie die voor beelden en botsingscontrole kan worden hergebruikt.
- `ProfileOperation` kent al zagen, boren, tappen en een numerieke volgorde.
- `PortalAssembly3DService` kan profiel- en hardwareplaatsingen als afzonderlijke 3D-objecten tonen.
- De masterdata kent voor enkele profielartikelen serie, groef en I-/B-type in tekstvorm.

### Blokkerende verschillen

1. `Material` bevat alleen afmetingen en geen valideerbare serie, sleuf, raster, type, kerngat of groefbanen.
2. `AssemblyPlacement` heeft nu een stabiele member-ID, maar nog geen profielkoppen, benoemde vlakken, groefbanen, invoegvector of sleutelpad.
3. `MachineBaseEngine` voegt M8-tap en Ø7-boring toe aan hetzelfde liggerrecord. Fysiek horen deze bewerkingen bij twee verschillende rollen.
4. De zwarte montagegaten worden bij frame- en tussenliggers in de liggergeometrie geplaatst; voor de beschreven standaardverbinding hoort het montagegat in het ontvangstprofiel op de werkelijke boutas.
5. De machinebasis gebruikt de exacte serie-8 artikelen `S208ZP/TIN 100342` en `S208HS825/TIN 100673`; plaatdikte, afzonderlijke klauwenvelop, montagehartposities en insteekweg zijn volledig vastgelegd. De verbinding blijft als geheel voorlopig zolang moment, sleufbaan en toegangsgatpositie niet vrijgegeven zijn.
6. `CON_ALU_END_THREAD` en `CON_ALU_CORNER` in de masterdata staan nog op `Open` en beschrijven geen gerichte kop-/sleufrollen.
7. De eerste planner en verbinding-ID's bestaan, maar BOM, profielbewerking en 3D-gat gebruiken die ID nog niet als gezamenlijke waarheid; momentgroepen ontbreken nog.

Daarom kan de huidige software al een betrouwbare volgordepreview tonen, maar nog geen productie-vrijgegeven assemblagehandleiding genereren.

## Implementatievolgorde

1. Voeg eerst profielserie- en groefdata toe aan de menselijke masterdatabron; valideer stabiele ID's en genereer de runtime-snapshot.
2. Registreer de werkelijk gebruikte standaardverbinders per sleuftype, inclusief bout, montagegat, gereedschap, moment, neuzen en verificatiestatus.
3. Voeg gerichte `AssemblyConnection`-records toe en migreer één product als proef, bij voorkeur `machinebasis`.
4. Corrigeer daarbij de bewerkingsrollen en 3D-locatie van montagegaten; behoud oude output niet als fallback wanneer alle verwijzingen aantoonbaar zijn gemigreerd.
5. Voeg compatibiliteits- en bereikbaarheidstests toe vóórdat automatisch hardware wordt gekozen.
6. Bouw een pure planner in Domain/Application en houd stapweergave uit de engines.
7. Render eerst statische stapkaarten uit dezelfde data; voeg daarna pas interactieve 3D, voortgang en fotoverificatie toe.

## Acceptatiecriteria voor de eerste proef

- Elk profiel en iedere fysieke verbinding heeft een stabiele unieke ID.
- Voor ieder knooppunt zijn getapte kop, sleufprofiel, sleufvlak, groefbaan en sleutelpad expliciet.
- Een sleuf-5, sleuf-8 of sleuf-10 verbinder kan nooit aan een ander sleuftype worden toegewezen.
- I-/B-type en raster worden gevalideerd naast de nominale sleufmaat.
- De planner blokkeert een gesloten lus zonder geldig sluitprofiel.
- Een paneelgroef kan niet vóór de benodigde connector-/paneelstap worden geblokkeerd.
- Iedere bout is bereikbaar tot zijn definitieve momentstap.
- BOM, profielbewerkingen, 3D-weergave en handleiding verwijzen naar dezelfde `connection_id` en `connector_id`.
- Een gegenereerde handleiding is op telefoon uitvoerbaar zonder zoomen om een control te bedienen.
- Een monteur die het product niet kent kan de proefassemblage zonder mondelinge aanvulling voltooien; fouten, terugstappen en zoektijd worden tijdens de proef geregistreerd.
