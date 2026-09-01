# SolidWorks-workercontract

Status: **actueel contract**.

De build gebruikt een vergrendelde interop-referentie zodat CI zonder SolidWorks-installatie kan compileren. Op een werkstation overschrijft de build die kopie na compilatie met de geïnstalleerde 3DEXPERIENCE R2026x-interop wanneer deze op het geconfigureerde pad bestaat. `InstalledSolidWorksInteropPath` kan als MSBuild-property worden overschreven.

Status: **actueel contract**

De worker is geen tweede geometrie-engine. `ProductModelBuildService` bouwt hetzelfde `WorkbenchModel` als de portal. De worker vormt uitsluitend een apart proces rond `SolidWorksComPartExporter`, zodat COM-fouten, time-outs en een herstartbare retry de portal niet meenemen.

De worker opent nooit stilzwijgend een tweede SolidWorks naast een bestaande lokale `SLDWORKS.exe`. Hij probeert eerst de standaard-COM-registratie en de 3DEXPERIENCE `SolidWorks_PID_*`-ROT-koppeling. Bestaat het proces al maar is COM nog niet beschikbaar, dan wacht hij maximaal twee minuten en stopt vervolgens met een expliciete fout. Een procesbrede named mutex voorkomt dat twee workers gelijktijdig de 3DEXPERIENCE-launcher starten. Een gevonden COM-object is pas bruikbaar wanneer `StartupProcessCompleted` waar is en `CommandInProgress` onwaar; `RevisionNumber()` alleen is geen gereedheidsbewijs omdat 3DEXPERIENCE COM al tijdens het laden van add-ins registreert.

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

## Losse assembly-/WAC-proef

De normale worker blijft voorlopig het multibody `*_CONTROLE.SLDPRT` leveren. Een echte componentassembly wordt pas maatgevend nadat de lokale werkplek de afzonderlijke proef heeft doorstaan:

```text
SWWerkplaats.Configurator.exe --solidworks-assembly-probe <result.json> <probe-map>
```

De proef staat bewust naast procescontract v1 en verandert de bestaande export niet. Hij maakt twee verschillende lokale `SLDPRT`-bestanden, voegt beide met `AddComponent5` in één `SLDASM` in, slaat die op, sluit hem, opent hem opnieuw en telt de componenten. `SolidWorksAssemblyProbeResult` meldt de exacte faalfase, HRESULT, fouttekst en of de fout herkenbaar door Windows Application Control/beleid is veroorzaakt. Een time-out moet door de aanroepende procesisolatie worden geregistreerd; een hangende beleidsdialoog mag nooit als geslaagde proef worden behandeld.

Gebruik lokaal `scripts/test-solidworks-assembly-insertion.ps1`; dit script begrenst de worker en legt gelijktijdig recente Code Integrity- en AppLocker-events vast.

Pas na `Status = Passed`, `InsertedComponentCount = 2` en `ReopenedComponentCount = 2` mag de parallelle SLDASM-/roundtripketen worden geactiveerd. De app schakelt Windows-beleid niet uit en voegt geen uitzonderingen toe; een beleidsblokkade wordt met de verzamelde diagnose aan beheer voorgelegd.

## Parallelle geaudite assembly

De geaudite keten staat naast procescontract v1 en wordt uitsluitend uitgevoerd wanneer de gebruiker in de projectexport het standaard uitgeschakelde vinkje **SolidWorks geometriecontrole** kiest. Een normale SolidWorks- of Projectdata-export start deze zware controle niet. Hij kan voor diagnose ook afzonderlijk worden gestart:

```text
SWWerkplaats.Configurator.exe --solidworks-audited-assembly-worker <input.json> <result.json>
```

Deze keten bouwt vanuit dezelfde `PortalAssembly3DService`-contractlijst lokale auditparts, plaatst alle instanties in één `AddComponents3`-batch met de gedeelde XYZ-Eulermatrices en slaat `<project>_AUDIT.SLDASM` op. Voor de roundtrip wordt een uniek benoemde bytekopie van de opgeslagen assembly geopend. Daarmee kan een al geopende 3DEXPERIENCE-assembly met dezelfde titel de controle niet blokkeren en wordt toch het bestand op schijf bewezen.

Vóór SolidWorks controleert de universele bronaudit iedere plaatplaatsing op expliciete plaatdikte en iedere bronbewerking één-op-één op type, maat, dieptewijze, wereldpositie en juiste zijde. Sleuven en rabbets die een aansluitend deel moeten bevatten dragen een stabiel fitcontract met minimale volumebezetting; een lege of onvoldoende gevulde groef wordt als `REQUIRED_POCKET_EMPTY` gerapporteerd. Nieuwe producten vallen automatisch onder deze audit zodra zij het gedeelde `WorkbenchModel` en rendercontract gebruiken.

Na opnieuw openen vergelijkt het JSON-rapport per stabiel audit-ID de verwachte en door SolidWorks teruggelezen transform, wereldbounds en twaalfdelige body-massasignatuur. Een ontbrekend component, gewijzigde body of een delta boven `0,05 mm` blokkeert de geometrie-audit. Daarna draait SolidWorks' eigen volumetrische interferentiecontrole. Overlap tussen primitives van één logisch component wordt als interne unie geregistreerd. Overlap met `ProvisionalRenderEnvelope` blijft zichtbaar als open data en blokkeert vrijgave; alleen een overlap tussen maatgevende componenten is een blokkerende interferentie. Geroteerde bodies met gaten of pockets worden voorlopig expliciet geblokkeerd; zij mogen niet met een vermoedelijke assentransformatie worden geëxporteerd.

Eén-voor-één `AddComponent5` plus solve/fix is verboden voor deze auditketen: dat pad blokkeert de 3DEXPERIENCE-sessie al na enkele componenten en een collectieve `FixComponent`-solve overschrijdt bij 206 componenten de worker-time-out. De controleassembly bewaart daarom expliciete componenttransforms zonder mates/fix; zij is bedoeld om onderdelen uit te zetten en maten te controleren. Voor een niet-versleepbare controlekopie kan de gebruiker de geaudite SLDASM als STEP opslaan en die STEP openen, maar de SLDASM plus audit-JSON blijven het traceerbare bronbewijs.

`GeometryPassedReleaseBlocked` betekent dat bronbewerkingen, transform, bounds, body-roundtrip en blokkerende interferenties zijn gecontroleerd, maar `ProvisionalRenderEnvelope` of `OpenData` nog aanwezig is. Dat is geldig controlebewijs, geen productie- of inkoopvrijgave. Bij een geselecteerde geometriecontrole worden de geaudite SLDASM en het JSON-auditrapport altijd bewaard, ook wanneer SolidWorks of Projectdata verder niet zijn geselecteerd. Procescontract v1 blijft ondertussen ongewijzigd het bestaande multibody controlepart en de bestaande klantoutput leveren; `ControlModelPath` wijst naar de geaudite SLDASM.

De handmatige pilot wordt gestart met `scripts/export-solidworks-audited-assembly.ps1 -InputPath <PortalQuoteRequest.json>`. Ook dit script draait de nieuwe keten apart en bewaakt de bestaande worker niet.
