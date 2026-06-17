using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CubbyCabinetProductBuilder : IProductBuilder
    {
        public string ProductId
        {
            get { return "vakjeskast"; }
        }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Vakjeskast",
                    Category = "kast",
                    DefaultWidthMm = ProductDefaults.CubbyCabinetWidthMm,
                    DefaultDepthMm = ProductDefaults.CubbyCabinetDepthMm,
                    DefaultHeightMm = ProductDefaults.CubbyCabinetHeightMm,
                    DefaultUnitCount = ProductDefaults.CubbyCabinetColumnCount,
                    DefaultShelfCount = ProductDefaults.CubbyCabinetRowCount,
                    DefaultDrawerCount = 0,
                    DefaultShelfStartMode = "grid",
                    SupportsProfiles = false,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = true,
                    SupportsAdjustableShelfHoles = false
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            throw new NotSupportedException("Vakjeskast is voorbereid als productfamilie, maar nog niet actief. Vul eerst de grid- en constructieparameters in.");
        }

        public CubbyCabinetConfig BuildConfig(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            request = request ?? new PortalQuoteRequest();

            return new CubbyCabinetConfig
            {
                ProjectName = "Vakjeskast_" + Dimension(request.WidthMm, ProductDefaults.CubbyCabinetWidthMm).ToString("0") + "x" + Dimension(request.DepthMm, ProductDefaults.CubbyCabinetDepthMm).ToString("0") + "x" + Dimension(request.HeightMm, ProductDefaults.CubbyCabinetHeightMm).ToString("0"),
                WidthMm = Dimension(request.WidthMm, ProductDefaults.CubbyCabinetWidthMm),
                DepthMm = Dimension(request.DepthMm, ProductDefaults.CubbyCabinetDepthMm),
                HeightMm = Dimension(request.HeightMm, ProductDefaults.CubbyCabinetHeightMm),
                ColumnCount = Count(request.CubbyColumnCount, ProductDefaults.CubbyCabinetColumnCount),
                RowCount = Count(request.CubbyRowCount, ProductDefaults.CubbyCabinetRowCount),
                PlinthHeightMm = ProductDefaults.CubbyCabinetPlinthHeightMm,
                PlinthDepthMm = ProductDefaults.CubbyCabinetPlinthDepthMm,
                IncludeBackPanel = request.IncludeBackPanel,
                IncludeAdjustableShelfHoles = false,
                AutoTabs = true,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5,
                ShelfClearanceMm = ProductDefaults.ShelfClearanceMm,
                DoorGapMm = ProductDefaults.DoorGapMm
            };
        }

        private static double Dimension(double value, double fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static int Count(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }
    }
}
