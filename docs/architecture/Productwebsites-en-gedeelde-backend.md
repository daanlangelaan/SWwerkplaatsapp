# Productwebsites en gedeelde backend

Status: **actueel architectuurcontract**.

## Besluit

De productfamilies mogen ieder een eigen publieke website krijgen, bijvoorbeeld
voor verzendkisten, workstations en robotcellen. Die websites zijn afzonderlijke
presentatie- en verkoopkanalen boven één gedeelde backend. Zij krijgen geen eigen
productregels, prijsengine, orderdatabase, BOM-logica, projectopslag of
productie-export.

Intern blijven masterdata, klanten, projecten, configuratierevisies, offertes,
orders, voorraad, inkoop, productie en documenten één waarheid.

```text
verzendkisten-site ─┐
workstations-site ──┼── publieke site-API ─┐
robotcellen-site ───┘                      │
                                          ▼
interne portal ─────────────── gedeelde application/backend
klantportal ───────────────────────────────┤
                                          ├── masterdata-runtime
                                          ├── project/orderdatabase
                                          ├── PortalData/documenten
                                          └── productie/SolidWorks-worker
```

Een productwebsite mag als afzonderlijke webapp of als een herbruikbare
site-shell met eigen configuratie worden uitgerold. De voorkeursroute is één
gedeelde sitecodebase met een server-side `SiteContext` per domein. Daardoor
blijven componenten, toegankelijkheid, foutafhandeling en API-contracten gelijk.

## Begrippen

- **Site**: een publiek kanaal met een stabiel `SiteId`, domein, naam, logo,
  presentatieprofiel, teksten en toegestane productfamilies.
- **Product**: een technisch/commercieel productrecord met een stabiel
  `Product-ID` uit masterdata. Een product kan op nul, één of meerdere sites
  worden aangeboden.
- **Organisatie**: een klantbedrijf of interne organisatie waaraan gebruikers en
  projecten zijn gekoppeld.
- **Gebruiker**: een persoon met lidmaatschappen en capabilities. Authenticatie
  wordt later toegevoegd, maar dit contract geldt nu al voor rolsimulatie.
- **Project**: de centrale interne eenheid met een globale `ProjectId` en
  optioneel de `SourceSiteId` waarop de aanvraag ontstond.
- **SiteContext**: het door de backend vastgestelde kanaal van de actuele request.

Een site is geen afzonderlijke technische tenant en krijgt geen eigen kopie van
de kerngegevens. Sitecontext bepaalt aanbod, presentatie en toegestane publieke
acties; organisatie- en gebruikersrechten bepalen welke projectdata iemand mag
zien.

## Eén waarheid en eigenaarschap

| Gegeven | Enige eigenaar | Sitespecifiek toegestaan |
|---|---|---|
| Productregels, geometrie en fabricagedata | masterdata en backend | alleen selectie uit toegestane Product-ID's |
| Leveranciers, aanbiedingen en artikeldata | masterdata-runtime | geen sitekopie |
| Prijsberekening en BOM | application/backend | expliciete commerciële sitepolicy via stabiel ID |
| Project, offerte en order | centrale operationele opslag | `SourceSiteId` en vastgelegde sitesnapshot |
| Voorraad, reservering en inkoop | centrale operationele opslag | geen geïsoleerde sitevoorraad tenzij later als echte magazijnlocatie gemodelleerd |
| Productie en vrijgave | centrale backend | nooit in de publieke site |
| Marketingtekst, logo en accent | sitepresentatiecontract | ja |
| Spacing, componentgedrag en toegankelijkheid | productbreed portal-designsysteem | geen afwijkende implementatie per site |
| Klantdocument | centrale documentregistratie | merk-/sitesnapshot en expliciete publicatie |

Een commerciële afwijking, zoals een andere marge of actie voor één site, wordt
geen browserliteral. Zij krijgt een stabiele backendpolicy met geldigheidsperiode
en wordt bij de offerte als revisiesnapshot vastgelegd.

## Sitecontract

De backend levert voor iedere publieke request minimaal:

- `SiteId` en publieke naam;
- primaire en alternatieve domeinen;
- presentatieprofiel en toegestane merkassets;
- taal en landinstellingen;
- toegestane productcategorieën en stabiele `Product-ID's`;
- toegestane publieke capabilities, zoals configureren of offerte aanvragen;
- contact- en juridische publicatieblokken;
- klantportalroute;
- actieve status en onderhoudsstatus.

De backend bepaalt `SiteId` uit de gevalideerde host-/deploymentconfiguratie. Een
vrij door de browser meegestuurde `SiteId` is nooit voldoende autorisatie. Een
onbekend domein of een product buiten de toegestane sitescope wordt geweigerd.

## Gebruikers en gelijktijdig gebruik

Het toekomstige actorcontract scheidt identiteit, organisatie, sitecontext en
capabilities:

```text
ActorContext
  UserId
  OrganizationId
  SiteId
  Roles[]
  Capabilities[]
  IsSimulated
```

- Meerdere klanten en medewerkers mogen gelijktijdig via verschillende sites
  dezelfde backend gebruiken.
- Iedere request is zelfstandig en draagt een server-side actor- en sitecontext;
  er is geen globale `CurrentSite` of `CurrentUser` in procesgeheugen.
- Een klant ziet projecten via zijn organisatielidmaatschap, niet alleen omdat
  de projecten van hetzelfde domein kwamen.
- Eén klantorganisatie kan projecten via meerdere productwebsites hebben en
  deze later onder één login terugzien.
- Interne medewerkers zien dezelfde projecten in de interne portal, inclusief
  herkomstsite en sitesnapshot.
- Rollen en capabilities worden door de backend afgedwongen. Een sitekleur,
  route of verborgen knop levert geen recht.

Totdat echte authenticatie bestaat, gebruikt de testomgeving hetzelfde contract
met expliciete `IsSimulated=true`. De testselector kiest zowel rol als site en is
technisch uitschakelbaar.

## Project-, offerte- en documentherkomst

Bij de eerste opgeslagen aanvraag legt de backend minimaal vast:

- globale `ProjectId`;
- `SourceSiteId`;
- stabiel `Product-ID`;
- klantorganisatie en contactpersoon indien bekend;
- sitepresentatie- en commerciële policyrevisie;
- oorspronkelijke aanvraag en tijdstip.

De herkomstsite blijft auditinformatie en verandert niet wanneer het project
intern wordt geopend of een gebruiker later via een andere site inlogt.

Een verstuurde offerte en klantbijlage krijgen een sitesnapshot van naam, logo,
contactgegevens, taal en toegepaste commerciële policy. Een latere rebranding mag
een reeds verstuurd document niet stilzwijgend veranderen. Technische data wordt
niet in die presentatie-snapshot gekopieerd.

Documenttoegang wordt bepaald door `OrganizationId`, `ProjectId`, documentstatus
en publicatiecontract. Een mapnaam, site of voorspelbare bestands-URL is geen
toegangscontrole.

## Frontendstrategie

Gebruik één herbruikbare publieke site-shell met gedeelde componenten voor:

- productintroductie en toepassingen;
- productkeuze;
- configurator;
- prijs- of offerteaanvraag;
- klantbijlagen en 3D-weergave;
- overgang naar het klantportal.

Per site mogen verschillen:

- domein, sitenaam, logo en accentkleur;
- marketinginhoud en volgorde van publieke secties;
- toegestane productfamilies;
- klantgerichte afbeeldingen en renders;
- contact- en juridische tekst;
- expliciete commerciële policy uit de backend.

Per site mogen niet verschillen door gekopieerde code:

- productvelden, defaults, technische grenzen of fabricageregels;
- prijs-, BOM- of validatielogica;
- project-, offerte- en orderstatussen;
- basiscomponenten, spacing, focus, foutgedrag en toegankelijkheid;
- klantautorisatie en documentselectie.

Een sitespecifieke afwijking wordt via een getypeerd contract of compositiepunt
toegevoegd. Er ontstaan geen `shipping-box-portal.js`, `workstation-pricing.js`
of vergelijkbare forks met eigen bedrijfslogica.

## Opslag- en schaalgrens

In de huidige lokale fase kan één backendproces meerdere sites en gelijktijdige
gebruikers afhandelen en als enige eigenaar SQLite en PortalData openen. De
afzonderlijke websites openen deze opslag nooit rechtstreeks.

Bij publieke uitrol praten websites via HTTPS met de private backend. Zodra meer
dan één backendinstantie, horizontale schaal of een externe productieserver nodig
is, wordt `IOrderRepository` vervangen door een serverdatabase-implementatie.
Het SQLite-bestand wordt niet gedeeld via een netwerkshare en niet door meerdere
webapp-processen als integratiepunt gebruikt.

## Verplichte testmatrix

De grote revisie test minimaal:

- iedere site toont uitsluitend de geconfigureerde productfamilies;
- een handmatig gewijzigde `SiteId` geeft geen toegang tot een ander aanbod of
  project;
- twee gebruikers op verschillende sites kunnen gelijktijdig configureren en
  projecten opslaan zonder contextlekkage;
- projecten uit alle sites verschijnen één keer in de interne projectadministratie;
- `SourceSiteId`, product-ID en revisiesnapshot blijven na offerte en order gelijk;
- een klantorganisatie ziet uitsluitend eigen gepubliceerde projecten en
  documenten, ook wanneer zij meerdere sites gebruikt;
- sitebranding verandert presentatie maar nooit prijs, BOM, geometrie of
  productiecontract zonder expliciete backendpolicy;
- een uitgeschakelde site weigert nieuwe publieke aanvragen zonder bestaande
  interne projecten onbereikbaar te maken;
- rol- en site-simulatie gebruiken dezelfde backendgrenzen als de latere login.

## Migratievolgorde

1. Voeg `SiteContext` en stabiele site-ID's toe als backendcontract.
2. Laat catalogus- en configurator-API's hun productaanbod op sitepolicy filteren.
3. Maak `ProjectId`, `OrganizationId` en `SourceSiteId` onderdeel van de centrale
   projectopslag.
4. Breid de ontwikkelrolsimulator uit met actor-, organisatie- en sitecontext.
5. Splits de publieke configurator-shell van de interne portalpresentatie zonder
   application- of domeinlogica te kopiëren.
6. Bouw de eerste productsite als configuratie van de gedeelde site-shell.
7. Bewijs met een tweede productsite dat geen productcode of backendlogica is
   gekopieerd.
8. Voeg later authenticatie, HTTPS, domeinrouting en productieopslag toe zonder
   de site-, actor- of projectcontracten te vervangen.

## Niet doen

- Geen database of backend per productwebsite.
- Geen product-ID of sitescope in browsercode hardcoden.
- Geen klant scheiden op e-mailadres, domeinnaam of vrije organisatienaam.
- Geen sitebranding gebruiken als autorisatiegrens.
- Geen afzonderlijke prijs-, BOM-, voorraad- of workflowengine per site.
- Geen globale mutable site- of gebruikerscontext in het backendproces.
- Geen tweede klantaccount eisen wanneer dezelfde organisatie via een andere
  productwebsite bestelt.
