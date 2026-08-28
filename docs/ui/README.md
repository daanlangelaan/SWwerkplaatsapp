# UI-contracten

Status: **actueel contract**.

Dit document specificeert de portal. De normatieve beslisboom, AI-werkwijze en
acceptatiegrens staan in `../architecture/Data-eigenaarschap-en-UI-grens.md`.
De productbrede visuele, interactieve en toegankelijkheidsregels staan in
`Portal-designsysteem.md` en gelden voor iedere nieuwe of gereviseerde portalpagina.

## Doel

De portal blijft een dunne presentatielaag. Productkennis, technische maten en
toegestane keuzes worden nooit opnieuw in HTML, CSS, SVG of JavaScript bepaald.
Presentatiegedrag wordt evenmin verspreid als losse magic numbers.

## Drie gescheiden bronnen

| Soort | Voorbeelden | Enige bron |
|---|---|---|
| Product- en fabricagedata | materiaal-ID, profielraster, gatdiameter, boutmaat, aantallen, grenzen, defaults | `config/product-master-data.xlsx` via `config/runtime/masterdata-runtime.json` en backendcontracten |
| Renderdata | exacte sleufassen, kernboringen, hardware-envelope, toegangsgaten, verbindingsnodes | backend-rendercontract, afgeleid van de product- en fabricagedata |
| Presentatie | kleur, lijndikte, ruimte, camera-fit, animatieduur, easing, labelgrootte | `config/ui/presentation-contract.json` |

SVG-coördinaten, layoutmaten, kleuren, animatietijden en camerawaarden zijn
presentatie. Zij horen niet in productmasterdata, maar worden wel centraal beheerd
en getest.

## Verantwoordelijkheid

- HTML maakt algemene werkvlakken, formulieren en acties.
- De catalogus-API levert producten, velden, keuzes, defaults en grenzen.
- Het assemblageplan levert profielen, verbindingen, gereedschap en technische waarden.
- Het rendercontract levert direct tekenbare geometrie.
- De presentatieconfig bepaalt uitsluitend hoe die informatie wordt getoond.

De interactieve 360-weergave in de portal en het zelfstandige offline
`Aanzichten/3D-model.html` gebruiken dezelfde WebGL-renderfuncties uit
`PortalHtml` en hetzelfde `PortalAssemblyPart`- en `PortalMotionContract`.
Een klantexport mag voor deze route geen tweede blokken-, SVG- of
SolidWorks-geometrie-engine introduceren. SolidWorks/GLB blijft een afzonderlijk
hoogwaardig CAD-presentatieproduct, niet de bron van de interactieve portalview.

De projectexport biedt deze twee klantmodellen afzonderlijk aan. `Interactief
3D` schrijft de gedeelde portal-WebGL-view met bewegingsregelaars naar
`03_Klantvoorstel/Aanzichten/3D-model.html`. `High-definition 3D + SW-bron`
schrijft het SolidWorks/GLB-model naar `03_Klantvoorstel/3D-high-definition/`
en bewaart altijd de bijbehorende native documenten in `02_SolidWorks` als
technisch naslagwerk. `Klantvoorstel` blijft onafhankelijk daarvan eigenaar van
de statische aanzichten en PDF/PPT-bijlage.

## Verboden in `PortalHtml`

- materiaal-, component-, leverancier- of artikel-ID's;
- productgebonden standaardwaarden en invoergrenzen;
- bout-, sleutel-, tap-, gat-, sleuf- of profielmaten;
- een technische fallback wanneer backenddata ontbreekt;
- afleiding van profielraster, sleufas of verbindingsaantal uit een bounding box.

Bij ontbrekende technische data toont de UI een expliciete onvolledige status. De
UI vult nooit stilzwijgend een bekende waarde zoals M8, SW5, 7 mm of 40 mm in.

## Wijzigingsroute

1. Productdata wijzigen in de Excel-masterdata en runtime genereren.
2. Backendcontract of rendercontract aanpassen.
3. Indien uitsluitend de uitstraling verandert: presentatiecontract aanpassen.
4. Contracttests en gerichte screenshots uitvoeren.
