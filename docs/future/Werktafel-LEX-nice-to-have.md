# Werktafel LEX - upgrades en toekomstconcepten

Status: **toekomst / nog niet besloten**.

## Status

Dit document parkeert de ideeen uit de conceptsparring rond de Werktafel LEX.
Het zijn **nice-to-haves en afzonderlijke onderzoeksrichtingen**. Er is nog geen
besluit genomen om een van deze ideeen te bouwen.

De bestaande, offerabele LEX blijft de uitgangspositie. Nieuwe ideeen mogen niet
stilzwijgend met elkaar worden gecombineerd: iedere upgrade of conceptvariant
moet afzonderlijk beoordeeld, geprijsd en gekozen kunnen worden.

## Uitgangspunten van de toepassing

- Op de tafel worden LED-modules op RVS-platen van circa 1,5 mm gemonteerd.
- De platen bestaan in verschillende afmetingen.
- Platen komen vanuit een verticaal opgebouwd invoerrek links van de werkplek.
- Na verwerking gaan de platen naar een afvoer- of uithardingsrek rechts.
- Na het gieten moeten de platen horizontaal en schokarm worden behandeld.
- De belangrijkste doelen zijn minder bukken, trekken, tillen en voorover reiken.
- De werkplek moet zittend en staand gebruikt kunnen worden.

## Basisproduct: LEX volgens het Item-principe

De budgetbasis blijft bewust eenvoudig:

- elektrisch hoogteverstelbare werktafel;
- plaat ondersteund door kogelpotten;
- plaat of bewegend blad handmatig in X-richting naar een gunstige werkhoek;
- systeemprofielconstructie;
- geen automatische aan- of afvoer;
- geen robot.

De onderstaande Item-upgrades zijn losse opties op deze basis en zijn niet
automatisch onderdeel van elkaar.

## Losse Item-upgrades

### U1 - Verstelbare randhaakjes

Haakjes rondom het werkvlak kunnen omhoog worden gezet om te voorkomen dat een
plaat van de kogelpotten rolt. Dit sluit aan op de oorspronkelijke Item-oplossing.

**Pluspunten**

- eenvoudig en zichtbaar;
- weinig besturing nodig;
- bruikbaar bij uiteenlopende plaatformaten.

**Aandachtspunten**

- handmatig instellen;
- de plaat is niet in iedere willekeurige werkpositie gefixeerd;
- uitstekende delen rondom het werkvlak kunnen hinderlijk zijn.

### U2 - Pneumatische anti-rolstoppers

Meerdere kleine frictiestoppers komen vanuit het werkblad omhoog en blokkeren de
plaat op de kogelpotten.

**Pluspunten**

- eenvoudige bediening;
- geen losse haakjes rondom de tafel;
- als zelfstandige upgrade toe te voegen.

**Aandachtspunten**

- positie en aantal moeten bij alle plaatformaten werken;
- de dunne RVS-plaat mag niet plaatselijk worden vervormd;
- de veilige toestand bij drukverlies moet nog worden bepaald.

### U3 - Vacuum Lock

Dit is het eenvoudige Item-concept met een vacuümvergrendeling. Een vaste,
centrale rechthoekige vacuümzone of enkele tactisch geplaatste zuignappen grijpen
de onderzijde van de plaat. De medewerker verplaatst de plaat nog steeds
handmatig over de kogelpotten.

**Functiegrens**

- het vacuüm houdt de plaat uitsluitend vast;
- de zuignap transporteert de plaat niet;
- er is geen lineaire transportas;
- er is geen Fairino-robot.

**Te onderzoeken**

- één centrale rechthoekige vacuümzone tegenover meerdere kleinere zones;
- benodigde slag om boven de uitstekende kogelpotten contact te maken;
- minimale plaatmaat en vrije, gladde ruimte aan de onderzijde;
- vervorming van 1,5 mm RVS bij onderdruk;
- vacuüm- en flowbewaking.

### U4 - Aan- en afvoerkarren met rollenbanen

Links en rechts van de LEX komen losse systeemprofielkarren op zwenkwielen. Elke
kar bevat meerdere horizontale niveaus met passieve rollenbanen, zodat platen in
en uit kunnen worden geschoven zonder te tillen.

De Z-as van de tafel wordt vanuit een interface per niveau aangestuurd. De
medewerker kiest bijvoorbeeld `invoer niveau 3` of `afvoer niveau 5`; de tafel
gaat vervolgens naar de bijbehorende overdrachtshoogte. Daarmee hoeft de tafel
niet handmatig op ieder vak te worden uitgelijnd.

**Functiegrens**

- platen worden nog steeds handmatig horizontaal in- en uitgeschoven;
- de karren bevatten geen aangedreven transport;
- er is geen vacuümtransport en geen robot.

**Te onderzoeken**

- externe opslag van niveauhoogtes omdat de elektrische poten maar een beperkt
  aantal interne geheugenstanden hebben;
- beschikbare interface van de HTE2-besturing voor absolute of relatieve
  hoogtecommando's;
- reproduceerbaarheid en tolerantie van iedere transferhoogte;
- mechanische positionering en vergrendeling van de zwenkwielkarren;
- een kleine rollenbrug voor een spleetvrije overgang;
- bescherming tegen uitrollen van platen tijdens het verrijden van een kar.

## Next-level concepten

De volgende richtingen zijn volledige conceptvarianten. Ze moeten niet als losse
budgetopties in de bestaande LEX worden gemengd zonder een nieuw ontwerpbesluit.

### N1 - Verkleinbaar werkvlak met kamdelen

Twee in elkaar schuivende, kamvormige werkbladdelen maken het werkoppervlak in de
Y-richting kleiner of groter. Daardoor kan de medewerker bij kleine platen dichter
rondom het werkstuk komen.

**Doel**

- minder reiken bij kleine platen;
- beter rondom bereikbaar;
- geschikter voor gedeeltelijk zittend werken.

**Belangrijkste onzekerheden**

- mechanische complexiteit van de overlappende profielen;
- vlakke en continue ondersteuning van dun RVS;
- openingen, vervuiling en reinigbaarheid bij gietwerk;
- extra rails, vergrendelingen en kosten.

Dit concept blijft voorlopig een onderzoeksidee; er is nog niet aangetoond dat de
extra complexiteit voldoende voordeel biedt boven het verschuiven naar een
werkhoek.

### N2 - Lineaire vacuüm-transporthulp zonder robot

Een aangedreven vacuümwagen beweegt langzaam in hoofdzaak in X-richting. De
vacuümgrijper trekt of duwt een plaat tussen invoerrek, kogeltafel en afvoerrek.
De plaat blijft daarbij door rollen of kogelpotten ondersteund.

**Functiegrens**

- dit is materiaaltransport, niet alleen plaatvergrendeling;
- dit concept gebruikt geen Fairino-robot;
- geen automatische montage- of giethandeling;
- bediening kan als langzame hold-to-run-functie worden uitgevoerd.

**Doel**

- niet bukken en aan platen uit rekken trekken;
- geen handmatige hoogtecorrecties tijdens de overdracht;
- gelijkmatige, schokarme afvoer na het gieten.

**Te onderzoeken**

- toegang tot de onderzijde vanuit ieder rekniveau;
- vaste centrale grijper tegenover een korte balkgrijper;
- verlies van vacuüm, stopgedrag en detectie van plaatpositie;
- scheefloop van grote platen bij één centraal aangrijppunt;
- machineveiligheid en afscherming van de bewegingszone.

### N3 - Fairino-handlingcel

Een Fairino-robotarm, eventueel op een lineaire rail, verzorgt het uitnemen,
positioneren en terugplaatsen van platen. Invoerrek, tafel en afvoerrek worden als
één handlingcel ontworpen.

**Mogelijke functies**

- verschillende vakhoogtes zelfstandig bereiken;
- plaat gecontroleerd uitnemen en horizontaal houden;
- positioneren en eventueel roteren vóór de bewerking;
- na bewerking schokarm in het gekozen afvoervak plaatsen;
- vacuümgrijper met druk- en flowbewaking.

Fairino hoort uitsluitend bij dit robotconcept en bij de volledige procescel
hieronder. Fairino is geen onderdeel van Vacuum Lock, de karrenupgrade of de
eenvoudige lineaire transporthulp.

### N4 - Volledige robot- en procescel

Dit is een latere automatiseringsrichting waarin handling en het gietproces in één
cel worden samengebracht.

**Mogelijke functies**

- automatische materiaalhandling met een robot;
- geautomatiseerde positionering en vergrendeling;
- doseernozzle voor het gietmateriaal;
- flowmeting en procesbewaking;
- horizontale, schokarme overdracht naar een uithardingsrek.

Deze variant vraagt een afzonderlijke processtudie, veiligheidsarchitectuur en
businesscase en valt buiten de huidige LEX-offerte.

## Scheidslijnen die behouden moeten blijven

| Idee | Houdt vast | Transporteert | Robot | Procesautomatisering |
|---|---:|---:|---:|---:|
| Item-basis | handmatig/mechanisch | handmatig | nee | nee |
| U2 Pneumatische stoppers | ja | nee | nee | nee |
| U3 Vacuum Lock | ja | nee | nee | nee |
| U4 Rollenbaankarren | nee | handmatig | nee | nee |
| N1 Kamvormig werkvlak | optioneel | handmatig | nee | nee |
| N2 Lineaire vacuümhulp | tijdens transport | ja | nee | nee |
| N3 Fairino-handlingcel | via grijper | ja | ja | nee |
| N4 Robot- en procescel | ja | ja | ja | ja |

## Parkeerstatus en vervolgstap

De conceptsparring wordt na dit document gepauzeerd. Er wordt nu geen keuze
afgedwongen en geen van de nice-to-haves wordt automatisch in de LEX Revolution
gebouwd.

Voor een volgende ontwerpronde zijn minimaal nodig:

- minimale en maximale plaatafmetingen en massa;
- vrije zones en eventuele gaten aan de onderzijde van iedere plaatvariant;
- aantal vakken, vakhoogtes en diepte van invoer- en afvoerrekken;
- gewenste cyclustijden en maximale toegestane snelheid;
- eigenschappen van het gietmateriaal en vereiste rusttijd;
- gewenste budgetniveaus voor basis, upgrades en automatisering.

Daarna kunnen de kansrijkste opties afzonderlijk worden beoordeeld op ergonomie,
technische haalbaarheid, veiligheid, bouwkosten en terugverdientijd.

## Software nice-to-have - gehost interactief offerteportaal

Dit is een afzonderlijk toekomstplan en valt nadrukkelijk buiten de huidige
LEX-oplevering. Voor de eerstvolgende LEX-offerte blijft de focus op een nette,
direct bruikbare klantoutput met vaste aanzichten, begeleidende tekst, belangrijke
afmetingen, PDF en het zelfstandige interactieve HTML/GLB-model.

In een latere fase kan deze output op de eigen webserver worden gepubliceerd. De
klant ontvangt dan één beveiligde link naar een offertepagina met:

- een interactief 3D-model met vaste aanzichtknoppen;
- belangrijke afmetingen, materialen, prijs, levertijd en voorwaarden;
- technische tekeningen en een PDF-download;
- acties voor `Akkoord`, `Wijziging aanvragen` en `Afwijzen`;
- een vastgelegde offerteversie met datum/tijd en klantbevestiging;
- automatische bevestiging en overdracht van een akkoord naar de werkplaatsapp.

Bij uitwerking moeten het publieke klantgedeelte en de interne werkplaatsomgeving
strikt gescheiden blijven. Denk daarbij aan HTTPS, unieke en eventueel aflopende
links, versiebeheer, minimale opslag van persoonsgegevens, toegangsregistratie,
back-ups en een onveranderbare momentopname van de geaccepteerde offerte.

**Parkeerbesluit augustus 2026:** niet bouwen vóór de LEX-tafel offerabel is en de
huidige klantoutput voldoende professioneel en betrouwbaar functioneert.

### Mogelijke tussenstap - interactieve LEX-viewer in Wix

De bedrijfswebsite `https://www.rdabv.com/` draait op Wix. Als kleinere tussenstap
kan later een afzonderlijke, niet in het hoofdmenu opgenomen Wix-pagina worden
gemaakt met een HTML-iframe voor het draaibare LEX-model. De offerte-PDF kan dan
een klikbare link en QR-code naar die pagina bevatten.

Mogelijke latere automatisering:

- genereer naast het zelfstandige HTML-bestand een compacte Wix-embeduitvoer;
- plaats het model op een afzonderlijke pagina, bijvoorbeeld `/3d-lex-<code>`;
- toon alleen het model, vaste aanzichten en basisproductinformatie;
- houd prijzen, persoonsgegevens en akkoordfunctionaliteit voorlopig buiten deze
  eenvoudige viewer;
- migreer pas naar het volledige gehoste offerteportaal wanneer meerdere offertes
  automatisch gepubliceerd moeten worden.

**Parkeerbesluit augustus 2026:** ook deze Wix-tussenstap wordt nu niet gebouwd.
Eerst de actuele LEX-uitvoering, offerte en bestaande PDF/HTML/GLB-output afronden.
