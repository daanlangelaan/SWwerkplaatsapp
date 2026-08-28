using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class HeightAdjustableWorkbenchProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "hoogteverstelbare_werktafel"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Hoogteverstelbare werktafel",
                    Category = "werkbank",
                    SupportsProfiles = true,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = false,
                    SupportsAdjustableShelfHoles = false
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            return new HeightAdjustableWorkbenchEngine().Build(factory.BuildHeightAdjustableWorkbench(request));
        }
    }
}
