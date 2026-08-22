# Repositorystructuur

## Canonieke mappen

```text
src/        applicatiebroncode
tests/      uitvoerbare regressie- en rooktests
config/     versieerbare configuratie, masterdata en catalogusassets
docs/       actuele contracten, productafspraken en ontwerpcontext
scripts/    herhaalbare beheer-, build- en validatiecommando's
.codex/     versieerbare projectskills
```

## Niet-canonieke en gegenereerde mappen

`bin/`, `obj/`, `.codex-artifacts/`, `artifacts/`, `output/`, `outputs/` en `tmp/` zijn vervangbare resultaten. Geen applicatielogica, masterdata of operationele orderdata mag uitsluitend daar bestaan.

Operationele portaldata staat lokaal onder:

```text
C:\SWWerkplaats\PortalData
```

Back-ups en volledige werkmapsnapshots staan buiten de repository onder `C:\software_builds\SWwerkplaatsapp-snapshots`.

## Tijdelijke uitzonderingen

- De webportal en WinForms-interface worden nog uit hetzelfde project gebouwd, maar via verschillende scripts en outputlocaties gestart. Dit wordt in de volgende consolidatiefase één buildroute.
- WinForms blijft alleen zolang de rail-/dragereditor nog niet naar de portal of een beheertool is gemigreerd.
- Excel is de menselijke masterdatabron; enkele runtimeconsumenten gebruiken nog JSON, CSV of hardcoded defaults totdat een gevalideerde masterdatasnapshot beschikbaar is.
- `--solidworks-worker` is geen tweede geometrie-engine. Het is de actieve subprocessgrens rond `SolidWorksComPartExporter` voor timeout, COM-isolatie en retry.
