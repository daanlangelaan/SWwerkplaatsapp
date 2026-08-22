namespace SWWerkplaats.Configurator.Domain
{
    public enum ToolKind
    {
        EndMill,
        Drill,
        VBit
    }

    public sealed class ToolDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ToolKind Kind { get; set; }
        public double DiameterMm { get; set; }
        public double FeedRateMmMin { get; set; }
        public double PlungeRateMmMin { get; set; }
        public double SpindleRpm { get; set; }
        public double PassDepthMm { get; set; }
        public int FluteCount { get; set; }
        public string Rotation { get; set; }
        public double IncludedAngleDeg { get; set; }
        public double TipDiameterMm { get; set; }
        public double ShankDiameterMm { get; set; }
        public double ConeLengthMm { get; set; }
        public double CylindricalCutLengthMm { get; set; }
        public double MaximumCutDepthMm { get; set; }

        public double RadiusMm
        {
            get { return DiameterMm / 2.0; }
        }
    }
}
