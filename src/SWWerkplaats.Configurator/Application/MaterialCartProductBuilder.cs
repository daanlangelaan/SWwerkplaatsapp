using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class MaterialCartProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "materiaalwagen"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Modulaire materiaal- en gereedschapswagen",
                    Category = "profielconstructie",
                    DefaultWidthMm = ProductDefaults.MaterialCartWidthMm,
                    DefaultDepthMm = ProductDefaults.MaterialCartDepthMm,
                    DefaultHeightMm = ProductDefaults.MaterialCartTopShelfHeightMm,
                    DefaultUnitCount = 1,
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
            return new MaterialCartEngine().Build(factory.BuildMaterialCart(request));
        }
    }
}
