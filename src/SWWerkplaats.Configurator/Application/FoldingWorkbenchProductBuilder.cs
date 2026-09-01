using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class FoldingWorkbenchProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "opvouwbare_werktafel"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Opvouwbare werktafel",
                    Category = "werkbank",
                    DefaultUnitCount = 1,
                    DefaultShelfStartMode = "none",
                    SupportsProfiles = false,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = false,
                    SupportsAdjustableShelfHoles = false
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new FoldingWorkbenchEngine().Build(factory.BuildFoldingWorkbench(request));
        }
    }
}
