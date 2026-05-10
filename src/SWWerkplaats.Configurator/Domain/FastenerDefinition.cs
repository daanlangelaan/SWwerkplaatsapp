namespace SWWerkplaats.Configurator.Domain
{
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
        public FastenerHeadKind HeadKind { get; set; }
        public double HeadDiameterMm { get; set; }
        public double HeadHeightMm { get; set; }
        public double HeadClearanceMm { get; set; }

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
