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
