using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class WorkbenchCabinetProductBuilder : IProductBuilder
    {
        public string ProductId
        {
            get { return "werkbankkast"; }
        }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Werkbank met kastonderbouw",
                    Category = "werkbank",
                    DefaultWidthMm = ProductDefaults.WorkbenchCabinetWidthMm,
                    DefaultDepthMm = ProductDefaults.WorkbenchCabinetDepthMm,
                    DefaultHeightMm = ProductDefaults.WorkbenchCabinetHeightMm,
                    DefaultUnitCount = ProductDefaults.WorkbenchCabinetUnitCount,
                    DefaultShelfCount = ProductDefaults.WorkbenchCabinetDefaultShelfCount,
                    DefaultDrawerCount = 0,
                    DefaultShelfStartMode = "bottom",
                    SupportsProfiles = false,
                    SupportsDrawers = true,
                    SupportsDoors = true,
                    SupportsBackPanel = true,
                    SupportsAdjustableShelfHoles = true
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new WorkbenchCabinetEngine().Build(factory.BuildWorkbenchCabinet(request));
        }
    }
}
