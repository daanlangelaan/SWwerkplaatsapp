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
        public ProductCatalogItem[] ListProducts()
        {
            return new[]
            {
                new ProductCatalogItem
                {
                    Product = "cabinet",
                    Name = "Cabinet / kast",
                    Category = "kast",
                    DefaultWidthMm = ProductDefaults.CabinetWidthMm,
                    DefaultDepthMm = ProductDefaults.CabinetDepthMm,
                    DefaultHeightMm = ProductDefaults.CabinetHeightMm,
                    DefaultUnitCount = ProductDefaults.CabinetUnitCount,
                    DefaultShelfCount = ProductDefaults.CabinetDefaultShelfCount,
                    DefaultDrawerCount = ProductDefaults.CabinetDefaultDrawerCount,
                    DefaultShelfStartMode = ProductDefaults.CabinetDefaultShelfStartMode,
                    SupportsProfiles = false,
                    SupportsDrawers = true,
                    SupportsDoors = true,
                    SupportsBackPanel = true,
                    SupportsAdjustableShelfHoles = true
                },
                new ProductCatalogItem
                {
                    Product = "werktafel",
                    Name = "Werktafel",
                    Category = "werkbank",
                    DefaultWidthMm = ProductDefaults.WorkbenchWidthMm,
                    DefaultDepthMm = ProductDefaults.WorkbenchDepthMm,
                    DefaultHeightMm = ProductDefaults.WorkbenchHeightMm,
                    DefaultUnitCount = ProductDefaults.WorkbenchUnitCount,
                    DefaultShelfCount = ProductDefaults.WorkbenchDefaultShelfCount,
                    DefaultDrawerCount = ProductDefaults.WorkbenchDefaultDrawerCount,
                    DefaultShelfStartMode = "bottom",
                    SupportsProfiles = true,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = false,
                    SupportsAdjustableShelfHoles = false
                }
            };
        }
    }
}
