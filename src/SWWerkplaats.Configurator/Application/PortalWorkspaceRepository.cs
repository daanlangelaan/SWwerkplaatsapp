using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using Microsoft.Data.Sqlite;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public interface IPortalWorkspaceRepository
    {
        void SyncOrders(IEnumerable<PortalOrderRecord> orders);
        List<PortalProjectRecord> ListProjects();
        PortalProjectRecord LoadProject(string projectId);
        void SaveProject(PortalProjectRecord project);
        List<PortalProductionJob> ListJobs();
        PortalProductionJob LoadJob(string jobId);
        void SaveJob(PortalProductionJob job);
        List<PortalInventoryItem> ListInventory();
        PortalInventoryItem LoadInventoryItem(string inventoryItemId);
        void SaveInventoryItem(PortalInventoryItem item);
        PortalInventoryItem ApplyInventoryMovement(PortalInventoryMovement movement);
    }

    public sealed class SqlitePortalWorkspaceRepository : IPortalWorkspaceRepository
    {
        private readonly string databasePath;
        private readonly JavaScriptSerializer serializer;

        public SqlitePortalWorkspaceRepository(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Workspace-databasepad ontbreekt.", "databasePath");
            this.databasePath = System.IO.Path.GetFullPath(databasePath);
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            Initialize();
        }

        public void SyncOrders(IEnumerable<PortalOrderRecord> orders)
        {
            foreach (var order in orders ?? Enumerable.Empty<PortalOrderRecord>()) SyncOrder(order);
        }

        public List<PortalProjectRecord> ListProjects()
        {
            var result = new List<PortalProjectRecord>();
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM workspace_projects ORDER BY created_at DESC, project_id DESC";
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(Normalize(serializer.Deserialize<PortalProjectRecord>(reader.GetString(0))));
            }
            return result;
        }

        public PortalProjectRecord LoadProject(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) return null;
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM workspace_projects WHERE project_id=$id";
                command.Parameters.AddWithValue("$id", projectId);
                var value = command.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(value) ? null : Normalize(serializer.Deserialize<PortalProjectRecord>(value));
            }
        }

        public void SaveProject(PortalProjectRecord project)
        {
            if (project == null || string.IsNullOrWhiteSpace(project.ProjectId)) throw new ArgumentException("Ongeldig projectrecord.", "project");
            project = Normalize(project);
            project.UpdatedAt = UtcNow();
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO workspace_projects(project_id,order_id,organization_id,source_site_id,status,created_at,updated_at,record_json) "
                    + "VALUES($project,$order,$organization,$site,$status,$created,$updated,$json) "
                    + "ON CONFLICT(project_id) DO UPDATE SET order_id=excluded.order_id,organization_id=excluded.organization_id,source_site_id=excluded.source_site_id,status=excluded.status,updated_at=excluded.updated_at,record_json=excluded.record_json";
                command.Parameters.AddWithValue("$project", project.ProjectId);
                command.Parameters.AddWithValue("$order", project.OrderId ?? string.Empty);
                command.Parameters.AddWithValue("$organization", project.OrganizationId ?? string.Empty);
                command.Parameters.AddWithValue("$site", project.SourceSiteId ?? string.Empty);
                command.Parameters.AddWithValue("$status", project.Status ?? string.Empty);
                command.Parameters.AddWithValue("$created", project.CreatedAt ?? project.UpdatedAt);
                command.Parameters.AddWithValue("$updated", project.UpdatedAt);
                command.Parameters.AddWithValue("$json", serializer.Serialize(project));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<PortalProductionJob> ListJobs()
        {
            var result = new List<PortalProductionJob>();
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM production_jobs ORDER BY updated_at DESC, job_id";
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(serializer.Deserialize<PortalProductionJob>(reader.GetString(0)));
            }
            return result;
        }

        public PortalProductionJob LoadJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return null;
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM production_jobs WHERE job_id=$id";
                command.Parameters.AddWithValue("$id", jobId);
                var value = command.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(value) ? null : serializer.Deserialize<PortalProductionJob>(value);
            }
        }

        public void SaveJob(PortalProductionJob job)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.JobId)) throw new ArgumentException("Ongeldige werkplaatstaak.", "job");
            job.UpdatedAt = UtcNow();
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO production_jobs(job_id,project_id,area_id,status,updated_at,record_json) VALUES($id,$project,$area,$status,$updated,$json) "
                    + "ON CONFLICT(job_id) DO UPDATE SET status=excluded.status,updated_at=excluded.updated_at,record_json=excluded.record_json";
                command.Parameters.AddWithValue("$id", job.JobId);
                command.Parameters.AddWithValue("$project", job.ProjectId ?? string.Empty);
                command.Parameters.AddWithValue("$area", job.AreaId ?? string.Empty);
                command.Parameters.AddWithValue("$status", job.Status ?? string.Empty);
                command.Parameters.AddWithValue("$updated", job.UpdatedAt);
                command.Parameters.AddWithValue("$json", serializer.Serialize(job));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<PortalInventoryItem> ListInventory()
        {
            var result = new List<PortalInventoryItem>();
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM inventory_items ORDER BY description,inventory_item_id";
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(serializer.Deserialize<PortalInventoryItem>(reader.GetString(0)));
            }
            return result;
        }

        public PortalInventoryItem LoadInventoryItem(string inventoryItemId)
        {
            if (string.IsNullOrWhiteSpace(inventoryItemId)) return null;
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM inventory_items WHERE inventory_item_id=$id";
                command.Parameters.AddWithValue("$id", inventoryItemId);
                var value = command.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(value) ? null : serializer.Deserialize<PortalInventoryItem>(value);
            }
        }

        public void SaveInventoryItem(PortalInventoryItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.StableItemId)) throw new ArgumentException("Voorraadartikel mist een stabiel masterdata-ID.", "item");
            if (string.IsNullOrWhiteSpace(item.InventoryItemId)) item.InventoryItemId = "INV-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(item.Description) || string.IsNullOrWhiteSpace(item.StockUnit)) throw new ArgumentException("Voorraadartikel mist omschrijving of voorraadeenheid.", "item");
            if (item.UnitsPerPurchase <= 0) item.UnitsPerPurchase = 1;
            if (item.PhysicalQuantity < 0 || item.ReservedQuantity < 0 || item.ReservedQuantity > item.PhysicalQuantity)
                throw new InvalidOperationException("Ongeldige fysieke of gereserveerde voorraad voor " + item.StableItemId + ".");
            item.UpdatedAt = UtcNow();
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO inventory_items(inventory_item_id,stable_item_id,description,updated_at,record_json) VALUES($id,$stable,$description,$updated,$json) "
                    + "ON CONFLICT(inventory_item_id) DO UPDATE SET stable_item_id=excluded.stable_item_id,description=excluded.description,updated_at=excluded.updated_at,record_json=excluded.record_json";
                command.Parameters.AddWithValue("$id", item.InventoryItemId);
                command.Parameters.AddWithValue("$stable", item.StableItemId);
                command.Parameters.AddWithValue("$description", item.Description);
                command.Parameters.AddWithValue("$updated", item.UpdatedAt);
                command.Parameters.AddWithValue("$json", serializer.Serialize(item));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public PortalInventoryItem ApplyInventoryMovement(PortalInventoryMovement movement)
        {
            if (movement == null || string.IsNullOrWhiteSpace(movement.InventoryItemId)) throw new ArgumentException("Voorraadboeking mist artikel.", "movement");
            if (movement.Quantity == 0) throw new ArgumentException("Voorraadboeking heeft hoeveelheid nul.", "movement");
            var item = LoadInventoryItem(movement.InventoryItemId);
            if (item == null) throw new InvalidOperationException("Voorraadartikel niet gevonden: " + movement.InventoryItemId);
            var type = (movement.MovementType ?? string.Empty).Trim().ToLowerInvariant();
            var quantity = Math.Abs(movement.Quantity);
            if (type == "receipt" || type == "return") item.PhysicalQuantity += quantity;
            else if (type == "issue") item.PhysicalQuantity -= quantity;
            else if (type == "reserve") item.ReservedQuantity += quantity;
            else if (type == "release") item.ReservedQuantity -= quantity;
            else if (type == "correction") item.PhysicalQuantity += movement.Quantity;
            else throw new InvalidOperationException("Onbekend voorraadboekingstype: " + movement.MovementType);
            if (item.PhysicalQuantity < 0 || item.ReservedQuantity < 0 || item.ReservedQuantity > item.PhysicalQuantity)
                throw new InvalidOperationException("Voorraadboeking veroorzaakt een negatieve of overgereserveerde voorraad.");
            movement.MovementId = string.IsNullOrWhiteSpace(movement.MovementId) ? "MOV-" + Guid.NewGuid().ToString("N").ToUpperInvariant() : movement.MovementId;
            movement.CreatedAt = UtcNow();
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            {
                item.UpdatedAt = movement.CreatedAt;
                using (var update = connection.CreateCommand())
                {
                    update.Transaction = transaction;
                    update.CommandText = "UPDATE inventory_items SET updated_at=$updated,record_json=$json WHERE inventory_item_id=$id";
                    update.Parameters.AddWithValue("$updated", item.UpdatedAt);
                    update.Parameters.AddWithValue("$json", serializer.Serialize(item));
                    update.Parameters.AddWithValue("$id", item.InventoryItemId);
                    if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Voorraadartikel kon niet worden bijgewerkt.");
                }
                using (var insert = connection.CreateCommand())
                {
                    insert.Transaction = transaction;
                    insert.CommandText = "INSERT INTO inventory_movements(movement_id,inventory_item_id,movement_type,quantity,project_id,created_at,record_json) VALUES($id,$item,$type,$quantity,$project,$created,$json)";
                    insert.Parameters.AddWithValue("$id", movement.MovementId);
                    insert.Parameters.AddWithValue("$item", movement.InventoryItemId);
                    insert.Parameters.AddWithValue("$type", type);
                    insert.Parameters.AddWithValue("$quantity", movement.Quantity);
                    insert.Parameters.AddWithValue("$project", movement.ProjectId ?? string.Empty);
                    insert.Parameters.AddWithValue("$created", movement.CreatedAt);
                    insert.Parameters.AddWithValue("$json", serializer.Serialize(movement));
                    insert.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            return item;
        }

        private void SyncOrder(PortalOrderRecord order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.OrderId)) return;
            var projectId = string.IsNullOrWhiteSpace(order.ProjectId) ? LegacyProjectId(order.OrderId) : order.ProjectId;
            var existing = LoadProject(projectId);
            var project = existing ?? new PortalProjectRecord
            {
                ProjectId = projectId,
                OrderId = order.OrderId,
                CreatedAt = order.CreatedAt,
                CustomerPublished = false
            };
            project.SourceSiteId = Value(order.SourceSiteId, project.SourceSiteId, "internal");
            project.OrganizationId = Value(order.OrganizationId, project.OrganizationId, "unassigned");
            project.ProjectName = Value(order.ProjectName, project.ProjectName, order.ProductName);
            project.ProductId = Value(order.ProductId, project.ProductId, string.Empty);
            project.ProductName = order.ProductName;
            project.CustomerName = order.CustomerName;
            project.CustomerEmail = order.CustomerEmail;
            project.DeliveryForm = order.DeliveryForm;
            project.ReceiptMethod = order.ReceiptMethod;
            project.AssemblyPriceExVat = order.AssemblyPriceExVat;
            project.AssemblyPriceStatus = order.AssemblyPriceStatus;
            project.ShippingPriceExVat = order.ShippingPriceExVat;
            project.ShippingPriceStatus = order.ShippingPriceStatus;
            project.Status = order.Status;
            project.CustomerStatus = CustomerStatus(order.Status);
            project.PriceExVat = order.PriceExVat;
            project.PriceIncVat = order.PriceIncVat;
            if (order.PurchaseLines != null && order.PurchaseLines.Count > 0) project.PurchaseLines = new List<PortalPurchaseSnapshotLine>(order.PurchaseLines);
            if (order.ProductionAreas != null && order.ProductionAreas.Count > 0) project.ProductionAreas = new List<PortalProductionAreaSnapshot>(order.ProductionAreas);
            if (order.Files != null && order.Files.Count > 0) project.Documents = Documents(project.ProjectId, order.Files);
            if (project.ProductionAreas.Count == 0) project.ProductionAreas = LegacyAreas(order);
            SaveProject(project);
            EnsureJobs(project);
        }

        private void EnsureJobs(PortalProjectRecord project)
        {
            var existing = ListJobs().Where(job => string.Equals(job.ProjectId, project.ProjectId, StringComparison.OrdinalIgnoreCase)).ToDictionary(job => job.AreaId, StringComparer.OrdinalIgnoreCase);
            foreach (var area in project.ProductionAreas)
            {
                PortalProductionJob job;
                if (!existing.TryGetValue(area.AreaId, out job))
                {
                    job = new PortalProductionJob
                    {
                        JobId = "JOB-" + project.ProjectId + "-" + area.AreaId,
                        ProjectId = project.ProjectId,
                        OrderId = project.OrderId,
                        AreaId = area.AreaId,
                        AreaLabel = area.Label,
                        Status = JobStatus(project.Status),
                        ItemCount = area.ItemCount,
                        ProductName = project.ProductName,
                        CustomerName = project.CustomerName,
                        Documents = AreaDocuments(project.Documents, area.AreaId)
                    };
                    SaveJob(job);
                }
                else
                {
                    var documents = AreaDocuments(project.Documents, area.AreaId);
                    var changed = !DocumentIds(job.Documents).SequenceEqual(DocumentIds(documents), StringComparer.OrdinalIgnoreCase);
                    if (!string.Equals(job.Status, "Gereed", StringComparison.OrdinalIgnoreCase) && string.Equals(project.Status, "Gereed", StringComparison.OrdinalIgnoreCase))
                    {
                        job.Status = "Gereed";
                        changed = true;
                    }
                    if (changed)
                    {
                        job.Documents = documents;
                        SaveJob(job);
                    }
                }
            }
        }

        private static List<PortalProjectDocument> AreaDocuments(IEnumerable<PortalProjectDocument> documents, string areaId)
        {
            return (documents ?? Enumerable.Empty<PortalProjectDocument>()).Where(document =>
            {
                var name = (document.FileName ?? string.Empty).ToLowerInvariant();
                if (string.Equals(areaId, "profile-machine", StringComparison.OrdinalIgnoreCase)) return name.Contains("profiel") || name.Contains("profile");
                if (string.Equals(areaId, "sheet-cnc", StringComparison.OrdinalIgnoreCase)) return document.Category == "Productiebestand" || name.Contains("plaat") || name.Contains("nest") || name.Contains("toolpath");
                if (string.Equals(areaId, "3d-print", StringComparison.OrdinalIgnoreCase)) return name.Contains("3d-print") || name.Contains("print");
                if (string.Equals(areaId, "assembly", StringComparison.OrdinalIgnoreCase)) return document.Category == "Assemblage-instructie" || document.Category == "Assemblagecontrole";
                if (string.Equals(areaId, "dispatch", StringComparison.OrdinalIgnoreCase)) return document.CustomerVisible || document.Category == "Offerte" || document.Category == "Factuur";
                return false;
            }).ToList();
        }

        private static IEnumerable<string> DocumentIds(IEnumerable<PortalProjectDocument> documents)
        {
            return (documents ?? Enumerable.Empty<PortalProjectDocument>()).Select(document => document.DocumentId ?? string.Empty).OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
        }

        private void Initialize()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(databasePath));
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS workspace_projects(project_id TEXT PRIMARY KEY,order_id TEXT NOT NULL UNIQUE,organization_id TEXT NOT NULL,source_site_id TEXT NOT NULL,status TEXT NOT NULL,created_at TEXT NOT NULL,updated_at TEXT NOT NULL,record_json TEXT NOT NULL);"
                    + "CREATE INDEX IF NOT EXISTS ix_workspace_projects_organization ON workspace_projects(organization_id,created_at DESC);"
                    + "CREATE INDEX IF NOT EXISTS ix_workspace_projects_site ON workspace_projects(source_site_id,created_at DESC);"
                    + "CREATE TABLE IF NOT EXISTS production_jobs(job_id TEXT PRIMARY KEY,project_id TEXT NOT NULL,area_id TEXT NOT NULL,status TEXT NOT NULL,updated_at TEXT NOT NULL,record_json TEXT NOT NULL,UNIQUE(project_id,area_id));"
                    + "CREATE INDEX IF NOT EXISTS ix_production_jobs_area_status ON production_jobs(area_id,status);"
                    + "CREATE TABLE IF NOT EXISTS inventory_items(inventory_item_id TEXT PRIMARY KEY,stable_item_id TEXT NOT NULL UNIQUE,description TEXT NOT NULL,updated_at TEXT NOT NULL,record_json TEXT NOT NULL);"
                    + "CREATE TABLE IF NOT EXISTS inventory_movements(movement_id TEXT PRIMARY KEY,inventory_item_id TEXT NOT NULL,movement_type TEXT NOT NULL,quantity REAL NOT NULL,project_id TEXT NOT NULL,created_at TEXT NOT NULL,record_json TEXT NOT NULL,FOREIGN KEY(inventory_item_id) REFERENCES inventory_items(inventory_item_id));"
                    + "CREATE INDEX IF NOT EXISTS ix_inventory_movements_item ON inventory_movements(inventory_item_id,created_at DESC);";
                command.ExecuteNonQuery();
            }
        }

        private SqliteConnection Open()
        {
            var builder = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared, Pooling = true, DefaultTimeout = 30 };
            var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000;";
                command.ExecuteNonQuery();
            }
            return connection;
        }

        private static PortalProjectRecord Normalize(PortalProjectRecord project)
        {
            if (project.PurchaseLines == null) project.PurchaseLines = new List<PortalPurchaseSnapshotLine>();
            if (project.ProductionAreas == null) project.ProductionAreas = new List<PortalProductionAreaSnapshot>();
            if (project.Documents == null) project.Documents = new List<PortalProjectDocument>();
            return project;
        }

        private static List<PortalProjectDocument> Documents(string projectId, IEnumerable<string> files)
        {
            return files.Where(file => !string.IsNullOrWhiteSpace(file)).Select((file, index) =>
            {
                var name = System.IO.Path.GetFileName(file);
                var lower = name.ToLowerInvariant();
                var category = lower.Contains("offerte") ? "Offerte"
                    : lower.Contains("factuur") ? "Factuur"
                    : lower.Contains("assemblage") && (lower.Contains("instruct") || lower.Contains("handleiding")) ? "Assemblage-instructie"
                    : lower.Contains("assemblage") ? "Assemblagecontrole"
                    : lower.Contains("klant") || lower.Contains("customer") ? "Klantbijlage"
                    : lower.EndsWith(".glb") || lower.EndsWith(".gltf") ? "3D-model"
                    : lower.Contains("nest") || lower.EndsWith(".tap") || lower.EndsWith(".nc") ? "Productiebestand"
                    : "Projectbestand";
                var customerVisible = category == "Offerte" || category == "Factuur" || category == "Assemblage-instructie"
                    || category == "Klantbijlage";
                return new PortalProjectDocument
                {
                    DocumentId = projectId + "-DOC-" + (index + 1).ToString("000", CultureInfo.InvariantCulture),
                    FileName = name,
                    Category = category,
                    Status = "Beschikbaar",
                    CustomerVisible = customerVisible
                };
            }).ToList();
        }

        private static List<PortalProductionAreaSnapshot> LegacyAreas(PortalOrderRecord order)
        {
            var files = order.Files ?? new List<string>();
            var result = new List<PortalProductionAreaSnapshot>();
            if (files.Any(file => file.StartsWith("Profiel", StringComparison.OrdinalIgnoreCase) || file.StartsWith("Afkort", StringComparison.OrdinalIgnoreCase))) result.Add(new PortalProductionAreaSnapshot { AreaId = "profile-machine", Label = "Profielenmachine", ItemCount = 1 });
            if (files.Any(file => file.IndexOf("Nesting", StringComparison.OrdinalIgnoreCase) >= 0 || file.StartsWith("Plaat", StringComparison.OrdinalIgnoreCase))) result.Add(new PortalProductionAreaSnapshot { AreaId = "sheet-cnc", Label = "Plaat-CNC", ItemCount = 1 });
            if (files.Any(file => file.IndexOf("3D-print", StringComparison.OrdinalIgnoreCase) >= 0)) result.Add(new PortalProductionAreaSnapshot { AreaId = "3d-print", Label = "3D-print", ItemCount = 1 });
            if (files.Any(file => file.StartsWith("Assemblage", StringComparison.OrdinalIgnoreCase))) result.Add(new PortalProductionAreaSnapshot { AreaId = "assembly", Label = "Assemblage", ItemCount = 1 });
            result.Add(new PortalProductionAreaSnapshot { AreaId = "dispatch", Label = "Completeren & verzending", ItemCount = 1 });
            return result;
        }

        private static string CustomerStatus(string orderStatus)
        {
            if (string.Equals(orderStatus, "Gereed", StringComparison.OrdinalIgnoreCase)) return "Gereed";
            if (string.Equals(orderStatus, "In productie", StringComparison.OrdinalIgnoreCase) || string.Equals(orderStatus, "In freeswachtrij", StringComparison.OrdinalIgnoreCase)) return "In productie";
            return "In voorbereiding";
        }

        private static string JobStatus(string orderStatus)
        {
            if (string.Equals(orderStatus, "Gereed", StringComparison.OrdinalIgnoreCase)) return "Gereed";
            if (string.Equals(orderStatus, "In productie", StringComparison.OrdinalIgnoreCase)) return "Bezig";
            if (string.Equals(orderStatus, "In freeswachtrij", StringComparison.OrdinalIgnoreCase)) return "Wachtrij";
            return "Voorbereiding";
        }

        private static string LegacyProjectId(string orderId)
        {
            return orderId.StartsWith("SW-", StringComparison.OrdinalIgnoreCase) ? "P-" + orderId.Substring(3) : "P-" + orderId;
        }

        private static string Value(string preferred, string current, string fallback)
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred : (!string.IsNullOrWhiteSpace(current) ? current : fallback);
        }

        private static string UtcNow()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
