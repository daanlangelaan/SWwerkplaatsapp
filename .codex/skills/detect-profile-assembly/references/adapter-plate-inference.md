# Functionele adapterplaten uit beelden afleiden

## Doel en bewijsgrens

Reconstrueer eerst wat de plaat doet en pas daarna hoe zij eruitziet. Een herkenbare silhouetkopie is onvoldoende wanneer bevestigingsvlakken, gaten of instelbereik niet kloppen. Behandel een beeldafleiding als een versiehypothese met `confirmed`, `probable` of `unresolved` per maat en functie.

Gebruik bewijs in deze volgorde:

1. fabrikanttekening, boorpatroon of native CAD;
2. meerdere aanzichten met bekende profielmoduulmaat;
3. zichtbaar bout-, moer-, draaipunt- of sleufhart;
4. contact met een reeds gereconstrueerd profielvlak;
5. silhouet, coatinggrens en lichtlijn.

Leid plaatdikte niet betrouwbaar af uit één perspectiefbeeld. Kies een voorlopige maakbare dikte op basis van belasting, vrije boutlengte en beschikbaar materiaal, en blokkeer definitieve vrijgave tot controle of proefstuk.

## Functionele grafiek

Maak voor iedere plaat een stabiel record met:

- plaatrol en fysieke hoeveelheid;
- gekoppelde onderdelen en exact benoemde contactvlakken;
- belastingpad tussen die vlakken;
- vastgelegde vrijheidsgraden en gewenste verstelrichting;
- buitengrens of minimale omhullende;
- materiaalkandidaat en dikte met status;
- spiegeling: identiek, gespiegeld of werkelijk verschillend;
- productieproces en relevante gereedschapsradius;
- bron en confidence per aanname.

Classificeer ieder gat of iedere sleuf afzonderlijk als:

- `fixed-fastener`: vaste profiel- of plaatverbinding;
- `pivot`: rotatiehart;
- `adjustment-slot`: lineaire of benaderd gebogen verstelling;
- `equipment-interface`: productspecifiek patroon uit fabrikantdata;
- `tool-access` of `clearance`: alleen opnemen wanneer toegang of botsingsvrijheid dit vereist;
- `decorative/lightening`: standaard weglaten bij een vereenvoudigde uitvoering.

Een gat zonder functionele eigenaar blijft `unresolved` en mag niet stilzwijgend in de productiegeometrie komen.

## Geometrie en T-slotlogica

Kalibreer plaatpunten op hetzelfde perspectiefvlak via homografie of bekende 40-mm slotsteek. Projecteer geen globale pixel/mm-schaal over verschillende vlakken. Koppel profielbevestigingen aan werkelijke T-sleufhartlijnen en geldige 40-mm moduulbanen; een gat dat visueel in het midden van een 80-mm vlak valt kan tussen twee sleuven liggen en is dan ongeldig.

Voor een instelbare verbinding:

1. leg het draaipunt of vaste referentiegat vast;
2. bepaal begin- en eindstand uit de gekoppelde delen;
3. bereken de baan van het tweede bevestigingspunt;
4. gebruik een rechte capsulesleuf alleen wanneer de afwijking ten opzichte van de werkelijke boog binnen de gekozen passing/tolerantie blijft;
5. gebruik anders een boogsleuf of discrete gaten en leg die keuze vast.

Productspecifieke interfaces, zoals een stuurwielbasis, komen uit de officiële montagetekening. Neem maximale inschroefdiepte en plaatdikte mee in de boutlengte; gebruik nooit nabijgelegen accessoiregaten als constructieve montagepunten zonder fabrikantbevestiging.

## Strategisch vereenvoudigen

Maak eerst een `keep-out`-kaart rond:

- alle gaten en sleuven plus benodigde randafstand;
- contactvlakken en drukverdelingszones;
- verwachte krachtlijnen tussen bevestigingsgroepen;
- bewegende delen en gereedschapstoegang.

Vorm daarna de buitencontour als de kleinste eenvoudige, afgeronde veelhoek die deze zones omvat. Verwijder interne uitsparingen tenzij hun functie aantoonbaar is. Behoud voldoende materiaal tussen gaten, sleufuiteinden en buitenrand volgens gekozen materiaal, dikte, belasting en maakproces; gebruik geen universele pixelafgeleide randafstand.

De vereenvoudigde plaat moet minder unieke bewerkingen hebben dan de referentie en toch dezelfde interface- en verstelfuncties leveren. Noteer expliciet welke zichtbare uitsparingen zijn verwijderd en waarom.

## Machineleesbaar contract en controle

Leg per plaatfamilie minimaal vast:

- stabiele ID en revisie;
- `expected_count` en spiegelrelatie;
- materiaal, dikte en contourpunten;
- gaten/sleuven met type, hart, maat, referentievlak en functionele eigenaar;
- gekoppelde profielrollen en T-slotbanen;
- bewijs/confidence en open proefstukpunten.

De bouw, BOM, nesting/CAM en 3D-assembly moeten hetzelfde record gebruiken. Regressie blokkeert bij een verkeerd aantal, ontbrekende contour, ontbrekend functioneel gat, ongeldige slotbaan, verkeerde plaatzijde of ongeautoriseerde decoratieve feature. Visuele goedkeuring vereist minstens twee aanzichten, maar vervangt de numerieke plaat- en gatencontrole niet.
