namespace SWWerkplaats.Configurator.Domain
{
    public enum ProfileOperationKind
    {
        SawCut,
        Drill,
        Tap
    }

    public sealed class ProfileOperation
    {
        public string ProfileId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public Material Material { get; set; }
        public double ProfileLengthMm { get; set; }
        public int Sequence { get; set; }
        public ProfileOperationKind Kind { get; set; }
        public string Side { get; set; }
        public double PositionFromEndAMm { get; set; }
        public double DiameterMm { get; set; }
        public bool ThroughHole { get; set; }
        public double SawAngleDeg { get; set; }
        public string WorkOrigin { get; set; }
        public string MachineHint { get; set; }
        public string Note { get; set; }
    }
}
