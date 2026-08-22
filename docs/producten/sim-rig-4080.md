# Sim-racing-rig 40×80

Status: **actueel productcontract — referentiehypothese R1, proefbouw vereist**.

## Bronnen en scope

De productvorm is afgeleid uit zeven door de gebruiker aangeleverde aanzichten van de Aluxprofiel 4080-rig. De buitenmaten 1350 × 680 × 660 mm en het gebruik van T-verbindingsplaten komen van de [Aluxprofiel-productpagina](https://www.aluxprofiel.nl/sim-racing-rig-4080-zilver-csl-dd-sidemount/a4118). De M6-zijmontage en maximale inschroefdiepte van 10 mm komen uit de officiële [Fanatec CSL DD Hard-Mount Planner](https://assets.fanatec.com/image/upload/v1743547943/downloads-prod/pdfs/CSL-DD_Hard-Mount-Planner_01.pdf).

Stuurwielbasis, pedalen en stoel zijn klantapparatuur en geen BOM-onderdelen. De rig levert alleen hun montage-interfaces.

## Bevroren R1-opbouw

- 2 × 40×80 basislangsligger, vlak: model X=80 en Y=40;
- 3 × 40×80 basisdwarsligger, vlak: model Y=40 en Z=80;
- 2 × 40×80 stuurstaander: model X=40 en Z=80;
- 1 × 40×80 stuurbrug, staand: model Y=80 en Z=40;
- 3 × 40×80 pedaalprofiel, vlak en gezamenlijk gekanteld;
- 6 custom platen: twee stuurzijplaten, twee staander-T-platen en twee pedaalhoekplaten;
- 6 seriepassende 40×80-eindkappen.

De machineleesbare versie staat in `config/sim-rig-4080-assembly-manifest.json`.

## Custom plaatstrategie

De referentie bevat veel decoratieve uitsparingen. R1 behoudt alleen:

- contact- en belastingvlakken;
- profielbevestiging op geldige T-slotbanen;
- draaipunten;
- noodzakelijke instelsleuven;
- de gekozen wheelbase-interface.

De huidige platen zijn S235 10 mm kandidaten. De CSL-DD-uitvoering gebruikt M6x20: 10 mm plaat plus maximaal 10 mm inschroefdiepte. De voorste accessoiregaten van de wheelbase mogen niet voor hard mounting worden gebruikt. De blanco uitvoering laat de twee productspecifieke M6-gaten weg, maar behoudt de profielverstelling.

## Open proefbouwpunten

Voor vrijgave moeten worden gemeten en vastgelegd:

1. exacte 40×80-profielserie en compatibele T-moeren/eindkappen;
2. fysieke CSL-DD-passing en boutvrije ruimte in alle stuurhoeken;
3. bereik en klemkracht van stuur- en pedaalsleuven;
4. pedaalplaatbelasting bij maximale hoek en zwaarste ondersteunde pedalen;
5. randafstanden en vervorming van de 10-mm platen;
6. stoel- en pedaalergonomie over de volledige parameterband.

Zolang `VAL-SIM-004` open staat, is het product geschikt voor configuratie, calculatie en proefbouw, maar niet voor ongecontroleerde serieproductie.
