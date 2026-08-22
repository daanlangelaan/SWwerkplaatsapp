using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public enum SheetCutSide
    {
        Outside,
        Inside
    }

    public enum SheetHoleSupportKind
    {
        ProfileNut,
        TappedProfileEnd,
        PanelScrew,
        HingeScrew,
        HingePlate,
        HingeCup,
        AdjustableFoot,
        PlinthClip,
        ShelfSupport,
        DrawerRail,
        MachiningCutout
    }

    public sealed class ProfilePart
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public double LengthMm { get; set; }
        public int Quantity { get; set; }
        public string OrientationNote { get; set; }
        public string BomStatus { get; set; }
        public List<DrillOperation> Drills { get; private set; }

        public ProfilePart()
        {
            Drills = new List<DrillOperation>();
        }
    }

    public sealed class SheetPart
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double CenterHeightMm { get; set; }
        public int Quantity { get; set; }
        public bool UseTabs { get; set; }
        public bool HasCornerNotches { get; set; }
        public double CornerNotchSizeMm { get; set; }
        public double CornerNotchLengthMm { get; set; }
        public double CornerNotchWidthMm { get; set; }
        public double CornerRadiusMm { get; set; }
        public bool HasToeKickNotch { get; set; }
        public double ToeKickDepthMm { get; set; }
        public double ToeKickHeightMm { get; set; }
        public bool MirrorInNestingX { get; set; }
        public double CustomContourCornerRadiusMm { get; set; }
        public string BomStatus { get; set; }
        public List<SheetHole> Holes { get; private set; }
        public List<SheetPocket> Pockets { get; private set; }
        public List<SheetContourPoint> CustomContour { get; private set; }

        public SheetPart()
        {
            Holes = new List<SheetHole>();
            Pockets = new List<SheetPocket>();
            CustomContour = new List<SheetContourPoint>();
        }
    }

    public sealed class SheetContourPoint
    {
        public SheetContourPoint() { }

        public SheetContourPoint(double xMm, double yMm)
        {
            Xmm = xMm;
            Ymm = yMm;
        }

        public double Xmm { get; set; }
        public double Ymm { get; set; }
    }

    public sealed class SheetHole
    {
        public SheetHole()
        {
            Face = OperationFace.CenterPlane;
            DepthMode = OperationDepthMode.Through;
        }

        public string Name { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public OperationFace Face { get; set; }
        public OperationDepthMode DepthMode { get; set; }
        public bool Countersunk { get; set; }
        public double CountersinkDiameterMm { get; set; }
        public double CountersinkDepthMm { get; set; }
        public SheetHoleSupportKind SupportKind { get; set; }
    }

    public sealed class SheetPocket
    {
        public SheetPocket()
        {
            Face = OperationFace.CenterPlane;
            DepthMode = OperationDepthMode.PocketFromFace;
            Shape = "rectangle";
        }

        public string Name { get; set; }
        public string Shape { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public OperationFace Face { get; set; }
        public OperationDepthMode DepthMode { get; set; }
        public string Note { get; set; }
    }
}
