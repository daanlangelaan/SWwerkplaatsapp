# Designstandaard klantbijlage

Doel: de klant moet binnen enkele seconden begrijpen wat wordt aangeboden, welk voordeel de uitvoering biedt en welke gegevens vóór akkoord gecontroleerd moeten worden.

De huidige Workstation-bijlage is de visuele referentiestandaard. Nieuwe producten gebruiken dezelfde rustige opbouw, typografie, marges, kleuren en paginavolgorde. Alleen de inhoud en relevante productvisualisaties veranderen.

## Vaste opbouw

1. **Voorstel** — klant en project één keer noemen, één sterke 3/4-render, geen maatvoering.
2. **Gebruik** — maximaal vier concrete voordelen, geschreven vanuit het werk van de gebruiker.
3. **Uitvoering** — materialen, werking en leveringsomvang in gewone taal; geen merken, leveranciers of typenummers.
4. **Maatcontrole** — alle klantmaten op één pagina met groot boven-, voor- en zijaanzicht.

## Ontwerpregels

- Geef iedere pagina één communicatiedoel en één duidelijke hoofdtitel.
- Zet informatie maar één keer in het document; technische details komen pas op de controlepagina.
- Schrijf voordeelgericht: eerst wat de klant ermee kan, daarna pas hoe de constructie dat ondersteunt.
- Gebruik een rustig raster, vaste marges en een beperkte afstandsschaal. Lijn tekst en beelden zichtbaar op elkaar uit.
- Gebruik zo min mogelijk kaders en kaarten. Witruimte ondersteunt de hiërarchie, maar mag nooit ten koste gaan van leesbaarheid van tekeningen.
- Houd lopende tekst kort en scanbaar. Gebruik duidelijke tussenkoppen en vermijd interne vaktaal.
- Gebruik voor normale tekst minimaal 4,5:1 contrast en voor grote tekst minimaal 3:1.
- Maak maatwaarden als gewone tekst op in plaats van ze alleen in een verkleinde tekening te laten staan.

## Commerciële taalfilter

In klantdocumenten staan geen interne projectnamen, controlebestanden, merken, leveranciers, artikelnummers of typecodes. Gebruik functionele omschrijvingen, zoals `elektrische hefkolommen`, `lineaire geleiding`, `kogelpotten` en `geanodiseerde aluminium systeemprofielen`.

## Herbruikbare productprofielen

De vaste PowerPoint-opmaak staat in één gedeelde template. Productspecifieke inhoud staat in `SolidWorksCustomerPresentationProfile.cs` en bevat:

- klantgerichte titel, belofte en voordelen;
- uitvoering, leveringsomvang en merkneutrale materiaalomschrijvingen;
- camerastandpunten voor de twee renders;
- productrelevante bewegingen en technische details voor de maatcontrole;
- optioneel een afwijkende templatebestandsnaam wanneer een product later echt een eigen layout nodig heeft.

Voor een nieuw product wordt eerst een eigen profiel toegevoegd. De exporter en de PowerPoint-layout hoeven daarbij niet te worden gekopieerd. Als nog geen productprofiel bestaat, gebruikt de exporter een neutrale generieke fallback zonder productspecifieke claims.

## Bronnen

- W3C WCAG 2.2, contrast minimum: https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum
- U.S. Web Design System, ontwerpprincipes: https://designsystem.digital.gov/design-principles/
- GOV.UK Design System, layout: https://design-system.service.gov.uk/styles/layout/
- GOV.UK Design System, spacing: https://design-system.service.gov.uk/styles/spacing/
