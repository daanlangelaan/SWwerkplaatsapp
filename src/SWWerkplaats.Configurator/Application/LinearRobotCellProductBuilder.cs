using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class LinearRobotCellProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "lineaire_robotcel"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                var master = LinearRobotCellMasterDataSettings.LoadRequired();
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Lineaire robotcel",
                    Category = "profielconstructie",
                    DefaultWidthMm = master.DefaultLengthMm,
                    DefaultDepthMm = master.DefaultWorktopDepthMm,
                    DefaultHeightMm = master.DefaultWorktopHeightMm,
                    DefaultUnitCount = 1,
                    SupportsProfiles = true
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            return new LinearRobotCellEngine().Build(factory.BuildLinearRobotCell(request));
        }
    }
}
