namespace SWWerkplaats.Configurator.Domain
{
    public sealed class DrillOperation
    {
        public string Side { get; set; }
        public double PositionFromEndAMm { get; set; }
        public double DiameterMm { get; set; }
        public bool ThroughHole { get; set; }
        public string Note { get; set; }
    }
}
