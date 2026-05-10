namespace SWWerkplaats.Configurator.Domain
{
    public enum AssemblyComponentKind
    {
        Profile,
        Sheet
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
        public AssemblyComponentKind Kind { get; set; }
        public string PartName { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public AssemblyOrientation Orientation { get; set; }
    }
}
