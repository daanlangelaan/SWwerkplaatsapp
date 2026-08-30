# Portalwerkruimten en revisiestatus

Status: **actueel architectuur- en implementatiecontract**.

## Besluit

De portal is functioneel verdeeld over echte routes binnen één lokale webserver.
Dat benadert de latere uitrolgrenzen zonder nu meerdere processen, databases of
gekopieerde backends te introduceren. Een route is een gebruikerswerkruimte; een
serverproces is een deploymentkeuze. Pas bij publieke uitrol worden publieke
sites, interne portal en workers afzonderlijk gehost achter HTTPS en een reverse
proxy.

Alle werkruimten lezen dezelfde application-services, runtime-masterdata,
projectdatabase en PortalData. De publieke productsitecontext filtert het aanbod,
maar maakt geen eigen order-, voorraad- of productwaarheid.

## Routes en taakgrens

| Route | Primaire taak | Backendbron | Hoofdrollen in de testsimulator |
|---|---|---|---|
| `/` | product configureren en aanvraag/order maken | catalogus-, quote- en orderservices | beheerder, verkoop, werkvoorbereider |
| `/app` | beheerdersoverzicht en doorstuurpunt naar de rolstart | workspace-dashboard | beheerder; overige rollen openen automatisch hun taakwerkplek |
| `/app/projects` | centrale projecten/database | projectrecords en documentsnapshots | beheerder, verkoop, werkvoorbereider, inkoper |
| `/app/projects/{ProjectId}` | dossier, documenten, werkgebieden en BOM-snapshot | beperkt of intern projectdetailcontract | intern of eigenaar van gepubliceerd klantproject |
| `/app/workshop` | gecombineerde profielenmachine-, plaat-CNC-, 3D-print- en assemblagewachtrij | productietaken | beheerder en werkvoorbereider |
| `/app/workshop/{AreaId}` | eigen gefilterde werkplaatswachtrij | productietaken binnen het werkgebied | betreffende operator |
| `/app/purchasing` | behoefte per artikel, project en leverancier | project-BOM plus voorraad | beheerder, werkvoorbereider, inkoper |
| `/app/inventory` | fysieke voorraad, reserveringen en verpakkingen | voorraadartikelen en boekingen | beheerder, inkoper |
| `/app/customer` | gepubliceerde orders en klantdocumentstatus | beperkt klantcontract | klant/testklant |
| `/library` | bestaande rail- en dragerbibliotheek | hardwarebibliotheek | bestaand compatibiliteitsbeheer |

Tabs mogen later binnen een projectdetail nauw verwante dossierdelen scheiden.
De hoofddelen hierboven blijven routes zodat verversen, teruggaan, bookmarks en
autorisatietests ondubbelzinnig blijven.

## Centrale projecteenheid

Iedere nieuwe order levert één globaal `ProjectId` met:

- `OrderId`, `ProductId`, `SourceSiteId` en `OrganizationId`;
- interne status en afzonderlijke klantstatus;
- vastgelegde prijs- en inkoopregels uit de orderrevisie;
- benodigde productiewerkgebieden uit het backendmodel;
- documentmetadata zonder opslagpad in het browsercontract;
- expliciete `CustomerPublished`-status.

Oude orderrecords worden bij lezen naar een projectrecord gemigreerd. Als oude
records geen werkgebied- of inkoopsnapshot bevatten, blijft die informatie leeg
of wordt alleen een herkenbare legacy-indeling uit bestaande exportbestanden
gemaakt. Er wordt geen fabricagekennis in de UI afgeleid.

## Werkplaats

De backend maakt productietaken voor de werkgebieden die daadwerkelijk in het
productiemodel voorkomen. De statussen zijn `Voorbereiding`, `Wachtrij`, `Bezig`,
`Geblokkeerd` en `Gereed`. Een blokkade vereist een reden. Een operatorcapability
geldt voor één werkgebied; werkvoorbereider en beheerder mogen alle taken
bijwerken.

De huidige taakstatus is een operationele werkplaatsstatus naast de bestaande
globale orderworkflow. Het later automatisch afleiden van de globale orderstatus
uit alle deeltaken vereist een afzonderlijk, getest workflowbesluit.

## Inkoop en voorraad

Inkoop groepeert de vastgelegde order-BOM op stabiel artikel en eenheid. De
backend trekt beschikbare voorraad af en rekent een besteladvies om naar hele
inkoopeenheden.

Een voorraadartikel kan alleen verwijzen naar een bestaand component of materiaal
uit `masterdata-runtime.json`. Omschrijving en identiteit worden door de backend
ingevuld. Operationele waarden blijven in de workspace-database:

- fysieke en gereserveerde hoeveelheid;
- voorraad- en inkoopeenheid;
- aantal voorraadeenheden per verpakking/doos;
- minimum, doel en magazijnlocatie;
- tijdlijn van ontvangst, uitgifte, reservering, vrijgave, retour en correctie.

Een boeking die negatieve of overgereserveerde voorraad veroorzaakt wordt
geweigerd. De eerste selectie van werkelijk te volgen bevestigingsmaterialen en
de gewenste minimum-/doelvoorraden blijft een bedrijfskeuze en wordt niet als
productregel in de UI vastgelegd.

## Klantgrens

`Bekijk als klant` gebruikt geen verborgen intern scherm maar het klantcontract:

- uitsluitend gepubliceerde projecten van dezelfde `OrganizationId`;
- klantstatus in plaats van interne workflowstatus;
- geen klantnaam, organisatie-ID, BOM, leverancier, werkgebied of opslagpad;
- uitsluitend expliciet klantzichtbare documentmetadata;
- ontbrekende offerte, factuur, assemblage-instructie of interactief 3D-model
  wordt eerlijk als nog niet beschikbaar gemeld.

Bestandsdownload wordt pas toegevoegd met een server-side document-ID,
organisatiecontrole, publicatiestatus en allowlist. Een lokaal pad wordt nooit
een download-URL.

## Tijdelijke actor- en sitesimulator

Tijdens deze revisiefase gebruikt iedere request de headers
`X-SW-Test-Role`, `X-SW-Test-Site`, `X-SW-Test-Organization` en optioneel
`X-SW-Test-User`. De backend vertaalt die naar hetzelfde `ActorContext` en
dezelfde capabilities die later na login worden geleverd. De simulator is
zichtbaar als testsimulatie en is geen beveiligingsmechanisme voor productie.

`config/portal-sites.json` bevat nu interne, algemene, verzendkisten-,
workstations- en robotcellensites. De producttoedeling en conceptnamen zijn
testconfiguratie; domeinen, merkassets en definitieve commerciële indeling
moeten voor publieke uitrol worden bevestigd.

De simulator staat niet tussen de hoofdonderdelen, maar in een afzonderlijk
`Testmodus`-menu. Een rolwissel opent direct de backend-geleverde `HomeRoute`.
Interne operators krijgen uitsluitend hun eigen werkgebied; beheerder en
werkvoorbereider houden de gecombineerde werkplaatsweergave. Organisatie wordt
alleen gevraagd voor de klantrol. De productsite staat onder een afzonderlijke
testoptie omdat zij het configuratoraanbod bepaalt en geen intern projectrecht.

## Deploymentpad

1. Nu: één lokale webserver, SQLite als enige operationele schrijver en
   bestanden als export-/herstelmirror.
2. Interne pilot: dezelfde routes achter een interne host, simulator alleen in
   ontwikkelconfiguratie.
3. Publieke pilot: afzonderlijke publieke sitehost en interne portalhost die via
   HTTPS dezelfde private API gebruiken.
4. Productie: echte identity provider, hostgevalideerde `SiteContext`,
   serverdatabase wanneer meerdere backendinstanties nodig zijn, objectopslag of
   documentservice en auditlog.

Meerdere losse lokale webservers vóór stap 3 voegen vooral configuratie- en
dataconsistentierisico toe. De contractgrenzen worden daarom nu getest, terwijl
de procesgrens later zonder herschrijving kan worden verplaatst.

## Regressiegrens

`tests/PortalWorkspace.IntegrationTests` bewijst minimaal:

- siteproductfiltering;
- klantpublicatie en organisatiescheiding;
- afwezigheid van interne projectvelden in klantdetails;
- operatorgrenzen per werkgebied;
- masterdata-gekoppelde voorraad, ontvangst en reservering;
- gecombineerd inkooptekort en hele inkoopeenheden;
- weigering van voorraadtoegang voor een klantrol.
