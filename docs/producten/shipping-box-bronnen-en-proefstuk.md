# Shipping box — bronnen en proefstukdata

Status: **actuele productafspraak**.

## Vastgelegde productkeuzes

- Product-ID: `shipping_box`.
- De gebruiker voert de **binnenlengte, binnenbreedte en binnenhoogte** in.
- Plaatmateriaal en dikte zijn kiesbaar; standaard is `osb_18`.
- De zes platen zijn demontabel en vlak op te slaan.
- Handgrepen zijn optioneel en worden in beide zijpanelen uitgefreesd.

## Bouwwijze en varianten

De configurator biedt twee montageverbindingen. De oorspronkelijke variant volgt de montagehandleiding van SnapCrate:

- bodem en deksel: sponning aan vier zijden;
- twee zijpanelen: sponning aan de twee verticale kopse zijden;
- voor- en achterpaneel: geen sponning;
- het aantal clips groeit mee met de lengte van de kistnaden.

De variant `Zelfschalende montagetappen bij clips` vervangt de lange sponningbanen door lokale tap-/uitsparingparen:

- iedere clippositie krijgt één korte positioneertap;
- aantal en positie schalen mee met dezelfde maximale clipafstand;
- tapbreedte is duidelijk breder dan de clip en schaalt vanaf circa `2,8 × clipbreedte` met plaatdikte en beschikbare steek;
- ontvangende uitsparingen krijgen extra breedteruimte voor passing en freesradius;
- alle overgangen in tap en uitsparing zijn minimaal R3 en daarmee maakbaar met een Ø6-frees;
- voor/achter dragen de verticale hoektappen, de zijpanelen ontvangen deze;
- wandpanelen dragen de bodem-/dekseltappen, bodem en deksel ontvangen deze;
- vier gewone CAM-vasthoudtabs blijven los hiervan op de buitencontour aanwezig.

Bron: <https://www.snapcrates.com/files/2011/07/SnapCrate_instructions-Sept11.pdf>

## Geselecteerde leverancier

Voorkeursleverancier is Nanjing Liangyue Packaging Products Co., Ltd. De geselecteerde clipfamilie is model `LY103-12`, omschreven als een stalen, verchroomde, zware veerclip voor houten kisten. Liangyue ondersteunt maatwerk op basis van tekening of sample.

- clip: <https://liangyuepacking.en.made-in-china.com/product/EAOprPFJmGYT/China-Heavy-Duty-Spring-Clips-for-Secure-Wooden-Box-Assembly.html>
- OSB clipkist: <https://liangyuepacking.en.made-in-china.com/product/zACptqIdgora/China-Reusable-OSB-Wooden-Crate-with-Steel-Clips-Industrial-Heavy-Duty-Shipping-Pallet-Box-for-Precision-Instrument-Packing.html>

## Voorlopige clip- en sleufgeometrie

Liangyue publiceert voor `LY103-12` geen bruikbaar maatblad. Daarom is de globale clipvorm uit de leveranciersfoto's genomen en gekalibreerd tegen een vergelijkbaar C058-type met gepubliceerde buitenmaten 63 × 71 × 35 mm en plaatdikte 1,5 mm. Dat vergelijkingsproduct is **geen tweede voorkeursleverancier**; het is alleen een tijdelijke schaalreferentie.

| Parameter | Proefstukwaarde | Status |
|---|---:|---|
| Cliparmen | 63 / 71 mm | kalibratiereferentie |
| Clipbreedte | 35 mm | kalibratiereferentie |
| Metaaldikte | 1,5 mm | kalibratiereferentie |
| Clipsleuf | 32 × 8 mm | afgeleid, inmeten |
| Hart sleuf tot plaatrand | 32 mm | afgeleid, inmeten |
| Eindmarge clipverdeling | 100 mm | ontwerpregel, beproeven |
| Maximale clipafstand | 350 mm | ontwerpregel, constructief beproeven |
| Sponningbreedte | plaatdikte + 0,4 mm | proefpassing |
| Sponningdiepte | 0,5 × plaatdikte | proefpassing |

Schaalreferentie: <https://wierfab.en.made-in-china.com/product/eTNrDlpMsQUz/China-Metal-Crate-Clips-Spring-Snap-Clips-Retaining-Crate.html>

## Vrijgavevoorwaarden

Voor productievrijgave zijn minimaal nodig:

1. vijf tot tien fysieke `LY103-12` samples;
2. clipbuitenmaten, vrije veerhoek, haakbreedte en materiaaldikte inmeten;
3. CNC-proefstrook met meerdere sleuflengtes, sleufbreedtes en randafstanden;
4. montage-, demontage- en uittrekproef in OSB 18 mm;
5. bepaling van de clipafstand op basis van nettolading, kistmaat en transportbelasting;
6. draagproef van de optionele handgreep;
7. besluit over pallet/skids, vochtbescherming en exportnorm ISPM 15 als massief hout wordt toegepast.

Tot die validatie blijven component en kritische regels in de master op `Proefstuk` / `Te controleren` staan en blokkeren ze productievrijgave.
