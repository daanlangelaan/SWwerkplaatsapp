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
            var topDrawerHeight = config.IncludeFullWidthTopDrawer ? Math.Min(config.FullWidthTopDrawerHeightMm, bodyHeight - config.PlinthHeightMm - 80) : 0;
            var shelfZoneTop = topDrawerHeight > 0 ? bodyHeight - topDrawerHeight : bodyHeight;

            AddSheet(model, Sheet("Werkblad", top, config.WidthMm, topDepth), 0, config.WorktopHeightMm - topT / 2.0, topCenterZ, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, Sheet("Plint voor", carcass, config.WidthMm, config.PlinthHeightMm), 0, config.PlinthHeightMm / 2.0, frontZ + config.PlinthDepthMm + t / 2.0, AssemblyOrientation.SheetVerticalX);

            var leftSide = SidePanel("Zijwand links", carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            AddRailHolesForPanel(leftSide, config, 0, bodyHeight);
            AddTopDrawerRailHolesForPanel(leftSide, config, 0, bodyHeight);
            AddAdjustableShelfHoles(leftSide, config, shelfZoneTop);
            AddSheet(model, leftSide, -config.WidthMm / 2.0 + t / 2.0, bodyHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            var rightSide = SidePanel("Zijwand rechts", carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            AddRailHolesForPanel(rightSide, config, config.UnitCount, bodyHeight);
            AddTopDrawerRailHolesForPanel(rightSide, config, config.UnitCount, bodyHeight);
            AddAdjustableShelfHoles(rightSide, config, shelfZoneTop);
            AddSheet(model, rightSide, config.WidthMm / 2.0 - t / 2.0, bodyHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            for (var i = 1; i < config.UnitCount; i++)
            {
                var x = -config.WidthMm / 2.0 + unitWidth * i;
                var divider = SidePanel("Tussenschot " + i.ToString(CultureInfo.InvariantCulture), carcass, config.DepthMm, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
                AddRailHolesForPanel(divider, config, i, bodyHeight);
                AddTopDrawerRailHolesForPanel(divider, config, i, bodyHeight);
                AddAdjustableShelfHoles(divider, config, shelfZoneTop);
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
                if (topDrawerHeight > 0)
                {
                    BuildTopDrawerForUnit(model, config, i + 1, unitCenterX, clearWidth, innerDepth, bodyHeight, topDrawerHeight, frontZ, drawer, front, carcass);
                }

                BuildUnit(model, config, unit, i + 1, unitCenterX, clearWidth, innerDepth, bodyHeight, shelfZoneTop, frontZ, drawer, front, carcass);
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
            double shelfZoneTop,
            double frontZ,
            Material drawerMaterial,
            Material frontMaterial,
            Material carcassMaterial)
        {
            var t = carcassMaterial.ThicknessMm;
            foreach (var shelfHeight in ShelfHeights(unit, config, config.PlinthHeightMm + 80, shelfZoneTop - 60))
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
                    var boxDepth = DrawerBoxDepth(config, innerDepth);
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
                    var drawerSideLeft = Sheet("Ladezijde links U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, boxDepth, frontHeight);
                    var drawerSideRight = Sheet("Ladezijde rechts U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, boxDepth, frontHeight);
                    AddDrawerRailHoles(drawerSideLeft, config);
                    AddDrawerRailHoles(drawerSideRight, config);
                    AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
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

        private static void BuildTopDrawerForUnit(
            WorkbenchModel model,
            CabinetConfig config,
            int unitNumber,
            double centerX,
            double clearWidth,
            double innerDepth,
            double bodyHeight,
            double drawerHeight,
            double frontZ,
            Material drawerMaterial,
            Material frontMaterial,
            Material carcassMaterial)
        {
            var t = carcassMaterial.ThicknessMm;
            var drawerT = drawerMaterial.ThicknessMm;
            var bottomY = bodyHeight - drawerHeight + config.DoorGapMm;
            var usableHeight = Math.Max(80, drawerHeight - config.DoorGapMm);
            var centerY = bottomY + usableHeight / 2.0;
            var boxDepth = DrawerBoxDepth(config, innerDepth);
            var boxWidth = Math.Max(80, clearWidth - 2.0 * config.DrawerSideClearanceMm);
            var bottomWidth = Math.Max(80, boxWidth - 2.0 * drawerT);
            var bottomDepth = Math.Max(80, boxDepth - drawerT);
            var frontWidth = Math.Max(80, clearWidth - config.DoorGapMm);
            var frontHeight = usableHeight - config.DoorGapMm;
            var boxFrontZ = frontZ;
            var boxCenterZ = boxFrontZ + boxDepth / 2.0;
            var bottomCenterZ = boxFrontZ + bottomDepth / 2.0;
            var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

            AddSheet(model, Sheet("Bovenlade front U" + unitNumber, frontMaterial, frontWidth, frontHeight), centerX, centerY, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
            AddSheet(model, Sheet("Bovenlade bodem U" + unitNumber, drawerMaterial, bottomWidth, bottomDepth), centerX, bottomY + drawerT / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
            var drawerSideLeft = Sheet("Bovenlade zijde links U" + unitNumber, drawerMaterial, boxDepth, frontHeight);
            var drawerSideRight = Sheet("Bovenlade zijde rechts U" + unitNumber, drawerMaterial, boxDepth, frontHeight);
            AddDrawerRailHoles(drawerSideLeft, config);
            AddDrawerRailHoles(drawerSideRight, config);
            AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, Sheet("Bovenlade achter U" + unitNumber, drawerMaterial, bottomWidth, frontHeight), centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
            AddRailHardware(model, config, unitNumber, 0);
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

        private static double DrawerBoxDepth(CabinetConfig config, double innerDepth)
        {
            return Math.Max(120, innerDepth - config.DrawerBackClearanceMm);
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

        private static IEnumerable<double> ShelfHeights(CabinetUnitConfig unit, CabinetConfig config, double min, double max)
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
                var target = min + (max - min) * i / (count + 1);
                heights.Add(SnapShelfHeightToSupportPattern(target, config, min, max));
            }

            return heights;
        }

        private static double SnapShelfHeightToSupportPattern(double target, CabinetConfig config, double min, double max)
        {
            if (!config.IncludeAdjustableShelfHoles || config.ShelfSupport == null || config.ShelfSupport.HoleSpacingMm <= 0)
            {
                return target;
            }

            var first = config.ShelfSupport.FirstHoleHeightMm;
            var spacing = config.ShelfSupport.HoleSpacingMm;
            if (target < first) return Math.Max(min, first);

            var steps = Math.Round((target - first) / spacing);
            var snapped = first + steps * spacing;
            if (snapped < min) snapped = min;
            if (snapped > max) snapped = max;
            return Math.Round(snapped, 3);
        }

        private static void AddRailHardware(WorkbenchModel model, CabinetConfig config, int unitNumber, int drawerNumber)
        {
            var location = unitNumber <= 0 ? "Bovenlade" : "Unit " + unitNumber + ", lade " + drawerNumber;
            model.Hardware.Add(new HardwareItem
            {
                Name = config.DrawerRail.Name,
                ArticleNumber = config.DrawerRail.Id,
                Quantity = 2,
                Unit = "st",
                Note = location + "; gatenpatroon " + config.DrawerRail.HoleCount + "x vanaf " + config.DrawerRail.FirstHoleOffsetMm.ToString("0") + " mm, steek " + config.DrawerRail.HoleSpacingMm.ToString("0") + " mm"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = config.DrawerRail.FastenerName,
                ArticleNumber = "RAIL_SCREW",
                Quantity = Math.Max(0, config.DrawerRail.HoleCount) * 4,
                Unit = "st",
                Note = "Railbevestiging " + location
            });
        }

        private static void AddRailHolesForPanel(SheetPart panel, CabinetConfig config, int boundaryIndex, double bodyHeight)
        {
            AddRailHolesForAdjacentUnit(panel, config, boundaryIndex, bodyHeight);
            AddRailHolesForAdjacentUnit(panel, config, boundaryIndex + 1, bodyHeight);
        }

        private static void AddTopDrawerRailHolesForPanel(SheetPart panel, CabinetConfig config, int boundaryIndex, double bodyHeight)
        {
            if (!config.IncludeFullWidthTopDrawer) return;
            AddTopDrawerRailHolesForAdjacentUnit(panel, config, boundaryIndex, bodyHeight);
            AddTopDrawerRailHolesForAdjacentUnit(panel, config, boundaryIndex + 1, bodyHeight);
        }

        private static void AddTopDrawerRailHolesForAdjacentUnit(SheetPart panel, CabinetConfig config, int unitNumber, double bodyHeight)
        {
            if (unitNumber < 1 || unitNumber > config.UnitCount) return;
            var rail = config.DrawerRail;
            if (rail == null) return;

            var drawerHeight = Math.Max(80, config.FullWidthTopDrawerHeightMm);
            var bottomY = bodyHeight - drawerHeight + config.DoorGapMm;
            var railY = bottomY + rail.VerticalOffsetMm;
            AddRailHoleLine(panel, rail.CabinetHolePositionsMm, rail.HoleCount, rail.FirstHoleOffsetMm, rail.HoleSpacingMm, rail.HoleDiameterMm, railY, "Bovenlade railgat U" + unitNumber);
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

                var positions = RailHolePositions(rail.CabinetHolePositionsMm, rail.HoleCount, rail.FirstHoleOffsetMm, rail.HoleSpacingMm);
                for (var holeIndex = 0; holeIndex < positions.Count; holeIndex++)
                {
                    var x = positions[holeIndex];
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

        private static void AddDrawerRailHoles(SheetPart panel, CabinetConfig config)
        {
            var rail = config.DrawerRail;
            if (rail == null || rail.DrawerHoleCount <= 0) return;

            var y = Math.Round(rail.DrawerVerticalOffsetMm, 3);
            if (y <= 5 || y >= panel.WidthMm - 5) return;

            AddRailHoleLine(panel, rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm, rail.DrawerHoleDiameterMm, y, "Laderailgat");
        }

        private static void AddRailHoleLine(SheetPart panel, string explicitPositions, int count, double firstOffset, double spacing, double diameter, double y, string name)
        {
            y = Math.Round(y, 3);
            if (y <= 5 || y >= panel.WidthMm - 5) return;

            var positions = RailHolePositions(explicitPositions, count, firstOffset, spacing);
            for (var holeIndex = 0; holeIndex < positions.Count; holeIndex++)
            {
                var x = positions[holeIndex];
                if (x <= 5 || x >= panel.LengthMm - 5) continue;
                if (HasHoleAt(panel, x, y, diameter)) continue;
                panel.Holes.Add(new SheetHole
                {
                    Name = name + " pos " + (holeIndex + 1),
                    Xmm = Math.Round(x, 3),
                    Ymm = y,
                    DiameterMm = diameter,
                    Countersunk = false,
                    SupportKind = SheetHoleSupportKind.ProfileNut
                });
            }
        }

        private static bool HasHoleAt(SheetPart panel, double x, double y, double diameter)
        {
            foreach (var hole in panel.Holes)
            {
                if (Math.Abs(hole.Xmm - x) < 0.01 && Math.Abs(hole.Ymm - y) < 0.01 && Math.Abs(hole.DiameterMm - diameter) < 0.01)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddAdjustableShelfHoles(SheetPart panel, CabinetConfig config, double usableHeight)
        {
            if (!config.IncludeAdjustableShelfHoles || config.ShelfSupport == null) return;
            var support = config.ShelfSupport;
            var spacing = Math.Max(1, support.HoleSpacingMm);
            var endY = Math.Min(panel.WidthMm - config.AdjustableShelfHoleEndMarginMm, usableHeight - config.AdjustableShelfHoleEndMarginMm);
            var frontX = support.FrontInsetMm;
            var backX = panel.LengthMm - support.BackInsetMm;
            if (frontX <= 5 || backX >= panel.LengthMm - 5 || frontX >= backX) return;

            var index = 1;
            for (var y = support.FirstHoleHeightMm; y <= endY; y += spacing)
            {
                AddShelfSupportHole(panel, frontX, y, support.HoleDiameterMm, index++);
                AddShelfSupportHole(panel, backX, y, support.HoleDiameterMm, index++);
            }
        }

        private static void AddShelfSupportHole(SheetPart panel, double x, double y, double diameter, int index)
        {
            if (HasHoleAt(panel, x, y, diameter)) return;
            panel.Holes.Add(new SheetHole
            {
                Name = "Legplankdragergat " + index,
                Xmm = Math.Round(x, 3),
                Ymm = Math.Round(y, 3),
                DiameterMm = diameter,
                Countersunk = false,
                SupportKind = SheetHoleSupportKind.ProfileNut
            });
        }

        private static List<double> RailHolePositions(string explicitPositions, int count, double firstOffset, double spacing)
        {
            var positions = new List<double>();
            if (!string.IsNullOrWhiteSpace(explicitPositions))
            {
                var parts = explicitPositions.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    double value;
                    if (double.TryParse(part.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        positions.Add(value);
                    }
                }
            }

            if (positions.Count > 0) return positions;

            for (var i = 0; i < count; i++)
            {
                positions.Add(firstOffset + i * spacing);
            }

            return positions;
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
            if (config.ShelfSupport == null) config.ShelfSupport = new ShelfSupportTemplate { Id = "default_shelf_pin", Name = "Default legplankdrager", ThicknessMm = 5, HeightMm = 12, HoleDiameterMm = 5, HoleSpacingMm = 32, FrontInsetMm = 50, BackInsetMm = 50, FirstHoleHeightMm = 160 };
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.WorktopHeightMm <= 0) throw new ArgumentException("Cabinet-afmetingen moeten groter zijn dan 0.");
            if (config.UnitCount <= 0) throw new ArgumentException("Aantal units moet minimaal 1 zijn.");
            if (config.WorktopHeightMm <= config.WorktopMaterial.ThicknessMm + config.PlinthHeightMm + 100) throw new ArgumentException("Bladhoogte is te laag voor plint en romp.");
            if (config.PlinthHeightMm < 0 || config.PlinthDepthMm < 0) throw new ArgumentException("Plintmaten mogen niet negatief zijn.");
            if (config.PlinthDepthMm >= config.DepthMm) throw new ArgumentException("Plintdiepte moet kleiner zijn dan de kastdiepte.");
            if (config.ShelfClearanceMm < 0) config.ShelfClearanceMm = 1;
            if (config.DrawerSideClearanceMm < 0) config.DrawerSideClearanceMm = 12;
            if (config.DrawerBackClearanceMm < 0) config.DrawerBackClearanceMm = 30;
            if (config.DoorGapMm < 0) config.DoorGapMm = 2;
            if (config.IncludeFullWidthTopDrawer && config.FullWidthTopDrawerHeightMm <= 0) config.FullWidthTopDrawerHeightMm = 160;
            if (config.AdjustableShelfHoleEndMarginMm <= 0) config.AdjustableShelfHoleEndMarginMm = 80;
        }
    }
}
