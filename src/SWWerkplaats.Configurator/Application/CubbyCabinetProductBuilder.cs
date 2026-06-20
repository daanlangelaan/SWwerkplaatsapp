using System;
using SWWerkplaats.Configurator.Drawing.Products.CubbyCabinet;
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
                    DefaultWidthMm = CubbyCabinetDrawingRules.OuterWidth(
                        ProductDefaults.CubbyCabinetColumnCount,
                        ProductDefaults.CubbyCabinetCellWidthMm,
                        ProductDefaults.DefaultSheetThicknessMm),
                    DefaultDepthMm = ProductDefaults.CubbyCabinetCellDepthMm,
                    DefaultHeightMm = CubbyCabinetDrawingRules.OuterHeight(
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
            return new CubbyCabinetEngine().Build(BuildConfig(factory, request));
        }

        public CubbyCabinetConfig BuildConfig(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            request = request ?? new PortalQuoteRequest();
            var materialSource = factory.BuildCabinet(request);
            var carcass = materialSource.CarcassMaterial;
            var back = materialSource.BackMaterial ?? carcass;
            var materialThickness = carcass == null || carcass.ThicknessMm <= 0 ? ProductDefaults.DefaultSheetThicknessMm : carcass.ThicknessMm;
            var columnCount = Count(request.CubbyColumnCount, Count(request.UnitCount, ProductDefaults.CubbyCabinetColumnCount));
            var rowCount = Count(request.CubbyRowCount, Count(request.DefaultShelfCount, ProductDefaults.CubbyCabinetRowCount));
            var requestedOuterWidth = Dimension(request.WidthMm, 0);
            var requestedOuterHeight = Dimension(request.HeightMm, 0);
            var cellWidth = Dimension(
                request.CubbyCellWidthMm,
                requestedOuterWidth > 0 ? CubbyCabinetDrawingRules.CellWidthFromOuter(columnCount, requestedOuterWidth, materialThickness) : ProductDefaults.CubbyCabinetCellWidthMm);
            var cellDepth = Dimension(request.CubbyCellDepthMm, Dimension(request.DepthMm, ProductDefaults.CubbyCabinetCellDepthMm));
            var cellHeight = Dimension(
                request.CubbyCellHeightMm,
                requestedOuterHeight > 0 ? CubbyCabinetDrawingRules.CellHeightFromOuter(rowCount, requestedOuterHeight, materialThickness) : ProductDefaults.CubbyCabinetCellHeightMm);
            var width = CubbyCabinetDrawingRules.OuterWidth(columnCount, cellWidth, materialThickness);
            var height = CubbyCabinetDrawingRules.OuterHeight(rowCount, cellHeight, materialThickness);
            var depth = CubbyCabinetDrawingRules.OuterDepth(cellDepth, materialThickness);

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
                CarcassMaterial = carcass,
                BackMaterial = back,
                SheetFastener = materialSource.SheetFastener,
                ShelfSupport = materialSource.ShelfSupport,
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
