# Masterdata beheren

Status: **actueel contract**.

## Doel

`config/product-master-data.xlsx` is de levende, controleerbare beheer- en wijzigingsbron voor alle bestaande en nieuwe producten. Nieuwe losse lijsten zijn niet toegestaan.

De werkmap wordt niet rechtstreeks door de actieve pricing- en CAM-code gelezen. `scripts/generate-masterdata-runtime.py` compileert Excel en de afbeeldingscatalogus deterministisch naar `config/runtime/masterdata-runtime.json`. Prijzen, leveranciersvoorkeuren en CAM-instellingen lezen deze gevalideerde snapshot. Materialen, beslag en enkele productdefaults komen tijdelijk nog uit bestaande JSON of code; die bronnen verdwijnen pas nadat hun consumenten zijn omgezet en regressietests slagen.

## Vaste indeling

- `Producten`: productidentiteit, categorie en expliciete overerving via `Basisproduct-ID`.
- `Materialen` en `Componenten`: productonafhankelijke bibliotheekrecords.
- `Product-regels`: productspecifieke keuzes. Gebruik `Referentietype` en stabiele `Referentie-ID(s)`.
- `Leveranciers`: `SuppliersTable` en daaronder `SupplierPreferencesTable`.
- `Prijs & inkoop`: één aanbieding per `Aanbieding-ID`, gekoppeld aan `Interne-ID` en optioneel `Leverancier-ID` en `Afbeelding-ID`.
- `Controles`: machineleesbare blokkades en vrijgaveregels.
- `Wijzigingslog`: iedere structurele wijziging en migratie.

De bedoelde tabelnamen en relaties staan in `config/master-data-schema.json`. Validator en snapshotgenerator gebruiken dit contract voor tabellen, sleutels en verboden legacy-tabbladen.

## Leveranciersselectie

Een voorkeur is geen productregel maar een relatie tussen categorie, subcategorie, leverancier en scope. De app filtert eerst op `Categorie`, `Subcategorie` en `Scope-type`/`Scope-ID`; vervolgens wint de laagste actieve `Rang`. `Alle producten` geldt ook voor producten die later worden toegevoegd. Een kandidaataanbieder krijgt pas invloed wanneer zijn voorkeur de status `Actief` heeft.

TechXXL is standaard rang 1 voor:

- `Profiel` / `Aluminium systeemprofielen`;
- `Beslag` / `Profieltoebehoren`.

## Afbeeldingen

De binaire afbeelding staat buiten Excel in `config/catalog-images/<supplier>/`. `config/catalog-images/image-catalog.json` is de canonieke afbeeldingsregistratie en koppelt `Afbeelding-ID`, leverancier, artikelcode, interne ID, bronpagina en lokaal bestand. Excel bevat in `Prijs & inkoop` alleen een kleine ingebedde preview. De snapshot bevat zowel aanbiedingen als het afbeeldingsregister; de portal gebruikt de afbeeldingsvelden van de gekoppelde aanbieding.

Bij toevoegen of vervangen:

1. sla het bestand met een stabiele, beschrijvende naam op in de leveranciersmap;
2. maak of wijzig het record in `image-catalog.json`;
3. zet hetzelfde `Afbeelding-ID` en lokale pad in de aanbieding;
4. vernieuw de Excel-preview;
5. controleer dat bronpagina, artikelcode en profielserie bij elkaar horen.

## Nieuw product

1. Maak een stabiel `Product-ID` en kies categorie/basisproduct.
2. Leg aantallen, oriëntaties en technische keuzes vast in `Product-regels`; gebruik geen vrije naam als referentie.
3. Voeg ontbrekende bibliotheekrecords toe voordat productregels ernaar verwijzen.
4. Koppel prijzen als aanbiedingen in `Prijs & inkoop`.
5. Gebruik bestaande leveranciersvoorkeuren; voeg alleen een nieuwe voorkeur toe als categorie, subcategorie of productscope werkelijk afwijkt.
6. Voeg noodzakelijke controles toe en registreer de wijziging.
7. Valideer alle foreign keys, unieke ID's en afbeeldingsbestanden; controleer het gewijzigde werkblad visueel.
8. Voer `python scripts/generate-masterdata-runtime.py` uit en daarna de minimale controles uit `AGENTS.md`.

## Migratieregel

Een oude tabel of sleutel mag worden verwijderd zodra elk gebruik naar de canonieke ID is omgezet en de referentie-audit geen ontbrekende of naamgebaseerde koppelingen meer vindt. Oude tabbladen worden niet als permanente fallback bewaard.

## Wijzigingen loggen

- Product-, component-, leverancier-, prijs-, CAM- en andere masterdatawijzigingen krijgen een unieke, oplopende regel in `Wijzigingslog` met datum, versie, betrokken stabiele ID's en besluitreden.
- De runtimegenerator neemt dit wijzigingslog mee in de snapshot; `Wijziging-ID` mag niet worden hergebruikt.
- Een uitsluitend redactionele documentatiewijziging krijgt geen kunstmatige masterdatarevisie. Die wordt via een gerichte Git-commit gelogd.
- Een documentatiewijziging die tegelijk een runtimecontract, ID, tabelstructuur of productregel verandert, wordt zowel in Git als in `Wijzigingslog` vastgelegd.
