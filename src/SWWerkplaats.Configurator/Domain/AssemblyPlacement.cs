namespace SWWerkplaats.Configurator.Domain
{
    public enum AssemblyComponentKind
    {
        Profile,
        Sheet,
        Purchased
    }

    public enum AssemblyOrientation
    {
        Default,
        SheetHorizontal,
        SheetVerticalX,
        SheetVerticalZ
    }

    public sealed class AssemblyPlacement
    {
        public string MemberId { get; set; }
        public string TraceId { get; set; }
        public ProfileStickerPlacement Sticker { get; set; }
        public AssemblyComponentKind Kind { get; set; }
        public string PartName { get; set; }
        public string ComponentId { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public AssemblyOrientation Orientation { get; set; }
        public string VisualKind { get; set; }
        public string Shape { get; set; }
        public double RotationXDeg { get; set; }
        public double RotationYDeg { get; set; }
        public double RotationZDeg { get; set; }
    }
}
