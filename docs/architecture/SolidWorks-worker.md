# SolidWorks-workercontract

Status: **actueel contract**.

De build gebruikt een vergrendelde interop-referentie zodat CI zonder SolidWorks-installatie kan compileren. Op een werkstation overschrijft de build die kopie na compilatie met de geïnstalleerde 3DEXPERIENCE R2026x-interop wanneer deze op het geconfigureerde pad bestaat. `InstalledSolidWorksInteropPath` kan als MSBuild-property worden overschreven.

Status: **actueel contract**

De worker is geen tweede geometrie-engine. `ProductModelBuildService` bouwt hetzelfde `WorkbenchModel` als de portal. De worker vormt uitsluitend een apart proces rond `SolidWorksComPartExporter`, zodat COM-fouten, time-outs en een herstartbare retry de portal niet meenemen.

## Procescontract v1

```text
SWWerkplaats.Configurator.exe --solidworks-worker <input.json> <result.json>
```

- `input.json`: één geserialiseerde `PortalQuoteRequest`.
- `result.json`: één `SolidWorksWorkerResult` met `ContractVersion = 1`.
- succes: `Ok = true` en een bestaand `AssemblyPath`.
- fout: `Ok = false` en een volledige `Error`.
- exitcode `0`: resultaat succesvol geschreven; exitcode `2`: exportfout.
- maximale duur vanuit de portal: tien minuten.
- alleen bekende tijdelijke RPC-fouten krijgen exact één retry.

Dezelfde gebouwde executable verzorgt portal, desktopcompatibiliteit en worker. Een later zelfstandig workerproject mag pas worden ingevoerd wanneer dit contract, de time-out, COM-isolatie en retry ongewijzigd door regressietests worden afgedekt.
