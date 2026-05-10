using System;
using System.Collections.Generic;
using System.Globalization;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class CabinetEngine
    {
        public WorkbenchModel Build(CabinetConfig config)
        {
            Validate(config);

            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                SheetFastener = config.SheetFastener
            };

            var carcass = config.CarcassMaterial;
            var top = config.WorktopMaterial;
            var drawer = config.DrawerMaterial;
            var front = config.FrontMaterial;
            var back = config.BackMaterial ?? carcass;
            var t = carcass.ThicknessMm;
            var topT = top.ThicknessMm;
            var bodyHeight = config.WorktopHeightMm - topT;
            var unitWidth = config.WidthMm / config.UnitCount;
            var innerDepth = config.DepthMm - t;
            var bayFitClearance = Math.Min(Math.Max(0, config.ShelfClearanceMm), 1.0);
            var bayWidth = Math.Max(20, unitWidth - t - 2.0 * bayFitClearance);
            var frontZ = -config.DepthMm / 2.0;
            var backZ = config.DepthMm / 2.0;
            var backThickness = config.IncludeBackPanel ? back.ThicknessMm : 0;
            var topDepth = config.DepthMm + backThickness;
            var topCenterZ = backThickness / 2.0;
            var plinthNotchDepth = Math.Min(config.DepthMm - 1, config.PlinthDepthMm + t + 2.0);

            AddSheet(model, Sheet("Werkblad", top, config.WidthMm, topDepth), 0, config.WorktopHeightMm - topT / 2.0, topCenterZ, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, Sheet("Plint voor", carcass, config.WidthMm, config.PlinthHeightMm), 0, config.PlinthHeightMm / 2.0, frontZ + config.PlinthDepthMm + t / 2.0, AssemblyOrientation.SheetVerticalX);

            var leftSide = SidePanel("Zijwand links", carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            AddRailHolesForPanel(leftSide, config, 0, bodyHeight);
            AddSheet(model, leftSide, -config.WidthMm / 2.0 + t / 2.0, bodyHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            var rightSide = SidePanel("Zijwand rechts", carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            AddRailHolesForPanel(rightSide, config, config.UnitCount, bodyHeight);
            AddSheet(model, rightSide, config.WidthMm / 2.0 - t / 2.0, bodyHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            for (var i = 1; i < config.UnitCount; i++)
            {
                var x = -config.WidthMm / 2.0 + unitWidth * i;
                var divider = SidePanel("Tussenschot " + i.ToString(CultureInfo.InvariantCulture), carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
                AddRailHolesForPanel(divider, config, i, bodyHeight);
                AddSheet(model, divider, x, bodyHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);
            }

            for (var i = 0; i < config.UnitCount; i++)
            {
                var unitCenterX = -config.WidthMm / 2.0 + unitWidth * (i + 0.5);
                AddSheet(model, Sheet("Bodem U" + (i + 1).ToString(CultureInfo.InvariantCulture), carcass, bayWidth, config.DepthMm), unitCenterX, config.PlinthHeightMm + t / 2.0, 0, AssemblyOrientation.SheetHorizontal);
            }

            if (config.IncludeBackPanel)
            {
                AddSheet(model, Sheet("Achterwand", back, config.WidthMm, bodyHeight), 0, bodyHeight / 2.0, backZ + back.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
            }

            for (var i = 0; i < config.UnitCount; i++)
            {
                var unit = GetUnit(config, i + 1);
                var unitCenterX = -config.WidthMm / 2.0 + unitWidth * (i + 0.5);
                var clearWidth = bayWidth;
                BuildUnit(model, config, unit, i + 1, unitCenterX, clearWidth, innerDepth, bodyHeight, frontZ, drawer, front, carcass);
            }

            AddHardware(model, config);
            return model;
        }

        private static void BuildUnit(
            WorkbenchModel model,
            CabinetConfig config,
            CabinetUnitConfig unit,
            int unitNumber,
            double centerX,
            double clearWidth,
            double innerDepth,
            double bodyHeight,
            double frontZ,
            Material drawerMaterial,
            Material frontMaterial,
            Material carcassMaterial)
        {
            var t = carcassMaterial.ThicknessMm;
            foreach (var shelfHeight in ShelfHeights(unit, config.PlinthHeightMm + 80, bodyHeight - 80))
            {
                var shelf = Sheet("Legplank U" + unitNumber + " H" + shelfHeight.ToString("0", CultureInfo.InvariantCulture), carcassMaterial, clearWidth, innerDepth);
                AddSheet(model, shelf, centerX, shelfHeight, 0, AssemblyOrientation.SheetHorizontal);
            }

            if (unit.DrawerCount > 0)
            {
                var usableHeight = Math.Max(80, unit.DrawerHeightMm);
                for (var drawerIndex = 0; drawerIndex < unit.DrawerCount; drawerIndex++)
                {
                    var bottomY = config.PlinthHeightMm + config.DoorGapMm + drawerIndex * (usableHeight + config.DoorGapMm);
                    var centerY = bottomY + usableHeight / 2.0;
                    var boxDepth = Math.Max(120, Math.Min(config.DrawerRail.LengthMm - 20, innerDepth - 70));
                    var boxWidth = Math.Max(80, clearWidth - 2.0 * config.DrawerSideClearanceMm);
                    var drawerT = drawerMaterial.ThicknessMm;
                    var bottomWidth = Math.Max(60, boxWidth - 2.0 * drawerT);
                    var bottomDepth = Math.Max(80, boxDepth - drawerT);
                    var frontWidth = Math.Max(80, clearWidth - config.DoorGapMm);
                    var frontHeight = usableHeight - config.DoorGapMm;
                    var boxFrontZ = frontZ;
                    var boxCenterZ = boxFrontZ + boxDepth / 2.0;
                    var bottomCenterZ = boxFrontZ + bottomDepth / 2.0;
                    var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

                    AddSheet(model, Sheet("Ladefront U" + unitNumber + "-" + (drawerIndex + 1), frontMaterial, frontWidth, frontHeight), centerX, centerY, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
                    AddSheet(model, Sheet("Ladebodem U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, bottomDepth), centerX, bottomY + drawerT / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
                    AddSheet(model, Sheet("Ladezijde links U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, boxDepth, frontHeight), centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, Sheet("Ladezijde rechts U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, boxDepth, frontHeight), centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, Sheet("Ladeachter U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, frontHeight), centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddRailHardware(model, config, unitNumber, drawerIndex + 1);
                }
            }

            if (unit.Door != CabinetDoorHand.Geen)
            {
                var height = bodyHeight - config.PlinthHeightMm - 2.0 * config.DoorGapMm;
                var door = Sheet("Draaideur " + unit.Door + " U" + unitNumber, frontMaterial, clearWidth - config.DoorGapMm, height);
                AddSheet(model, door, centerX, config.PlinthHeightMm + config.DoorGapMm + height / 2.0, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
                model.Hardware.Add(new HardwareItem { Name = "Scharnieren " + unit.Door, ArticleNumber = "HINGE_TEMPLATE", Quantity = 2, Unit = "st", Note = "Voor draaideur unit " + unitNumber });
            }

            if (unit.SlidingDoors)
            {
                var maxWidth = unit.SlidingDoorMaxWidthMm > 0 ? unit.SlidingDoorMaxWidthMm : clearWidth / 2.0;
                var panels = Math.Max(2, (int)Math.Ceiling(clearWidth / maxWidth));
                var panelWidth = clearWidth / panels + 25;
                var panelHeight = bodyHeight - config.PlinthHeightMm - 2.0 * config.DoorGapMm;
                for (var p = 0; p < panels; p++)
                {
                    var x = centerX - clearWidth / 2.0 + (p + 0.5) * clearWidth / panels;
                    var z = frontZ - frontMaterial.ThicknessMm / 2.0 - p % 2 * 8;
                    AddSheet(model, Sheet("Schuifdeur U" + unitNumber + "-" + (p + 1), frontMaterial, panelWidth, panelHeight), x, config.PlinthHeightMm + config.DoorGapMm + panelHeight / 2.0, z, AssemblyOrientation.SheetVerticalX);
                }

                model.Hardware.Add(new HardwareItem { Name = "Schuifdeurrail set", ArticleNumber = "SLIDING_TRACK_TEMPLATE", Quantity = 1, Unit = "set", Note = "Unit " + unitNumber + ", max paneelbreedte " + maxWidth.ToString("0") + " mm" });
            }
        }

        private static SheetPart Sheet(string name, Material material, double length, double width)
        {
            return new SheetPart
            {
                Name = name,
                Material = material,
                LengthMm = Math.Round(length, 2),
                WidthMm = Math.Round(width, 2),
                Quantity = 1,
                UseTabs = false
            };
        }

        private static SheetPart SidePanel(string name, Material material, double length, double width, double notchDepth, double notchHeight)
        {
            var panel = Sheet(name, material, length, width);
            if (notchDepth > 0 && notchHeight > 0)
            {
                panel.HasToeKickNotch = true;
                panel.ToeKickDepthMm = Math.Round(Math.Min(notchDepth, length - 1), 2);
                panel.ToeKickHeightMm = Math.Round(Math.Min(notchHeight, width - 1), 2);
            }

            return panel;
        }

        private static void AddSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            sheet.CenterHeightMm = y;
            sheet.UseTabs = sheet.LengthMm * sheet.WidthMm < 300 * 300;
            model.Sheets.Add(sheet);
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Sheet,
                PartName = sheet.Name,
                LengthMm = sheet.LengthMm,
                WidthMm = sheet.WidthMm,
                Xmm = x,
                Ymm = y,
                Zmm = z,
                Orientation = orientation
            });
        }

        private static CabinetUnitConfig GetUnit(CabinetConfig config, int unitNumber)
        {
            foreach (var unit in config.Units)
            {
                if (unit.UnitNumber == unitNumber) return unit;
            }

            return new CabinetUnitConfig { UnitNumber = unitNumber };
        }

        private static IEnumerable<double> ShelfHeights(CabinetUnitConfig unit, double min, double max)
        {
            var explicitHeights = new List<double>();
            if (!string.IsNullOrWhiteSpace(unit.ShelfHeightsMm))
            {
                var parts = unit.ShelfHeightsMm.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    double value;
                    if (double.TryParse(part.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > min && value < max)
                    {
                        explicitHeights.Add(value);
                    }
                }
            }

            if (explicitHeights.Count > 0)
            {
                return explicitHeights;
            }

            var count = Math.Max(0, unit.ShelfCount);
            var heights = new List<double>();
            for (var i = 1; i <= count; i++)
            {
                heights.Add(min + (max - min) * i / (count + 1));
            }

            return heights;
        }

        private static void AddRailHardware(WorkbenchModel model, CabinetConfig config, int unitNumber, int drawerNumber)
        {
            model.Hardware.Add(new HardwareItem
            {
                Name = config.DrawerRail.Name,
                ArticleNumber = config.DrawerRail.Id,
                Quantity = 2,
                Unit = "st",
                Note = "Unit " + unitNumber + ", lade " + drawerNumber + "; gatenpatroon " + config.DrawerRail.HoleCount + "x vanaf " + config.DrawerRail.FirstHoleOffsetMm.ToString("0") + " mm, steek " + config.DrawerRail.HoleSpacingMm.ToString("0") + " mm"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = config.DrawerRail.FastenerName,
                ArticleNumber = "RAIL_SCREW",
                Quantity = Math.Max(0, config.DrawerRail.HoleCount) * 4,
                Unit = "st",
                Note = "Railbevestiging unit " + unitNumber + ", lade " + drawerNumber
            });
        }

        private static void AddRailHolesForPanel(SheetPart panel, CabinetConfig config, int boundaryIndex, double bodyHeight)
        {
            AddRailHolesForAdjacentUnit(panel, config, boundaryIndex, bodyHeight);
            AddRailHolesForAdjacentUnit(panel, config, boundaryIndex + 1, bodyHeight);
        }

        private static void AddRailHolesForAdjacentUnit(SheetPart panel, CabinetConfig config, int unitNumber, double bodyHeight)
        {
            if (unitNumber < 1 || unitNumber > config.UnitCount) return;
            var unit = GetUnit(config, unitNumber);
            if (unit.DrawerCount <= 0) return;

            var rail = config.DrawerRail;
            var drawerHeight = Math.Max(80, unit.DrawerHeightMm);
            for (var drawerIndex = 0; drawerIndex < unit.DrawerCount; drawerIndex++)
            {
                var bottomY = config.PlinthHeightMm + config.DoorGapMm + drawerIndex * (drawerHeight + config.DoorGapMm);
                var railY = bottomY + rail.VerticalOffsetMm;
                if (railY <= 10 || railY >= bodyHeight - 10) continue;

                for (var holeIndex = 0; holeIndex < rail.HoleCount; holeIndex++)
                {
                    var x = rail.FirstHoleOffsetMm + holeIndex * rail.HoleSpacingMm;
                    if (x <= 5 || x >= panel.LengthMm - 5) continue;
                    panel.Holes.Add(new SheetHole
                    {
                        Name = "Railgat U" + unitNumber + " lade " + (drawerIndex + 1) + " pos " + (holeIndex + 1),
                        Xmm = Math.Round(x, 3),
                        Ymm = Math.Round(railY, 3),
                        DiameterMm = rail.HoleDiameterMm,
                        Countersunk = false,
                        SupportKind = SheetHoleSupportKind.ProfileNut
                    });
                }
            }
        }

        private static void AddHardware(WorkbenchModel model, CabinetConfig config)
        {
            model.Hardware.Add(new HardwareItem
            {
                Name = "Plaatkast montage schroeven",
                ArticleNumber = "CABINET_SCREW_TEMPLATE",
                Quantity = Math.Max(24, config.UnitCount * 12),
                Unit = "st",
                Note = "Indicatief voor romp/plint; exacte beslaglibrary volgt later"
            });
        }

        private static void Validate(CabinetConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.CarcassMaterial == null) throw new ArgumentException("Romp materiaal ontbreekt.");
            if (config.WorktopMaterial == null) throw new ArgumentException("Blad materiaal ontbreekt.");
            if (config.DrawerMaterial == null) config.DrawerMaterial = config.CarcassMaterial;
            if (config.FrontMaterial == null) config.FrontMaterial = config.CarcassMaterial;
            if (config.BackMaterial == null) config.BackMaterial = config.CarcassMaterial;
            if (config.DrawerRail == null) throw new ArgumentException("Rail-template ontbreekt.");
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.WorktopHeightMm <= 0) throw new ArgumentException("Cabinet-afmetingen moeten groter zijn dan 0.");
            if (config.UnitCount <= 0) throw new ArgumentException("Aantal units moet minimaal 1 zijn.");
            if (config.WorktopHeightMm <= config.WorktopMaterial.ThicknessMm + config.PlinthHeightMm + 100) throw new ArgumentException("Bladhoogte is te laag voor plint en romp.");
            if (config.PlinthHeightMm < 0 || config.PlinthDepthMm < 0) throw new ArgumentException("Plintmaten mogen niet negatief zijn.");
            if (config.PlinthDepthMm >= config.DepthMm) throw new ArgumentException("Plintdiepte moet kleiner zijn dan de kastdiepte.");
            if (config.ShelfClearanceMm < 0) config.ShelfClearanceMm = 1;
            if (config.DrawerSideClearanceMm < 0) config.DrawerSideClearanceMm = 12;
            if (config.DoorGapMm < 0) config.DoorGapMm = 2;
        }
    }
}
