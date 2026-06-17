using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CatalogData
    {
        public Material[] Sheets { get; set; }
        public Material[] Profiles { get; set; }
        public string[] Statuses { get; set; }
    }

    public sealed class CatalogApplicationService
    {
        public CatalogData GetCatalog()
        {
            return new CatalogData
            {
                Sheets = LibraryCatalog.Sheets(),
                Profiles = LibraryCatalog.Profiles(),
                Statuses = OrderWorkflowStatus.All()
            };
        }
    }
}
