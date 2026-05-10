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
