# Beslissingen voor de portalpilot

Status: **toekomst — resterende input nodig vóór de interne/publieke pilot**.

De revisie kan met de huidige testcontracten worden doorontwikkeld. De volgende
punten zijn bewust niet door de software ingevuld omdat zij bedrijfskeuzes,
merkkeuzes of echte toegangsafspraken zijn.

## Productwebsites

- definitieve sitenaam, domein, logo, accent en contact-/juridische blokken voor
  verzendkisten, workstations en robotcellen;
- bevestigen welke Product-ID's op iedere site horen;
- bepalen of de algemene SW Werkplaats-site alle producten toont of alleen naar
  de gespecialiseerde sites verwijst.

## Rollen en klantorganisaties

- de normale pilotkeuze bestaat uit `Bedrijfsbeheer`, `Productiemedewerker` en
  `Klant`; verkoop, werkvoorbereiding en inkoop blijven als latere specialisaties;
- de pilot gebruikt één algemene `Productiemedewerker` voor alle wachtrijen;
  specialistrollen blijven bewaard voor latere automatische machine-pc-context;
- definitieve medewerkersrollen en uitzonderingen op de huidige capabilitymatrix;
- wie klantpublicatie, voorraadcorrectie en gereedmelding mag uitvoeren;
- bron en beheerproces voor klantorganisaties, vestigingen en gebruikers;
- keuze of één klantorganisatie standaard al haar projecten uit alle sites ziet.

## Klantpublicatie en documenten

- welke projectstatus een order automatisch of handmatig publiceerbaar maakt;
- welke offerte, 3D-bijlage, factuur en assemblage-instructie de officiële
  klantversie is;
- welke bronsystemen factuur- en betaalstatus gaan leveren;
- bewaartermijnen, revisies en intrekken/vervangen van gepubliceerde documenten.

## Voorraad

- eerste lijst bevestigingsmaterialen en verbruiksartikelen die werkelijk worden
  gevolgd;
- voorraad- en inkoopeenheid, doosinhoud, magazijnlocatie, minimum en doel per
  artikel;
- moment waarop reservering, uitgifte en retour worden geboekt;
- bevoegdheid en controleproces voor correctie en telling.

Deze waarden worden na besluit als masterdata- en operationele records ingevoerd,
niet als browserdefaults.

## Werkplaatsflow

- definitieve naam voor `Profielenmachine` en overgang van de huidige CNC-indeling;
- prioriteitsregels, gewenste datum en volgorde binnen iedere wachtrij;
- automatische globale orderstatus wanneer enkele of alle deeltaken gereed zijn;
- blokkadecategorieën, escalatie en wie een blokkade mag vrijgeven;
- welke plaat-, tap-, profiel- en printoverzichten als primaire operatorweergave
  gelden en welke alleen projectdocument zijn.

## Reeds besloten: levering

- toegestane levervormen: bouwpakket en gemonteerd;
- standaard levervorm: bouwpakket;
- montageprijs: op aanvraag, handmatig vast te stellen door Bedrijfsbeheer;
- ontvangst: verzenden of afhalen; verzenden is de standaard en afhalen blijft toegestaan;
- automatische verpakking/verzendkosten volgen pas met gevalideerde klant-,
  maat-, gewicht- en vervoerdersdata en worden tot die tijd niet geschat.

## Uitrol

- moment waarop de rolsimulator wordt uitgeschakeld;
- identity provider, HTTPS-domeinen en reverse-proxykeuze;
- interne en publieke hostnamen en netwerkgrens;
- serverdatabase/documentopslag zodra meer dan één backendinstantie nodig is.
