namespace SWWerkplaats.Configurator.Domain
{
    public enum MaterialKind
    {
        Profile,
        Sheet,
        Connector,
        Hardware
    }

    public sealed class Material
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public MaterialKind Kind { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double ThicknessMm { get; set; }
        public double StockLengthMm { get; set; }
        public double SheetLengthMm { get; set; }
        public double SheetWidthMm { get; set; }
    }
}
