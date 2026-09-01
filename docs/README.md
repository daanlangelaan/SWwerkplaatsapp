# Documentatie-index

Gebruik actuele contracten en beheerhandleidingen als beslisbron. Iteratieverslagen en toekomstideeën zijn context, geen runtimecontract.

## Actuele contracten

- `Masterdata-beheer.md`: product-, component-, leveranciers-, prijs- en afbeeldingsbeheer.
- `architecture/App-structuur.md`: lagen, verantwoordelijkheden en groeipad.
- `architecture/Repository-structuur.md`: canonieke mappen, runtime-data en gegenereerde bestanden.
- `architecture/Data-eigenaarschap-en-UI-grens.md`: beslisboom en verplichte migratieroute voor masterdata, backendcontracten en presentatie.
- `architecture/SolidWorks-worker.md`: subprocesscontract, time-out en retry voor SolidWorks.
- `architecture/Operationele-opslag.md`: SQLite-contract, bestandsmirror, back-up en schaalgrens.
- `architecture/Productwebsites-en-gedeelde-backend.md`: meerdere productwebsites, sitecontext, gebruikers en één interne waarheid.
- `architecture/Portal-werkruimten.md`: actuele portalroutes, rolgrenzen, projectdossier, werkplaats, inkoop, voorraad en klantcontract.
- `drawing-strategy/Tekencontract.md`: teken- en oriëntatiecontracten.
- `drawing-strategy/Code-structuur.md`: huidige lagen van de tekenlogica.
- `drawing-strategy/Profielvisualisatie-contract.md`: visuele laagopbouw en regressiecontract voor aluminium profielen en assemblage-instructies.
- `manufacturing/Profiel-CNC-en-stickeroutput.md`: canonieke freesvolgorde, stickerplaatsing, CNC-operatorstops en printerneutrale Excel-uitvoer.
- `deployment/Lokale-server.md`: lokale portalconfiguratie.
- `klantbijlage-designstandaard.md`: klantdocumenten en presentatie-output.
- `ui/README.md`: grens tussen productdata, renderdata en pure presentatie.
- `ui/Portal-designsysteem.md`: productbrede UI/UX-, component-, rollen- en toegankelijkheidsregels voor de portalrevisie.
- `ui/Assemblagevisualisatie.md`: semantische rollen en rendercontract van de assemblage-assistent.
- `ui/3D-camera-en-animatie.md`: camera-, interactie- en animatiecontract zonder productlogica.

## Productafspraken

Productgebonden invoer, bronnen en proefstukstatus staan onder `producten/`. Productdocumentatie vervangt geen machineleesbare productregel in `config/product-master-data.xlsx`.

## Toekomst en archief

- `future/`: ideeën en concepten waarover nog geen bouwbesluit is genomen.
- `future/Portal-pilot-beslissingen.md`: opgespaarde bedrijfs-, rol-, voorraad-, document- en uitrolkeuzes voor de pilot.
- `future/Assemblagehandleiding-systeemprofielen.md`: bevestigde standaardverbinderlogica, benodigde verbindingsgraaf en beeldgerichte LEGO/IKEA-UX voor de toekomstige handleidingmodule.
- `archive/`: oude plannen en ervaringslogs; nuttig als context, nooit leidend boven actuele contracten en code.

Nieuwe documenten krijgen bovenaan expliciet `Status: actueel contract`, `Status: toekomst` of `Status: archief`.
