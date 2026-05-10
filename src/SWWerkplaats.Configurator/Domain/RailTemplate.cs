namespace SWWerkplaats.Configurator.Domain
{
    public sealed class RailTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double LengthMm { get; set; }
        public int HoleCount { get; set; }
        public double FirstHoleOffsetMm { get; set; }
        public double HoleSpacingMm { get; set; }
        public double VerticalOffsetMm { get; set; }
        public double HoleDiameterMm { get; set; }
        public string FastenerName { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
