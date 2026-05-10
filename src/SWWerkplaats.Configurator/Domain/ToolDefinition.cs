namespace SWWerkplaats.Configurator.Domain
{
    public enum ToolKind
    {
        EndMill,
        Drill
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

        public double RadiusMm
        {
            get { return DiameterMm / 2.0; }
        }
    }
}
