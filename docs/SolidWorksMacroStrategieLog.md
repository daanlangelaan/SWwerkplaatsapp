# SolidWorks macro strategie-log

Doel: voorkomen dat we oude SolidWorks macro-strategieen opnieuw proberen zonder te zien wat er eerder gebeurde.

## Werkwijze

- Lees dit bestand voordat `SolidWorksMacroExporter` of part-orientaties worden aangepast.
- Voeg elke poging toe met datum, status, symptoom en besluit.
- Als een strategie faalt in SolidWorks, markeer hem expliciet als "niet opnieuw proberen" tenzij de onderliggende oorzaak eerst is opgelost.

## Strategieen

| Datum | Onderdeel | Strategie | Status | Observatie | Besluit |
| --- | --- | --- | --- | --- | --- |
| 2026-04-30 | Werktafel platen | Horizontale plaat op Top Plane tekenen en in assembly direct plaatsen | Werkt | Werkblad, onderblad en tussenblad liggen goed op de profielen. | Behouden als basis voor horizontale platen. |
| 2026-04-30 | Werktafel profielen | Profielen in part-richting tekenen en met directe coordinaten in assembly plaatsen | Werkt | Assembly is stabiel en onderdelen sluiten na toevoegen. | Behouden. |
| 2026-05-10 | Cabinet `VERTICAL_Z` zijwanden/tussenschotten | Basisvlak op Right Plane tekenen en dikte extruderen | Faalt | SolidWorks toont: `Kan sheet basis niet extruden ... Orientatie: VERTICAL_Z`. | Niet opnieuw proberen als basisschetsroute. |
| 2026-05-10 | Cabinet `VERTICAL_Z` zijwanden/tussenschotten | Basis op Front Plane tekenen als dikte x hoogte, daarna diepte als extrude-richting | Actief | Dit vermijdt de Right Plane basis-extrude fout en maakt het part al in de assembly-richting. | Huidige baseline. |
| 2026-05-10 | Cabinet gaten/plint in verticale platen | Gaten en plintuitsparingen op hetzelfde basisschetsvlak forceren | Faalt/deels fout | Gaten/plintlijnen komen op verkeerde plane of worden niet betrouwbaar gecut. | Niet gebruiken voor face-bewerkingen. |
| 2026-05-10 | Cabinet gaten/plint in verticale platen | Aparte face/offset-plane per bewerkingszijde maken, sketch daarop, daarna cut through thickness | Gepland | Past bij hoe de delen in de echte orientatie staan en voorkomt basisschets-conflicten. | Volgende stap na stabiele basispart-generatie. |
| 2026-05-10 | Cabinet `VERTICAL_Z` gaten/plint | Basispart blijft Front Plane; bewerkingen op offset-plane parallel aan Right Plane op `thickness / 2` | In test | Macro maakt `PlintuitsparingVlak`, `Kopkamervlak_VerticaalZ` en `Gatenvlak_VerticaalZ` als aparte ref-planes. Cirkels/rechthoek worden in Y/Z getekend en daarna door dikte gecuts. | Alleen verder tunen als SolidWorks nog meldt dat `InsertRefPlane` of `FeatureCut3` faalt. |
| 2026-05-10 | Cabinet `VERTICAL_Z` gaten/plint | Face-plane sketch tweezijdig of middenvlak-cut | Faalt/deels fout | De sketch ligt op het plaatoppervlak, niet in het midden. Tweezijdig cutten is daarom conceptueel fout en kan buiten het materiaal snijden. | Niet opnieuw gebruiken voor deze face-bewerkingen. |
| 2026-05-10 | Cabinet `VERTICAL_Z` gaten/plint | Face-plane sketch op oppervlak, daarna eenzijdig naar binnen cutten met omgekeerde richting als eerste poging | In test | Past bij de huidige part-opbouw: basispart in assembly-orientatie, face-plane op `thickness / 2`. Plintuitsparing wordt als gesloten rechthoek gemaakt. | Testen in SolidWorks; als de cut-richting nog verkeerd is, alleen de eerste/backup richting wisselen, niet terug naar middenvlak-cut. |
| 2026-05-10 | Cabinet `VERTICAL_Z` face-cuts | FeatureCut3 direct vanuit de actieve face-sketch uitvoeren | Faalt | SolidWorks maakt de face-plane en de schets, maar gaten/plint worden niet gecut; gebruiker hoort fouttoon. Plint kan als losse lijn zichtbaar blijven. | Niet opnieuw direct vanuit open sketch cutten; eerst schets sluiten en exact die schets selecteren. |
| 2026-05-10 | Cabinet `VERTICAL_Z` plintuitsparing | `CreateCornerRectangle` op verticaal offset-plane | Faalt/deels fout | Op dit vlak kan de plintuitsparing als streep/ongeldige contour eindigen. | Niet gebruiken voor plint op `VERTICAL_Z`; altijd vier expliciete lijnen tekenen. |
| 2026-05-10 | Cabinet `VERTICAL_Z` face-cuts | Actieve schets object ophalen, schets sluiten, exact die schets selecteren, dan eenzijdig naar binnen cutten met richtingsfallback | In test | Past bij de observatie dat het plane en de schets bestaan maar de feature niet ontstaat. Eerste poging gebruikt de geflipte richting, daarna de andere richting, daarna een double-depth fallback. | Testen in SolidWorks; als dit nog faalt, de volgende stap is selectie/logging per sketchnaam uitbreiden in de gegenereerde macro. |
| 2026-05-10 | Cabinet `VERTICAL_Z` face-plane schetsen | Modelcoordinaten direct aan `CreateLine`/`CreateCircleByRadius` geven op offset-plane | Faalt | SolidWorks interpreteert coordinaten op een actieve face/offset-plane als sketchruimte. Daardoor wordt `faceX = 9mm` lokale sketch-X en verandert de plintrechthoek in een middenstreep. | Niet opnieuw doen; modelpunten altijd met `ISketch.ModelToSketchTransform` naar actieve sketchruimte omzetten. |
| 2026-05-10 | Cabinet `VERTICAL_Z` face-plane schetsen | Modelpunten via actieve sketch-transform omrekenen en dan lijnen/cirkels in 2D sketchruimte tekenen | In test | De SolidWorks API biedt `ModelToSketchTransform` voor model-naar-sketch conversie. Dit moet plintcontouren en gaten op offset-planes uit de lokale-coordinate val halen. | Huidige strategie voor alle `VERTICAL_Z` face-plane lijnen en cirkels. |
