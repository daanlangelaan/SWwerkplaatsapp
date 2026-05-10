namespace SWWerkplaats.Configurator.Domain
{
    public sealed class CutListItem
    {
        public string MaterialName { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public double LengthMm { get; set; }
        public double SawAngleDeg { get; set; }
        public string Note { get; set; }
    }
}
