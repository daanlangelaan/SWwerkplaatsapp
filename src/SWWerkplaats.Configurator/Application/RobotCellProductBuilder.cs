using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class RobotCellProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "robotcel"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Robot cel",
                    Category = "profielconstructie",
                    DefaultWidthMm = ProductDefaults.RobotCellWidthMm,
                    DefaultDepthMm = ProductDefaults.RobotCellDepthMm,
                    DefaultHeightMm = ProductDefaults.RobotCellWorktopHeightMm,
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
            return new RobotCellEngine().Build(factory.BuildRobotCell(request));
        }
    }
}
