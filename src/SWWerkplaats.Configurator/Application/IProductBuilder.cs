using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public interface IProductBuilder
    {
        string ProductId { get; }
        ProductCatalogItem CatalogItem { get; }
        WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request);
    }

    public sealed class CabinetProductBuilder : IProductBuilder
    {
        public string ProductId
        {
            get { return "cabinet"; }
        }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
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
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            return new CabinetEngine().Build(factory.BuildCabinet(request));
        }
    }

    public sealed class WorkbenchProductBuilder : IProductBuilder
    {
        public string ProductId
        {
            get { return "werktafel"; }
        }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
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
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            return new WorkbenchEngine().Build(factory.BuildWorkbench(request));
        }
    }
}
