namespace SWWerkplaats.Configurator.Domain
{
    public sealed class HardwareItem
    {
        public string Name { get; set; }
        public string ArticleNumber { get; set; }
        public string PricingCategory { get; set; }
        public string PricingKey { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public string Note { get; set; }
        public string ModelStatus { get; set; }
        public string BomStatus { get; set; }
    }
}
