# Lade-pocket visualisatie - iteraties en lessen

Dit document legt vast wat geprobeerd is bij de 3D visualisatie van ladepockets/rabatten, zodat we deze route later niet opnieuw doorlopen.

## Stand van zaken

- Aantal iteraties aan ladevisualisatie: ongeveer 17.
- Werkbladpockets waren in 2-3 iteraties bruikbaar.
- De ladevisualisatie is nog niet productiegetrouw genoeg.
- De huidige fallback is bewust teruggezet naar de minder slechte variant met kleine visuele overlap.
- De productie-output/TAP-data kan wel pockets bevatten; het probleem zit in de klantvisualisatie/mesh.

## Wat wel werkt bij het werkblad

`buildPocketedSheetGeometry` werkt voor werkbladpockets omdat:

1. De plaat niet eerst als doos wordt gemaakt.
2. Er direct een 2D contour wordt opgebouwd.
3. De pocket wordt als inkeping in die contour verwerkt.
4. Daarna wordt de contour met `ExtrudeGeometry` over de derde as geextrudeerd.
5. Daardoor bestaat er nooit een volle doorlopende face over de pocket.

Belangrijk: dit werkt vooral goed voor pockets die als doorlopende randinkeping in de 2D contour passen.

## Wat geprobeerd is bij lades

1. Pocketdata toegevoegd aan ladefront, ladezijde, ladeachter en ladebodem.
2. Ladebodem en ladezijden groter/langer gemaakt zodat zij in de rabatten kunnen vallen.
3. Pockets in `Assembly3D` gemapt naar planes `z` en `x`.
4. Eerst losse overlay-/markeerblokjes gebruikt om pockets zichtbaar te krijgen.
5. Overlayblokjes verwijderd omdat dit geen echte mesh is.
6. `buildRecessedBoxGeometry` gemaakt: een box-face opdelen in cellen en pockets 3 mm dieper leggen.
7. Drawer-specifieke edge-lines toegevoegd om pocketranden te tonen.
8. Drawer-specifieke edge-lines weer verwijderd omdat ze lijnen verstopten of onechte lijnen gaven.
9. `EdgesGeometry` met threshold geprobeerd.
10. `EdgesGeometry` zonder threshold geprobeerd, gelijk aan werkblad-randweergave.
11. Pocket floors tijdelijk weggelaten om overlap te maskeren.
12. Pocket floors teruggezet omdat dit fysiek onjuist was.
13. Buitenfaces in `buildRecessedBoxGeometry` opgesplitst in segmenten rond pocketdieptes.
14. Een door-en-door `buildCutoutExtrudedGeometry` geprobeerd. Dit was fout: ladepanelen werden als contourgaten weergegeven in plaats van 3 mm pockets.
15. Die door-en-door cutout teruggedraaid.
16. Een aparte `buildLayeredPocketGeometry` geprobeerd om werkbladachtig per segment te extruderen.
17. Ook die route teruggedraaid omdat het visueel slechter werd en niet letterlijk dezelfde gedeelde werkbladfunctie gebruikte.

## Wat niet opnieuw proberen

- Geen losse overlay-meshes of transparante blokjes voor pockets.
- Geen edge-only oplossingen; lijnen lossen ontbrekende geometrie niet op.
- Geen drawer-only speciale lijnfunctie.
- Geen door-en-door cutout voor 3 mm rabatten.
- Geen aparte nieuwe pocketfunctie bouwen zonder hem eerst echt uit `buildPocketedSheetGeometry` te generaliseren.
- Geen wijzigingen aan meerdere assen tegelijk zonder een exacte as-mapping op papier.

## Waarschijnlijke echte oplossing

De werkbladmethode moet later echt worden gegeneraliseerd naar een gedeelde functie, bijvoorbeeld:

`buildPocketedExtrudedGeometry(THREE, profileAxis, thicknessAxis, extrudeAxis, pockets)`

Diezelfde functie moet dan worden gebruikt voor:

- werkblad: profiel-as `x`, dikte-as `y`, extrude-as `z`;
- ladefront/ladeachter: profiel-as `x`, dikte-as `z`, extrude-as `y` of de correcte lokale as na verificatie;
- ladezijden: profiel-as `z`, dikte-as `x`, extrude-as `y`.

Vooraf verplicht:

1. Exact vastleggen welke lokale as lengte, hoogte, dikte en extruderichting is per onderdeel.
2. Exact vastleggen aan welke zijde de 3 mm pocket ligt.
3. Eerst de bestaande werkbladoutput via de nieuwe algemene functie exact gelijk houden.
4. Pas daarna lades aansluiten.

## Huidige keuze

Voor nu houden we de ladevisualisatie liever minder mooi maar begrijpelijk, met de kleine overlap, dan een slechtere of misleidende poging met verkeerde gaten of extra frames.
