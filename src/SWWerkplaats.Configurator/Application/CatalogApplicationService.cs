using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CatalogData
    {
        public Material[] Sheets { get; set; }
        public Material[] Profiles { get; set; }
        public RailTemplate[] Rails { get; set; }
        public ShelfSupportTemplate[] ShelfSupports { get; set; }
        public string[] Statuses { get; set; }
        public ProductCatalogItem[] Products { get; set; }
    }

    public sealed class CatalogApplicationService
    {
        private readonly ICatalogRepository catalog;

        public CatalogApplicationService()
            : this(new LibraryCatalogRepository())
        {
        }

        public CatalogApplicationService(ICatalogRepository catalog)
        {
            this.catalog = catalog ?? new LibraryCatalogRepository();
        }

        public CatalogData GetCatalog()
        {
            return new CatalogData
            {
                Sheets = catalog.Sheets(),
                Profiles = catalog.Profiles(),
                Rails = catalog.DrawerRails(),
                ShelfSupports = catalog.ShelfSupports(),
                Statuses = OrderWorkflowStatus.All(),
                Products = new ProductCatalogApplicationService().ListProducts()
            };
        }
    }
}
