# Werkbank met kastonderbouw

Product-id: `werkbankkast`.

Dit product gebruikt een eigen `WorkbenchCabinetEngine` en levert daarna hetzelfde `WorkbenchModel` als de andere productfamilies. Daardoor blijven nesting, prijs, CNC, SolidWorks, 3D en orderoutput gedeeld.

## Constructieregels

- Een werkbank heeft één doorlopende bodemplaat. De configuratie wordt geweigerd wanneer die plaat niet uit het gekozen plaatmateriaal past.
- `UnitCount` betekent het aantal deurposities.
- Deurposities worden vanaf links in groepen van twee ingedeeld.
- Een deurpaar heeft een linker deur met scharnieren links en een rechter deur met scharnieren rechts.
- Iedere unitscheiding krijgt een normaal tussenschot. Een tussenschot achter een T-aanslag stopt 15mm achter de kastvoorzijde en grijpt 3mm in de centreersleuf van de aanslag; zo is er geen materiaalbotsing. De gatenrijen en railgaten behouden hun wereld-offset vanaf de kastvoorzijde.
- Tussen de twee deuren van ieder deurpaar blijft aan de voorkant een haakse deuraanslag van standaard 50mm tegen het volledige tussenschot staan. Bij 18mm plaat resteert aan beide zijden circa 16mm aanslagvlak. Samen vormen ze de benodigde T-vorm.
- De T-aanslag krijgt aan de achterzijde een verticale centreersleuf over de voorkant van het tussenschot en drie doorlopende montagegaten op de hartlijn.
- Bij bovenlades stopt de T-aanslag 3mm onder het ladefront en vervalt de bovengroef in het werkblad, zodat de T-aanslag niet met de ladeconstructie kruist.
- Alleen een scheiding tussen twee deurparen wordt dubbel uitgevoerd: twee volledige panelen van 18mm liggen symmetrisch rond de unitscheiding. Bij vier deurposities is dus alleen het middelste tussenschot 36mm dik.
- De 16,5mm deur-overlap wordt tegen de werkelijke binnenvlakken van deze 36mm constructie berekend. Daardoor blijft tussen deur 2 en 3 exact 3mm ruimte.
- De draaideuren lopen aan de onderzijde door tot 3mm boven de onderkant van de doorlopende bodemplaat en dekken daardoor ook de voorzijde van die plaat af.
- Bij een oneven aantal deurposities wordt de laatste positie een enkele links scharnierende deur.
- Onder iedere dragende unitgrens staat een IKEA SEKTION-poot voor en achter. Bij vier units zijn dat tien poten, oftewel vijf verpakkingen van twee.
- De poot is vastgelegd als IKEA SEKTION 905.560.71: nominale hoogte 114mm, verstelbaar van 89 tot 130mm, voetdiameter circa 51mm en maximaal 125kg belasting per poot volgens IKEA. De systeemdraagkracht mag niet worden berekend door deze waarden simpelweg op te tellen; bodemplaat, verbindingen en stabiliteit zijn eerder maatgevend.
- De ingestelde poothoogte bepaalt de hoogte van de onderzijde van de doorlopende bodemplaat. De losse voorzetplint wordt even hoog als de poothoogte minus de ingestelde vloerspeling.
- De hartlijn van de voorpoten volgt `plintterugstand + plintdikte + max(clipmaat + 3mm adapterachterwand, halve montagevoetbreedte + 1mm)`. Standaard is dat `45 + 18 + 28,4 = 91,4mm` vanaf het kastfront.
- Bij een gekozen zijplint houdt de buitenste pootrij rekening met de grootste uitsteek van de 76mm montagevoet: `18 + 47 + 1 = 66mm` vanaf de buitenrand. Zonder zijplint blijft de algemene zij-/achterpootinset van toepassing.
- De voorplint klikt met de meegeleverde C-clips op alle voorste poten. Optionele zijplinten gebruiken aan hun zijde de voorste én achterste hoekpoot; daarvoor zijn twee extra clips/adapters per zijplint nodig.
- De originele FÖRBÄTTRA-plint is een holle kunststof extrusie. De C-clip schuift zijwaarts in een losse kunststof drager; die drager klikt in de langsgroeven achter op de IKEA-plint. Een massieve houten plint heeft daarom een geschroefde adapter met een overeenkomstige inschuifgroef nodig.
- Voor- en zijplint delen de voorste hoekpoot. Zoals in de IKEA-montagewijze wordt de clip van één van de twee plinten daar omgekeerd geplaatst, zodat de clips elkaar niet raken.
- De zijplint loopt vanaf dezelfde terugliggende voorlijn als de voorplint tot 3mm vóór de kastachterzijde en ligt aan de buitenzijde vlak met de kastwand. Voor enkelzijdige CNC-productie gebruikt de engine een stompe hoek: de voorplint wordt aan iedere gekozen zijde exact één plaatdikte ingekort en sluit tegen de binnenzijde van de zijplint.
- Het ingemeten montageblok is 76×51×12mm. De twee klikpennen zijn Ø9,6×11,5mm, staan 33mm h.o.h. en liggen 18mm vanaf de korte penzijde. De CNC maakt Ø10mm doorlopende pengaten.
- Het centrale gat in het kunststof is Ø4,5mm en ligt 24mm vanaf de korte penzijde. Daarin wordt handmatig een korte Ø4-houtschroef gemonteerd. De bodemplaat krijgt hiervoor bewust geen CNC-voorboring: een Ø4-gat zou de houtschroef onvoldoende grip geven en een kleinere pilotfrees zou een extra toolchange veroorzaken.
- De sleufvormige klikopname is 50×32mm en accepteert de poot in twee richtingen. Daardoor kan het pootcentrum 32 of 47mm vanaf de korte penzijde liggen. De constructie gebruikt vast de 47mm-stand.
- De montagevoet ligt haaks op de staandergroef. Hierdoor blijven de pengaten naast de enkele en dubbele staandergroeven, terwijl het hart van de ronde poot onder de dragende unitgrens blijft.
- De meegeleverde plintclip wordt niet rechtstreeks in de houten plint geboord. De gemeten inschuiftong is 28×34,5×3,3mm en wordt van boven in een geprinte geleider geschoven. De geleider gebruikt standaard 0,25mm printspeling per zijde, een eindstop onderaan en twee borglippen.
- De volledige adapter V2 is fysiek goedgekeurd: cliptong, inschuifkamer, montagevleugel en kopzittingen. Adapter V2 heeft een 38mm brede kern met een 6mm links/rechts gespiegelde montagevleugel. Het onderste Ø4,5-doorvoergat blijft 23mm onder het cliphart; het bovenste ligt 23mm erboven en 19mm zijwaarts op de vleugel. Beide liggen daardoor volledig buiten de schuifbaan en krijgen een echte conische kopzitting Ø8,3×4,2mm, gebaseerd op de gemeten Ø4-schroefkop. De korte vooradapter gebruikt twee verzonken Ø4×16-schroeven; bij 3mm adapteruitstand blijft 13mm grip en 5mm restmateriaal. De verlengde zijadapter gebruikt twee Ø4×35-schroeven; bij 22,6mm adapteruitstand blijft 12,4mm grip en 5,6mm restmateriaal. Bij een buitenhoek wijst de vleugel altijd naar het midden van de betreffende plint.
- Iedere adapterpositie krijgt aan de binnenzijde van de houten plint twee blinde CNC-pilotgaten Ø3×10mm, exact tegenover de adaptergaten. Bij 18mm plintmateriaal blijft 8mm hout aan de zichtzijde intact. De Ø3mm 2-fluit carbidefrees maakt ook de overige kleine pilotgaten; de Ø6mm-frees verzorgt de grotere gaten, groeven, verdiepingen en contouren.
- Op een gedeelde voorhoek staat de zijclip 7mm hoger dan de voorclip. Hierdoor hebben de twee C-clips een afzonderlijk aangrijpniveau op dezelfde ronde poot.
- De officiële montage-instructie toont drie C-clips per verpakking van twee poten. De BOM rekent daarom per gekochte pootverpakking met drie meegeleverde clips.
- Deuren krijgen 35mm scharnierpotten en schroefgaten. Buitenwanden en de twee afzonderlijke panelen van een dubbel middenschot krijgen montageplaatgaten aan de juiste binnenzijde.
- Bodem en werkblad krijgen positioneergroeven en montagegaten voor alle dragende zijwanden en volledige tussenschotten.
- Buitenwanden en volledige tussenschotten krijgen een voorste en achterste systeem-32-gatenrij. In een enkel gedeeld tussenschot zijn deze gaten door-en-door, zodat één CNC-zijde volstaat; de twee delen van het dubbele middenschot krijgen ieder alleen de gaten aan hun kastzijde. Zo heeft iedere unit vier oplegpunten.
- Legplanken worden per unit als losse plaatdelen gemaakt en overspannen nooit twee units. De twee planken naast een T-aanslag krijgen aan hun voorste binnenhoek automatisch een passende uitsparing.
- Optioneel krijgt iedere unit één bovenlade. De engine maakt per unit een ladefront, bodem, twee zijden en achterzijde; de deuren worden automatisch lager.
- Een uitgefreesde ladehandgreep is één doorlopende capsulesleuf: een recht middendeel met twee halfronde uiteinden. Nesting, toolpathpreview, G-code, 3D en SolidWorks gebruiken hetzelfde bewerkingspatroon.
- Railgaten in de twee buitenwanden worden 12mm blind vanaf de kastzijde geboord. In interne tussenschotten blijven de gaten door-en-door, zodat één CNC-zijde volstaat. Het gewenste ontvangende pilotgat is voor zowel kastzijde als ladezijde expliciet Ø3mm en staat numeriek in de componentregel `measured_500` van de productmaster; code en audit gebruiken deze waarde zonder hardcoded Ø3-terugval. De twee panelen van het dubbele middenschot dragen ieder slechts één rail en hebben daarom geen tegenoverliggende schroeven in dezelfde plaat. Bij de enkele T-tussenschotten dragen beide plaatzijden een rail. Daar is Ø4,2×9,5mm plaatkop door 0,5mm railmateriaal op dezelfde AliExpress-gatposities in exact 18mm plaat fysiek getest en goedgekeurd; de nominale tipruimte is 0mm. Een andere schroef, railmateriaalweg of plaatdikte vereist automatisch een nieuwe controle.

## Configureerbare portalvelden

- totale breedte, diepte en werkhoogte;
- aantal deurposities;
- plaatmateriaal en achterwandmateriaal;
- achterwand wel of niet opnemen;
- ingestelde poothoogte binnen het bereik van de gekozen SEKTION-poot en terugligging van de losse plint;
- zijplint links en/of rechts;
- hartafstand van de zij-/achterpootposities tot de buitenranden;
- afstand van het achtervlak van de cliptong tot de pootas; de portal toont daarnaast de berekende pootposities en adapteruitstanden;
- breedte van de deuraanslag.
- aantal legplanken per unit en startverdeling vanaf onder of boven;
- aantal beschikbare legplankposities (standaard 6); de engine verdeelt dit aantal over de beschikbare hoogte, vast op het systeem-32-raster onder de bovenlade;
- offset van de voorzijde van de legplanken ten opzichte van de kastvoorzijde;
- systeem-32-legplankgaten wel of niet opnemen.
- bovenlade per unit, ladefronthoogte en optionele uitgefreesde handgrepen.

## Belangrijkste validaties

- deurpositie minimaal 180mm breed;
- ingestelde poothoogte tussen 89 en 130mm;
- voldoende kasthoogte tussen bodem en werkblad;
- geldige inset voor de stelpoten;
- de legplankoffset wordt begrensd door de achterste systeem-32-gatenrij; de voorste gatenrij schuift met dezelfde maat mee;
- de doorlopende bodemplaat moet in één stuk uit het gekozen plaatformaat passen.

## SolidWorks-eindcontrole

- In de webconfigurator maakt **Genereer SolidWorks-controle** losse `SLDPRT`-bestanden, een VBA-macro en een geopend multibody-controlepart op de werkelijke assemblycoördinaten.
- De COM-koppeling herkent ook de 3DEXPERIENCE ROT-moniker `SolidWorks_PID_*`. Zonder actieve sessie wordt SOLIDWORKS Design via de lokale `CATSTART.exe`-launcher gestart; bij een verlopen login wacht de worker maximaal vijf minuten terwijl de gebruiker zich aanmeldt.
- Het multibodypart bevat ook de zichtbare SEKTION-montagevoeten, ronde stelpoten, cliptongen en de korte/verlengde plintclip-adapters. De SolidWorks-map bevat tevens afzonderlijke adapterparts en STL-printbestanden.
- `VrijgaveControle.txt` wordt voor iedere SolidWorks-export aangemaakt. Bij een harde geometriefout wordt de SolidWorks-export afgebroken.
- De SolidWorks-export is een pasvorm- en interferentiecontrole. Rechthoekige blinde pockets/rabatten en doorlopende handgreepsleuven worden in het multibody-controlepart als echte materiaalverwijdering opgebouwd. De CNC-authoriteit blijft de gedeelde engine plus `Plaatgaten.csv`, `CAM-operaties.csv`, nesting en G-code.

## Nog fysiek te bevestigen

- lengte van de korte Ø4-houtschroef, gemeten onder de kop en passend bij de kopzitting in het 12mm montageblok;
- [goedgekeurd] korte adapter V2 inclusief 0,25mm printspeling, montagevleugel en kopzittingen;
- na de proefpassing controleren of 7mm niveauverschil voldoende ruimte tussen de twee clips op een gedeelde hoek geeft.
