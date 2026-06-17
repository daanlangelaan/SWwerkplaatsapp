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
            var topDrawerHeight = TopDrawerHeight(config, bodyHeight);
            var shelfZoneTop = topDrawerHeight > 0 ? bodyHeight - topDrawerHeight : bodyHeight;

            var worktop = Sheet("Werkblad", top, config.WidthMm, topDepth);
            AddDividerGroovesToHorizontalSheet(worktop, config, config.WidthMm, config.DepthMm, "Tussenschot in werkblad");
            AddWorktopToUprightHoles(worktop, config, topDepth);
            AddSheet(model, worktop, 0, config.WorktopHeightMm - topT / 2.0, topCenterZ, AssemblyOrientation.SheetHorizontal);
            var plinth = Sheet("Plint voor", carcass, config.WidthMm, config.PlinthHeightMm);
            AddPlinthToUprightHoles(plinth, config);
            AddSheet(model, plinth, 0, config.PlinthHeightMm / 2.0, frontZ + config.PlinthDepthMm + t / 2.0, AssemblyOrientation.SheetVerticalX);

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
                var dividerHeight = bodyHeight + AlignmentGrooveDepthMm(worktop);
                var divider = SidePanel("Tussenschot " + i.ToString(CultureInfo.InvariantCulture), carcass, config.DepthMm, dividerHeight, plinthNotchDepth, config.PlinthHeightMm);
                AddRailHolesForPanel(divider, config, i, bodyHeight);
                AddTopDrawerRailHolesForPanel(divider, config, i, bodyHeight);
                AddAdjustableShelfHoles(divider, config, shelfZoneTop);
                AddSheet(model, divider, x, dividerHeight / 2.0, 0, AssemblyOrientation.SheetVerticalZ);
            }

            for (var i = 0; i < config.UnitCount; i++)
            {
                var unitCenterX = -config.WidthMm / 2.0 + unitWidth * (i + 0.5);
                var bottom = Sheet("Bodem U" + (i + 1).ToString(CultureInfo.InvariantCulture), carcass, bayWidth, config.DepthMm);
                AddSheet(model, bottom, unitCenterX, config.PlinthHeightMm + t / 2.0, 0, AssemblyOrientation.SheetHorizontal);
            }

            if (config.IncludeBackPanel)
            {
                var backPanel = Sheet("Achterwand", back, config.WidthMm, bodyHeight);
                AddDividerGroovesToBackPanel(backPanel, config, bodyHeight);
                AddBackPanelMountingHoles(backPanel, config, bodyHeight);
                AddSheet(model, backPanel, 0, bodyHeight / 2.0, backZ + back.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
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
                    var bottomY = DrawerBottomY(config, drawerIndex, usableHeight, shelfZoneTop);
                    var centerY = bottomY + usableHeight / 2.0;
                    var boxDepth = DrawerBoxDepth(config, innerDepth);
                    var boxWidth = Math.Max(80, clearWidth - 2.0 * config.DrawerSideClearanceMm);
                    var drawerT = drawerMaterial.ThicknessMm;
                    var frontPocketDepth = DrawerGrooveDepthMm();
                    var sideLength = boxDepth + frontPocketDepth;
                    var bottomWidth = Math.Max(60, boxWidth - 2.0 * drawerT);
                    var bottomDepth = Math.Max(80, boxDepth - drawerT);
                    var frontWidth = Math.Max(80, clearWidth - config.DoorGapMm);
                    var frontHeight = usableHeight - config.DoorGapMm;
                    var boxFrontZ = frontZ;
                    var boxCenterZ = boxFrontZ - frontPocketDepth + sideLength / 2.0;
                    bottomWidth = Math.Max(80, bottomWidth + 2.0 * DrawerGrooveDepthMm());
                    bottomDepth = Math.Max(80, bottomDepth + 2.0 * DrawerGrooveDepthMm());
                    var bottomCenterZ = boxFrontZ - frontPocketDepth + bottomDepth / 2.0;
                    var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

                    var drawerFront = Sheet("Ladefront U" + unitNumber + "-" + (drawerIndex + 1), frontMaterial, frontWidth, frontHeight);
                    AddDrawerFrontGrooves(drawerFront, boxWidth, drawerMaterial);
                    AddSheet(model, drawerFront, centerX, centerY, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
                    var drawerBottom = Sheet("Ladebodem U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, bottomDepth);
                    var drawerSideLeft = Sheet("Ladezijde links U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, sideLength, frontHeight);
                    var drawerSideRight = Sheet("Ladezijde rechts U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, sideLength, frontHeight);
                    var drawerBack = Sheet("Ladeachter U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, frontHeight);
                    AddDrawerBottomGroove(drawerSideLeft, drawerMaterial);
                    AddDrawerBottomGroove(drawerSideRight, drawerMaterial);
                    AddDrawerBackGroove(drawerSideLeft, drawerMaterial);
                    AddDrawerBackGroove(drawerSideRight, drawerMaterial);
                    AddDrawerBottomGroove(drawerBack, drawerMaterial);
                    AddDrawerAssemblyHoles(config, drawerFront, drawerBottom, drawerSideLeft, drawerSideRight, drawerBack, boxWidth, drawerT);
                    AddDrawerRailHoles(drawerSideLeft, config);
                    AddDrawerRailHoles(drawerSideRight, config);
                    AddSheet(model, drawerBottom, centerX, bottomY + DrawerGrooveBottomOffsetMm() + drawerT / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
                    AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, drawerBack, centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddRailHardware(model, config, unitNumber, drawerIndex + 1);
                }
            }

            if (unit.Door != CabinetDoorHand.Geen)
            {
                var height = bodyHeight - config.PlinthHeightMm - 2.0 * config.DoorGapMm;
                var door = Sheet("Draaideur " + unit.Door + " U" + unitNumber, frontMaterial, clearWidth - config.DoorGapMm, height);
                AddDoorHingeHoles(door, config, unit.Door);
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
            var frontPocketDepth = DrawerGrooveDepthMm();
            var sideLength = boxDepth + frontPocketDepth;
            var frontWidth = Math.Max(80, clearWidth - config.DoorGapMm);
            var frontHeight = usableHeight - config.DoorGapMm;
            var boxFrontZ = frontZ;
            var boxCenterZ = boxFrontZ - frontPocketDepth + sideLength / 2.0;
            bottomWidth = Math.Max(80, bottomWidth + 2.0 * DrawerGrooveDepthMm());
            bottomDepth = Math.Max(80, bottomDepth + 2.0 * DrawerGrooveDepthMm());
            var bottomCenterZ = boxFrontZ - frontPocketDepth + bottomDepth / 2.0;
            var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

            var drawerFront = Sheet("Bovenlade front U" + unitNumber, frontMaterial, frontWidth, frontHeight);
            AddDrawerFrontGrooves(drawerFront, boxWidth, drawerMaterial);
            AddSheet(model, drawerFront, centerX, centerY, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
            var drawerBottom = Sheet("Bovenlade bodem U" + unitNumber, drawerMaterial, bottomWidth, bottomDepth);
            var drawerSideLeft = Sheet("Bovenlade zijde links U" + unitNumber, drawerMaterial, sideLength, frontHeight);
            var drawerSideRight = Sheet("Bovenlade zijde rechts U" + unitNumber, drawerMaterial, sideLength, frontHeight);
            var drawerBack = Sheet("Bovenlade achter U" + unitNumber, drawerMaterial, bottomWidth, frontHeight);
            AddDrawerBottomGroove(drawerSideLeft, drawerMaterial);
            AddDrawerBottomGroove(drawerSideRight, drawerMaterial);
            AddDrawerBackGroove(drawerSideLeft, drawerMaterial);
            AddDrawerBackGroove(drawerSideRight, drawerMaterial);
            AddDrawerBottomGroove(drawerBack, drawerMaterial);
            AddDrawerAssemblyHoles(config, drawerFront, drawerBottom, drawerSideLeft, drawerSideRight, drawerBack, boxWidth, drawerT);
            AddDrawerRailHoles(drawerSideLeft, config);
            AddDrawerRailHoles(drawerSideRight, config);
            AddSheet(model, drawerBottom, centerX, bottomY + DrawerGrooveBottomOffsetMm() + drawerT / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerBack, centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
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

        private static double TopDrawerHeight(CabinetConfig config, double bodyHeight)
        {
            return config.IncludeFullWidthTopDrawer ? Math.Min(config.FullWidthTopDrawerHeightMm, bodyHeight - config.PlinthHeightMm - 80) : 0;
        }

        private static double DrawerShelfZoneTop(CabinetConfig config, double bodyHeight)
        {
            var topDrawerHeight = TopDrawerHeight(config, bodyHeight);
            return topDrawerHeight > 0 ? bodyHeight - topDrawerHeight : bodyHeight;
        }

        private static double DrawerBottomY(CabinetConfig config, int drawerIndex, double drawerHeight, double shelfZoneTop)
        {
            if (IsTopStart(config.ShelfStartMode))
            {
                return shelfZoneTop - config.DoorGapMm - drawerHeight - drawerIndex * (drawerHeight + config.DoorGapMm);
            }

            return config.PlinthHeightMm + config.DoorGapMm + drawerIndex * (drawerHeight + config.DoorGapMm);
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

        private static void AddDividerGroovesToHorizontalSheet(SheetPart sheet, CabinetConfig config, double totalWidth, double depth, string prefix)
        {
            if (sheet == null || config.UnitCount <= 1) return;
            var materialThickness = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var grooveWidth = Math.Min(sheet.LengthMm - 2, materialThickness + AlignmentGrooveClearanceMm());
            var grooveDepth = AlignmentGrooveDepthMm(sheet);
            var unitWidth = totalWidth / config.UnitCount;
            for (var i = 1; i < config.UnitCount; i++)
            {
                var dividerCenterX = unitWidth * i;
                AddPocket(
                    sheet,
                    prefix + " " + i.ToString(CultureInfo.InvariantCulture),
                    dividerCenterX - grooveWidth / 2.0,
                    0,
                    grooveWidth,
                    Math.Min(depth, sheet.WidthMm),
                    grooveDepth,
                    "3mm verdiepte positioneergroef voor kopse kant tussenschot");
            }
        }

        private static void AddBottomAlignmentGrooves(SheetPart bottom, CabinetConfig config, int unitNumber)
        {
            if (bottom == null) return;
            var materialThickness = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var grooveWidth = Math.Min(bottom.LengthMm / 2.0, materialThickness + AlignmentGrooveClearanceMm());
            var grooveDepth = AlignmentGrooveDepthMm(bottom);
            var note = "3mm verdiepte montagerabat voor zijwand/tussenschot; prototype bij bodemplaat per unit";
            AddPocket(bottom, "Linker staander-rabat", 0, 0, grooveWidth, bottom.WidthMm, grooveDepth, note);
            AddPocket(bottom, "Rechter staander-rabat", bottom.LengthMm - grooveWidth, 0, grooveWidth, bottom.WidthMm, grooveDepth, note);
        }

        private static void AddDrawerBottomGroove(SheetPart panel, Material drawerMaterial)
        {
            if (panel == null || drawerMaterial == null) return;
            var grooveHeight = Math.Min(panel.WidthMm - 2.0, drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            if (grooveHeight <= 0) return;
            AddPocket(
                panel,
                "Ladebodem rabat",
                0,
                DrawerGrooveBottomOffsetMm(),
                panel.LengthMm,
                grooveHeight,
                DrawerGrooveDepthMm(),
                "3mm verdiepte groef zodat ladebodem in zij-/achterplaat valt");
        }

        private static void AddDrawerBackGroove(SheetPart sidePanel, Material drawerMaterial)
        {
            if (sidePanel == null || drawerMaterial == null) return;
            var grooveWidth = Math.Min(sidePanel.LengthMm - 2.0, drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            if (grooveWidth <= 0) return;
            AddPocket(
                sidePanel,
                "Ladeachter rabat",
                sidePanel.LengthMm - grooveWidth,
                0,
                grooveWidth,
                sidePanel.WidthMm,
                DrawerGrooveDepthMm(),
                "3mm verdiept rabat zodat ladeachter in de zijplaat valt");
        }

        private static void AddDrawerFrontGrooves(SheetPart front, double boxWidth, Material drawerMaterial)
        {
            if (front == null || drawerMaterial == null) return;
            var grooveWidth = Math.Min(front.LengthMm / 3.0, drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            var grooveHeight = Math.Min(front.WidthMm - 2.0 * DrawerGrooveBottomOffsetMm(), drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            var sideInset = Math.Max(0, (front.LengthMm - boxWidth) / 2.0);
            var verticalY = DrawerGrooveBottomOffsetMm();
            var verticalHeight = Math.Max(10, front.WidthMm - 2.0 * DrawerGrooveBottomOffsetMm());
            AddPocket(
                front,
                "Ladefront linker zij-rabat",
                sideInset,
                verticalY,
                grooveWidth,
                verticalHeight,
                DrawerGrooveDepthMm(),
                "3mm verdiept rabat voor linker ladezijde in binnenkant front");
            AddPocket(
                front,
                "Ladefront rechter zij-rabat",
                front.LengthMm - sideInset - grooveWidth,
                verticalY,
                grooveWidth,
                verticalHeight,
                DrawerGrooveDepthMm(),
                "3mm verdiept rabat voor rechter ladezijde in binnenkant front");
            AddPocket(
                front,
                "Ladefront bodem-rabat",
                sideInset,
                DrawerGrooveBottomOffsetMm(),
                Math.Max(10, front.LengthMm - 2.0 * sideInset),
                grooveHeight,
                DrawerGrooveDepthMm(),
                "3mm verdiept rabat voor ladebodem in binnenkant front");
        }

        private static void AddWorktopToUprightHoles(SheetPart worktop, CabinetConfig config, double topDepth)
        {
            if (worktop == null || config == null) return;
            var diameter = AssemblyHoleDiameter(config);
            var unitWidth = config.WidthMm / config.UnitCount;
            var edgeInset = 45.0;
            var zStart = edgeInset;
            var zEnd = Math.Max(zStart, Math.Min(topDepth, config.DepthMm) - edgeInset);

            for (var i = 0; i <= config.UnitCount; i++)
            {
                var localX = unitWidth * i;
                if (i == 0) localX = (config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm) / 2.0;
                else if (i == config.UnitCount) localX = config.WidthMm - (config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm) / 2.0;
                AddMountingLine(worktop, localX, zStart, localX, zEnd, diameter, 260, "werkblad naar staander " + i.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AddPlinthToUprightHoles(SheetPart plinth, CabinetConfig config)
        {
            if (plinth == null || config == null) return;
            var diameter = AssemblyHoleDiameter(config);
            var unitWidth = config.WidthMm / config.UnitCount;
            var sideT = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var ys = PatternPositions(plinth.WidthMm, 35, 150, 2);

            for (var i = 0; i <= config.UnitCount; i++)
            {
                var x = unitWidth * i;
                if (i == 0) x = sideT / 2.0;
                else if (i == config.UnitCount) x = config.WidthMm - sideT / 2.0;
                foreach (var y in ys)
                {
                    AddUniqueCabinetHole(plinth, x, y, diameter, "plint naar staander " + i.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void AddDrawerAssemblyHoles(
            CabinetConfig config,
            SheetPart drawerFront,
            SheetPart drawerBottom,
            SheetPart drawerSideLeft,
            SheetPart drawerSideRight,
            SheetPart drawerBack,
            double boxWidth,
            double drawerThickness)
        {
            var diameter = AssemblyHoleDiameter(config);
            var frontSideInset = Math.Max(0, (drawerFront.LengthMm - boxWidth) / 2.0);
            var leftSideX = frontSideInset + drawerThickness / 2.0;
            var rightSideX = drawerFront.LengthMm - frontSideInset - drawerThickness / 2.0;
            var frontYs = PatternPositions(drawerFront.WidthMm, 32, 95, drawerFront.WidthMm > 135 ? 3 : 2);
            foreach (var y in frontYs)
            {
                AddUniqueCabinetHole(drawerFront, leftSideX, y, diameter, "ladefront naar linker zijkant");
                AddUniqueCabinetHole(drawerFront, rightSideX, y, diameter, "ladefront naar rechter zijkant");
            }

            var sideYs = PatternPositions(drawerSideLeft.WidthMm, 32, 95, drawerSideLeft.WidthMm > 135 ? 3 : 2);
            var backX = Math.Max(20, drawerSideLeft.LengthMm - drawerThickness / 2.0);
            foreach (var y in sideYs)
            {
                AddUniqueCabinetHole(drawerSideLeft, backX, y, diameter, "ladezijde links naar achterzijde");
                AddUniqueCabinetHole(drawerSideRight, backX, y, diameter, "ladezijde rechts naar achterzijde");
            }

            var bottomY = DrawerGrooveBottomOffsetMm() + drawerThickness / 2.0;
            var bottomInset = Math.Max(30, drawerThickness + 14);
            AddMountingLine(drawerFront, frontSideInset + bottomInset, bottomY, drawerFront.LengthMm - frontSideInset - bottomInset, bottomY, diameter, 180, "ladefront naar ladebodem");
            AddMountingLine(drawerBack, bottomInset, bottomY, drawerBack.LengthMm - bottomInset, bottomY, diameter, 180, "ladeachter naar ladebodem");
            AddMountingLine(drawerSideLeft, bottomInset, bottomY, drawerSideLeft.LengthMm - bottomInset, bottomY, diameter, 180, "linker ladezijde naar ladebodem");
            AddMountingLine(drawerSideRight, bottomInset, bottomY, drawerSideRight.LengthMm - bottomInset, bottomY, diameter, 180, "rechter ladezijde naar ladebodem");
        }

        private static void AddDividerGroovesToBackPanel(SheetPart backPanel, CabinetConfig config, double bodyHeight)
        {
            if (backPanel == null || config.UnitCount <= 1) return;
            var materialThickness = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var grooveWidth = Math.Min(backPanel.LengthMm - 2, materialThickness + AlignmentGrooveClearanceMm());
            var grooveDepth = AlignmentGrooveDepthMm(backPanel);
            var unitWidth = config.WidthMm / config.UnitCount;
            for (var i = 1; i < config.UnitCount; i++)
            {
                var dividerCenterX = unitWidth * i;
                AddPocket(
                    backPanel,
                    "Tussenschot achterwandgroef " + i.ToString(CultureInfo.InvariantCulture),
                    dividerCenterX - grooveWidth / 2.0,
                    0,
                    grooveWidth,
                    Math.Min(bodyHeight, backPanel.WidthMm),
                    grooveDepth,
                    "3mm verdiepte positioneergroef voor achterzijde tussenschot");
            }
        }

        private static void AddBackPanelMountingHoles(SheetPart backPanel, CabinetConfig config, double bodyHeight)
        {
            if (backPanel == null || config == null) return;
            var diameter = AssemblyHoleDiameter(config);
            var unitWidth = config.WidthMm / config.UnitCount;
            var sideT = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var yStart = Math.Max(45, config.PlinthHeightMm + 35);
            var yEnd = Math.Max(yStart, Math.Min(backPanel.WidthMm - 45, bodyHeight - 45));

            for (var i = 0; i <= config.UnitCount; i++)
            {
                var x = unitWidth * i;
                if (i == 0) x = sideT / 2.0;
                else if (i == config.UnitCount) x = config.WidthMm - sideT / 2.0;
                AddMountingLine(backPanel, x, yStart, x, yEnd, diameter, 260, "achterwand naar staander " + i.ToString(CultureInfo.InvariantCulture));
            }

            AddMountingLine(backPanel, 45, backPanel.WidthMm - 35, backPanel.LengthMm - 45, backPanel.WidthMm - 35, diameter, 300, "achterwand naar werkblad");
            AddMountingLine(backPanel, 45, config.PlinthHeightMm + sideT / 2.0, backPanel.LengthMm - 45, config.PlinthHeightMm + sideT / 2.0, diameter, 300, "achterwand naar bodemlijn");
        }

        private static void AddDoorHingeHoles(SheetPart door, CabinetConfig config, CabinetDoorHand hand)
        {
            if (door == null || hand == CabinetDoorHand.Geen) return;
            var diameter = AssemblyHoleDiameter(config);
            var hingeInsetX = 35.0;
            var screwSpacing = 32.0;
            var x = hand == CabinetDoorHand.Links ? hingeInsetX : door.LengthMm - hingeInsetX;
            var yPositions = new List<double> { Math.Min(120.0, door.WidthMm / 2.0), Math.Max(120.0, door.WidthMm - 120.0) };
            foreach (var y in yPositions)
            {
                AddUniqueCabinetHole(door, x, y - screwSpacing / 2.0, diameter, "scharnier op deurblad", SheetHoleSupportKind.HingeScrew);
                AddUniqueCabinetHole(door, x, y + screwSpacing / 2.0, diameter, "scharnier op deurblad", SheetHoleSupportKind.HingeScrew);
            }
        }

        private static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, string note)
        {
            if (sheet == null || length <= 0 || width <= 0 || depth <= 0) return;
            var safeX = Math.Max(0, Math.Min(sheet.LengthMm, x));
            var safeY = Math.Max(0, Math.Min(sheet.WidthMm, y));
            var safeLength = Math.Max(0, Math.Min(length, sheet.LengthMm - safeX));
            var safeWidth = Math.Max(0, Math.Min(width, sheet.WidthMm - safeY));
            if (safeLength <= 0 || safeWidth <= 0) return;
            sheet.Pockets.Add(new SheetPocket
            {
                Name = name,
                Xmm = Math.Round(safeX, 3),
                Ymm = Math.Round(safeY, 3),
                LengthMm = Math.Round(safeLength, 3),
                WidthMm = Math.Round(safeWidth, 3),
                DepthMm = Math.Round(Math.Min(depth, Math.Max(0.1, sheet.Material.ThicknessMm - 0.1)), 3),
                Face = OperationFace.CenterPlane,
                DepthMode = OperationDepthMode.PocketFromFace,
                Note = note
            });
        }

        private static double AlignmentGrooveDepthMm(SheetPart sheet)
        {
            if (sheet == null || sheet.Material == null) return 3.0;
            return Math.Min(3.0, Math.Max(0.1, sheet.Material.ThicknessMm - 0.1));
        }

        private static double AlignmentGrooveClearanceMm()
        {
            return 1.0;
        }

        private static double DrawerGrooveDepthMm()
        {
            return 3.0;
        }

        private static double DrawerGrooveClearanceMm()
        {
            return 0.8;
        }

        private static double DrawerGrooveBottomOffsetMm()
        {
            return 2.0;
        }

        private static double BlindOutsidePanelHoleDepthMm()
        {
            return 12.0;
        }

        private static double AssemblyHoleDiameter(CabinetConfig config)
        {
            return 4.5;
        }

        private static List<double> PatternPositions(double length, double edgeInset, double maxSpacing, int minimumCount)
        {
            var positions = new List<double>();
            if (length <= 0) return positions;
            var start = Math.Min(Math.Max(8, edgeInset), length / 2.0);
            var end = Math.Max(start, length - start);
            var usable = Math.Max(0, end - start);
            var count = Math.Max(minimumCount, (int)System.Math.Ceiling(usable / Math.Max(1, maxSpacing)) + 1);
            if (count <= 1)
            {
                positions.Add(System.Math.Round(length / 2.0, 3));
                return positions;
            }

            for (var i = 0; i < count; i++)
            {
                var t = (double)i / (count - 1);
                positions.Add(System.Math.Round(start + usable * t, 3));
            }

            return positions;
        }

        private static void AddMountingLine(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing, string note)
        {
            var length = System.Math.Sqrt(System.Math.Pow(x2 - x1, 2) + System.Math.Pow(y2 - y1, 2));
            var segments = System.Math.Max(1, (int)System.Math.Ceiling(length / System.Math.Max(1, maxSpacing)));
            for (var i = 0; i <= segments; i++)
            {
                var t = (double)i / segments;
                AddUniqueCabinetHole(sheet, x1 + (x2 - x1) * t, y1 + (y2 - y1) * t, diameter, note);
            }
        }

        private static void AddUniqueCabinetHole(SheetPart sheet, double x, double y, double diameter, string note)
        {
            AddUniqueCabinetHole(sheet, x, y, diameter, note, SheetHoleSupportKind.PanelScrew);
        }

        private static void AddUniqueCabinetHole(SheetPart sheet, double x, double y, double diameter, string note, SheetHoleSupportKind supportKind)
        {
            if (sheet == null) return;
            x = System.Math.Round(System.Math.Max(6, System.Math.Min(sheet.LengthMm - 6, x)), 3);
            y = System.Math.Round(System.Math.Max(6, System.Math.Min(sheet.WidthMm - 6, y)), 3);
            if (HasHoleAt(sheet, x, y, diameter)) return;
            sheet.Holes.Add(new SheetHole
            {
                Name = "Montagegat " + note + " " + (sheet.Holes.Count + 1),
                Xmm = x,
                Ymm = y,
                DiameterMm = diameter,
                DepthMm = 0,
                Face = OperationFace.CenterPlane,
                DepthMode = OperationDepthMode.Through,
                Countersunk = false,
                SupportKind = supportKind
            });
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
            if (IsAnchoredShelfStart(config.ShelfStartMode))
            {
                return AnchoredShelfHeights(count, config, min, max);
            }

            var heights = new List<double>();
            for (var i = 1; i <= count; i++)
            {
                var target = min + (max - min) * i / (count + 1);
                heights.Add(SnapShelfHeightToSupportPattern(target, config, min, max));
            }

            return heights;
        }

        private static IEnumerable<double> AnchoredShelfHeights(int count, CabinetConfig config, double min, double max)
        {
            var heights = new List<double>();
            if (count <= 0 || max <= min) return heights;

            var span = max - min;
            var pitch = Math.Min(160.0, span / (count + 1));
            var startTop = string.Equals((config.ShelfStartMode ?? "").Trim(), "top", StringComparison.OrdinalIgnoreCase);

            for (var i = 1; i <= count; i++)
            {
                var target = startTop
                    ? max - pitch * (count - i + 1)
                    : min + pitch * i;
                var snapped = SnapShelfHeightToSupportPattern(target, config, min, max);
                if (snapped > min && snapped < max && !ContainsNear(heights, snapped))
                {
                    heights.Add(snapped);
                }
            }

            heights.Sort();
            return heights;
        }

        private static bool IsAnchoredShelfStart(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            return value == "top" || value == "boven" || value == "bottom" || value == "onder";
        }

        private static bool IsTopStart(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            return value == "top" || value == "boven";
        }

        private static bool ContainsNear(List<double> values, double value)
        {
            foreach (var existing in values)
            {
                if (Math.Abs(existing - value) < 1.0) return true;
            }

            return false;
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
            AddRailHoleLine(panel, rail.CabinetHolePositionsMm, rail.HoleCount, rail.FirstHoleOffsetMm, rail.HoleSpacingMm, rail.HoleDiameterMm, railY, "Bovenlade railgat U" + unitNumber, BlindDepthForOutsidePanel(panel));
        }

        private static void AddRailHolesForAdjacentUnit(SheetPart panel, CabinetConfig config, int unitNumber, double bodyHeight)
        {
            if (unitNumber < 1 || unitNumber > config.UnitCount) return;
            var unit = GetUnit(config, unitNumber);
            if (unit.DrawerCount <= 0) return;

            var rail = config.DrawerRail;
            var drawerHeight = Math.Max(80, unit.DrawerHeightMm);
            var shelfZoneTop = DrawerShelfZoneTop(config, bodyHeight);
            for (var drawerIndex = 0; drawerIndex < unit.DrawerCount; drawerIndex++)
            {
                var bottomY = DrawerBottomY(config, drawerIndex, drawerHeight, shelfZoneTop);
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
                        DepthMm = BlindDepthForOutsidePanel(panel),
                        Face = BlindFaceForOutsidePanel(panel),
                        DepthMode = BlindDepthForOutsidePanel(panel) > 0 ? OperationDepthMode.BlindFromFace : OperationDepthMode.Through,
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

            AddRailHoleLine(panel, rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm, rail.DrawerHoleDiameterMm, y, "Laderailgat", 0);
        }

        private static void AddRailHoleLine(SheetPart panel, string explicitPositions, int count, double firstOffset, double spacing, double diameter, double y, string name, double depthMm)
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
                    DepthMm = depthMm,
                    Face = depthMm > 0 ? BlindFaceForOutsidePanel(panel) : OperationFace.CenterPlane,
                    DepthMode = depthMm > 0 ? OperationDepthMode.BlindFromFace : OperationDepthMode.Through,
                    Countersunk = false,
                    SupportKind = SheetHoleSupportKind.ProfileNut
                });
            }
        }

        private static double BlindDepthForOutsidePanel(SheetPart panel)
        {
            if (panel == null || panel.Name == null) return 0;
            if (panel.Name.StartsWith("Zijwand links", StringComparison.OrdinalIgnoreCase) ||
                panel.Name.StartsWith("Zijwand rechts", StringComparison.OrdinalIgnoreCase))
            {
                return BlindOutsidePanelHoleDepthMm();
            }

            return 0;
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
                AddShelfSupportHole(panel, frontX, y, support.HoleDiameterMm, index++, BlindDepthForOutsidePanel(panel));
                AddShelfSupportHole(panel, backX, y, support.HoleDiameterMm, index++, BlindDepthForOutsidePanel(panel));
            }
        }

        private static void AddShelfSupportHole(SheetPart panel, double x, double y, double diameter, int index, double depthMm)
        {
            if (HasHoleAt(panel, x, y, diameter)) return;
            panel.Holes.Add(new SheetHole
            {
                Name = "Legplankdragergat " + index,
                Xmm = Math.Round(x, 3),
                Ymm = Math.Round(y, 3),
                DiameterMm = diameter,
                DepthMm = depthMm,
                Face = depthMm > 0 ? BlindFaceForOutsidePanel(panel) : OperationFace.CenterPlane,
                DepthMode = depthMm > 0 ? OperationDepthMode.BlindFromFace : OperationDepthMode.Through,
                Countersunk = false,
                SupportKind = SheetHoleSupportKind.ProfileNut
            });
        }

        private static OperationFace BlindFaceForOutsidePanel(SheetPart panel)
        {
            if (panel == null || panel.Name == null) return OperationFace.CenterPlane;
            if (panel.Name.StartsWith("Zijwand links", StringComparison.OrdinalIgnoreCase)) return OperationFace.NegativeX;
            if (panel.Name.StartsWith("Zijwand rechts", StringComparison.OrdinalIgnoreCase)) return OperationFace.PositiveX;
            return OperationFace.CenterPlane;
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
            var panelScrewCount = CountPanelScrewHoles(model);
            if (panelScrewCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "Houtschroef 4x45 hout-op-hout",
                    ArticleNumber = "WOODSCREW_4X45",
                    Quantity = panelScrewCount,
                    Unit = "st",
                    Note = "Voor plaat-op-plaat verbindingen; gebaseerd op gegenereerde 4,5mm montagegaten"
                });
            }

            var hingeScrewCount = CountHingeScrews(model);
            if (hingeScrewCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "Houtschroef 4x12 scharnier-op-hout",
                    ArticleNumber = "WOODSCREW_4X12_HINGE",
                    Quantity = hingeScrewCount,
                    Unit = "st",
                    Note = "Korte schroeven voor scharnieren op houten plaatmateriaal, gerekend 4 per scharnier"
                });
            }
        }

        private static int CountHingeScrews(WorkbenchModel model)
        {
            var count = 0;
            if (model == null) return count;
            foreach (var item in model.Hardware)
            {
                if (item == null || item.ArticleNumber == null) continue;
                if (item.ArticleNumber.IndexOf("HINGE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count += Math.Max(0, item.Quantity) * 4;
                }
            }

            return count;
        }

        private static int CountPanelScrewHoles(WorkbenchModel model)
        {
            var count = 0;
            if (model == null) return count;
            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    if (hole.SupportKind == SheetHoleSupportKind.PanelScrew)
                    {
                        count += Math.Max(1, sheet.Quantity);
                    }
                }
            }

            return count;
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
