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
        public string SlidingDoorMaterialId { get; set; }
        public string ProfileMaterialId { get; set; }
        public string ProfileSawingMode { get; set; }
        public bool IncludeBackPanel { get; set; }
        public bool IncludeTopDrawer { get; set; }
        public bool IncludeDrawerPullCutouts { get; set; }
        public bool IncludeAdjustableShelfHoles { get; set; }
        public int DefaultShelfCount { get; set; }
        public int AdjustableShelfPositionCount { get; set; }
        public string ShelfStartMode { get; set; }
        public double ShelfFrontInsetMm { get; set; }
        public int DefaultDrawerCount { get; set; }
        public string DoorMode { get; set; }
        public int SlidingDoorStartUnit { get; set; }
        public int SlidingDoorEndUnit { get; set; }
        public double SlidingDoorOverlapMm { get; set; }
        public string CustomerName { get; set; }
        public string ProjectName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string Notes { get; set; }
        public bool IncludeLowerShelf { get; set; }
        public bool IncludeMiddleShelf { get; set; }
        public double LowerShelfHeightMm { get; set; }
        public double MiddleShelfHeightMm { get; set; }
        public double MachineBaseWorktopHeightMm { get; set; }
        public string MachineBaseWorktopMaterialId { get; set; }
        public string MachineBaseLowerBeamProfileId { get; set; }
        public string MachineBaseWorktopBeamProfileId { get; set; }
        public double MachineBaseWorktopIntermediateBeamMaxSpacingMm { get; set; }
        public string MachineBaseFrontProtectionMode { get; set; }
        public double MachineBaseControlCabinetWidthMm { get; set; }
        public double MachineBaseControlCabinetDepthMm { get; set; }
        public double MachineBaseControlCabinetHeightMm { get; set; }
        public string MachineBaseControlCabinetPosition { get; set; }
        public int MachineBaseControlCabinetDoorCount { get; set; }
        public string MachineBaseControlCabinetHingeSide { get; set; }
        public int MachineBaseFrontDoorCount { get; set; }
        public string MachineBaseFrontSingleDoorHingeSide { get; set; }
        public double RobotCellIntermediateBeamMaxSpacingMm { get; set; }
        public int CubbyColumnCount { get; set; }
        public int CubbyRowCount { get; set; }
        public double CubbyCellWidthMm { get; set; }
        public double CubbyCellDepthMm { get; set; }
        public double CubbyCellHeightMm { get; set; }
        public double CubbyGridInsetMm { get; set; }
        public double CubbyBackGrooveDepthMm { get; set; }
        public double CubbyCombSlotClearanceMm { get; set; }
        public double WorkbenchCabinetPlinthHeightMm { get; set; }
        public double WorkbenchCabinetPlinthSetbackMm { get; set; }
        public bool WorkbenchCabinetIncludeLeftSidePlinth { get; set; }
        public bool WorkbenchCabinetIncludeRightSidePlinth { get; set; }
        public double WorkbenchCabinetFootInsetMm { get; set; }
        public double WorkbenchCabinetPlinthClipCenterBehindBackFaceMm { get; set; }
        public double WorkbenchCabinetDoorStopWidthMm { get; set; }
        public double WorkbenchCabinetTopDrawerHeightMm { get; set; }
        public double? WorkbenchCabinetFrontPanelCornerRadiusMm { get; set; }
        public bool? ExportIncludeCam { get; set; }
        public bool? ExportIncludeSolidWorks { get; set; }
        public bool? ExportIncludeCustomerPackage { get; set; }
        public bool? ExportIncludeThreeDPrint { get; set; }
        public bool? ExportIncludeControls { get; set; }
        public bool TestFitFirstSheet { get; set; }
        public bool RevisionAfterMilledTestSheetOne { get; set; }
        public bool ShippingBoxIncludeHandles { get; set; }
        public string ShippingBoxJointMode { get; set; }
        public bool? EnableWoodScrewCountersinks { get; set; }
        public bool? EnableOutsideEdgeChamfer { get; set; }
        // Alleen behouden voor oude opgeslagen aanvragen; nieuwe portal gebruikt twee losse opties.
        public bool? EnableCountersinkAndEdgeChamfer { get; set; }
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
        public string Shape { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double SizeXmm { get; set; }
        public double SizeYmm { get; set; }
        public double SizeZmm { get; set; }
        public double BodyDiameterMm { get; set; }
        public double FlangeDiameterMm { get; set; }
        public double FlangeThicknessMm { get; set; }
        public double FlangeRecessDepthMm { get; set; }
        public double InsertionLengthMm { get; set; }
        public double BallDiameterMm { get; set; }
        public double WorkingHeightMm { get; set; }
        public double CornerRadiusMm { get; set; }
        public List<PortalAssemblyHole> Holes { get; private set; }
        public List<PortalAssemblyPocket> Pockets { get; private set; }
        public List<PortalAssemblyOutlinePoint> Outline { get; private set; }

        public PortalAssemblyPart()
        {
            Holes = new List<PortalAssemblyHole>();
            Pockets = new List<PortalAssemblyPocket>();
            Outline = new List<PortalAssemblyOutlinePoint>();
        }
    }

    public sealed class PortalAssemblyOutlinePoint
    {
        public double Umm { get; set; }
        public double Vmm { get; set; }
    }

    public sealed class PortalAssemblyHole
    {
        public string Name { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public string Plane { get; set; }
        public bool IsThroughCutout { get; set; }
        public bool Countersunk { get; set; }
        public double CountersinkDiameterMm { get; set; }
        public double CountersinkDepthMm { get; set; }
    }

    public sealed class PortalAssemblyPocket
    {
        public string Name { get; set; }
        public string Shape { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double SizeXmm { get; set; }
        public double SizeYmm { get; set; }
        public double SizeZmm { get; set; }
        public string Plane { get; set; }
        public bool IsThroughCutout { get; set; }
        public double MinorDiameterMm { get; set; }
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

    public sealed class PortalSolidWorksExportResponse
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public string OutputFolder { get; set; }
        public string AssemblyPath { get; set; }
        public string ControlModelPath { get; set; }
        public string CustomerModelPath { get; set; }
        public string CustomerHtmlPath { get; set; }
        public string CustomerPowerPointPath { get; set; }
        public string CustomerAppendixPdfPath { get; set; }
        public string CustomerDrawingPath { get; set; }
        public string CustomerDrawingPdfPath { get; set; }
        public string MacroPath { get; set; }
        public int PartCount { get; set; }
        public int FileCount { get; set; }
        public int PlacementCount { get; set; }
    }

    public sealed class SolidWorksWorkerResult
    {
        public int ContractVersion { get; set; }
        public bool Ok { get; set; }
        public string AssemblyPath { get; set; }
        public string Error { get; set; }
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
