namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductCatalogItem
    {
        public string Product { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double DefaultWidthMm { get; set; }
        public double DefaultDepthMm { get; set; }
        public double DefaultHeightMm { get; set; }
        public int DefaultUnitCount { get; set; }
        public int DefaultShelfCount { get; set; }
        public int DefaultDrawerCount { get; set; }
        public string DefaultShelfStartMode { get; set; }
        public bool SupportsProfiles { get; set; }
        public bool SupportsDrawers { get; set; }
        public bool SupportsDoors { get; set; }
        public bool SupportsBackPanel { get; set; }
        public bool SupportsAdjustableShelfHoles { get; set; }
    }

    public sealed class ProductCatalogApplicationService
    {
        private readonly ProductRegistry products;

        public ProductCatalogApplicationService()
            : this(new ProductRegistry())
        {
        }

        public ProductCatalogApplicationService(ProductRegistry products)
        {
            this.products = products ?? new ProductRegistry();
        }

        public ProductCatalogItem[] ListProducts()
        {
            return products.CatalogItems();
        }
    }
}
