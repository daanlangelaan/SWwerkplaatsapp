namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ShelfSupportTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double ThicknessMm { get; set; }
        public double HeightMm { get; set; }
        public double HoleDiameterMm { get; set; }
        public double HoleSpacingMm { get; set; }
        public double FrontInsetMm { get; set; }
        public double BackInsetMm { get; set; }
        public double FirstHoleHeightMm { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
