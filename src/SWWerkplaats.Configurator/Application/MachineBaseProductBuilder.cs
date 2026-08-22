using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class MachineBaseProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "machinebasis"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Parametrische machinebasis",
                    Category = "machineframe",
                    DefaultWidthMm = ProductDefaults.MachineBaseWidthMm,
                    DefaultDepthMm = ProductDefaults.MachineBaseDepthMm,
                    DefaultHeightMm = ProductDefaults.MachineBaseHeightMm,
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
            return new MachineBaseEngine().Build(factory.BuildMachineBase(request));
        }
    }
}
