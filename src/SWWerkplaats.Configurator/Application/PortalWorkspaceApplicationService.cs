using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class PortalWorkspaceApplicationService
    {
        private static readonly string[] AllowedJobStatuses = { "Voorbereiding", "Wachtrij", "Bezig", "Geblokkeerd", "Gereed" };
        private readonly IPortalWorkspaceRepository repository;
        private readonly OrderApplicationService orders;
        private readonly PortalRolePolicy roles;
        private readonly PortalSiteCatalog sites;

        public PortalWorkspaceApplicationService(IPortalWorkspaceRepository repository, OrderApplicationService orders, PortalRolePolicy roles, PortalSiteCatalog sites)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository");
            this.orders = orders ?? throw new ArgumentNullException("orders");
            this.roles = roles ?? throw new ArgumentNullException("roles");
            this.sites = sites ?? throw new ArgumentNullException("sites");
        }

        public void Sync()
        {
            repository.SyncOrders(orders.ListOrders());
        }

        public PortalWorkspaceContextResponse GetContext(PortalActorContext actor)
        {
            if (actor == null) throw new ArgumentNullException("actor");
            return new PortalWorkspaceContextResponse
            {
                Actor = actor,
                AvailableRoles = roles.List(),
                AvailableSites = sites.List()
            };
        }

        public List<PortalProjectView> ListProjects(PortalActorContext actor)
        {
            Sync();
            IEnumerable<PortalProjectRecord> projects = repository.ListProjects();
            var customerView = PortalRolePolicy.Has(actor, PortalCapabilities.CustomerProjectRead)
                && !PortalRolePolicy.Has(actor, PortalCapabilities.ProjectReadAll);
            if (customerView)
            {
                projects = projects.Where(project => project.CustomerPublished
                    && string.Equals(project.OrganizationId, actor.OrganizationId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.ProjectReadAll);
            }

            return projects.Select(project => ToView(project, customerView)).ToList();
        }

        public List<PortalProductionJob> ListJobs(PortalActorContext actor)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.JobsReadAll);
            Sync();
            var jobs = repository.ListJobs();
            if (PortalRolePolicy.Has(actor, PortalCapabilities.JobsUpdateAll) || string.IsNullOrWhiteSpace(actor.DefaultWorkAreaId)) return jobs;
            return jobs.Where(job => string.Equals(job.AreaId, actor.DefaultWorkAreaId, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public PortalProjectDetail GetProject(PortalActorContext actor, string projectId)
        {
            Sync();
            var project = repository.LoadProject(projectId);
            if (project == null) throw new InvalidOperationException("Project niet gevonden: " + projectId);
            var customerView = PortalRolePolicy.Has(actor, PortalCapabilities.CustomerProjectRead)
                && !PortalRolePolicy.Has(actor, PortalCapabilities.ProjectReadAll);
            if (customerView)
            {
                if (!project.CustomerPublished || !string.Equals(project.OrganizationId, actor.OrganizationId, StringComparison.OrdinalIgnoreCase))
                    throw new PortalAccessDeniedException("Dit project is niet gepubliceerd voor de gekozen klantorganisatie.");
            }
            else PortalRolePolicy.Ensure(actor, PortalCapabilities.ProjectReadAll);

            var detail = new PortalProjectDetail
            {
                Project = ToView(project, customerView),
                Documents = project.Documents.Where(document => !customerView || document.CustomerVisible).ToList()
            };
            if (!customerView)
            {
                detail.PurchaseLines = new List<PortalPurchaseSnapshotLine>(project.PurchaseLines);
                detail.ProductionAreas = new List<PortalProductionAreaSnapshot>(project.ProductionAreas);
            }
            AddMissingDocument(detail, "Offerte", "Offerte of offertebijlage is nog niet beschikbaar.");
            AddMissingDocument(detail, "Factuur", "Factuur is nog niet beschikbaar.");
            AddMissingDocument(detail, "Assemblage-instructie", "Assemblage-instructies zijn nog niet beschikbaar.");
            if (!detail.Documents.Any(document => document.Category == "Klantbijlage" && (document.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) || document.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))))
                detail.OpenData.Add("Interactief 3D-klantmodel is nog niet beschikbaar.");
            return detail;
        }

        public PortalProductionJob ChangeJobStatus(PortalActorContext actor, string jobId, PortalJobStatusRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var job = repository.LoadJob(jobId);
            if (job == null) throw new InvalidOperationException("Werkplaatstaak niet gevonden: " + jobId);
            PortalRolePolicy.EnsureJobUpdate(actor, job.AreaId);
            var status = AllowedJobStatuses.FirstOrDefault(value => string.Equals(value, request.Status, StringComparison.OrdinalIgnoreCase));
            if (status == null) throw new InvalidOperationException("Ongeldige werkplaatsstatus: " + request.Status);
            if (string.Equals(status, "Geblokkeerd", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.BlockedReason))
                throw new InvalidOperationException("Geef een reden op wanneer een werkplaatstaak wordt geblokkeerd.");
            job.Status = status;
            job.BlockedReason = string.Equals(status, "Geblokkeerd", StringComparison.OrdinalIgnoreCase) ? request.BlockedReason.Trim() : null;
            repository.SaveJob(job);
            return job;
        }

        public List<PortalPurchaseRequirement> ListPurchaseRequirements(PortalActorContext actor)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.PurchasingRead);
            Sync();
            var inventory = repository.ListInventory().Where(item => item.TrackStock)
                .GroupBy(item => item.StableItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var lines = repository.ListProjects()
                .Where(project => !string.Equals(project.Status, "Gereed", StringComparison.OrdinalIgnoreCase))
                .SelectMany(project => project.PurchaseLines.Select(line => new { Project = project, Line = line }))
                .Where(value => !string.IsNullOrWhiteSpace(value.Line.StableItemId));

            return lines.GroupBy(value => value.Line.StableItemId + "\n" + (value.Line.Unit ?? ""), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first = group.First().Line;
                    PortalInventoryItem stock;
                    inventory.TryGetValue(first.StableItemId, out stock);
                    var required = group.Sum(value => value.Line.RequiredQuantity);
                    var unitMatches = stock != null && string.Equals(stock.StockUnit, first.Unit, StringComparison.OrdinalIgnoreCase);
                    var available = unitMatches ? stock.AvailableQuantity : 0m;
                    var shortage = Math.Max(0m, required - available);
                    var unitsPerPurchase = stock == null || stock.UnitsPerPurchase <= 0 ? 1m : stock.UnitsPerPurchase;
                    var purchaseUnits = shortage <= 0 ? 0m : Math.Ceiling(shortage / unitsPerPurchase);
                    return new PortalPurchaseRequirement
                    {
                        StableItemId = first.StableItemId,
                        Description = first.Description,
                        Category = first.Category,
                        RequiredQuantity = required,
                        Unit = first.Unit,
                        AvailableQuantity = available,
                        ShortageQuantity = shortage,
                        UnitsPerPurchase = unitsPerPurchase,
                        SuggestedPurchaseUnits = purchaseUnits,
                        PurchaseUnit = stock == null || string.IsNullOrWhiteSpace(stock.PurchaseUnit) ? first.Unit : stock.PurchaseUnit,
                        Supplier = first.Supplier,
                        SupplierArticleCode = first.SupplierArticleCode,
                        PurchaseUnitPrice = first.PurchaseUnitPrice,
                        EstimatedPurchaseTotal = purchaseUnits * unitsPerPurchase * first.PurchaseUnitPrice,
                        PriceStatus = first.PriceStatus,
                        AvailabilityStatus = stock == null ? "Niet als voorraadartikel gevolgd" : (unitMatches ? "Voorraad gekoppeld" : "Voorraadeenheid komt niet overeen met BOM-eenheid"),
                        ProjectIds = group.Select(value => value.Project.ProjectId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList()
                    };
                })
                .OrderByDescending(value => value.ShortageQuantity)
                .ThenBy(value => value.Description)
                .ToList();
        }

        public List<PortalInventoryItem> ListInventory(PortalActorContext actor)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.InventoryRead);
            return repository.ListInventory();
        }

        public List<PortalInventoryCandidate> ListInventoryCandidates(PortalActorContext actor)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.InventoryRead);
            var masterdata = MasterDataRuntimeCatalog.LoadRequired();
            var values = new List<PortalInventoryCandidate>();
            values.AddRange(masterdata.Records("components").Select(record => new PortalInventoryCandidate
            {
                StableItemId = MasterDataRuntimeCatalog.Value(record, "Component-ID"),
                Description = MasterDataRuntimeCatalog.Value(record, "Naam"),
                Category = "Component"
            }));
            values.AddRange(masterdata.Records("materials").Select(record => new PortalInventoryCandidate
            {
                StableItemId = MasterDataRuntimeCatalog.Value(record, "Materiaal-ID"),
                Description = FirstValue(MasterDataRuntimeCatalog.Value(record, "Klantnaam"), MasterDataRuntimeCatalog.Value(record, "Kwaliteit/type"), MasterDataRuntimeCatalog.Value(record, "Materiaal-ID")),
                Category = "Materiaal"
            }));
            return values.Where(value => !string.IsNullOrWhiteSpace(value.StableItemId))
                .GroupBy(value => value.StableItemId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(value => value.Description)
                .ThenBy(value => value.StableItemId)
                .ToList();
        }

        public PortalInventoryItem SaveInventoryItem(PortalActorContext actor, PortalInventoryItem item)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.InventoryUpdate);
            if (item == null) throw new ArgumentNullException("item");
            var candidate = ListInventoryCandidates(actor).FirstOrDefault(value => string.Equals(value.StableItemId, item.StableItemId, StringComparison.OrdinalIgnoreCase));
            if (candidate == null) throw new InvalidOperationException("Voorraadartikel verwijst niet naar een bestaand component of materiaal in de runtime-masterdata: " + item.StableItemId);
            item.StableItemId = candidate.StableItemId;
            item.Description = candidate.Description;
            repository.SaveInventoryItem(item);
            return repository.LoadInventoryItem(item.InventoryItemId);
        }

        public PortalInventoryItem ApplyInventoryMovement(PortalActorContext actor, string inventoryItemId, PortalInventoryMovementRequest request)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.InventoryUpdate);
            if (request == null) throw new ArgumentNullException("request");
            return repository.ApplyInventoryMovement(new PortalInventoryMovement
            {
                InventoryItemId = inventoryItemId,
                MovementType = request.MovementType,
                Quantity = request.Quantity,
                ProjectId = request.ProjectId,
                Note = request.Note,
                ActorUserId = actor.UserId
            });
        }

        public PortalProjectView SetCustomerPublication(PortalActorContext actor, string projectId, PortalProjectPublicationRequest request)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.CustomerPublish);
            if (request == null) throw new ArgumentNullException("request");
            Sync();
            var project = repository.LoadProject(projectId);
            if (project == null) throw new InvalidOperationException("Project niet gevonden: " + projectId);
            project.CustomerPublished = request.Published;
            repository.SaveProject(project);
            return ToView(project, false);
        }

        public PortalWorkspaceDashboard GetDashboard(PortalActorContext actor)
        {
            PortalRolePolicy.Ensure(actor, PortalCapabilities.DashboardRead);
            var projects = PortalRolePolicy.Has(actor, PortalCapabilities.ProjectReadAll)
                || PortalRolePolicy.Has(actor, PortalCapabilities.CustomerProjectRead)
                ? ListProjects(actor)
                : new List<PortalProjectView>();
            var result = new PortalWorkspaceDashboard
            {
                ProjectCount = projects.Count,
                PublishedProjectCount = projects.Count(project => project.CustomerPublished),
                RecentProjects = projects.Take(8).ToList()
            };
            if (PortalRolePolicy.Has(actor, PortalCapabilities.JobsReadAll))
            {
                var jobs = ListJobs(actor);
                result.WaitingJobCount = jobs.Count(job => string.Equals(job.Status, "Wachtrij", StringComparison.OrdinalIgnoreCase));
                result.BlockedJobCount = jobs.Count(job => string.Equals(job.Status, "Geblokkeerd", StringComparison.OrdinalIgnoreCase));
                result.ActiveJobs = jobs.Where(job => !string.Equals(job.Status, "Gereed", StringComparison.OrdinalIgnoreCase)).Take(8).ToList();
            }
            if (PortalRolePolicy.Has(actor, PortalCapabilities.PurchasingRead))
                result.PurchaseShortageCount = ListPurchaseRequirements(actor).Count(value => value.ShortageQuantity > 0);
            if (PortalRolePolicy.Has(actor, PortalCapabilities.InventoryRead))
                result.LowStockCount = repository.ListInventory().Count(item => item.TrackStock && item.AvailableQuantity < item.MinimumQuantity);
            return result;
        }

        private static PortalProjectView ToView(PortalProjectRecord project, bool customerView)
        {
            return new PortalProjectView
            {
                ProjectId = project.ProjectId,
                SourceSiteId = project.SourceSiteId,
                OrganizationId = customerView ? null : project.OrganizationId,
                OrderId = project.OrderId,
                ProjectName = project.ProjectName,
                ProductName = project.ProductName,
                CustomerName = customerView ? null : project.CustomerName,
                Status = customerView ? project.CustomerStatus : project.Status,
                CustomerPublished = project.CustomerPublished,
                CreatedAt = project.CreatedAt,
                PriceIncVat = project.PriceIncVat
            };
        }

        private static string FirstValue(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static void AddMissingDocument(PortalProjectDetail detail, string category, string message)
        {
            if (!detail.Documents.Any(document => string.Equals(document.Category, category, StringComparison.OrdinalIgnoreCase))) detail.OpenData.Add(message);
        }
    }
}
