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
        TappedProfileEnd
    }

    public sealed class ProfilePart
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public double LengthMm { get; set; }
        public int Quantity { get; set; }
        public string OrientationNote { get; set; }
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
        public bool HasToeKickNotch { get; set; }
        public double ToeKickDepthMm { get; set; }
        public double ToeKickHeightMm { get; set; }
        public List<SheetHole> Holes { get; private set; }

        public SheetPart()
        {
            Holes = new List<SheetHole>();
        }
    }

    public sealed class SheetHole
    {
        public string Name { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double DiameterMm { get; set; }
        public bool Countersunk { get; set; }
        public double CountersinkDiameterMm { get; set; }
        public double CountersinkDepthMm { get; set; }
        public SheetHoleSupportKind SupportKind { get; set; }
    }
}
