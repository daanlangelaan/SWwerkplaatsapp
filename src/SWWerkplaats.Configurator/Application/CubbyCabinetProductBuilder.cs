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
                    DefaultWidthMm = OuterWidth(
                        ProductDefaults.CubbyCabinetColumnCount,
                        ProductDefaults.CubbyCabinetCellWidthMm,
                        ProductDefaults.DefaultSheetThicknessMm),
                    DefaultDepthMm = ProductDefaults.CubbyCabinetCellDepthMm,
                    DefaultHeightMm = OuterHeight(
                        ProductDefaults.CubbyCabinetRowCount,
                        ProductDefaults.CubbyCabinetCellHeightMm,
                        ProductDefaults.DefaultSheetThicknessMm),
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
            var columnCount = Count(request.CubbyColumnCount, ProductDefaults.CubbyCabinetColumnCount);
            var rowCount = Count(request.CubbyRowCount, ProductDefaults.CubbyCabinetRowCount);
            var cellWidth = Dimension(request.CubbyCellWidthMm, ProductDefaults.CubbyCabinetCellWidthMm);
            var cellDepth = Dimension(request.CubbyCellDepthMm, ProductDefaults.CubbyCabinetCellDepthMm);
            var cellHeight = Dimension(request.CubbyCellHeightMm, ProductDefaults.CubbyCabinetCellHeightMm);
            var materialThickness = ProductDefaults.DefaultSheetThicknessMm;
            var width = OuterWidth(columnCount, cellWidth, materialThickness);
            var height = OuterHeight(rowCount, cellHeight, materialThickness);
            var depth = cellDepth;

            return new CubbyCabinetConfig
            {
                ProjectName = "Vakjeskast_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                CellWidthMm = cellWidth,
                CellDepthMm = cellDepth,
                CellHeightMm = cellHeight,
                ColumnCount = columnCount,
                RowCount = rowCount,
                GridInsetMm = Dimension(request.CubbyGridInsetMm, ProductDefaults.CubbyCabinetGridInsetMm),
                CombSlotClearanceMm = Dimension(request.CubbyCombSlotClearanceMm, ProductDefaults.CubbyCabinetCombSlotClearanceMm),
                BackGrooveDepthMm = Dimension(request.CubbyBackGrooveDepthMm, ProductDefaults.CubbyCabinetBackGrooveDepthMm),
                BackGrooveClearanceMm = ProductDefaults.CubbyCabinetBackGrooveClearanceMm,
                BackFastenerMaxSpacingMm = ProductDefaults.CubbyCabinetBackFastenerMaxSpacingMm,
                DividerBackFastenerMaxSpacingMm = ProductDefaults.CubbyCabinetDividerBackFastenerMaxSpacingMm,
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

        private static double OuterWidth(int columns, double cellWidth, double materialThickness)
        {
            return columns * cellWidth + (columns + 1) * materialThickness;
        }

        private static double OuterHeight(int rows, double cellHeight, double materialThickness)
        {
            return rows * cellHeight + (rows + 1) * materialThickness;
        }
    }
}
