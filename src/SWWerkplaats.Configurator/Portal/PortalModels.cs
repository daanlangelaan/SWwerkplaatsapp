using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalQuoteRequest
    {
        public string Product { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public int Quantity { get; set; }
        public int UnitCount { get; set; }
        public string SheetMaterialId { get; set; }
        public string DrawerMaterialId { get; set; }
        public string BackMaterialId { get; set; }
        public string ProfileMaterialId { get; set; }
        public bool IncludeBackPanel { get; set; }
        public bool IncludeTopDrawer { get; set; }
        public bool IncludeAdjustableShelfHoles { get; set; }
        public int DefaultShelfCount { get; set; }
        public string ShelfStartMode { get; set; }
        public int DefaultDrawerCount { get; set; }
        public string DoorMode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string Notes { get; set; }
        public bool IncludeLowerShelf { get; set; }
        public bool IncludeMiddleShelf { get; set; }
        public double LowerShelfHeightMm { get; set; }
        public double MiddleShelfHeightMm { get; set; }
    }

    public sealed class PortalQuoteResponse
    {
        public string QuoteId { get; set; }
        public string ProductName { get; set; }
        public string Summary { get; set; }
        public decimal PriceExVat { get; set; }
        public decimal Material { get; set; }
        public decimal Hardware { get; set; }
        public decimal Machine { get; set; }
        public decimal Labour { get; set; }
        public decimal Margin { get; set; }
        public decimal Vat { get; set; }
        public decimal PriceIncVat { get; set; }
        public string LeadTime { get; set; }
        public int SheetPartCount { get; set; }
        public int ProfilePartCount { get; set; }
        public string PreviewSvg { get; set; }
        public string NestingSvg { get; set; }
        public List<PortalAssemblyPart> Assembly3D { get; private set; }
        public List<string> Files { get; private set; }

        public PortalQuoteResponse()
        {
            Assembly3D = new List<PortalAssemblyPart>();
            Files = new List<string>();
        }
    }

    public sealed class PortalAssemblyPart
    {
        public string Name { get; set; }
        public string Kind { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double SizeXmm { get; set; }
        public double SizeYmm { get; set; }
        public double SizeZmm { get; set; }
        public List<PortalAssemblyHole> Holes { get; private set; }
        public List<PortalAssemblyPocket> Pockets { get; private set; }

        public PortalAssemblyPart()
        {
            Holes = new List<PortalAssemblyHole>();
            Pockets = new List<PortalAssemblyPocket>();
        }
    }

    public sealed class PortalAssemblyHole
    {
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public string Plane { get; set; }
    }

    public sealed class PortalAssemblyPocket
    {
        public string Name { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double SizeXmm { get; set; }
        public double SizeYmm { get; set; }
        public double SizeZmm { get; set; }
        public string Plane { get; set; }
    }

    public sealed class PortalOrderRecord
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal PriceExVat { get; set; }
        public decimal PriceIncVat { get; set; }
        public string OutputFolder { get; set; }
        public string QueueFolder { get; set; }
        public List<string> Files { get; private set; }

        public PortalOrderRecord()
        {
            Files = new List<string>();
        }
    }

    public sealed class PortalOrderResponse
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public PortalOrderRecord Order { get; set; }
    }

    public sealed class PortalOrderStatusRequest
    {
        public string Status { get; set; }
        public string Role { get; set; }
    }

    public static class PortalOrderStatus
    {
        public const string Nieuw = Domain.OrderWorkflowStatus.Nieuw;
        public const string TeControleren = Domain.OrderWorkflowStatus.TeControleren;
        public const string Goedgekeurd = Domain.OrderWorkflowStatus.Goedgekeurd;
        public const string InFreeswachtrij = Domain.OrderWorkflowStatus.InFreeswachtrij;
        public const string InProductie = Domain.OrderWorkflowStatus.InProductie;
        public const string Gereed = Domain.OrderWorkflowStatus.Gereed;
    }
}
