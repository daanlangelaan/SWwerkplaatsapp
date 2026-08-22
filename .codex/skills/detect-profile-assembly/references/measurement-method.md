# Meetmethode voor profielconstructies uit beelden

## 1. Bewijs rangschikken

Gebruik bewijs in deze volgorde:

1. CAD- of assemblycoördinaten en expliciete maatvoering.
2. Zichtbaar eindvlak met herkenbare moduulverdeling.
3. T-sleufhartlijnen en bekende 40-mm steek.
4. Verhouding tussen de twee zichtbare profielvlakken.
5. Aansluitingen op reeds bevestigde profielen.
6. Silhouet en lichtlijnen.

Een lichtlijn langs een profielrand kan op een T-sleuf lijken. Een niet-zichtbare sleuf bewijst niet dat het vlak slechts 40 mm breed is.

## 2. Beeldassen bepalen

- Orthografisch: bepaal welke beeldrichting overeenkomt met model-X, -Y en -Z.
- Perspectief: groepeer lange parallelle constructielijnen en bepaal per groep het verdwijnpunt. De drie dominante groepen zijn kandidaat-assen.
- Gebruik per hoofdvlak een eigen homografie of schaal. Diepte-afmetingen hebben in perspectief geen constante pixel/mm-verhouding.
- Corrigeer lensvervorming wanneer rechte lange profielen zichtbaar krom lopen.

## 3. Eén profiel segmenteren

Werk per profielcrop en ken een ID toe, bijvoorbeeld `P01`.

1. Volg de twee buitenste langsranden.
2. Zoek T-sleufhartlijnen die dezelfde verdwijnrichting volgen.
3. Markeer het zichtbare begin en einde.
4. Noteer occlusies afzonderlijk; trek een verborgen eindpunt alleen door tot een geometrisch onderbouwd aansluitvlak.
5. Controleer dezelfde ID in een tweede aanzicht.

## 4. Profieltype en oriëntatie

Veelgebruikte 40-mm moduuldoorsneden:

| Familie | Moduulvlak | Kenmerkende verhouding |
|---|---:|---:|
| 40x40 | 1 x 1 | 1:1 |
| 40x80 | 1 x 2 | 1:2 |
| 80x80 | 2 x 2 | 1:1 met twee modules per vlak |
| 40x160 | 1 x 4 | 1:4 |

Tel moduulvelden via sleufhartlijnen en buitenranden. Bepaal daarna welk doorsnedevlak naar model-X, -Y of -Z wijst. Schrijf bijvoorbeeld: `P03: as X, doorsnede Y=80, Z=40`.

Een vierkante projectie is niet automatisch 40x40: het kan een 80x80-profiel op schaal of een eindaanzicht van een ander profiel zijn. Gebruik een bekende maat, slotsteek of aansluiting voor schaal.

Tel eerst fysieke volumes en pas daarna beeldlijnen. Meerdere parallelle lijnen op één 80-mm vlak zijn doorgaans buitenranden plus T-sleuflijnen van hetzelfde profiel. Alleen een afzonderlijk silhouet met eigen begin/eindvlak of een bevestigde onderbreking rechtvaardigt een extra profielrecord.

## 5. Pixels gebruiken

Kalibreer bij voorkeur met twee bekende punten op hetzelfde vlak:

`mm_per_pixel = bekende_afstand_mm / pixelafstand`

Als alleen T-sleuven beschikbaar zijn, gebruik de 40-mm moduulsteek als kandidaatkalibratie en bevestig die met een tweede maat. Meet loodrecht op de profielas na perspectiefcorrectie.

`profile_pixel_probe.py` kan lange rand- en sleuflijnen voorstellen, een crop afzonderlijk analyseren, langs een scanlijn donkere minima en pixelafstanden rapporteren, en een controle-overlay maken. Lijnsegmenten en minima zijn meetkandidaten, geen definitieve profielgrenzen.

## 6. Relaties vastleggen

Maak een contactgrafiek. Noteer voor elk profiel:

- `terminates_at`: eindvlak stopt tegen een ander profielvlak;
- `continues_past`: profiel loopt door voorbij de aansluiting;
- `flush_with`: buitenvlakken liggen in hetzelfde vlak;
- `offset_from`: bekende verspringing;
- `gap_to`: positieve ruimte;
- `overlaps`: geometrische doorsnijding.

Maak onderscheid tussen vlakcontact, lijncontact en puntcontact. Als een ligger zowel bij de binnenzijde als precies op de bovenzijde van een staander eindigt, kan de render aangesloten lijken terwijl de volumes slechts langs één rand raken.

`Gelijkliggend` moet altijd aan één benoemd buitenvlak en één moduulbaan worden gekoppeld. Een 80-mm profielvlak bevat twee 40-mm banen. Een aansluitend 40-mm profiel mag exact de buiten/frontbaan of de aangrenzende binnen/achterbaan bezetten. De geldige hartposities liggen 20 mm aan weerszijden van het 80-mm profielhart en verschillen onderling precies 40 mm. Het 40-mm profiel op het 80-mm profielhart plaatsen is ongeldig: dan valt het tussen de beide T-sleufhartlijnen. Controleer behalve de silhouetlijn daarom de werkelijk samenvallende T-sleufhartlijn en benoem de gekozen baan (`outer/front flush` of `one-module recessed`).

Gebruik een tolerantie passend bij de bron: circa 0,5 mm voor exacte CAD-data, 1–2 mm voor gegenereerde maatmodellen, of een uit pixelonzekerheid afgeleide tolerantie voor beelden.

## 7. Referentie vergelijken

Maak de referentie-inventaris vóórdat je de kandidaatcode leest of wijzigt. Zo voorkom je dat bestaande aannames de waarneming kleuren. Geef profielen semantische rollen en vergelijk daarna:

| Controle | Voorbeeld |
|---|---|
| ontbrekend/extra | onderste ring ontbreekt |
| familie | 40x80 versus 80x80 |
| as | ligger X versus Z |
| oriëntatie | 40x80 staand versus vlak |
| bereik | tussen staanders versus doorlopend |
| vlak | bovenvlak gelijk of versprongen |
| knoopvlak | ligger-eindvlak tegen staander-zijvlak |

Een intern geldige kandidaatcontactgrafiek bewijst niet dat de referentie is gevolgd. Vergelijk ook de profielrollen, aantallen en specifieke face pairs.

Voer vóór de eerste kandidaatvergelijking een onafhankelijke bewijs-gate uit: noteer per doorsnede welk zichtbaar vlak één, twee of vier 40-mm modules bevat en welk buitenvlak doorloopt over de knoop. Gebruik kandidaatcoördinaten nooit als bewijs voor de referentie. Als de pixel-/sleufwaarneming en het referentiemanifest botsen, is het manifest afgekeurd—ook bij een delta van nul.

Rapporteer bij herhaalde leden zowel het aantal fysieke leden als het aantal profieltypen. Voorbeeld: een robotcel-onderframe met vier omtrekliggers en één dwarsligger bevat vijf fysieke profielen, maar slechts één specificatie wanneer alles `40x80 staand` is. Dit voorkomt dat T-sleuven of dwarsliggers ten onrechte als een extra profielsoort of liggerlaag worden uitgelegd.

Bevries vóór de bouw een telmanifest per laag en rol. Een correcte regel is bijvoorbeeld `onderlaag / lower-crossmember / 40x80 staand, Y=80 / verwacht 1`. Bewaar onzekere aantallen als expliciet `unresolved` met bewijsbehoefte; vertaal ze nooit stilzwijgend naar nul. Vergelijk na iedere iteratie het werkelijke aantal met het verwachte aantal en rapporteer de delta. Controleer daarnaast de axis mapping numeriek: een juist aantal met `Y=40` blijft een fout resultaat.

Maak tevens een eindvlakkenlijst. Classificeer elk uiteinde als verbonden, afgedekt, bewerkt/bevestigd of open. Voor ieder open uiteinde controleer je of een eindkap zichtbaar of constructief vereist is. Een gekozen eindkap moet dezelfde serie/type en doorsnede hebben als het profiel; een 160x40-kap uit een andere groefserie is niet automatisch passend.

Versienummer de referentiemanifestatie per visuele ronde. Noteer per gewijzigde aanname bijvoorbeeld: `R1 onderring vlak, waarschijnlijk` → `R2 onderring staand, bevestigd door frontvlak/staanderbreedte`. Een delta van nul tegen een fout manifest is geen geldige goedkeuring.

## 8. Oplevering

Lever minimaal twee profieltabelen, een delta, contactgrafiek, onzekerhedenlijst en gelabelde overlays wanneer een referentie en kandidaat aanwezig zijn. Splits waarneming en conclusie: bijvoorbeeld `twee sleufhartlijnen zichtbaar` versus `waarschijnlijk 80-mm vlak`.
