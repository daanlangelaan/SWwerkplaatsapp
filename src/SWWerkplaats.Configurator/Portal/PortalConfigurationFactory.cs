using System;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalConfigurationFactory
    {
        private readonly ICatalogRepository catalog;

        public PortalConfigurationFactory()
            : this(new LibraryCatalogRepository())
        {
        }

        public PortalConfigurationFactory(ICatalogRepository catalog)
        {
            this.catalog = catalog ?? new LibraryCatalogRepository();
        }

        public WorkbenchConfig BuildWorkbench(PortalQuoteRequest request)
        {
            var width = Clamp(request.WidthMm, 300, 3020, ProductDefaults.WorkbenchWidthMm);
            var depth = Clamp(request.DepthMm, 300, 1520, ProductDefaults.WorkbenchDepthMm);
            var height = Clamp(request.HeightMm, 300, 2400, ProductDefaults.WorkbenchHeightMm);
            var topSheet = CloneMaterial(FindSheet(request.SheetMaterialId));
            topSheet.ThicknessMm = topSheet.ThicknessMm <= 0 ? 18 : topSheet.ThicknessMm;

            var fastener = CloneFastener(catalog.SheetFasteners()[ProductDefaults.DefaultFastenerIndex]);
            return new WorkbenchConfig
            {
                ProjectName = "Werktafel_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                FrameProfile = CloneMaterial(FindProfile(request.ProfileMaterialId)),
                TopSheet = topSheet,
                ShelfSheet = CloneMaterial(FindSheet(request.SheetMaterialId)),
                IncludeLowerFrame = true,
                LowerFrameHeightMm = Clamp(request.LowerShelfHeightMm, 80, height - topSheet.ThicknessMm - 80, 180),
                IncludeLowerShelf = request.IncludeLowerShelf,
                IncludeMiddleLayer = request.IncludeMiddleShelf,
                MiddleLayerHeightMm = Clamp(request.MiddleShelfHeightMm, 120, height - topSheet.ThicknessMm - 60, 450),
                IncludeMiddleShelf = request.IncludeMiddleShelf,
                ShelfCornerClearanceMm = 2,
                BoltMaxSpacingMm = 300,
                TopOverhangFrontMm = 0,
                TopOverhangBackMm = 0,
                TopOverhangLeftMm = 0,
                TopOverhangRightMm = 0,
                SheetFastener = fastener,
                ConnectorHoleDiameterMm = fastener.ClearanceHoleDiameterMm,
                CountersinkSheetHoles = true,
                CountersinkDiameterMm = fastener.CounterboreDiameterMm,
                CountersinkDepthMm = fastener.CounterboreDepthMm,
                AutoTabs = true,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5
            };
        }

        public CabinetConfig BuildCabinet(PortalQuoteRequest request)
        {
            var width = Clamp(request.WidthMm, 300, 3020, ProductDefaults.CabinetWidthMm);
            var depth = Clamp(request.DepthMm, 250, 1520, ProductDefaults.CabinetDepthMm);
            var height = Clamp(request.HeightMm, 300, 2400, ProductDefaults.CabinetHeightMm);
            var units = (int)Clamp(request.UnitCount, 1, 12, ProductDefaults.CabinetUnitCount);
            var carcass = CloneMaterial(FindSheet(request.SheetMaterialId));
            var drawer = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.DrawerMaterialId) ? ProductDefaults.DefaultDrawerMaterialId : request.DrawerMaterialId));
            var back = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.BackMaterialId) ? ProductDefaults.DefaultBackMaterialId : request.BackMaterialId));
            var rail = CloneRail(catalog.DrawerRails()[ProductDefaults.DefaultDrawerRailIndex]);

            var config = new CabinetConfig
            {
                ProjectName = "Cabinet_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                WorktopHeightMm = height,
                UnitCount = units,
                PlinthHeightMm = ProductDefaults.CabinetPlinthHeightMm,
                PlinthDepthMm = ProductDefaults.CabinetPlinthDepthMm,
                IncludeBackPanel = request.IncludeBackPanel,
                CarcassMaterial = carcass,
                WorktopMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                DrawerMaterial = drawer,
                FrontMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                BackMaterial = back,
                SheetFastener = CloneFastener(catalog.SheetFasteners()[ProductDefaults.DefaultFastenerIndex]),
                DrawerRail = rail,
                ShelfSupport = CloneShelfSupport(catalog.ShelfSupports()[ProductDefaults.DefaultShelfSupportIndex]),
                IncludeFullWidthTopDrawer = request.IncludeTopDrawer,
                FullWidthTopDrawerHeightMm = ProductDefaults.FullWidthTopDrawerHeightMm,
                ShelfStartMode = NormalizeShelfStartMode(request.ShelfStartMode),
                IncludeAdjustableShelfHoles = request.IncludeAdjustableShelfHoles,
                AdjustableShelfHoleEndMarginMm = ProductDefaults.AdjustableShelfHoleEndMarginMm,
                AutoTabs = true,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5,
                ShelfClearanceMm = ProductDefaults.ShelfClearanceMm,
                DrawerSideClearanceMm = Math.Max(13, rail.ThicknessMm),
                DrawerBackClearanceMm = ProductDefaults.DrawerBackClearanceMm,
                DoorGapMm = ProductDefaults.DoorGapMm
            };

            for (var i = 1; i <= units; i++)
            {
                config.Units.Add(new CabinetUnitConfig
                {
                    UnitNumber = i,
                    ShelfCount = Math.Max(0, request.DefaultShelfCount),
                    ShelfHeightsMm = "",
                    DrawerCount = Math.Max(0, request.DefaultDrawerCount),
                    DrawerHeightMm = 160,
                    Door = ParseDoor(request.DoorMode),
                    SlidingDoors = string.Equals(request.DoorMode, "sliding", StringComparison.OrdinalIgnoreCase),
                    SlidingDoorMaxWidthMm = 600
                });
            }

            return config;
        }

        public ToolDefinition DefaultTool()
        {
            return catalog.DefaultEndMill(ProductDefaults.DefaultToolDiameterMm, ProductDefaults.DefaultToolPassDepthMm);
        }

        public MachineProfile DefaultMachine()
        {
            return ProductDefaults.DefaultMachine();
        }

        private static CabinetDoorHand ParseDoor(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            if (value == "links" || value == "left") return CabinetDoorHand.Links;
            if (value == "rechts" || value == "right") return CabinetDoorHand.Rechts;
            return CabinetDoorHand.Geen;
        }

        private static string NormalizeShelfStartMode(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            if (value == "boven" || value == "top") return "top";
            return "bottom";
        }

        private static double Clamp(double value, double min, double max, double fallback)
        {
            if (value <= 0) value = fallback;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private Material FindSheet(string id)
        {
            var sheets = catalog.Sheets();
            return FindMaterial(sheets, id, sheets[ProductDefaults.DefaultSheetIndex]);
        }

        private Material FindProfile(string id)
        {
            var profiles = catalog.Profiles();
            return FindMaterial(profiles, id, profiles[ProductDefaults.DefaultProfileIndex]);
        }

        private static Material FindMaterial(Material[] materials, string id, Material fallback)
        {
            foreach (var material in materials)
            {
                if (string.Equals(material.Id, id, StringComparison.OrdinalIgnoreCase)) return material;
            }

            return fallback;
        }

        public static Material CloneMaterial(Material material)
        {
            return new Material
            {
                Id = material.Id,
                Name = material.Name,
                Kind = material.Kind,
                WidthMm = material.WidthMm,
                HeightMm = material.HeightMm,
                ThicknessMm = material.ThicknessMm,
                StockLengthMm = material.StockLengthMm,
                SheetLengthMm = material.SheetLengthMm,
                SheetWidthMm = material.SheetWidthMm
            };
        }

        public static FastenerDefinition CloneFastener(FastenerDefinition fastener)
        {
            return new FastenerDefinition
            {
                Id = fastener.Id,
                Name = fastener.Name,
                Standard = fastener.Standard,
                NominalDiameterMm = fastener.NominalDiameterMm,
                ClearanceHoleDiameterMm = fastener.ClearanceHoleDiameterMm,
                HeadKind = fastener.HeadKind,
                HeadDiameterMm = fastener.HeadDiameterMm,
                HeadHeightMm = fastener.HeadHeightMm,
                HeadClearanceMm = fastener.HeadClearanceMm
            };
        }

        public static RailTemplate CloneRail(RailTemplate rail)
        {
            return new RailTemplate
            {
                Id = rail.Id,
                Name = rail.Name,
                LengthMm = rail.LengthMm,
                ThicknessMm = rail.ThicknessMm,
                CabinetHoleCount = rail.CabinetHoleCount,
                CabinetFirstHoleOffsetMm = rail.CabinetFirstHoleOffsetMm,
                CabinetHoleSpacingMm = rail.CabinetHoleSpacingMm,
                CabinetHolePositionsMm = rail.CabinetHolePositionsMm,
                CabinetVerticalOffsetMm = rail.CabinetVerticalOffsetMm,
                CabinetHoleDiameterMm = rail.CabinetHoleDiameterMm,
                DrawerHoleCount = rail.DrawerHoleCount,
                DrawerFirstHoleOffsetMm = rail.DrawerFirstHoleOffsetMm,
                DrawerHoleSpacingMm = rail.DrawerHoleSpacingMm,
                DrawerHolePositionsMm = rail.DrawerHolePositionsMm,
                DrawerVerticalOffsetMm = rail.DrawerVerticalOffsetMm,
                DrawerHoleDiameterMm = rail.DrawerHoleDiameterMm,
                FastenerName = rail.FastenerName
            };
        }

        public static ShelfSupportTemplate CloneShelfSupport(ShelfSupportTemplate support)
        {
            return new ShelfSupportTemplate
            {
                Id = support.Id,
                Name = support.Name,
                ThicknessMm = support.ThicknessMm,
                HeightMm = support.HeightMm,
                HoleDiameterMm = support.HoleDiameterMm,
                HoleSpacingMm = support.HoleSpacingMm,
                FrontInsetMm = support.FrontInsetMm,
                BackInsetMm = support.BackInsetMm,
                FirstHoleHeightMm = support.FirstHoleHeightMm
            };
        }
    }
}
