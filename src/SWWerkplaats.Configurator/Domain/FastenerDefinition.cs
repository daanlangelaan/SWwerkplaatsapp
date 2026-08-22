namespace SWWerkplaats.Configurator.Domain
{
    public enum FastenerUsageKind
    {
        StructuralBolt,
        WoodScrew,
        ComponentScrew
    }

    public enum FastenerHeadKind
    {
        SocketHeadCap,
        Countersunk
    }

    public sealed class FastenerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Standard { get; set; }
        public double NominalDiameterMm { get; set; }
        public double ClearanceHoleDiameterMm { get; set; }
        public double ReceivingPilotHoleDiameterMm { get; set; }
        public FastenerHeadKind HeadKind { get; set; }
        public double HeadDiameterMm { get; set; }
        public double HeadHeightMm { get; set; }
        public double HeadClearanceMm { get; set; }
        public FastenerUsageKind UsageKind { get; set; }
        public double LengthMm { get; set; }
        public double[] AvailableLengthsMm { get; set; }
        public double MinimumEdgePenetrationMm { get; set; }
        public double MinimumTipClearanceMm { get; set; }

        public double CounterboreDiameterMm
        {
            get { return HeadDiameterMm + HeadClearanceMm; }
        }

        public double CounterboreDepthMm
        {
            get { return HeadHeightMm; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
