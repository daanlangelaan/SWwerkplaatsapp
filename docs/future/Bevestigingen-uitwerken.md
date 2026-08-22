# Bevestigingen uitwerken

Status: **toekomst / open werklijst**. Alleen uitgewerkte regels in code, masterdata en regressietests zijn leidend.

Blijvende werklijst voor bevestigers, gaten, lengteselectie en CAM-bewerkingen. Gebruik deze lijst wanneer gevraagd wordt: **“Wat staat er nog open rond bevestigingen?”**

## Vastgelegde besluiten

- Productstandaarden staan per producttype vast; afgeleide producten erven via `BaseProductId`.
- `cabinet`, `werkbankkast` en `vakjeskast`: hout-op-hout met houtschroef Ø4 en CNC-doorvoergat Ø4.
- `werktafel` en afgeleide LEX-producten: afzonderlijke constructieboutstandaard; voorlopig M8.
- Houtschroef-handelslengtes: 12, 16, 20, 25, 30, 35, 40, 45, 50, 55 en 60 mm.
- Schroeven in een houten kopse kant krijgen minimaal 20 mm grip.
- Schroeven door een component of plaatvlak krijgen meer dan 4 mm grip en houden minimaal 2 mm restmateriaal aan de zichtzijde.
- Tegenoverliggende schroeven in hetzelfde plaatdeel moeten minimaal 2 mm vrije tipruimte houden; negatieve tipruimte betekent dat de schroeftrajecten overlappen.
- Beslagspecifieke bevestigers voor rails, scharnieren en overige componenten blijven voorlopig bij het betreffende component vastgelegd.
- Werkbankkast met 18 mm plaat: hout-op-hout 4x40; korte plintclip 4x16; verlengde zijplintclip 4x35; SEKTION-voet door 2 mm kunststof 4x16.
- Alle lengtes uit de houtschroeffamilie `WOODSCREW_4` hebben een gewenst ontvangend pilotgat Ø3 mm. Het Ø4-gat is uitsluitend het doorvoergat in het eerste deel. Een product mag het pilotgat bewust handmatig uitvoeren, maar de gewenste diameter blijft centraal vastgelegd.
- Werkbankkastrails: Ø4,2x9,5 mm plaatkop door 0,5 mm railmateriaal. De montage op dezelfde AliExpress-gatposities aan beide zijden van een exact 18 mm T-tussenschot is fysiek getest en goedgekeurd; nominale tipruimte is 0 mm. Deze vrijgave geldt alleen voor deze exacte combinatie.
- SEKTION plintclipadapter V2 is inclusief cliptong, montagevleugel en Ø8,3x4,2 mm kopzittingen fysiek getest en goedgekeurd.

## Open — eerstvolgende uitwerking

- [x] **Portalkeuze voor souvereinen en kanten.** De optie staat standaard uit; zolang de V-freesgegevens ontbreken blokkeert inschakelen de productie-export met een duidelijke melding.
- [ ] **Souvereinen als CAM-bewerking.** Na gereedschapsvrijgave een afzonderlijke, gecontroleerde toolchange en bewerkingsgang genereren.
- [ ] **Souvereinfrees vastleggen.** Werkelijke tool-ID, hoek, tipdiameter, snijdiameter, toerental, voeding, plunge en veilige maximale diepte meten/invoeren.
- [ ] **Kopgeometrie per schroef.** Verzonkdiameter en -diepte uit werkelijke schroefkop bepalen; onderscheid maken tussen echte conische verzinking en vlakke kopkamer.
- [ ] **Kanten nalopen als afzonderlijke CAM-optie.** Buitenranden met dezelfde V-frees afschuinen; afschuining (bijvoorbeeld 0,5–1,0 mm) instelbaar en visueel controleerbaar maken.
- [ ] **Bewerkingsvolgorde bepalen.** Vastleggen of souvereinen/kanten vóór of na contourfrezen gebeurt en hoe delen tijdens die bewerking met tabs of vacuüm geborgd blijven.
- [ ] **Tweezijdige delen afvangen.** Alleen randen en gaten bewerken die vanaf de actuele plaatzijde bereikbaar zijn; omkeerbewerking expliciet plannen.
- [ ] **CAM-vrijgavecontrole uitbreiden.** Export blokkeren als de gekozen V-frees, hoek, diepte of materiaalzijde niet volledig is vastgelegd.

## Open — schroefselectie en houtverbindingen

- [ ] Verbindingstype expliciet modelleren: hout-op-hout kopse kant, hout-op-hout vlak, component-op-hout, plaat-op-aluminium en aluminium-op-aluminium.
- [ ] Per verbinding het doorsteekpakket, ontvangende richting en beschikbare inschroefdiepte opslaan; niet afleiden uit alleen plaatdikte.
- [ ] Minimale rand- en eindafstanden per materiaal vastleggen om splijten en uitbreken te voorkomen.
- [x] Pilotgat versus doorvoergat apart gemodelleerd in de bevestigerdefinitie: Ø4 doorvoergat en Ø3 gewenst ontvangend pilotgat voor `WOODSCREW_4`.
- [ ] Regels per materiaalsoort toevoegen: multiplex, betonplex, OSB, massief hout, HPL en kunststoffen.
- [ ] Schroefdiameter en lengte ook toetsen op belastingsklasse en aantal bevestigingspunten; geometrisch passend is niet automatisch constructief voldoende.
- [ ] Schroefkopkeuze vastleggen: verzonken, bolkop, cilinderkop en flenskop, inclusief ringgebruik en zichtzijde.
- [ ] Handmatige bevestigingen zonder CNC-pilotgat expliciet in assemblagecontrole en werkinstructie tonen.
- [ ] Werkelijke SEKTION-voetkopzitting nog éénmaal fysiek controleren; 2 mm kunststofweg en 4x16 zijn nu als opgegeven maat vastgelegd.
- [x] **Enkele T-tussenwand met rails opgelost.** Alleen U1 en U3 dragen werkelijk een rail aan beide plaatzijden; de twee U2-platen ieder één. Voor U1/U3 is de exacte combinatie Ø4,2x9,5 mm plaatkop, 0,5 mm railmateriaal en 18 mm plaat op de bestaande AliExpress-gatposities fysiek getest en goedgekeurd. De audit accepteert de nominale 0 mm tipruimte uitsluitend zolang deze volledige maatsignatuur ongewijzigd blijft.
- [x] Werkelijke materiaalweg van de kastschroef door de ladegeleider is vastgelegd als `CabinetFastenerPassingStackMm = 0,5 mm`.

## Open — systeemprofielen en boutverbindingen

- [ ] Per profielverbinder het verbindertype vastleggen: kopse draad, T-moer, hoeklijn, ankerverbinder, plaatadapter of doorgaande bout.
- [ ] Boutdiameter kiezen op basis van profielserie, verbinder en belasting in plaats van één algemene M8-default.
- [ ] Klempakket automatisch berekenen: plaat, profielwand, ring, moer, adapter en eventuele afstandsbus.
- [ ] Handelslengte kiezen met voldoende maar niet overmatige draaduitsteek.
- [ ] Minimale draadinschroeflengte per materiaal vastleggen: aluminium draad, staalmoer, T-moer en getapte profielkop.
- [ ] Ring, borgmoer, veerring, schroefdraadborging en aanhaalmoment per verbinding opnemen.
- [ ] Gatspeling per boutnorm vastleggen: nauw, normaal of ruim gat; niet nominale boutdiameter als universeel doorvoergat gebruiken.
- [ ] Verzinking/kopkamer toetsen aan profielwanddikte en beschikbare sleutelruimte.
- [ ] Corrosiecombinaties en materiaalkeuze opnemen, bijvoorbeeld verzinkt staal versus RVS in aluminium.

## Open — componentbibliotheek en gegevensbeheer

- [ ] Algemene componentbibliotheek ontwerpen voor schroeven, bouten, moeren, ringen, rails, scharnieren, poten en profielverbinders.
- [ ] Productstandaard apart houden van de componentbibliotheek: de bibliotheek beschrijft wat bestaat; de productstandaard beschrijft wat standaard gekozen wordt.
- [ ] Per component bron, leverancier, artikelnummer, gemeten/nominale maat en verificatiestatus opslaan.
- [ ] Varianten en vervangers beheren zonder dat bestaande projecten stilletjes van maat veranderen.
- [ ] Gebruikte componentversie en productstandaard mee-exporteren naar projectdata.
- [ ] Prijsregels koppelen aan diameter/lengtevariant; geen generieke prijs overnemen voor alle schroeven.
- [ ] Voorraad en voorkeurslengtes toevoegen zodat een veilige beschikbare maat gekozen kan worden.

## Open — controles en testdekking

- [ ] Regressietest: geen enkel `PanelScrew`-gat in houtproducten mag terugvallen op een boutdiameter.
- [ ] Tests voor 10, 12, 15, 18 en 21 mm plaatdikte en voor componentstapels van verschillende dikte.
- [ ] Grensgevallen testen: exact 2 mm restmateriaal, exact 20 mm kopse grip en geen passende handelslengte.
- [ ] Tegenoverliggende bevestigers testen: dezelfde positie, bijna dezelfde positie, kruisende schroefassen en voldoende vrije tipruimte.
- [ ] Controleren dat “geen passende lengte” altijd export blokkeert en nooit naar de dichtstbijzijnde onveilige maat afrondt.
- [ ] BOM, gatenlijst, CAM-commentaar, 3D-model en assemblage-instructie op dezelfde bevestiger-ID en lengte controleren.
- [ ] Fysieke proefstukken vastleggen met datum, materiaal, gereedschap, gatdiameter, schroef en resultaat.

## Nice-to-haves

- [ ] Keuze-uitleg in de portal tonen: waarom deze diameter/lengte is gekozen en hoeveel grip/restmateriaal resteert.
- [ ] Alternatieve veilige handelslengtes tonen bij voorraadtekort.
- [ ] Waarschuwing voor overmatig lange of zware schroeven, ook wanneer ze geometrisch nog passen.
- [ ] Automatische montagevolgorde en benodigd bit-/sleutelgereedschap genereren.
- [ ] Herbruikbare verbindingstemplates maken die bij het afleiden van een nieuw product worden meegekopieerd.
