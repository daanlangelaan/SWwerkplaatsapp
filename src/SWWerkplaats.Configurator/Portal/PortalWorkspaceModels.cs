using System;
using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalSiteDefinition
    {
        public string SiteId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string PresentationProfile { get; set; }
        public string AccentColorToken { get; set; }
        public bool AllowAllProducts { get; set; }
        public List<string> AllowedProductIds { get; set; }
        public List<string> OpenData { get; set; }

        public PortalSiteDefinition()
        {
            AllowedProductIds = new List<string>();
            OpenData = new List<string>();
        }
    }

    public sealed class PortalSiteCatalogContract
    {
        public int ContractVersion { get; set; }
        public List<PortalSiteDefinition> Sites { get; set; }

        public PortalSiteCatalogContract()
        {
            Sites = new List<PortalSiteDefinition>();
        }
    }

    public sealed class PortalRoleDefinition
    {
        public string RoleId { get; set; }
        public string Label { get; set; }
        public string HomeRoute { get; set; }
        public string DefaultWorkAreaId { get; set; }
        public List<string> Capabilities { get; set; }

        public PortalRoleDefinition()
        {
            Capabilities = new List<string>();
        }
    }

    public sealed class PortalActorContext
    {
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string SiteId { get; set; }
        public string RoleId { get; set; }
        public string RoleLabel { get; set; }
        public string HomeRoute { get; set; }
        public string DefaultWorkAreaId { get; set; }
        public bool IsSimulated { get; set; }
        public List<string> Capabilities { get; set; }

        public PortalActorContext()
        {
            Capabilities = new List<string>();
        }
    }

    public sealed class PortalWorkspaceContextResponse
    {
        public PortalActorContext Actor { get; set; }
        public List<PortalRoleDefinition> AvailableRoles { get; set; }
        public List<PortalSiteDefinition> AvailableSites { get; set; }

        public PortalWorkspaceContextResponse()
        {
            AvailableRoles = new List<PortalRoleDefinition>();
            AvailableSites = new List<PortalSiteDefinition>();
        }
    }

    public sealed class PortalPurchaseSnapshotLine
    {
        public string StableItemId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public decimal PurchaseUnitPrice { get; set; }
        public decimal PurchaseTotal { get; set; }
        public string SupplierId { get; set; }
        public string Supplier { get; set; }
        public string SupplierArticleCode { get; set; }
        public string OfferId { get; set; }
        public string PriceStatus { get; set; }
        public string OrderUrl { get; set; }
    }

    public sealed class PortalProductionAreaSnapshot
    {
        public string AreaId { get; set; }
        public string Label { get; set; }
        public int ItemCount { get; set; }
    }

    public sealed class PortalProjectRecord
    {
        public string ProjectId { get; set; }
        public string SourceSiteId { get; set; }
        public string OrganizationId { get; set; }
        public string OrderId { get; set; }
        public string ProjectName { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string Status { get; set; }
        public string CustomerStatus { get; set; }
        public bool CustomerPublished { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public decimal PriceExVat { get; set; }
        public decimal PriceIncVat { get; set; }
        public List<PortalPurchaseSnapshotLine> PurchaseLines { get; set; }
        public List<PortalProductionAreaSnapshot> ProductionAreas { get; set; }
        public List<PortalProjectDocument> Documents { get; set; }

        public PortalProjectRecord()
        {
            PurchaseLines = new List<PortalPurchaseSnapshotLine>();
            ProductionAreas = new List<PortalProductionAreaSnapshot>();
            Documents = new List<PortalProjectDocument>();
        }
    }

    public sealed class PortalProjectView
    {
        public string ProjectId { get; set; }
        public string SourceSiteId { get; set; }
        public string OrganizationId { get; set; }
        public string OrderId { get; set; }
        public string ProjectName { get; set; }
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public bool CustomerPublished { get; set; }
        public string CreatedAt { get; set; }
        public decimal PriceIncVat { get; set; }
    }

    public sealed class PortalProductionJob
    {
        public string JobId { get; set; }
        public string ProjectId { get; set; }
        public string OrderId { get; set; }
        public string AreaId { get; set; }
        public string AreaLabel { get; set; }
        public string Status { get; set; }
        public int ItemCount { get; set; }
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
        public string BlockedReason { get; set; }
        public string UpdatedAt { get; set; }
        public List<PortalProjectDocument> Documents { get; set; }

        public PortalProductionJob()
        {
            Documents = new List<PortalProjectDocument>();
        }
    }

    public sealed class PortalProjectDocument
    {
        public string DocumentId { get; set; }
        public string FileName { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public bool CustomerVisible { get; set; }
    }

    public sealed class PortalProjectDetail
    {
        public PortalProjectView Project { get; set; }
        public List<PortalPurchaseSnapshotLine> PurchaseLines { get; set; }
        public List<PortalProductionAreaSnapshot> ProductionAreas { get; set; }
        public List<PortalProjectDocument> Documents { get; set; }
        public List<string> OpenData { get; set; }

        public PortalProjectDetail()
        {
            PurchaseLines = new List<PortalPurchaseSnapshotLine>();
            ProductionAreas = new List<PortalProductionAreaSnapshot>();
            Documents = new List<PortalProjectDocument>();
            OpenData = new List<string>();
        }
    }

    public sealed class PortalInventoryItem
    {
        public string InventoryItemId { get; set; }
        public string StableItemId { get; set; }
        public string Description { get; set; }
        public string StockUnit { get; set; }
        public string PurchaseUnit { get; set; }
        public decimal UnitsPerPurchase { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal TargetQuantity { get; set; }
        public string Location { get; set; }
        public bool TrackStock { get; set; }
        public string UpdatedAt { get; set; }

        public decimal AvailableQuantity
        {
            get { return PhysicalQuantity - ReservedQuantity; }
        }
    }

    public sealed class PortalInventoryCandidate
    {
        public string StableItemId { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }

    public sealed class PortalInventoryMovement
    {
        public string MovementId { get; set; }
        public string InventoryItemId { get; set; }
        public string MovementType { get; set; }
        public decimal Quantity { get; set; }
        public string ProjectId { get; set; }
        public string Note { get; set; }
        public string ActorUserId { get; set; }
        public string CreatedAt { get; set; }
    }

    public sealed class PortalInventoryMovementRequest
    {
        public string MovementType { get; set; }
        public decimal Quantity { get; set; }
        public string ProjectId { get; set; }
        public string Note { get; set; }
    }

    public sealed class PortalPurchaseRequirement
    {
        public string StableItemId { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
        public decimal UnitsPerPurchase { get; set; }
        public decimal SuggestedPurchaseUnits { get; set; }
        public string PurchaseUnit { get; set; }
        public string Supplier { get; set; }
        public string SupplierArticleCode { get; set; }
        public decimal PurchaseUnitPrice { get; set; }
        public decimal EstimatedPurchaseTotal { get; set; }
        public string PriceStatus { get; set; }
        public string AvailabilityStatus { get; set; }
        public List<string> ProjectIds { get; set; }

        public PortalPurchaseRequirement()
        {
            ProjectIds = new List<string>();
        }
    }

    public sealed class PortalWorkspaceDashboard
    {
        public int ProjectCount { get; set; }
        public int PublishedProjectCount { get; set; }
        public int WaitingJobCount { get; set; }
        public int BlockedJobCount { get; set; }
        public int PurchaseShortageCount { get; set; }
        public int LowStockCount { get; set; }
        public List<PortalProjectView> RecentProjects { get; set; }
        public List<PortalProductionJob> ActiveJobs { get; set; }

        public PortalWorkspaceDashboard()
        {
            RecentProjects = new List<PortalProjectView>();
            ActiveJobs = new List<PortalProductionJob>();
        }
    }

    public sealed class PortalJobStatusRequest
    {
        public string Status { get; set; }
        public string BlockedReason { get; set; }
    }

    public sealed class PortalProjectPublicationRequest
    {
        public bool Published { get; set; }
    }
}
