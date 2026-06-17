# Portal 3D pocket strategie

Deze notitie is leidend bij wijzigingen aan de 3D visualisatie van pockets, rabatten en verdiepingen in het klantportaal.

## Doel

Een plaatdeel met een pocket blijft in de viewer een plaatdeel. De pocket wordt uit de mesh van dat plaatdeel opgebouwd, niet als los transparant blokje, markering of extra laag bovenop het onderdeel.

## Regels

1. Een productieplaat is een visuele mesh.
2. Een pocket is geometrie op de betreffende plaatface:
   - buitenvlak opdelen in cellen rond de pocket;
   - het pocketvlak zelf niet als volle face tekenen;
   - pocketbodem tekenen op `pocket depth`;
   - vier pocketwanden tekenen tussen buitenvlak en pocketbodem.
3. Geen losse overlay-boxes gebruiken voor pockets op parts die al als pocketed mesh worden gebouwd.
4. Plaatdelen die in een pocket vallen mogen doorlopen tot in de pocketdiepte, maar de ontvangende plaat moet de pocket echt uit de mesh hebben.
   - Het instekende onderdeel blijft op werkelijke/productiemaat zichtbaar.
   - Geen visuele inkorting toepassen om overlap te verbergen.
5. Horizontaal, verticaal-X en verticaal-Z gebruiken dezelfde strategie:
   - horizontaal: face-as `y`;
   - verticaal voor/achter: face-as `z`;
   - verticaal zijdeel: face-as `x`.
6. Eerst controleren welke `Plane` een pocket heeft voordat je bepaalt welke meshbuilder wordt gebruikt.
7. UI-markeringen zijn alleen aanvullend. Ze vervangen nooit echte pocket-geometrie.

## Praktische checklist bij revisies

- Controleer `Assembly3D[].Pockets` voor het onderdeel dat je wijzigt.
- Controleer of `buildThreeParts` naar een echte pocket-mesh route gaat.
- Controleer dat er geen fallback-overlay wordt toegevoegd voor datzelfde onderdeel.
- Controleer dat de pocket-face niet ook nog als volle box-face bestaat.
- Controleer bij lades ook het instekende onderdeel: bodem, achterzijde en zijdelen moeten in de viewer dezelfde maat houden als in de productie-output.
- Controleer met een ladefront, ladezijde, ladeachter, werkblad en achterwand.
