# App-structuur en groeipad

Doel: de configurator uitbreidbaar maken zonder dat elke nieuwe productfamilie opnieuw dezelfde teken-, gaten-, pricing- en outputproblemen moet oplossen.

## Richting

De UI moet dun blijven. De backend bepaalt productregels, defaults, validatie, geometrie, prijs, nesting, productie-output en workflowstatussen.

```text
UI / Portal
  verzamelt invoer
  toont response
  start acties

Application
  quote maken
  order maken
  workflowstatus wijzigen
  productie-output genereren

Domain
  productconfiguraties
  geometriecontracten
  bewerkingen
  materialen en beslagmodellen

Infrastructure
  catalogi uit JSON/CSV/database
  orderopslag
  bestanden/export
  lokale webserver

Manufacturing / SolidWorks
  vertaalt het domeinmodel naar CNC, nesting, SolidWorks en controlebestanden
```

## Belangrijkste afspraken

1. UI-code bevat geen productlogica behalve veldweergave en acties.
2. Productfamilies bouwen allemaal naar hetzelfde productiecontract: onderdelen, plaatsingen, gaten, pockets, beslag en workflowdata.
3. Tekenstrategie is een contract, geen losse kennis in chat of toevallige code.
4. Catalogusdata komt stapsgewijs uit bestanden of database, niet uit hardcoded UI-keuzes.
5. Orderflow is expliciet: klantconfiguratie, controle, vrijgave, freeswachtrij, productie, gereed.
6. Lokale server blijft voorlopig simpel, maar alle API's moeten later achter een echte server of reverse proxy kunnen draaien.

## Gewenste lagen

### Domain

Bevat pure modellen en afspraken:

- productconfiguraties;
- `WorkbenchModel`;
- sheet/profile parts;
- gaten/pockets;
- tekencontracten;
- workflowstatussen.

Deze laag mag niet weten van HTML, HTTP, bestandspaden, SolidWorks COM of Mach3-bestanden.

### Application

Bevat gebruiksscenario's:

- catalogus ophalen;
- quote bouwen;
- preview bouwen;
- order aanmaken;
- controle uitvoeren;
- order vrijgeven naar freeswachtrij.

Deze laag mag Domain gebruiken en services aanroepen, maar moet zelf weinig UI- of exportdetails bevatten.

### Infrastructure

Bevat opslag en adapters:

- JSON/CSV catalogus;
- toekomstige SQLite/database;
- bestandsopslag voor orders;
- lokale server;
- notificatiebestanden.

### Presentation

Bestaat uit portal UI en eventueel desktop UI. De UI praat met application-services en rendert responses.

## Huidige pijnpunten

- `PortalHtml` bevat UI, API-contractkennis en viewerlogica in een grote string.
- `PortalWebServer` mixt routing, API, static files en shutdown.
- `PortalConfigurationFactory` bevat productdefaults die later data/config moeten worden.
- `CabinetEngine` en `WorkbenchEngine` bevatten productregels en geometrie tegelijk.
- `SolidWorksMacroExporter` bevat historische routes naast de actuele strategie.

## Migratievolgorde

1. Documenteer en compileer tekencontracten.
2. Voeg application-services toe rond bestaande code zonder gedrag te wijzigen.
3. Trek catalogus/defaults achter repository-interfaces.
4. Splits productbuilders achter een gemeenschappelijke interface.
5. Maak controleur- en uitvoerderworkflow expliciet.
6. Voeg vakjeskast toe als eerste nieuwe productfamilie via de nieuwe route.

## Niet doen

- Niet meteen herschrijven naar een volledig webframework.
- Niet eerst database afdwingen voordat de domeincontracten stabiel zijn.
- Niet oude SolidWorks-orientatieproblemen oplossen met assembly-transforms.
- Niet de werkende portal breken om een schonere mapstructuur te krijgen.
