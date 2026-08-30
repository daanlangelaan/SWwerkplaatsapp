# Productbreed portal-designsysteem

Status: **actueel presentatie- en interactiecontract**.

## Doel en reikwijdte

Dit contract is verplicht voor de interne portal, de toekomstige klantportal,
de configurator, projectdossiers, werkplaatswachtrijen, inkoop, voorraadbeheer,
assemblagebediening, beheerpagina's en iedere afzonderlijke productwebsite.

De huidige Workstation-klantbijlage blijft de visuele familiestandaard: rustige
typografie, veel functionele witruimte, een beperkt kleurenpalet, heldere
uitlijning en één communicatiedoel per vlak. De portal neemt die stijl over, maar
niet de paginavolgorde of documentlayout. Een operationeel scherm wordt ingericht
voor beslissen, vergelijken, invoeren en voortgang bewaken.

Exacte kleuren, typografie, spacing, radii, layoutmaten, bedieningsmaten,
breakpoints, schaduwen en bewegingstijden staan uitsluitend in
`config/ui/presentation-contract.json`. HTML en browser-JavaScript gebruiken
geen tweede set literals of fallbacks. Product- en fabricagedata blijven onder
het contract `../architecture/Data-eigenaarschap-en-UI-grens.md` vallen.
De verdeling tussen meerdere productwebsites en de gedeelde backend staat in
`../architecture/Productwebsites-en-gedeelde-backend.md`.

## Productwebsites en themavarianten

Alle productwebsites gebruiken dezelfde componenten, typografieschaal, spacing,
bedieningsmaten, focusregels, foutpatronen en toegankelijkheidsgrens. Een
sitepresentatieprofiel mag naam, logo, accentkleur, klantbeelden en publieke
marketinginhoud kiezen. Het mag geen productregel of nieuwe componentvariant met
afwijkend gedrag introduceren.

De backend levert de actuele `SiteContext` op basis van de gevalideerde host. De
UI leest de sitescope en het presentatieprofiel uit dat contract en bevat geen
hardcoded lijst van domeinen of Product-ID's. De globale ontwerptokens blijven de
basis; een site-overlay mag alleen vooraf aangewezen merksleutels vervangen.

Een offerte of klantdocument gebruikt een vastgelegde sitesnapshot. Een
sitewijziging verandert geen reeds verstuurd document en geen technische data.

## Ontwerpprincipes

1. **Begin bij de taak.** Iedere pagina heeft één primaire gebruikersvraag en
   maximaal één dominante primaire actie.
2. **Maak de toestand eerlijk zichtbaar.** Concept, ontbrekende data, blokkade,
   vrijgave, wachttijd en fout worden expliciet benoemd; een loader of lege tabel
   mag een onbekende toestand niet verhullen.
3. **Toon informatie op het moment dat zij nodig is.** Klanttaal, inkoopdetail,
   CAM-data en beheerinformatie worden niet op hetzelfde scherm gestapeld.
4. **Behoud context.** Projectnummer, klant, revisie en actuele status blijven
   herkenbaar bij wisselen tussen projectweergaven.
5. **Voorkom herstelwerk.** Financiële, vrijgave-, bestel- en productieacties
   tonen hun gevolg en vragen bevestiging wanneer het gevolg niet eenvoudig
   terug te draaien is.
6. **Toegankelijkheid is de ondergrens.** De volledige taakflow voldoet aan WCAG
   2.2 AA; losse componenten of alleen de startpagina zijn niet voldoende.
7. **Kleur is nooit de enige betekenisdrager.** Iedere status heeft tekst en
   waar nuttig een pictogram, patroon of vorm.
8. **Gebruik echte backendcontracten.** Rollen, rechten, statussen, tellingen,
   blokkades en toegestane acties komen uit de backend en worden niet uit labels
   of zichtbaarheid in de UI afgeleid.

## Visuele familie en informatiedichtheid

Alle portalonderdelen gebruiken dezelfde ontwerpwaarden en componenten. De
informatiedichtheid verschilt uitsluitend door de taak:

| Context | Dichtheid | Richting |
|---|---|---|
| Klantportal | rustig | merkneutraal, voordeelgericht, alleen gepubliceerde informatie |
| Configurator en project | standaard | invoer, visuele controle en beslissingen in duidelijke secties |
| Inkoop en voorraad | compact | vergelijkbare regels, aantallen, leveranciers en tekorten in tabellen |
| Werkplaats | scanbaar | grote status, eerstvolgende handeling en bediening met werkhandschoenmarge |
| Beheer | compact | volledige bronstatus, IDs, auditinformatie en gerichte waarschuwingen |

Compact betekent minder verticale ruimte, nooit kleinere leesbare tekst,
onzichtbare labels of kleinere primaire aanraakdoelen.

## Typografie en tekst

- Gebruik de centrale systeemfont-stack zodat de portal snel en herkenbaar op
  Windows, macOS en mobiel rendert.
- Gebruik `display` alleen voor een start- of lege toestand, `pageTitle` exact
  één keer als paginatitel en `sectionTitle` voor hoofdsecties.
- Lopende tekst gebruikt `body`; tabellen en metadata mogen `bodySmall` gebruiken.
  `caption` is uitsluitend aanvullende informatie en nooit de enige weergave van
  een verplicht veld, fout of status.
- Lopende tekst blijft waar mogelijk binnen `readingMaxWidth`.
- Gebruik gewone Nederlandse zinnen. Knoppen beginnen met een werkwoord:
  `Offerte versturen`, `Taak starten`, `Ontvangst boeken`.
- Zet geen hele labels in hoofdletters. Interne ID's, artikelnummers en eenheden
  behouden hun exacte schrijfwijze.
- Klantweergaven volgen daarnaast de commerciële taalfilter uit
  `../klantbijlage-designstandaard.md`.

## Ruimte, raster en oppervlakken

- Alle marges en tussenruimten gebruiken de centrale spacingschaal. Willekeurige
  tussenwaarden zijn niet toegestaan.
- De primaire pagina staat binnen `contentMaxWidth`; tekstformulieren blijven
  binnen `formMaxWidth`.
- Gebruik een paneel alleen voor een werkelijk afzonderlijke taak of statusgroep.
  Geneste kaarten, decoratieve dashboards en een kaart om iedere losse waarde
  zijn verboden.
- Scheid tabellen met uitlijning en regels, niet met een kaart per rij.
- Schaduwen zijn terughoudend en hebben functionele betekenis: `panel` voor een
  zelfstandig oppervlak en `floating` voor een tijdelijk bovenliggend vlak.
- Op compact formaat wordt de lees- en taakvolgorde één kolom. Geen horizontale
  pagina-scroll; technisch canvas en brede tabellen krijgen een expliciete eigen
  zoom-, scroll- of detailweergave.

## Navigatie

- Hoofdonderdelen zoals Configurator, Projecten, Werkplaats en Inkoop zijn echte
  routes. Zij zijn direct te openen, te vernieuwen en te bookmarken.
- De eerste navigatieactie gebruikt het door de backend geleverde rollabel en
  opent de rolstart: `Overzicht`, `Offertes & projecten`, `Productieplanning`,
  `Inkoopplanning`, `Productieoverzicht`, `Mijn wachtrij` of `Mijn orders`. Rol-, site- en
  organisatiekeuze zijn testcontext en geen gelijkwaardige hoofdnavigatie.
- Vaste hoofdnavigatie gebruikt taakgerichte termen: `Product configureren`,
  `Projecten`, `Productie`, `Inkoop` en `Voorraad`.
- De testsimulator staat in een apart, herkenbaar `Testmodus`-menu. Rolwissels
  worden direct toegepast. Klantorganisatie wordt alleen bij de klantrol
  gevraagd en productsite staat als afzonderlijke geavanceerde testcontext.
- De actieve route is zowel visueel als semantisch herkenbaar.
- Tabs zijn alleen voor nauw verwante informatie binnen één pagina wanneer de
  gebruiker niet alles tegelijk hoeft te zien. Tabs zijn geen vervanging voor de
  hoofdroutering en verbergen geen verplichte stapvolgorde.
- Het productieoverzicht toont alle openstaande taken en afzonderlijke
  wachtrijlinks met aantallen. Een wachtrijroute filtert één backend-geleverd
  werkgebied; een specialistische machinecontext toont uitsluitend die route.
- Een lineaire voortgangsindicator wordt alleen gebruikt voor minstens drie
  stabiele hoofdstappen. Niet-lineaire project- of productiestatussen gebruiken
  een statusoverzicht of takenlijst.
- De terugactie brengt de gebruiker naar de vorige logische context en niet
  standaard naar de portaalstart.

## Rollen en zichtbaarheid

- Iedere pagina en actie heeft een benoemde capability, bijvoorbeeld
  `quote.send`, `engineering.release`, `stock.correct` of `workshop.complete`.
- De backend levert de actuele actor, rollen en capabilities. De UI toont alleen
  passende bediening, maar de backend weigert dezelfde verboden actie ook.
- De ontwikkelmodus mag een testrol simuleren via hetzelfde actorcontract. Deze
  simulatie is expliciet gemarkeerd, gelogd en technisch uitschakelbaar.
- `Bekijk als klant` gebruikt het echte klantcontract. Intern verborgen velden
  mogen niet alsnog in de response of paginabron aanwezig zijn.

## Formulieren en configuratie

- Labels staan blijvend bij het veld; placeholders zijn alleen voorbeelden.
- Vraag alleen informatie die voor de actuele stap nodig is. Reeds bekende
  project- en klantgegevens worden niet opnieuw gevraagd.
- Eenheid, formaat, bron en gevolg worden vóór invoer duidelijk gemaakt.
- Validatie gebeurt bij het veld en bij verzenden in een foutoverzicht. Het
  foutoverzicht krijgt focus, linkt naar ieder foutveld en gebruikt dezelfde
  fouttekst als naast het veld.
- Fouten beschrijven wat mis is en hoe het kan worden opgelost. Alleen een rode
  rand of `Ongeldig` is onvoldoende.
- Een wijziging met financiële, technische of productiegevolgen toont welke
  berekeningen en vrijgaven verouderd raken.
- Automatisch opslaan toont een tekstuele status met tijdstip. `Opgeslagen` mag
  pas verschijnen nadat de backend de wijziging heeft bevestigd.

## Tabellen, wachtrijen en vergelijken

- Gebruik tabellen wanneer gebruikers waarden tussen rijen of kolommen moeten
  vergelijken, zoals BOM, voorraad, inkoop, orders en productietaken.
- Iedere tabel heeft een zichtbare titel, echte kolomkoppen, een logische
  toetsenbordvolgorde en een lege toestand die oorzaak en mogelijke actie noemt.
- Numerieke waarden worden op de komma uitgelijnd; eenheden staan consequent in
  de kop of bij iedere waarde, nooit gemengd.
- Sorteerbaarheid en filters zijn zichtbaar en behouden hun toestand bij openen
  van een detail en terugkeren.
- Een volledige rij mag selecteerbaar zijn, maar de rij bevat daarnaast een
  benoemde detailactie voor toetsenbord- en schermlezergebruik.
- Kritieke taakbediening staat niet uitsluitend in een hovermenu.
- Werkplaatswachtrijen tonen eerst blokkade, prioriteit, eerstvolgende handeling,
  project, onderdeel en benodigde middelen; commerciële details zijn secundair.

## Status, feedback en lege toestanden

- Statussen gebruiken één semantische set: neutraal, informatie, succes,
  waarschuwing, blokkade/fout. Domeinstatussen houden hun backendnaam en worden
  op deze presentatierollen afgebeeld.
- Waarschuwingstekst gebruikt `warningText` op lichte oppervlakken; de algemene
  waarschuwingskleur is niet automatisch geschikt voor kleine tekst.
- Asynchrone statusberichten worden programmatisch aangekondigd zonder de focus
  onnodig te verplaatsen. Een bevestigingsdialoog krijgt alleen focus wanneer de
  gebruiker werkelijk moet beslissen.
- Laden behoudt de globale context en voorkomt layoutverspringing. Na tien
  seconden verschijnt een bruikbare voortgangs- of wachttijdmelding.
- Een lege toestand maakt onderscheid tussen `nog geen gegevens`, `geen
  zoekresultaten`, `geen rechten` en `data kon niet worden geladen`.
- Succesmeldingen noemen het resultaat en waar het terug te vinden is.

## Acties en foutpreventie

- Per taakvlak is er maximaal één primaire actie. Secundaire acties zijn visueel
  rustiger; gevaarlijke acties gebruiken niet de primaire accentkleur.
- Vrijgeven, bestellen, factureren, voorraad corrigeren, productie gereedmelden
  en klantpublicatie tonen vooraf de relevante revisie en gevolgen.
- Destructieve acties vragen bevestiging met het concrete doelobject. Gebruik
  geen algemene vraag als `Weet je het zeker?`.
- Waar mogelijk krijgt de gebruiker een herstelbare actie of expliciete
  tijdlijnboeking in plaats van stil overschrijven.
- Knoppen worden tijdens een request beschermd tegen dubbel uitvoeren, maar de
  interface blijft de actieve handeling en foutstatus tonen.

## Bediening en toegankelijkheid

- WCAG 2.2 AA is verplicht. Normale tekst heeft minimaal 4,5:1 contrast; grote
  tekst minimaal 3:1. Informatieve componentgrenzen en focusindicatoren halen
  minimaal 3:1 tegen aangrenzende kleuren.
- Alle functionaliteit werkt met toetsenbord. Er is geen toetsenbordval en de
  DOM-volgorde is gelijk aan de zichtbare taakvolgorde.
- Het zichtbare focuskader gebruikt de centrale kleur, dikte en offset, wordt
  niet verwijderd en wordt niet door sticky bediening bedekt.
- De productstandaard voor primaire bediening is `minimumTarget` van 44 CSS-px.
  Een compacte control van 36 px is alleen toegestaan in een desktopdatatabel
  met voldoende tussenruimte en een gelijkwaardige bediening van minimaal 44 px.
- Draggen, roteren of schuiven heeft altijd knoppen, velden of een andere
  single-pointerbediening als alternatief.
- Status, fout, selectie en voortgang zijn niet alleen via kleur zichtbaar.
- De pagina ondersteunt tekstzoom, reflow, portret en landschap zonder verlies
  van data of acties.

## Beweging, 3D en technische visualisatie

- Beweging ondersteunt begrip en geeft feedback; zij is niet decoratief en
  blokkeert geen invoer.
- Algemene UI-beweging gebruikt `designTokens.motionMs`; assemblagebeweging
  gebruikt het afzonderlijke assemblagecontract.
- Bij `prefers-reduced-motion: reduce` worden niet-essentiële overgangen
  verwijderd en technische animaties direct in hun ondubbelzinnige eindtoestand
  getoond.
- 3D- en canvasbediening activeert drag/zoom pas na een expliciete actie en mag
  mobiel scrollen niet blokkeren.
- Een technische maat, positie of status blijft als tekst beschikbaar en wordt
  niet uitsluitend in canvas, kleur of animatie weergegeven.

## Verplichte componentset voor de grote revisie

De revisie introduceert één herbruikbare implementatie van:

- applicatieheader en hoofdroutering;
- paginakop met context, status en primaire actie;
- projectcontextbalk;
- subnavigatie;
- formuliergroep, veld, hint en fout;
- foutoverzicht;
- knopgroepen en bevestigingsdialoog;
- statuslabel en blokkadepaneel;
- datatabel, filters, sortering en paginering;
- wachtrijrij en taakdetail;
- lege toestand, laadstatus en statusmelding;
- documentregel en publicatiestatus;
- lineaire stapindicator voor werkelijk lineaire flows;
- technische viewer met toegankelijk bedieningsalternatief.

Een productscherm mag deze patronen samenstellen, maar geen lokale kopie met
afwijkende spacing, focus, foutgedrag of statusbetekenis maken.

## Test- en acceptatiecontract

Iedere hoofdflow wordt minimaal getest als Systeembeheerder, Verkoop & offertes, Werkvoorbereiding,
Inkoper, relevante Operator en Klant/testklant. Per flow gelden:

- directe route, refresh en terugnavigatie behouden context;
- zichtbare bediening én API-autorisatie passen bij de testrol;
- klantresponses bevatten geen interne velden of documentverwijzingen;
- toetsenbordvolgorde, focus, foutoverzicht en statusmelding werken;
- contrast en 200% tekstzoom voldoen;
- weergave is bruikbaar op 390, 768 en 1440 CSS-px;
- normale en gereduceerde beweging behouden dezelfde betekenis;
- laden, leeg, fout, geblokkeerd, concept en gereed zijn afzonderlijk getest;
- screenshots van kernstatussen worden als visuele regressie vergeleken;
- technische waarden blijven aantoonbaar afkomstig uit backendcontracten.
- dezelfde flow wordt voor iedere actieve productsite uitgevoerd; wisselen van
  site verandert uitsluitend toegestane producten en presentatie, nooit
  projectrechten of technische contracten.

De grote portalrevisie is pas voltooid wanneer deze matrix voor Configurator,
Projecten, Werkplaats, Inkoop, Voorraad en de klantweergave slaagt.

## Externe normbasis

- [WCAG 2.2](https://www.w3.org/TR/WCAG22/): normatieve toegankelijkheidseisen.
- [W3C Understanding WCAG 2.2](https://www.w3.org/WAI/WCAG22/Understanding/):
  uitleg over onder meer contrast, focus, target size, dragalternatieven,
  foutidentificatie en statusberichten.
- [U.S. Web Design System design principles](https://designsystem.digital.gov/design-principles/):
  echte gebruikersbehoeften, vertrouwen, toegankelijkheid en continuïteit.
- [U.S. Web Design System step indicator](https://designsystem.digital.gov/components/step-indicator/):
  voortgang alleen voor stabiele, lineaire processen met meerdere hoofdstappen.
- [GOV.UK Design System tabs](https://design-system.service.gov.uk/components/tabs/):
  tabs alleen voor nauw verwante informatie, niet als hoofdroutering.
- [GOV.UK Design System tables](https://design-system.service.gov.uk/components/table/):
  tabellen voor echte rij-/kolomvergelijking, niet voor pagina-layout.
- [GOV.UK Design System error summary](https://design-system.service.gov.uk/components/error-summary/):
  foutoverzicht met focus en links naar dezelfde veldfouten.
- [GOV.UK Design System spacing](https://design-system.service.gov.uk/styles/spacing/):
  een beperkte, responsive spacingschaal.
- [MDN media queries for accessibility](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Media_queries/Using_for_accessibility):
  respecteer de systeemvoorkeur voor gereduceerde beweging.
