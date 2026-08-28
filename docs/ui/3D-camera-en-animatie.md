# 3D-camera en animatie

Status: **actueel contract**.

## Afbakening

Camera en animatie verduidelijken een al door de backend vastgelegde handeling. Zij
mogen geen technische positie, volgorde of maat bepalen.

## Camera-presets

Het machineleesbare contract kent afzonderlijke presets voor:

- montageoverzicht;
- verbindingsdetail;
- gereedschapsdetail;
- verzamelstap;
- vrije 3D-viewer.

Een preset bevat alleen fitmarges, zoomgrenzen, kijkrichtingvoorkeuren en
interactie-instellingen. Het focuspunt komt altijd uit het rendercontract.

## Animatie

Animatie-instellingen bevatten alleen tijd, easing, pauzes, herhaling en visuele
accentsterkte. Afstand en richting komen uit de stapdata. Bij `prefers-reduced-motion`
wordt de eindtoestand direct en volledig getoond.

## Gedragsregels

- Stapwissel verandert niet vanzelf de open/dicht-status van het overzicht.
- Inline 3D blokkeert op mobiel nooit onbedoeld verticaal scrollen.
- Vrij roteren en zoomen wordt pas actief na een expliciete actie.
- Detail en overzicht kunnen dezelfde kijkrichting delen, maar houden een eigen fit.
- Labels worden na iedere camerawijziging opnieuw geprojecteerd.
- Animaties eindigen deterministisch in de technisch juiste toestand.

## Testmatrix

- 390 px mobiel;
- 768 px tablet;
- 1440 px desktop;
- volledig scherm;
- normale en gereduceerde beweging;
- ingeklapt en uitgeklapt overzicht;
- één en meerdere herhalingen.

