# Hoogteverstelbare werktafel

## Productdefinitie

De hoogteverstelbare werktafel gebruikt hetzelfde tweekoloms HTE2-onderstel, dezelfde 80×80-voetprofielen, stabilisatieplaat, hoekadapters en stelvoeten als het bestaande Workstation. Het product heeft geen kogelpotten, geen horizontaal verschuifbaar blad en geen borg-, eindstop- of railmechanisme. Het gekozen werkblad en het volledige bovenframe bewegen uitsluitend verticaal met de HTE2-kolommen.

De beheerbron is product-ID `hoogteverstelbare_werktafel` in `config/product-master-data.xlsx`. De applicatie leest de gegenereerde runtime-snapshot en staat alleen de daarin gekoppelde profiel- en plaatmateriaal-ID's toe.

## Bevroren constructiemanifest

Voor de standaardmaat 1650×1000×850 mm bestaat de dragende profielconstructie uit acht profielen:

| Aantal | Onderdeel | Profiel | As / ligging |
| ---: | --- | --- | --- |
| 2 | Voetprofiel | 80×80 | Z-as onder de HTE2-kolommen |
| 2 | Vast bovenframe voor/achter | keuze 40×40 of 40×80 | X-as; bij 40×80 staat de 80-mm zijde verticaal |
| 2 | Vast bovenframe links/rechts | keuze 40×40 of 40×80 | Z-as tussen de voor- en achterligger |
| 2 | Onderstelaansluiting bovenframe | keuze 40×40 of 40×80 | Z-as, exact boven de twee HTE2-kolomharten |

De vier buitenprofielen vormen rondom een vast, coplanair draagframe. De twee binnenprofielen sluiten met vlakcontact aan op de HTE2-bovenplaten en verbinden het onderstel met de voor- en achterligger. De kop-op-sleufcontacten bepalen het aantal standaardverbinders; de vier vrije X-koppen krijgen een variantpassende afdekkap.

Het vaste werkblad bevat geen kogelpotgaten. De configureerbare keuzes zijn HPL wit glad 10 mm en GrandPlex door-en-door Okoumé multiplex 40 mm in handelsformaat 2500×1220 mm. HPL blijft de standaardkeuze. De tweede plaat is de verticale HPL-stabilisatieplaat tussen de kolommen.

Het werkblad wordt met acht TechXXL montagebeugels 40×40×20 ZN (TIN 100391) bevestigd: twee symmetrisch verdeelde beugels op ieder van de vier Z-draagprofielen. Dezelfde backendservice plaatst tien beugels op de vijf X-draagprofielen van het Workstation. Beugel, M6-profielbout en M6-T-moer worden vanuit de fysieke plaatsingen met de BOM gesynchroniseerd.

De HTE2-setprijs is een generieke componentaanbieding en geldt voor ieder product dat `geming_hte2_o1_400` toepast; er is geen productspecifieke prijsregel meer.

## Parameters en beweging

- Breedte: 1200–2200 mm.
- Diepte: 700–1400 mm.
- Veilige gezamenlijke werkhoogte voor beide bovenframevarianten: 770–1130 mm.
- Standaardmaat: 1650×1000×850 mm.
- Bovenframekeuze: 40×40 of 40×80 staand.
- Werkbladkeuze: HPL wit glad 10 mm of Okoumé GrandPlex multiplex 40 mm.
- Beweging: uitsluitend verticaal; er bestaat voor dit product geen horizontale bewegingsas.

## Belasting en stijfheid

De app toont en exporteert een indicatief rekenrapport bij een masterdata-gestuurde referentiebelasting van 1 kN. Het rapport vermeldt profiel, overspanning, aantal parallelle liggers, E-modulus, traagheidsmoment, berekende doorbuiging en alle ontbrekende vrijgavegegevens. Dit is een vergelijkingsmodule, geen productievrijgave: projectspecifieke ontwerpbelasting, grensdoorbuiging, veiligheidsfactor, gekwalificeerde HTE2-capaciteit en belastbaarheid van de beugel/werkblad-interface moeten nog worden vastgesteld.

## Vrijgavestatus

Het product is een conceptproduct. Klantvoorstel, interactief 3D en projectdata mogen als concept worden geëxporteerd. CAM, SolidWorks-productie-export en productievrijgave blijven geblokkeerd totdat de volgende punten zijn gesloten:

- bevestiging van de HPL-stabilisatieplaat;
- belasting- en stijfheidscontrole;

Voor de stabilisatieplaat bevat de huidige HTE2-masterdata alleen het sleufpatroon van de O1-eindplaten. De bemate zijgroeven van het vaste kolomlichaam ontbreken. Daardoor mogen de gewenste vier bouten per kolomzijde nog niet automatisch worden geplaatst of op lengte berekend.
