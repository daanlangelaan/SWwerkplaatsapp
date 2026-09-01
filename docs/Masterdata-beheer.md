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

## Productovererving en configuratieblokkades

De cataloguscontrole is app-breed, maar technische waarden zijn niet automatisch
voor ieder product gelijk. Een product erft productregels en invoercontracten
uitsluitend langs zijn expliciete `Basisproduct-ID`-keten. Zet een gedeelde
materiaalkeuze of parametergrens daarom op het hoogste basisproduct waarvoor die
waarde technisch geldig is; een afgeleid product legt alleen een expliciete
override vast wanneer zijn grens strenger of zijn keuze anders is.

Een wijziging aan `shipping_box` geeft dus geen kast- of werkbankproduct vrij.
Een wijziging aan basisproduct `cabinet` stroomt wel door naar `vakjeskast` en
`werkbankkast`, tenzij een van die producten dezelfde regelsoort overschrijft.
De backend gebruikt het opgeloste contract zowel voor `CanConfigure` als voor de
werkelijke invoerklemming en materiaaldefault. Daardoor mag de modelbouwer geen
afwijkende hardcoded grens of anonieme materiaalfallback meer gebruiken.

## Leveranciersselectie

Een voorkeur is geen productregel maar een relatie tussen categorie, subcategorie, leverancier en scope. De app filtert eerst op `Categorie`, `Subcategorie` en `Scope-type`/`Scope-ID`; vervolgens wint de laagste actieve `Rang`. `Alle producten` geldt ook voor producten die later worden toegevoegd. Een kandidaataanbieder krijgt pas invloed wanneer zijn voorkeur de status `Actief` heeft.

Houd `SupplierPreferencesTable` fysiek gesorteerd op categorie, subcategorie en rang. Daarmee staat het beheer per inkoopcategorie bij elkaar zonder een tweede leveranciers- of voorkeurstabblad als concurrerende bron te maken.

TechXXL is standaard rang 1 voor:

- `Profiel` / `Aluminium systeemprofielen`;
- `Beslag` / `Profieltoebehoren`.

Goedkoop Bouwmaterialen is standaard rang 1 voor `Materiaal` / `Houtachtige platen` en kandidaatbron voor `Materiaal` / `Trespa platen`. Kunststofplatenshop blijft rang 1 voor `Materiaal` / `Kunststof platen`.

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

## Technische leveranciersgeometrie

Een technische maat heet alleen leveranciersdata wanneer zij aan een concreet,
stabiel intern materiaal- of component-ID én aan een leveranciersaanbieding met
artikelcode en bron is gekoppeld. Een maat uit een gelijkend product, afbeelding,
UI-render of andere profielserie is geen exacte bron.

Leg profieldoorsneden vast bij het materiaalrecord en hardware-enveloppen bij het
componentrecord. Een verbindingsrecept verwijst vervolgens uitsluitend naar die
stabiele records. Minimaal vereist voor de assemblagerenderer:

- profiel: sleufmondbreedte en -diepte, sleufkamerbreedte en -diepte,
  buitenradius en kernboringdiameter;
- standaardverbinder: totale plaat- en klemgeometrie, onderlinge klempositie en
  positie ten opzichte van het profieluiteinde;
- bout: norm of exact artikel, draadmaat en lengte, kopdiameter en -hoogte en
  binnenzeskantmaat;
- bewijs: leverancier-ID, artikelcode, bron-URL of beheerd datablad, revisie en
  verificatiestatus.

Zolang één van deze waarden of de exacte artikelkeuze ontbreekt, blijft het
backend-rendercontract `ProvisionalRenderEnvelope`, is `OpenData` niet leeg en
mag de envelope niet worden gebruikt voor CAM, inkoop of productievrijgave.

### TechXXL CAD-download

Gebruik voor TechXXL altijd de leverancierspagina en het artikelnummer als
startpunt:

1. open `https://www.techxxl.nl/part-<TIN>.html` en controleer TIN, ID, serie,
   type en raster;
2. volg **CAD gegevens** naar `https://www.techxxl.nl/cad-<TIN>.html`;
3. voer het geldige weekwachtwoord in en verzend het formulier; technisch is dit
   een `POST` met veldnaam `eingabe`;
4. accepteer alleen een respons met een CAD-bestandsnaam in
   `Content-Disposition`; een HTML-respons is een fout of verlopen wachtwoord;
5. leg bestandsnaam, formaat, SHA-256, controledatum, TIN en leveranciers-ID vast
   in het materiaal- of componentrecord;
6. controleer de geometrie tegen de zichtbare leveranciersmaten voordat een veld
   `ExactSupplierGeometry` wordt.

Het wachtwoord is circa één week geldig. Bewaar het uitsluitend in de lokale
Codex-leveranciersopslag buiten de repository, met verkrijgings- en vervaldatum.
Neem het nooit op in Excel, runtime-JSON, broncode, documentatie of Git. Na de
vervaldatum moet een nieuw wachtwoord bij TechXXL worden opgevraagd.

De machinebasis gebruikt één consistente groef-8-keten: `alu_system_80x40`
(TIN 100535), `alu_system_80x80` (TIN 100545),
`techxxl_standard_connector_8_40` (TIN 100342) en
`techxxl_button_head_iso7380_m8x25` (TIN 100673). De profielen hebben ronde
kernboringen Ø6,8 mm voor M8. De renderer mag deze als ronde gaten tonen.

## Migratieregel

Een oude tabel of sleutel mag worden verwijderd zodra elk gebruik naar de canonieke ID is omgezet en de referentie-audit geen ontbrekende of naamgebaseerde koppelingen meer vindt. Oude tabbladen worden niet als permanente fallback bewaard.

## Wijzigingen loggen

- Product-, component-, leverancier-, prijs-, CAM- en andere masterdatawijzigingen krijgen een unieke, oplopende regel in `Wijzigingslog` met datum, versie, betrokken stabiele ID's en besluitreden.
- De runtimegenerator neemt dit wijzigingslog mee in de snapshot; `Wijziging-ID` mag niet worden hergebruikt.
- Een uitsluitend redactionele documentatiewijziging krijgt geen kunstmatige masterdatarevisie. Die wordt via een gerichte Git-commit gelogd.
- Een documentatiewijziging die tegelijk een runtimecontract, ID, tabelstructuur of productregel verandert, wordt zowel in Git als in `Wijzigingslog` vastgelegd.
