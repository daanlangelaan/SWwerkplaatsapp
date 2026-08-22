using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class LexRevolutionProductBuilder : IProductBuilder
    {
        public string ProductId
        {
            get { return "werktafel_lex_revolution"; }
        }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Workstation ontwikkelvariant",
                    Category = "werkbank",
                    DefaultWidthMm = ProductDefaults.LexWorkbenchWidthMm,
                    DefaultDepthMm = ProductDefaults.LexWorkbenchDepthMm,
                    DefaultHeightMm = ProductDefaults.LexWorkbenchHeightMm,
                    DefaultUnitCount = 1,
                    DefaultShelfCount = 0,
                    DefaultDrawerCount = 0,
                    DefaultShelfStartMode = "bottom",
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
            return new LexWorkbenchEngine().Build(factory.BuildLexRevolutionWorkbench(request));
        }
    }
}
