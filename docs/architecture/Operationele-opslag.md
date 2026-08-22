# Operationele opslag

Status: **actueel contract**.

## Huidig contract

`IOrderRepository` is de grens tussen orderflow en opslag. Standaard gebruikt de portal `SqliteOrderRepository` met `portal-orders.sqlite` onder de externe PortalData-map. De database bevat ordermetadata en de oorspronkelijke aanvraag; gegenereerde productie- en klantbestanden blijven in de ordermap staan.

Bij eerste start worden bestaande bestandsorders idempotent in SQLite geregistreerd. Iedere nieuwe of gewijzigde order wordt ook als JSON gespiegeld. Daardoor blijven exports inspecteerbaar en is herstel mogelijk zonder dat applicatiecode twee onafhankelijke waarheden hoeft te combineren.

SQLite draait met WAL, foreign keys, een busy-timeout en korte transacties. De integratietest voert parallelle schrijfacties en een migratie uit.

## Schaalgrens

Dit ontwerp ondersteunt meerdere gelijktijdige gebruikers via één portalproces. Het ondersteunt niet meerdere applicatieservers of een databasebestand op een netwerkshare. Als horizontale schaal nodig wordt, blijft het application-contract gelijk en komt er een PostgreSQL- of SQL Server-implementatie van `IOrderRepository`.

## Back-up

Maak samen een consistente back-up van `portal-orders.sqlite` en de PortalData-ordermappen. Stop de portal of gebruik een SQLite-aware online backup; kopieer niet alleen willekeurig het hoofd-databasebestand terwijl WAL actief is.
