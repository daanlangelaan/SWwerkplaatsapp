# Repositorystructuur

Status: **actueel contract**.

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

## Actuele grenzen

- `scripts/build-configurator.ps1` is de enige buildroute. Oude scriptnamen zijn uitsluitend wrappers naar die route.
- De webportal is standaard en bevat ook de rail-/dragerbibliotheek op `/library`; WinForms blijft tijdelijk als compatibiliteitsschil.
- Excel is de menselijke masterdatabron. `config/runtime/masterdata-runtime.json` is de gegenereerde applicatiesnapshot; enkele overige catalogusconsumenten gebruiken nog JSON of hardcoded defaults.
- SQLite bewaart operationele ordermetadata buiten Git; orderbestanden blijven een leesbare export- en herstelmirror.
- `--solidworks-worker` is geen tweede geometrie-engine. Het is de actieve subprocessgrens rond `SolidWorksComPartExporter` voor timeout, COM-isolatie en retry.
