# Code-structuur tekenlogica

De tekenlogica wordt in lagen opgebouwd zodat nieuwe producten niet opnieuw hoeven uit te vinden welke plaatzijde, coordinaten en gatenstrategie correct zijn.

## Algemene laag

`src/SWWerkplaats.Configurator/Drawing`

- `SheetDrawing`: plaatdelen aanmaken en in het assemblymodel plaatsen.
- `SheetOperations`: gaten, pockets en montagegatlijnen op lokale sheetcoordinaten.
- `SheetPatterns`: herbruikbare verdelingen zoals gaten langs een lijn of tussen randafstanden.

Deze laag kent geen product zoals kast, werkbank of vakjeskast. De laag kent alleen plaatdelen, lokale sheetcoordinaten en het bestaande tekencontract.

## Productlaag

Elke productfamilie bepaalt zelf welke onderdelen en bewerkingen nodig zijn:

- kast: staanders, werkblad, lades, legplanken;
- vakjeskast: buitenkast, achterwand, verticale kamdelen, horizontale kamdelen;
- werkbank: profielen, platen en bevestigingspunten.

Productlagen mogen de algemene laag gebruiken, maar niet andersom.

## Migratieregel

Bestaand gedrag wordt stap voor stap verplaatst. Eerst krijgt bestaande code wrappers naar de algemene laag. Pas daarna worden product-specifieke strategieklassen gemaakt, zoals `CubbyCabinetDrawingStrategy`.
