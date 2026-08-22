namespace SWWerkplaats.Configurator.Domain
{
    /// <summary>
    /// Productgebonden keuze van bevestigerfamilies. Een afgeleid product mag een
    /// waarde leeg laten; de resolver neemt die dan over van BaseProductId.
    /// </summary>
    public sealed class ProductFastenerStandard
    {
        public string ProductId { get; set; }
        public string BaseProductId { get; set; }
        public string WoodToWoodFastenerId { get; set; }
        public string StructuralFastenerId { get; set; }
    }
}
