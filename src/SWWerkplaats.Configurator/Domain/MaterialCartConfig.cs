namespace SWWerkplaats.Configurator.Domain
{
    public sealed class MaterialCartConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double TopShelfHeightMm { get; set; }
        public int ShelfCount { get; set; }
        public string HandleSide { get; set; }
        public string SteeringMode { get; set; }
        public Material FrameProfile { get; set; }
        public Material ShelfMaterial { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
    }
}
