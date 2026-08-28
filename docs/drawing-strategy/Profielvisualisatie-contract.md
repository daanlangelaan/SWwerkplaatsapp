# Visuele opbouw aluminium profielen

Status: **actueel contract**.

Dit document legt de visuele opbouw van aluminium T-sleufprofielen en hun assemblage-instructies vast. Werk dit contract bij zodra een nieuwe profielmaat, bewerking, verbinding of instructielaag wordt toegevoegd. De fysieke geometrie en productiedata blijven altijd leidend; dit document beschrijft hoe die gegevens herkenbaar en zonder misleidende overlays worden afgebeeld.

## Bronnen en verantwoordelijkheden

- `AssemblyPlacement` bepaalt de echte positie, afmeting en oriëntatie van ieder profiel.
- Profielbewerkingen bepalen de echte gaten, toegangsgaten en hun zijden.
- De verbindingsgraaf bepaalt welke verbinders, bewegingen en gereedschapsassen bij een stap horen.
- Stickerdata bepalen één profiel-lokaal anker, het montagevlak en de kaartidentiteit.
- `ProfileRenderContractService` en `AssemblyHardwareRenderContractService`
  vertalen deze bronnen naar direct renderbare backendcontracten.
- `PortalHtml.cs` projecteert die contracten naar overzicht, stapdetail en
  gekoppelde 3D. UI-helpers mogen geen technische maat, sleufverdeling,
  boutvorm of gatdiameter aanvullen.

## Vaste laagvolgorde

Van fysiek naar instructief wordt elk beeld als volgt opgebouwd:

1. massieve profielgeometrie met echte geëxtrudeerde T-sleufdoorsnede;
2. fysieke sleufbodems en interne profielholtes;
3. echte productieboringen en toegangsgaten;
4. ondoorzichtige kopse details, zoals kernboringen en extrusieholtes;
5. verbinders, bouten en overige hardware;
6. de fysieke sticker op het profielvlak;
7. UI-only instructielagen: highlight, kaart, verbindingslijn, bewegingspijl, gereedschapsas en herhaalaantal.

Een hogere laag mag een lagere laag alleen bedekken wanneer dat fysiek klopt of wanneer een lokale instructiehighlight dit bewust en beperkt doet.

## T-sleuven en materiaallagen

- T-sleuven zijn geometrie. Teken geen tweede transparant of halftransparant lint over een reeds gemodelleerde sleuf.
- Gebruik geen breed vlak op het profieloppervlak om diepte te suggereren. Dat maakt de sleuf dicht, verandert het silhouet en veroorzaakt kijkhoekafhankelijke artefacten.
- Kopse kernboringen en holtes zijn ondoorzichtige, lokale details op het werkelijke eindvlak. Zij lopen niet door over de lengte van het profiel.
- Transparantie is alleen toegestaan voor bewust gedeëmphasiseerde context, doel-/eindposities en tijdelijke ghosting. Actieve, ontvangende en gemonteerde profielen blijven materieel ondoorzichtig.
- Gebruik `polygonOffset` alleen voor werkelijk coplanaire lokale details. Het is geen oplossing voor dubbele geometrie.

## Gaten en bewerkingen

- Toon zichtbare eindboringen en sleuteltoegangsgaten in zowel het montageoverzicht als het stapdetail.
- Projecteer gaten vanuit dezelfde profielbewerking en hetzelfde verbindingsknooppunt; maak geen losse decoratieve gaten met afwijkende positie.
- Een minimale schermmaat of contrastomtrek mag de leesbaarheid verbeteren, maar verandert nooit diameter, zijde of lokaal anker.
- Wanneer dezelfde vastzethandeling op meerdere equivalente punten geldt, highlight ieder betrokken gat en toon de sleutel eenmaal met `×N`.

## Verbinders, beweging en gereedschap

- Toon voorgemonteerde verbinders klein in het overzicht wanneer de camera ze fysiek kan zien. Normale diepte-afdekking bepaalt wat achter een profiel verdwijnt.
- Gebruik voor standaardverbinders het profielkopvlak als nulreferentie: plaat- en boutkophart liggen volgens het contract naar buiten, boutschachthart en klauwinsteekweg naar binnen. De UI kent alleen deze richting; alle afstanden en envelopmaten komen uit het backendcontract.
- Laat bij een expliciete stap `verbinder aanbrengen` uitsluitend de betrokken verbinder- en boutmeshes rustig pulseren in overzicht en detail. Laat profielen, stickers en bewegingspijlen stabiel. Gebruik bij `prefers-reduced-motion` één vaste heldere hardwarehighlight in plaats van animatie.
- Animeer bij `verbinder aanbrengen` iedere bout met bijbehorende standaardverbinder als één star hardwaredeel vanaf buiten langs de werkelijke profielas naar zijn definitieve positie in de profielkop. Werk één profielkop tegelijk af in stabiele trace-ID- en kopvolgorde. Speel bij twee zichtbare vensters eerst de volledige reeks in het overzicht af, zet dat overzicht daarna terug naar het beginbeeld en speel vervolgens dezelfde invoer in het detail af; laat het detail op het complete eindbeeld stoppen. Reeds aangekomen hardware blijft tijdens de reeks staan. Gebruik bij `prefers-reduced-motion` direct het complete statische eindbeeld.
- Kies in het lokale detail van `verbinder aanbrengen` de profielkop waarop bout en standaardverbinder volledig zichtbaar kunnen worden getoond. Deze hardwarezichtbaarheid gaat vóór het stickerherkenningspunt: toon de andere kop zonder sticker wanneer de stickerkop de verbinder afdekt. Richt uitsluitend deze detailcamera op de uitstekende hardware, laat de volgende stap haar eigen oriëntatie bepalen en laat een losse bewegingspijl weg wanneer de insteekrichting uit de volledig zichtbare hardware al ondubbelzinnig blijkt.
- Laat bij een stap `schuif in T-sleuf` alle werkelijk betrokken voorgemonteerde verbinders inclusief bout én de specifieke ontvangende sleufbaan gelijktijdig rustig pulseren in overzicht en detail. Animeer daarvoor de bestaande ondoorzichtige sleufbodem; leg nooit een extra transparant vlak over de T-sleuf en laat de overige profielvlakken stabiel.
- Animeer herhaalde, onderling onafhankelijke inschuifhandelingen in trace-ID-volgorde: speel eerst het montageoverzicht links één volledige cyclus af, schuif daar telkens één profiel van begin- naar eindpositie, laat eerder geplaatste profielen staan, pauzeer kort bij het complete eindbeeld en keer terug naar het beginbeeld. Start pas daarna de lokale detailanimatie rechts. Speel die detailbeweging exact het opgegeven herhaalaantal af, toon per cyclus centraal een concentrisch groeiend volgnummer `1…N` en stop na de laatste aankomst op het eindbeeld. Laat profielgeometrie, sticker en voorgemonteerde hardware als één eenheid bewegen; de bout en verbinder blijven daarbij fysiek in dezelfde profielkop. Verberg bij aankomst het gele doelspook zodat uitsluitend het werkelijk geplaatste blauwe profiel zichtbaar blijft. Verkort de bewegingspijl voortdurend tot de resterende afstand en verberg haar bij aankomst. Gebruik bij `prefers-reduced-motion` een statisch compleet eindbeeld.
- Gebruik voor iedere `slide-into-slot`-cyclus het bijbehorende sleutel-/toegangsgat als voorwaardelijke visuele stop. Een binnen beeld én naar de camera gericht gat laat het profiel op de echte gatpositie stoppen. Zonder zichtbaar gat loopt het profiel langs de T-sleuf door tot voorbij het zichtbare uiteinde van het ontvangende profiel en verdwijnt de volledige profielcontour vóór de volgende cyclus begint. Bereken dit per stabiele trace-ID uit de actuele camera, gatpositie en vlaknormaal; de regel geldt ook automatisch voor toekomstige inschuifstappen die dezelfde generator gebruiken.
- De verplaatsingsvector van een `slide-into-slot`-stap loopt altijd parallel aan de langsas van het ontvangende T-slotprofiel. Kies het minimum- of maximumuiteinde met de kortste vrije invoerafstand; bij staanders wordt daarom van onderen of boven ingeschoven en nooit horizontaal door de profieldoorsnede. Een exact detail gebruikt dezelfde assemblydelen en eindcoördinaten als het overzicht; lange ontvangende profielen mogen alleen rondom de echte aansluitingspositie worden ingekort.
- Een lokaal verbindingsdetail is letterlijk een camera-uitsnede van één werkelijk knooppunt uit dezelfde overzichtsscène. Selecteer actieve en ontvangende profielen via hun stabiele trace-ID's en fysieke contactpunt, behoud wereldcoördinaten en rotaties ongewijzigd en neem de kijkrichting van de overzichtscamera over. Alleen cameramiddelpunt en lokale fit mogen afwijken; bouw geen synthetische vervangingshoek wanneer assemblygeometrie beschikbaar is. Kies bij herhaalde equivalente knooppunten bij voorkeur een goed zichtbare hoek linksonder in het actuele overzicht.
- Plaats een zijwaartse T-sleufverbinding in een lokale detailuitsnede niet precies op het kunstmatig ingekorte uiteinde van het ontvangende profiel. Laat voldoende ontvangende sleuf voorbij het knooppunt zichtbaar doorlopen en plaats de gekozen profielkop op de nabije buitenwand van het ontvangende profiel; de rest van het bewegende profiel moet volledig buiten diens doorsnede liggen. Zo blijft het echte kop-tegen-zijvlakcontact exact behouden en leest de animatie als inschuiven en aansluiten, niet als doorsnijding.
- Centreer een `slide-into-slot`-detail bij de vrije invoerzijde op het echte uiteinde van het ontvangende profiel uit het overzicht, niet op een willekeurige kruising verderop. Kies van de geldige invoerhoeken de laagst gelegen, goed zichtbare uitsnede en zoom zo dat minimaal één open ontvangende T-sleuf én zowel het bewegende als het ontvangende profiel tegelijk herkenbaar blijven. Behoud daarbij dezelfde wereldgeometrie, invoerzijde en camerakijkrichting als het overzicht.
- Leid iedere beweging af uit de echte verplaatsingsvector. Pijlpunt en schacht liggen in de feitelijke inschuifrichting en gebruiken de zalm/oranje instructiekleur, zodat zij niet samenvallen met het blauwe actieve profiel.
- Plaats een sleutel op een echt verbonden, goed zichtbaar knooppunt. De gereedschapsas loopt door het echte gat of boutcentrum.
- In een vastzetstap via een toegangsgat is de standaardverbinder al in de profielkop voorgemonteerd. Teken in het detail geen tweede verbinder of bout; toon alleen het echte toegangsgat, de instekende inbussleutel en een duidelijke ruimtelijke draaipijl rond dezelfde gereedschapsas.
- Toon bij een complete subassembly-verplaatsing de complete starre groep. Groepeer alle al gemonteerde profielen, stickers, gaten en hardware onder één mover met één vector; speel de eerdere deelmontage niet opnieuw af. Een detail mag lange ontvangende profielen inkorten, maar niet het contactvlak, de doorsnedeoriëntatie of de eindpositie veranderen.
- Kies bij `aandraaien` uit de echte, zichtbare knooppunten in het overzicht bij voorkeur de onderste vrije voethoek. Gebruik letterlijk dezelfde wereldgeometrie en kijkrichting als links, zoom alleen lokaal in op gat en bout en houd de sleutel klein genoeg om het contact zichtbaar te laten.

## Stickers en kaarten

- Iedere fysieke sticker en haar kaart gebruiken hetzelfde stabiele trace-ID en hetzelfde profiel-lokale anker.
- Kies bij meerdere geometrisch gelijkwaardige herhaalverbindingen voor het detail bij voorkeur het profieluiteinde met de zichtbare sticker. Behoud daarbij de echte kopnaam en visualiseer de verbinding die daadwerkelijk aan datzelfde uiteinde hoort.
- Liggers krijgen hun sticker op de gemonteerde bovenzijde. Staanders krijgen de montage-/zichtzijde volgens de vastgelegde stickerdata. Bewaar daarbij zowel het lokale assemblagevlak als het fysieke doorsnedevlak en diens vlakbreedte. Voor het staande 40×80-werkbladframe is de bovenzijde het korte 40-mm-vlak; een lokale asnaam mag nooit zelfstandig naar 40 of 80 mm worden vertaald.
- In fullscreen schaalt de hele kaart mee: vlak, tekst, witruimte, botsingsmarge en lijnankers.
- Slimme kaartplaatsing geldt in ieder fullscreenvenster, zowel overzicht als detail en onafhankelijk van het wel of niet koppelen van de camera’s.
- Deel de kaartplaatsing op volgens de op dat moment geprojecteerde langsas van het profiel. Plaats kaarten van horizontale of licht schuine liggers boven het profiel in verspringende hoogtelijnen, met een hoofdzakelijk verticale of licht diagonale verbindingslijn. Plaats kaarten van verticale of steile staanders links of rechts naast het profiel in verspringende zijlanen, met een hoofdzakelijk horizontale of licht diagonale lijn. Classificeer opnieuw na iedere camera-rotatie of zoom; gebruik hiervoor nooit alleen de vaste profielrol of wereldas.
- Plaats kaarten dicht bij hun eigen anker. Minimaliseer overlap, lijnkruising en lijnlengte. De lijn begint exact in het midden van de fysieke rode stickermarkering en eindigt op de dichtstbijzijnde kaartrand.
- Buiten beeld blijft de kaart aan hetzelfde fysieke anker verbonden via de overeenkomstige beeldrand; draaiing mag geen nieuwe of willekeurige koppeling maken.

## Verantwoordelijkheid per weergave

| Weergave | Verplicht zichtbaar | Niet doen |
| --- | --- | --- |
| Machineoverzicht | totale context, actuele subassembly, rustige highlights | alle detailkaarten of toekomstige profielen vooruit tonen |
| Stapoverzicht | bewegend, ontvangend en doel in echte montagecontext; zichtbare gaten en verbinders | een los representatief profiel tonen wanneer een subassembly beweegt |
| Stapdetail | fysiek contact, beweging, hardware, gaten en gereedschap leesbaar | verbinding reconstrueren met andere oriëntatie of fictieve overlap |
| Fullscreen gekoppeld | overzicht en detail tegelijk, gesynchroniseerde kijkrichting met eigen fit | één canvas verplaatsen of één weergave verbergen |

Op brede fullscreenwerkplekken schaalt niet alleen de 3D-inhoud maar ook de bedieningslaag begrensd mee. Stapkop, overzichts- en detaillabels, legenda, onderdelen en materialen, voortgang en alle primaire knoppen moeten op grote resoluties zonder browserzoom leesbaar en met ruime klikvlakken bedienbaar blijven. Gebruik responsieve `clamp()`-maten met een bovengrens, zodat tekst niet te klein blijft maar ook geen modelruimte verdringt.

Geef in fullscreen het montageoverzicht en het detail altijd exact even brede en even hoge hoofdvensters, onafhankelijk van de schermverhouding. Plaats onderdelen, materialen en gereedschapsinformatie in een afzonderlijke derde kolom; trek die kolom nooit af van alleen het detailcanvas. Herbereken beide rendererfits na iedere viewportwijziging. Zoom bij verbinden en aandraaien het rechtervenster lokaal verder in op het echte contactknooppunt, maar houd de getekende sleutel op een normale begrensde hulpmiddelschaal.

De orthografische camera en WebGL-renderer gebruiken bij iedere render exact de actuele CSS-breedte en -hoogte van hun eigen viewport. Leg nooit een grotere minimale renderbreedte of -hoogte op dan het zichtbare canvas: een afwijkende interne aspectratio wordt door CSS uitgerekt en vervormt het frame. Resizen mag uitsluitend uniforme schaal en vrije marge wijzigen; profielverhoudingen, hoeken en geprojecteerde lengteverhoudingen blijven invariant.
| 2D/fallback | dezelfde stapbetekenis en kleurtaal | informatie toevoegen die 3D/data tegenspreekt |

## Regressiematrix

| Contract | Automatische bewaking | Visuele controle |
| --- | --- | --- |
| Echte T-sleuf zonder transparante afdeklaag | broncontract verbiedt legacy-sleufvlak | schuine en kopse kijkhoek |
| Gaten in overzicht én detail | aanroepen voor productie- en toegangsgaten | stap met eindboring en stap met sleuteltoegang |
| Kleine verbinders in overzicht | overzichtsconnector gekoppeld aan stap | voor en na inschuiven |
| Eén actuele detailrenderer | exact één rendererdefinitie | wisselen tussen alle staptypen |
| Stickerkaart dicht bij juist anker | fullscreen smart-layoutcontract | gekoppeld aan/uit en tijdens draaien |
| Vier equivalente gaten = sleutel `×4` | doelpunten uit echte verbindingen | staander-/subassembly-vastzetstap |
| Geen profielen vóór introductiestap | planner-/domeincontract | verzamelstap tegenover latere plaatsstap |

## Wijzigingschecklist

Bij iedere visuele profielwijziging:

1. bepaal of het element fysieke geometrie, productiebewerking, hardware, sticker of UI-only instructie is;
2. plaats het in de juiste laag en hergebruik de stabiele bron-ID;
3. controleer overzicht en detail in gewone én fullscreenmodus;
4. controleer gekoppelde 3D tijdens draaien, zoomen en een buitenbeeldanker;
5. doorloop minstens verzamelen, verbinder plaatsen, inschuiven en vastzetten;
6. voeg of actualiseer een regressiecontract voor de generieke regel;
7. werk dit document en zo nodig de profielassemblageskill bij;
8. voer de repositorycontroles, configuratorbuild en productregressies uit.

Een modelsnelheid of redeneerinstelling is nooit de technische borging. De borging bestaat uit één bronimplementatie, expliciete laaggrenzen, regressietests en een visuele browsercontrole.
