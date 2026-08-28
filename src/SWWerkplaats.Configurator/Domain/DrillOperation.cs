namespace SWWerkplaats.Configurator.Domain
{
    public sealed class DrillOperation
    {
        public string FaceId { get; set; }
        public int SlotIndex { get; set; }
        public double SlotAxisOffsetMm { get; set; }
        public string Side { get; set; }
        public double PositionFromEndAMm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public bool ThroughHole { get; set; }
        public string Note { get; set; }
    }
}
