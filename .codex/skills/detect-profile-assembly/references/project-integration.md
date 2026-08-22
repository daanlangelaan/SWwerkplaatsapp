# Integratie met SWwerkplaatsapp

## Exacte bron

De constructie wordt primair beschreven door `WorkbenchModel.AssemblyPlacements`. `PortalAssembly3DService` zet die om naar portalonderdelen met `Name`, `Kind`, `Shape`, een middelpunt (`Xmm`, `Ymm`, `Zmm`) en buitenmaten (`SizeXmm`, `SizeYmm`, `SizeZmm`). Voor profielcontrole is `Kind == "profile"` leidend.

Bereken vlakken als `X- = Xmm - SizeXmm/2`, `X+ = Xmm + SizeXmm/2`, en idem voor Y en Z. De langste maat is normaal de profielas; bevestig dit met materiaalcode of onderdeelnaam bij korte profielen.

## Exacte audit uitvoeren

Exporteer portal-assemblydelen als JSON-array of als object met `AssemblyParts`, `assemblyParts`, `Parts` of `parts`:

```powershell
python .codex/skills/detect-profile-assembly/scripts/profile_geometry_audit.py assembly.json --output profiel-audit.md --json-output profiel-audit.json
```

De auditor classificeert bekende 40-mm moduuldoorsneden, rapporteert bounding faces en zoekt contacten, vlakgelijkheid, nabije spleten en volume-overlap.

De auditor filtert op profielen. Controleer daarom een gerapporteerde spleet tegen de volledige assembly: bij de robotcel is de 10-mm ruimte tussen achterligger en achterrail bijvoorbeeld bewust gevuld door het werkblad.

Leg naast de geometrie-audit een bouwlaagmanifest vast en controleer dit met `scripts/profile_layer_manifest_check.py`. Rollen worden gematcht op een expliciete naam/prefix en kunnen zowel profielen als accessoires omvatten. Zo blijft bijvoorbeeld `onderlaag / onderste dwarsligger / 40x80 staand, Y=80 / verwacht 1` door analyse, modelbouw, BOM en regressie dezelfde eis. Voeg in projectcode een build-time assert toe voor zowel telling als numerieke doorsnede-oriëntatie.

Voor ongelijke profielbreedtes moet projectcode discrete 40-mm moduulbanen gebruiken. Een 40-mm ligger op een 80-mm staander krijgt langs de betreffende dwarsas alleen een centrum op `staanderhart - 20` of `staanderhart + 20`; de gekozen positie wordt semantisch opgeslagen als buiten/front gelijk of één moduul verdiept. Een centrum gelijk aan het staanderhart blokkeert acceptatie.

## Regressievergelijking

Leg de gereconstrueerde referentie één keer vast als assembly-JSON met stabiele, unieke profielrollen. Audit daarna referentie en actuele output en vergelijk beide rapporten:

```powershell
python .codex/skills/detect-profile-assembly/scripts/profile_geometry_compare.py verwacht-audit.json actueel-audit.json --output profiel-delta.md --json-output profiel-delta.json
```

De vergelijker controleert profielinventaris, as, doorsnede-oriëntatie, buitenvlakken en contact-face pairs. Een build mag pas visueel worden goedgekeurd wanneer de exacte delta geen onverklaarde verschillen bevat.

## Rendercontrole

Controleer na de exacte audit een isometrisch vooraanzicht, een gedraaid achter-/zijaanzicht en een onderaanzicht als dwarsliggers of voetverbindingen relevant zijn. Loop de profieltabel ID voor ID af en controleer lengte, richting, doorsnede-oriëntatie en aansluitvlak.

## Pixelprobe uitvoeren

Gebruik de gebundelde workspace-Python wanneer het gewone Python geen OpenCV bevat:

```powershell
& 'C:\Users\dylan\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' .codex/skills/detect-profile-assembly/scripts/profile_pixel_probe.py afbeelding.png --crop 100,200,800,300 --scan 130,260,820,260 --overlay profiel-lijnen.png --json-output profiel-lijnen.json
```

Kies de crop rond één profiel of één coplanaire profielgroep. Dat vermindert foutieve lijnen van bladkanten, schaduwen en UI-randen.

## Projectacceptatie

- Elk primair profiel heeft een unieke ID en bevestigde as.
- Het rapport onderscheidt fysieke profieldelen van unieke profielspecificaties; sleuflijnen en randhighlights worden niet als extra delen geteld.
- Het bouwlaagmanifest bevat verwachte aantallen per rol; de kandidaat heeft voor elke rol een delta van nul.
- De twee doorsnedematen zijn expliciet aan modelassen gekoppeld.
- Geen onverwachte overlap of spleet groter dan de gekozen tolerantie.
- Elk bedoeld dragend knooppunt bestaat als vlakcontact in de contactgrafiek; lijn- of puntcontact is onvoldoende.
- Elk contact gebruikt dezelfde face pair als de gereconstrueerde referentie.
- Elk `flush_with`-oordeel noemt de vergeleken buitenvlakcoördinaten; gelijke hartlijnen gelden niet als bewijs.
- Iedere verbinding tussen een 40-mm en 80-mm profielbreedte bezet een geldige 40-mm moduulbaan; centrumuitlijning is afgekeurd.
- Alle blootgestelde profieluiteinden zijn geïnventariseerd en hebben een serie/type-compatibele afdekkap of een expliciet geaccepteerde open-eindreden.
- De exacte referentie-candidate delta bevat geen onverklaarde ontbrekende, extra of verkeerd georiënteerde profielen.
- Een exacte delta van nul wordt verworpen wanneer de gelijkhoekige render nog een referentie-vreemde silhouetstap of moduulverdeling toont.
- De render toont dezelfde topologie vanuit minstens twee hoeken.
