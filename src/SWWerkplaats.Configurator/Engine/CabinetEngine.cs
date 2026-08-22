using System;
using System.Collections.Generic;
using System.Globalization;
using SWWerkplaats.Configurator.Drawing;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class CabinetEngine
    {
        private const double DefaultSlidingDoorOverlapMm = 25.0;
        private const double DefaultSlidingDoorFreeSpaceBehindMm = 10.0;
        private const double DefaultSlidingDoorTrackCenterSpacingMm = 18.0;
        private const double DefaultSlidingDoorTopProfileHeightMm = 25.0;
        private const double DefaultSlidingDoorBottomProfileHeightMm = 18.0;
        private const double DefaultSlidingDoorTapeThicknessMm = 1.0;
        private const double DefaultSlidingDoorTopProfileDepthMm = 15.0;
        private const double DefaultSlidingDoorBottomProfileDepthMm = 18.0;
        private const double DefaultSlidingDoorProfileWallThicknessMm = 2.0;

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
            var frontZ = -config.DepthMm / 2.0;
            var backZ = config.DepthMm / 2.0;
            var backThickness = config.IncludeBackPanel ? back.ThicknessMm : 0;
            var topDepth = config.DepthMm + backThickness;
            var topCenterZ = backThickness / 2.0;
            var backAlignmentDepth = config.IncludeBackPanel ? ProductDrawingStrategy.GrooveDepthForMaterial(back) : 0;
            var plinthNotchDepth = Math.Min(config.DepthMm - 1, config.PlinthDepthMm + t + 2.0);
            var topDrawerHeight = TopDrawerHeight(config, bodyHeight);
            var shelfZoneTop = topDrawerHeight > 0 ? bodyHeight - topDrawerHeight : bodyHeight;

            var worktop = Sheet("Werkblad", top, config.WidthMm, topDepth);
            AddDividerGroovesToHorizontalSheet(worktop, config, config.WidthMm, config.DepthMm + backAlignmentDepth, "Tussenschot in werkblad");
            AddWorktopToUprightHoles(worktop, config, topDepth);
            AddSheet(model, worktop, 0, config.WorktopHeightMm - topT / 2.0, topCenterZ, AssemblyOrientation.SheetHorizontal);
            var plinth = Sheet("Plint voor", carcass, config.WidthMm, config.PlinthHeightMm);
            AddPlinthToUprightHoles(plinth, config);
            AddSheet(model, plinth, 0, config.PlinthHeightMm / 2.0, frontZ + config.PlinthDepthMm + t / 2.0, AssemblyOrientation.SheetVerticalX);

            var sideDepth = config.IncludeBackPanel
                ? ProductDrawingStrategy.PlateSizeWithSingleGrooveInsertion(config.DepthMm, backAlignmentDepth)
                : config.DepthMm;
            var sideCenterZ = config.IncludeBackPanel
                ? ProductDrawingStrategy.CenterOffsetForSingleGrooveInsertion(backAlignmentDepth)
                : 0;

            var leftSide = SidePanel("Zijwand links", carcass, sideDepth, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            AddBottomReceivingGrooveToUpright(leftSide, config);
            AddRailHolesForPanel(leftSide, config, 0, bodyHeight);
            AddTopDrawerRailHolesForPanel(leftSide, config, 0, bodyHeight);
            AddAdjustableShelfHolesForPanel(leftSide, config, 0, bodyHeight, shelfZoneTop);
            AddBottomToUprightHoles(leftSide, config, 1);
            AddSheet(model, leftSide, -config.WidthMm / 2.0 + t / 2.0, bodyHeight / 2.0, sideCenterZ, AssemblyOrientation.SheetVerticalZ);

            var rightSide = SidePanel("Zijwand rechts", carcass, sideDepth, bodyHeight, plinthNotchDepth, config.PlinthHeightMm);
            rightSide.MirrorInNestingX = true;
            AddBottomReceivingGrooveToUpright(rightSide, config);
            AddRailHolesForPanel(rightSide, config, config.UnitCount, bodyHeight);
            AddTopDrawerRailHolesForPanel(rightSide, config, config.UnitCount, bodyHeight);
            AddAdjustableShelfHolesForPanel(rightSide, config, config.UnitCount, bodyHeight, shelfZoneTop);
            AddBottomToUprightHoles(rightSide, config, config.UnitCount);
            AddSheet(model, rightSide, config.WidthMm / 2.0 - t / 2.0, bodyHeight / 2.0, sideCenterZ, AssemblyOrientation.SheetVerticalZ);

            for (var i = 1; i < config.UnitCount; i++)
            {
                var x = -config.WidthMm / 2.0 + unitWidth * i;
                var dividerHeight = bodyHeight + AlignmentGrooveDepthMm(worktop);
                var dividerDepth = config.DepthMm + backAlignmentDepth;
                var divider = SidePanel("Tussenschot " + i.ToString(CultureInfo.InvariantCulture), carcass, dividerDepth, dividerHeight, plinthNotchDepth, config.PlinthHeightMm);
                AddBottomReceivingGrooveToUpright(divider, config);
                AddSlidingDoorPassThroughToDivider(divider, config, i, bodyHeight, shelfZoneTop);
                AddRailHolesForPanel(divider, config, i, bodyHeight);
                AddTopDrawerRailHolesForPanel(divider, config, i, bodyHeight);
                AddAdjustableShelfHolesForPanel(divider, config, i, bodyHeight, shelfZoneTop);
                AddBottomToUprightHoles(divider, config, i);
                AddBottomToUprightHoles(divider, config, i + 1);
                AddSheet(model, divider, x, dividerHeight / 2.0, backAlignmentDepth / 2.0, AssemblyOrientation.SheetVerticalZ);
            }

            for (var i = 0; i < config.UnitCount; i++)
            {
                var bottomInsertDepth = ProductDrawingStrategy.GrooveDepthForMaterial(carcass);
                var bottomFit = BottomFitForUnit(config, i, t, bottomInsertDepth);
                var bottomDepth = config.IncludeBackPanel
                    ? ProductDrawingStrategy.PlateSizeWithSingleGrooveInsertion(config.DepthMm, bottomInsertDepth)
                    : config.DepthMm;
                var bottomCenterZ = config.IncludeBackPanel ? ProductDrawingStrategy.CenterOffsetForSingleGrooveInsertion(bottomInsertDepth) : 0;
                var bottom = Sheet("Bodem U" + (i + 1).ToString(CultureInfo.InvariantCulture), carcass, bottomFit.WidthMm, bottomDepth);
                AddBottomToPlinthHoles(bottom, config, i + 1);
                AddSheet(model, bottom, bottomFit.CenterXmm, config.PlinthHeightMm + t / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
            }

            if (config.IncludeBackPanel)
            {
                var backPanel = Sheet("Achterwand", back, config.WidthMm, bodyHeight);
                AddSideReceivingGroovesToBackPanel(backPanel, config, bodyHeight);
                AddDividerGroovesToBackPanel(backPanel, config, bodyHeight);
                AddBottomReceivingGrooveToBackPanel(backPanel, config, bodyHeight);
                AddBackPanelMountingHoles(backPanel, config, bodyHeight);
                AddSheet(model, backPanel, 0, bodyHeight / 2.0, backZ + back.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
            }

            for (var i = 0; i < config.UnitCount; i++)
            {
                var unit = GetUnit(config, i + 1);
                var bayFit = BottomFitForUnit(config, i, t, 0);
                var unitCenterX = bayFit.CenterXmm;
                var clearWidth = Math.Max(20, bayFit.WidthMm - 2.0 * bayFitClearance);
                if (topDrawerHeight > 0)
                {
                    BuildTopDrawerForUnit(model, config, i + 1, unitCenterX, clearWidth, innerDepth, bodyHeight, topDrawerHeight, frontZ, drawer, front, carcass);
                }

                BuildUnit(model, config, unit, i + 1, unitCenterX, clearWidth, innerDepth, bodyHeight, shelfZoneTop, frontZ, drawer, front, carcass);
            }

            BuildSlidingDoorRanges(model, config, innerDepth, bodyHeight, shelfZoneTop, frontZ, carcass);
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
            var shelfZone = ShelfZoneForUnit(unit, config, shelfZoneTop);
            var shelfFrontInset = ShelfFrontInset(config, unit, innerDepth);
            var shelfDepth = Math.Max(80, innerDepth - shelfFrontInset);
            var shelfCenterZ = shelfFrontInset / 2.0;
            foreach (var shelfHeight in ShelfHeights(unit, config, shelfZone.MinMm, shelfZone.MaxMm, unit.DrawerCount > 0))
            {
                var shelf = Sheet("Legplank U" + unitNumber + " H" + shelfHeight.ToString("0", CultureInfo.InvariantCulture), carcassMaterial, clearWidth, shelfDepth);
                AddShelfBracketMountingHoles(shelf, config);
                AddSheet(model, shelf, centerX, shelfHeight, shelfCenterZ, AssemblyOrientation.SheetHorizontal);
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
                    var boxFrontZ = InnerFrontZ(frontZ, frontMaterial);
                    var boxCenterZ = boxFrontZ - frontPocketDepth + sideLength / 2.0;
                    bottomWidth = Math.Max(80, bottomWidth + 2.0 * DrawerGrooveDepthMm());
                    bottomDepth = Math.Max(80, bottomDepth + 2.0 * DrawerGrooveDepthMm());
                    var bottomCenterZ = boxFrontZ - frontPocketDepth + bottomDepth / 2.0;
                    var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

                    var drawerFront = Sheet("Ladefront U" + unitNumber + "-" + (drawerIndex + 1), frontMaterial, frontWidth, frontHeight);
                    AddDrawerFrontGrooves(drawerFront, boxWidth, drawerMaterial);
                    AddDrawerPullCutout(drawerFront, config);
                    AddSheet(model, drawerFront, centerX, centerY, FlushFrontCenterZ(frontZ, frontMaterial), AssemblyOrientation.SheetVerticalX);
                    var drawerBottom = Sheet("Ladebodem U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, bottomDepth);
                    var drawerSideLeft = Sheet("Ladezijde links U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, sideLength, frontHeight);
                    var drawerSideRight = Sheet("Ladezijde rechts U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, sideLength, frontHeight);
                    drawerSideRight.MirrorInNestingX = true;
                    var drawerBack = Sheet("Ladeachter U" + unitNumber + "-" + (drawerIndex + 1), drawerMaterial, bottomWidth, frontHeight);
                    AddDrawerBottomGroove(drawerSideLeft, drawerMaterial);
                    AddDrawerBottomGroove(drawerSideRight, drawerMaterial);
                    AddDrawerBackGroove(drawerSideLeft, drawerMaterial);
                    AddDrawerBackGroove(drawerSideRight, drawerMaterial);
                    AddDrawerBottomGroove(drawerBack, drawerMaterial);
                    AddDrawerAssemblyHoles(config, drawerFront, drawerBottom, drawerSideLeft, drawerSideRight, drawerBack, boxWidth, drawerT);
                    AddDrawerRailHoles(drawerSideLeft, config);
                    AddDrawerRailHoles(drawerSideRight, config);
                    var drawerBottomCenterY = centerY - frontHeight / 2.0 + DrawerGrooveBottomOffsetMm() + drawerT / 2.0;
                    AddSheet(model, drawerBottom, centerX, drawerBottomCenterY, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
                    AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
                    AddSheet(model, drawerBack, centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddRailHardware(model, config, unitNumber, drawerIndex + 1);
                }
            }

            if (unit.Door != CabinetDoorHand.Geen)
            {
                var doorZone = DoorPanelZone(unit, config, shelfZoneTop);
                if (doorZone.HeightMm >= 80)
                {
                    var door = Sheet("Draaideur " + unit.Door + " U" + unitNumber, frontMaterial, clearWidth - config.DoorGapMm, doorZone.HeightMm);
                    AddDoorHingeHoles(door, config, unit.Door);
                    AddSheet(model, door, centerX, doorZone.CenterYmm, frontZ - frontMaterial.ThicknessMm / 2.0, AssemblyOrientation.SheetVerticalX);
                    model.Hardware.Add(new HardwareItem { Name = "Scharnieren " + unit.Door, ArticleNumber = "HINGE_TEMPLATE", Quantity = 2, Unit = "st", Note = "Voor draaideur unit " + unitNumber });
                }
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
            var boxFrontZ = InnerFrontZ(frontZ, frontMaterial);
            var boxCenterZ = boxFrontZ - frontPocketDepth + sideLength / 2.0;
            bottomWidth = Math.Max(80, bottomWidth + 2.0 * DrawerGrooveDepthMm());
            bottomDepth = Math.Max(80, bottomDepth + 2.0 * DrawerGrooveDepthMm());
            var bottomCenterZ = boxFrontZ - frontPocketDepth + bottomDepth / 2.0;
            var backCenterZ = boxFrontZ + boxDepth - drawerT / 2.0;

            var drawerFront = Sheet("Bovenlade front U" + unitNumber, frontMaterial, frontWidth, frontHeight);
            AddDrawerFrontGrooves(drawerFront, boxWidth, drawerMaterial);
            AddDrawerPullCutout(drawerFront, config);
            AddSheet(model, drawerFront, centerX, centerY, FlushFrontCenterZ(frontZ, frontMaterial), AssemblyOrientation.SheetVerticalX);
            var drawerBottom = Sheet("Bovenlade bodem U" + unitNumber, drawerMaterial, bottomWidth, bottomDepth);
            var drawerSideLeft = Sheet("Bovenlade zijde links U" + unitNumber, drawerMaterial, sideLength, frontHeight);
            var drawerSideRight = Sheet("Bovenlade zijde rechts U" + unitNumber, drawerMaterial, sideLength, frontHeight);
            drawerSideRight.MirrorInNestingX = true;
            var drawerBack = Sheet("Bovenlade achter U" + unitNumber, drawerMaterial, bottomWidth, frontHeight);
            AddDrawerBottomGroove(drawerSideLeft, drawerMaterial);
            AddDrawerBottomGroove(drawerSideRight, drawerMaterial);
            AddDrawerBackGroove(drawerSideLeft, drawerMaterial);
            AddDrawerBackGroove(drawerSideRight, drawerMaterial);
            AddDrawerBottomGroove(drawerBack, drawerMaterial);
            AddDrawerAssemblyHoles(config, drawerFront, drawerBottom, drawerSideLeft, drawerSideRight, drawerBack, boxWidth, drawerT);
            AddDrawerRailHoles(drawerSideLeft, config);
            AddDrawerRailHoles(drawerSideRight, config);
            var drawerBottomCenterY = centerY - frontHeight / 2.0 + DrawerGrooveBottomOffsetMm() + drawerT / 2.0;
            AddSheet(model, drawerBottom, centerX, drawerBottomCenterY, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, centerY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerBack, centerX, centerY, backCenterZ, AssemblyOrientation.SheetVerticalX);
            AddRailHardware(model, config, unitNumber, 0);
        }

        private static SheetPart Sheet(string name, Material material, double length, double width)
        {
            return SheetDrawing.CreateSheet(name, material, length, width);
        }

        private static double DrawerBoxDepth(CabinetConfig config, double innerDepth)
        {
            return Math.Max(120, innerDepth - config.DrawerBackClearanceMm);
        }

        private static double ShelfFrontInset(CabinetConfig config, CabinetUnitConfig unit, double innerDepth)
        {
            if (config == null) return 0;
            var requested = Clamp(config.ShelfFrontInsetMm, 0, Math.Max(0, innerDepth - 80));
            if (unit != null && unit.SlidingDoors)
            {
                requested = Math.Max(requested, SlidingDoorRequiredShelfInset(config));
            }

            return Clamp(requested, 0, Math.Max(0, innerDepth - 80));
        }

        private static double SlidingDoorRequiredShelfInset(CabinetConfig config)
        {
            if (config == null) return 0;
            var doorThickness = MaterialThickness(config.SlidingDoorMaterial ?? config.FrontMaterial ?? config.CarcassMaterial);
            var rearTrackCenter = SlidingDoorTrackCenterFromFront(config, 1, doorThickness);
            var profileDepth = SlidingDoorBottomProfileDepth(config);
            var freeSpace = SlidingDoorFreeSpaceBehind(config);
            return rearTrackCenter + profileDepth / 2.0 + freeSpace;
        }

        private static void BuildSlidingDoorRanges(
            WorkbenchModel model,
            CabinetConfig config,
            double innerDepth,
            double bodyHeight,
            double shelfZoneTop,
            double frontZ,
            Material carcassMaterial)
        {
            if (model == null || config == null || !HasSlidingDoors(config)) return;

            foreach (var range in SlidingDoorRanges(config))
            {
                BuildSlidingDoorRange(model, config, range, innerDepth, bodyHeight, shelfZoneTop, frontZ, carcassMaterial);
            }
        }

        private static void BuildSlidingDoorRange(
            WorkbenchModel model,
            CabinetConfig config,
            UnitRange range,
            double innerDepth,
            double bodyHeight,
            double shelfZoneTop,
            double frontZ,
            Material carcassMaterial)
        {
            var doorMaterial = config.SlidingDoorMaterial ?? config.FrontMaterial ?? config.CarcassMaterial;
            var t = MaterialThickness(carcassMaterial);
            var doorThickness = MaterialThickness(doorMaterial);
            var overlap = SlidingDoorOverlap(config);
            var trackSpacing = SlidingDoorTrackCenterSpacing(config);
            var bottomProfileDepth = SlidingDoorBottomProfileDepth(config);
            var bottomProfileHeight = SlidingDoorBottomProfileHeight(config);
            var topProfileHeight = SlidingDoorTopProfileHeight(config);
            var tape = SlidingDoorTapeThickness(config);
            var latDepth = SlidingDoorTopLatDepthMm(config);
            var trackClearance = SlidingDoorTrackClearance(config, doorThickness);
            var profileWallThickness = SlidingDoorProfileWallThickness(config);
            var rangeLeft = UnitClearLeftX(config, range.Start, t);
            var rangeRight = UnitClearRightX(config, range.End, t);
            var rangeWidth = Math.Max(80, rangeRight - rangeLeft);
            var rangeCenterX = (rangeLeft + rangeRight) / 2.0;
            var zone = SlidingDoorOpeningZone(config, range, shelfZoneTop);
            if (zone.HeightMm < 140) return;

            var latTop = zone.CenterYmm + zone.HeightMm / 2.0;
            var latUnderside = latTop - t;
            var bottomPlateTop = config.PlinthHeightMm + t;
            var bottomGuideBottom = bottomPlateTop + tape;
            var bottomGuideTop = bottomGuideBottom + bottomProfileHeight;
            var doorBottom = Math.Max(bottomGuideBottom, bottomGuideTop - 8.0);
            var doorTop = Math.Max(doorBottom + 80, latUnderside - tape + 8.0);
            var doorHeight = Math.Min(zone.HeightMm, doorTop - doorBottom);
            var doorCenterY = doorBottom + doorHeight / 2.0;

            var lat = Sheet("Schuifdeur bovenlat U" + range.Start.ToString(CultureInfo.InvariantCulture) + "-" + range.End.ToString(CultureInfo.InvariantCulture), carcassMaterial, rangeWidth, latDepth);
            AddSlidingDoorTopLatHoles(lat, config, range, rangeLeft, t);
            AddSheet(model, lat, rangeCenterX, latTop - t / 2.0, frontZ + latDepth / 2.0, AssemblyOrientation.SheetHorizontal);

            for (var unitNumber = range.Start; unitNumber <= range.End; unitNumber++)
            {
                var fit = BottomFitForUnit(config, unitNumber - 1, t, 0);
                var leftExtra = unitNumber == range.Start ? 0 : overlap / 2.0;
                var rightExtra = unitNumber == range.End ? 0 : overlap / 2.0;
                var panelWidth = Math.Max(80, fit.WidthMm - 2.0 * config.DoorGapMm + leftExtra + rightExtra);
                var panelCenterX = fit.CenterXmm + (rightExtra - leftExtra) / 2.0;
                var trackIndex = (unitNumber - range.Start) % 2;
                var trackCenterFromFront = SlidingDoorTrackCenterFromFront(config, trackIndex, doorThickness);
                var panelCenterZ = frontZ + trackCenterFromFront;

                AddSheet(
                    model,
                    Sheet("Schuifdeur U" + unitNumber.ToString(CultureInfo.InvariantCulture), doorMaterial, panelWidth, doorHeight),
                    panelCenterX,
                    doorCenterY,
                    panelCenterZ,
                    AssemblyOrientation.SheetVerticalX);
            }

            AddSlidingDoorProfileVisual(model, "Schuifdeur onderprofiel voor U" + range.Start + "-" + range.End, rangeCenterX, bottomGuideBottom + bottomProfileHeight / 2.0, frontZ + SlidingDoorTrackCenterFromFront(config, 0, doorThickness), rangeWidth, bottomProfileDepth, bottomProfileHeight);
            AddSlidingDoorProfileVisual(model, "Schuifdeur onderprofiel achter U" + range.Start + "-" + range.End, rangeCenterX, bottomGuideBottom + bottomProfileHeight / 2.0, frontZ + SlidingDoorTrackCenterFromFront(config, 1, doorThickness), rangeWidth, bottomProfileDepth, bottomProfileHeight);
            AddSlidingDoorProfileVisual(model, "Schuifdeur bovenprofiel voor U" + range.Start + "-" + range.End, rangeCenterX, latUnderside - tape - topProfileHeight / 2.0, frontZ + profileWallThickness / 2.0, rangeWidth, profileWallThickness, topProfileHeight);
            AddSlidingDoorProfileVisual(model, "Schuifdeur bovenprofiel midden U" + range.Start + "-" + range.End, rangeCenterX, latUnderside - tape - topProfileHeight / 2.0, frontZ + profileWallThickness + trackClearance + profileWallThickness / 2.0, rangeWidth, profileWallThickness, topProfileHeight);
            AddSlidingDoorProfileVisual(model, "Schuifdeur bovenprofiel achter U" + range.Start + "-" + range.End, rangeCenterX, latUnderside - tape - topProfileHeight / 2.0, frontZ + 2.0 * profileWallThickness + 2.0 * trackClearance + profileWallThickness / 2.0, rangeWidth, profileWallThickness, topProfileHeight);

            model.Hardware.Add(new HardwareItem
            {
                Name = "Aluminium U-profiel onder 18x18x18x2 + tape",
                ArticleNumber = "SLIDING_BOTTOM_U_18",
                Quantity = 2,
                Unit = "st",
                Note = "Lengte " + rangeWidth.ToString("0", CultureInfo.InvariantCulture) + " mm voor schuifdeuren U" + range.Start + "-" + range.End + "; tape 1mm onder profiel"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Aluminium hoekprofiel boven 25x15x2 + tape",
                ArticleNumber = "SLIDING_TOP_L_25X15",
                Quantity = 3,
                Unit = "st",
                Note = "Lengte " + rangeWidth.ToString("0", CultureInfo.InvariantCulture) + " mm voor schuifdeuren U" + range.Start + "-" + range.End + "; tape 1mm boven profiel"
            });
        }

        private static void AddSlidingDoorPassThroughToDivider(SheetPart divider, CabinetConfig config, int boundaryIndex, double bodyHeight, double shelfZoneTop)
        {
            if (divider == null || config == null) return;
            var left = GetUnit(config, boundaryIndex);
            var right = GetUnit(config, boundaryIndex + 1);
            if (left == null || right == null || !left.SlidingDoors || !right.SlidingDoors) return;

            var bottomProfileDepth = SlidingDoorBottomProfileDepth(config);
            var trackSpacing = SlidingDoorTrackCenterSpacing(config);
            var doorThickness = MaterialThickness(config.SlidingDoorMaterial ?? config.FrontMaterial ?? config.CarcassMaterial);
            var materialThickness = MaterialThickness(divider.Material);
            var profilePassDepth = bottomProfileDepth + trackSpacing + doorThickness + 6.0;
            var passDepth = Math.Min(divider.LengthMm - 2.0, Math.Max(profilePassDepth, SlidingDoorTopLatDepthMm(config)));
            var zone = SlidingDoorOpeningZone(config, new UnitRange(boundaryIndex, boundaryIndex + 1), shelfZoneTop);
            var bottom = Math.Max(config.PlinthHeightMm, config.PlinthHeightMm + materialThickness + SlidingDoorTapeThickness(config));
            var top = zone.CenterYmm + zone.HeightMm / 2.0;
            var height = Math.Max(0, top - bottom);
            if (passDepth <= 4 || height <= 80) return;

            SheetOperations.AddThroughCutout(
                divider,
                "Schuifdeur doorvoer",
                0,
                bottom,
                passDepth,
                height,
                OperationFace.CenterPlane,
                "Door-en-door uitsparing zodat schuifdeurpanelen door het tussenschot kunnen schuiven.");
        }

        private static void AddSlidingDoorTopLatHoles(SheetPart lat, CabinetConfig config, UnitRange range, double rangeLeftX, double materialThickness)
        {
            if (lat == null || config == null) return;
            var diameter = AssemblyHoleDiameter(config);
            var ys = PatternPositions(lat.WidthMm, 12.0, 120.0, 2);
            var firstBoundary = Math.Max(0, range.Start - 1);
            var lastBoundary = Math.Min(config.UnitCount, range.End);

            for (var boundary = firstBoundary; boundary <= lastBoundary; boundary++)
            {
                var supportX = SupportCenterX(config, boundary, materialThickness);
                var localX = Clamp(supportX - rangeLeftX, 12.0, Math.Max(12.0, lat.LengthMm - 12.0));
                foreach (var y in ys)
                {
                    AddUniqueCabinetHole(lat, localX, y, diameter, "schuifdeur bovenlat naar staander " + boundary.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void AddSlidingDoorProfileVisual(WorkbenchModel model, string name, double x, double y, double z, double length, double depth, double height)
        {
            if (model == null) return;
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Profile,
                PartName = name,
                LengthMm = Math.Max(2, length),
                WidthMm = Math.Max(2, depth),
                HeightMm = Math.Max(2, height),
                Xmm = x,
                Ymm = y,
                Zmm = z,
                Orientation = AssemblyOrientation.Default
            });
        }

        private static DoorZone SlidingDoorOpeningZone(CabinetConfig config, UnitRange range, double shelfZoneTop)
        {
            var bottom = config.PlinthHeightMm + config.DoorGapMm;
            var top = shelfZoneTop - config.DoorGapMm;
            for (var unitNumber = range.Start; unitNumber <= range.End; unitNumber++)
            {
                var unit = GetUnit(config, unitNumber);
                var zone = DoorPanelZone(unit, config, shelfZoneTop);
                top = Math.Min(top, zone.CenterYmm + zone.HeightMm / 2.0);
                bottom = Math.Max(bottom, zone.CenterYmm - zone.HeightMm / 2.0);
            }

            var height = Math.Max(0, top - bottom);
            return new DoorZone(bottom + height / 2.0, height);
        }

        private static List<UnitRange> SlidingDoorRanges(CabinetConfig config)
        {
            var ranges = new List<UnitRange>();
            if (config == null) return ranges;

            var start = 0;
            for (var unitNumber = 1; unitNumber <= config.UnitCount; unitNumber++)
            {
                var sliding = GetUnit(config, unitNumber).SlidingDoors;
                if (sliding && start == 0)
                {
                    start = unitNumber;
                }
                else if (!sliding && start > 0)
                {
                    ranges.Add(new UnitRange(start, unitNumber - 1));
                    start = 0;
                }
            }

            if (start > 0) ranges.Add(new UnitRange(start, config.UnitCount));
            return ranges;
        }

        private static bool HasSlidingDoors(CabinetConfig config)
        {
            if (config == null) return false;
            for (var i = 1; i <= config.UnitCount; i++)
            {
                if (GetUnit(config, i).SlidingDoors) return true;
            }

            return false;
        }

        private static bool UnitHasSlidingDoors(CabinetConfig config, int unitNumber)
        {
            return config != null
                && unitNumber >= 1
                && unitNumber <= config.UnitCount
                && GetUnit(config, unitNumber).SlidingDoors;
        }

        private static double UnitClearLeftX(CabinetConfig config, int unitNumber, double materialThickness)
        {
            var fit = BottomFitForUnit(config, unitNumber - 1, materialThickness, 0);
            return fit.CenterXmm - fit.WidthMm / 2.0;
        }

        private static double UnitClearRightX(CabinetConfig config, int unitNumber, double materialThickness)
        {
            var fit = BottomFitForUnit(config, unitNumber - 1, materialThickness, 0);
            return fit.CenterXmm + fit.WidthMm / 2.0;
        }

        private static double SupportCenterX(CabinetConfig config, int boundaryIndex, double materialThickness)
        {
            if (config == null) return 0;
            var unitCount = Math.Max(1, config.UnitCount);
            var unitWidth = config.WidthMm / unitCount;
            if (boundaryIndex <= 0) return -config.WidthMm / 2.0 + materialThickness / 2.0;
            if (boundaryIndex >= unitCount) return config.WidthMm / 2.0 - materialThickness / 2.0;
            return -config.WidthMm / 2.0 + unitWidth * boundaryIndex;
        }

        private static double SlidingDoorTopLatDepthMm(CabinetConfig config)
        {
            var material = config == null ? null : (config.SlidingDoorMaterial ?? config.FrontMaterial ?? config.CarcassMaterial);
            var doorThickness = MaterialThickness(material);
            var profileWallThickness = SlidingDoorProfileWallThickness(config);
            var clearance = SlidingDoorTrackClearance(config, doorThickness);
            var profileStackDepth = 3.0 * profileWallThickness + 2.0 * clearance;
            return Math.Max(60.0, profileStackDepth + 18.0);
        }

        private static double SlidingDoorProfileWallThickness(CabinetConfig config)
        {
            return DefaultSlidingDoorProfileWallThicknessMm;
        }

        private static double SlidingDoorTrackClearance(CabinetConfig config, double doorThickness)
        {
            return Math.Max(doorThickness + 4.0, SlidingDoorTrackCenterSpacing(config));
        }

        private static double SlidingDoorTrackCenterFromFront(CabinetConfig config, int trackIndex, double doorThickness)
        {
            var profileWallThickness = SlidingDoorProfileWallThickness(config);
            var clearance = SlidingDoorTrackClearance(config, doorThickness);
            return profileWallThickness + clearance / 2.0 + Math.Max(0, trackIndex) * (clearance + profileWallThickness);
        }

        private static double SlidingDoorOverlap(CabinetConfig config)
        {
            return config != null && config.SlidingDoorOverlapMm > 0
                ? config.SlidingDoorOverlapMm
                : DefaultSlidingDoorOverlapMm;
        }

        private static double SlidingDoorFreeSpaceBehind(CabinetConfig config)
        {
            return config != null && config.SlidingDoorFreeSpaceBehindMm > 0
                ? config.SlidingDoorFreeSpaceBehindMm
                : DefaultSlidingDoorFreeSpaceBehindMm;
        }

        private static double SlidingDoorTrackCenterSpacing(CabinetConfig config)
        {
            return config != null && config.SlidingDoorTrackCenterSpacingMm > 0
                ? config.SlidingDoorTrackCenterSpacingMm
                : DefaultSlidingDoorTrackCenterSpacingMm;
        }

        private static double SlidingDoorTopProfileHeight(CabinetConfig config)
        {
            return config != null && config.SlidingDoorTopProfileHeightMm > 0
                ? config.SlidingDoorTopProfileHeightMm
                : DefaultSlidingDoorTopProfileHeightMm;
        }

        private static double SlidingDoorBottomProfileHeight(CabinetConfig config)
        {
            return config != null && config.SlidingDoorBottomProfileHeightMm > 0
                ? config.SlidingDoorBottomProfileHeightMm
                : DefaultSlidingDoorBottomProfileHeightMm;
        }

        private static double SlidingDoorTapeThickness(CabinetConfig config)
        {
            return config != null && config.SlidingDoorTapeThicknessMm > 0
                ? config.SlidingDoorTapeThicknessMm
                : DefaultSlidingDoorTapeThicknessMm;
        }

        private static double SlidingDoorTopProfileDepth(CabinetConfig config)
        {
            return config != null && config.SlidingDoorTopProfileDepthMm > 0
                ? config.SlidingDoorTopProfileDepthMm
                : DefaultSlidingDoorTopProfileDepthMm;
        }

        private static double SlidingDoorBottomProfileDepth(CabinetConfig config)
        {
            return config != null && config.SlidingDoorBottomProfileDepthMm > 0
                ? config.SlidingDoorBottomProfileDepthMm
                : DefaultSlidingDoorBottomProfileDepthMm;
        }

        private static double MaterialThickness(Material material)
        {
            return material == null ? 18.0 : material.ThicknessMm;
        }

        private static double FlushFrontCenterZ(double frontZ, Material frontMaterial)
        {
            var thickness = frontMaterial == null ? 18.0 : frontMaterial.ThicknessMm;
            return frontZ + thickness / 2.0;
        }

        private static double InnerFrontZ(double frontZ, Material frontMaterial)
        {
            var thickness = frontMaterial == null ? 18.0 : frontMaterial.ThicknessMm;
            return frontZ + thickness;
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
            return shelfZoneTop - config.DoorGapMm - drawerHeight - drawerIndex * (drawerHeight + config.DoorGapMm);
        }

        private static VerticalZone ShelfZoneForUnit(CabinetUnitConfig unit, CabinetConfig config, double shelfZoneTop)
        {
            var min = config.PlinthHeightMm + 80;
            var max = shelfZoneTop - 60;
            if (unit == null || unit.DrawerCount <= 0)
            {
                return new VerticalZone(min, max);
            }

            var drawerHeight = Math.Max(80, unit.DrawerHeightMm);
            var drawerCount = Math.Max(0, unit.DrawerCount);
            var lowestDrawerBottom = DrawerBottomY(config, drawerCount - 1, drawerHeight, shelfZoneTop);
            max = Math.Min(max, lowestDrawerBottom - 60);

            return new VerticalZone(min, max);
        }

        private static DoorZone DoorPanelZone(CabinetUnitConfig unit, CabinetConfig config, double shelfZoneTop)
        {
            var bottom = config.PlinthHeightMm + config.DoorGapMm;
            var top = shelfZoneTop - config.DoorGapMm;
            if (unit != null && unit.DrawerCount > 0)
            {
                var drawerHeight = Math.Max(80, unit.DrawerHeightMm);
                top = DrawerBottomY(config, unit.DrawerCount - 1, drawerHeight, shelfZoneTop) - config.DoorGapMm;
            }

            var height = Math.Max(0, top - bottom);
            return new DoorZone(bottom + height / 2.0, height);
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
                    OperationFace.NegativeY,
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

        private static void AddBottomReceivingGrooveToUpright(SheetPart upright, CabinetConfig config)
        {
            if (upright == null || config == null) return;
            if (IsInternalDivider(upright)) return;

            var materialThickness = MaterialThickness(config.CarcassMaterial);
            var grooveHeight = Math.Min(upright.WidthMm - 2.0, materialThickness + AlignmentGrooveClearanceMm());
            if (grooveHeight <= 0) return;

            var y = Math.Max(0, config.PlinthHeightMm - AlignmentGrooveClearanceMm() / 2.0);
            if (y + grooveHeight > upright.WidthMm)
            {
                y = Math.Max(0, upright.WidthMm - grooveHeight);
            }

            AddPocket(
                upright,
                "Bodem positioneergroef",
                0,
                y,
                upright.LengthMm,
                grooveHeight,
                AlignmentGrooveDepthMm(upright),
                InnerPocketFaceForVerticalZPanel(upright),
                "3mm verdiepte groef zodat bodemplaat in staander valt en niet hoeft uit te lijnen");
        }

        private static bool IsInternalDivider(SheetPart panel)
        {
            return panel != null
                && panel.Name != null
                && panel.Name.StartsWith("Tussenschot ", StringComparison.OrdinalIgnoreCase);
        }

        private static HorizontalFit BottomFitForUnit(CabinetConfig config, int unitIndex, double materialThickness, double grooveInsertDepth)
        {
            var unitCount = Math.Max(1, config.UnitCount);
            var unitWidth = config.WidthMm / unitCount;
            var leftPanelCenterX = unitIndex == 0
                ? -config.WidthMm / 2.0 + materialThickness / 2.0
                : -config.WidthMm / 2.0 + unitWidth * unitIndex;
            var rightPanelCenterX = unitIndex == unitCount - 1
                ? config.WidthMm / 2.0 - materialThickness / 2.0
                : -config.WidthMm / 2.0 + unitWidth * (unitIndex + 1);

            var clearLeftX = leftPanelCenterX + materialThickness / 2.0;
            var clearRightX = rightPanelCenterX - materialThickness / 2.0;
            var leftInsert = unitIndex == 0 ? Math.Max(0, grooveInsertDepth) : 0;
            var rightInsert = unitIndex == unitCount - 1 ? Math.Max(0, grooveInsertDepth) : 0;
            var partLeftX = clearLeftX - leftInsert;
            var partRightX = clearRightX + rightInsert;
            return new HorizontalFit((partLeftX + partRightX) / 2.0, Math.Max(20, partRightX - partLeftX));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void AddBottomReceivingGrooveToBackPanel(SheetPart backPanel, CabinetConfig config, double bodyHeight)
        {
            if (backPanel == null || config == null) return;
            var materialThickness = MaterialThickness(config.CarcassMaterial);
            var grooveHeight = Math.Min(backPanel.WidthMm - 2.0, materialThickness + AlignmentGrooveClearanceMm());
            if (grooveHeight <= 0) return;

            var y = Math.Max(0, config.PlinthHeightMm - AlignmentGrooveClearanceMm() / 2.0);
            var maxY = Math.Min(backPanel.WidthMm, bodyHeight) - grooveHeight;
            if (y > maxY) y = Math.Max(0, maxY);

            AddPocket(
                backPanel,
                "Bodemlijn achterwandgroef",
                0,
                y,
                backPanel.LengthMm,
                grooveHeight,
                AlignmentGrooveDepthMm(backPanel),
                OperationFace.NegativeZ,
                "3mm verdiepte groef zodat achterzijde bodemplaten in achterwand valt");
        }

        private static void AddSideReceivingGroovesToBackPanel(SheetPart backPanel, CabinetConfig config, double bodyHeight)
        {
            if (backPanel == null || config == null) return;
            var materialThickness = MaterialThickness(config.CarcassMaterial);
            var grooveWidth = Math.Min(backPanel.LengthMm / 2.0, materialThickness + AlignmentGrooveClearanceMm());
            var grooveHeight = Math.Min(backPanel.WidthMm, bodyHeight);
            var grooveDepth = AlignmentGrooveDepthMm(backPanel);
            if (grooveWidth <= 0 || grooveHeight <= 0 || grooveDepth <= 0) return;

            AddPocket(
                backPanel,
                "Linker zijwand achterwandgroef",
                0,
                0,
                grooveWidth,
                grooveHeight,
                grooveDepth,
                OperationFace.NegativeZ,
                "3mm verdiepte groef zodat linker zijwand in achterwand valt");

            AddPocket(
                backPanel,
                "Rechter zijwand achterwandgroef",
                backPanel.LengthMm - grooveWidth,
                0,
                grooveWidth,
                grooveHeight,
                grooveDepth,
                OperationFace.NegativeZ,
                "3mm verdiepte groef zodat rechter zijwand in achterwand valt");
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
                DrawerPocketFace(panel),
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
                DrawerPocketFace(sidePanel),
                "3mm verdiept rabat zodat ladeachter in de zijplaat valt");
        }

        private static void AddDrawerFrontGrooves(SheetPart front, double boxWidth, Material drawerMaterial)
        {
            if (front == null || drawerMaterial == null) return;
            var grooveWidth = Math.Min(front.LengthMm / 3.0, drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            var grooveHeight = Math.Min(front.WidthMm - 2.0 * DrawerGrooveBottomOffsetMm(), drawerMaterial.ThicknessMm + DrawerGrooveClearanceMm());
            var sideInset = Math.Max(0, (front.LengthMm - boxWidth) / 2.0);
            var verticalY = DrawerGrooveBottomOffsetMm();
            var verticalHeight = Math.Min(
                front.WidthMm - DrawerGrooveBottomOffsetMm(),
                Math.Max(10, front.WidthMm - 2.0 * DrawerGrooveBottomOffsetMm() + DrawerGrooveDepthMm()));
            AddPocket(
                front,
                "Ladefront linker zij-rabat",
                sideInset,
                verticalY,
                grooveWidth,
                verticalHeight,
                DrawerGrooveDepthMm(),
                OperationFace.PositiveZ,
                "3mm verdiept rabat voor linker ladezijde in binnenkant front");
            AddPocket(
                front,
                "Ladefront rechter zij-rabat",
                front.LengthMm - sideInset - grooveWidth,
                verticalY,
                grooveWidth,
                verticalHeight,
                DrawerGrooveDepthMm(),
                OperationFace.PositiveZ,
                "3mm verdiept rabat voor rechter ladezijde in binnenkant front");
            AddPocket(
                front,
                "Ladefront bodem-rabat",
                sideInset,
                DrawerGrooveBottomOffsetMm(),
                Math.Max(10, front.LengthMm - 2.0 * sideInset),
                grooveHeight,
                DrawerGrooveDepthMm(),
                OperationFace.PositiveZ,
                "3mm verdiept rabat voor ladebodem in binnenkant front");
        }

        private static void AddDrawerPullCutout(SheetPart front, CabinetConfig config)
        {
            if (front == null || config == null || !config.IncludeDrawerPullCutouts) return;

            const double preferredLength = 140.0;
            const double preferredHeight = 32.0;
            const double minLength = 96.0;
            const double sideMargin = 60.0;
            const double topOffset = 30.0;
            const double minVerticalMargin = 26.0;

            var length = Math.Min(preferredLength, front.LengthMm - 2.0 * sideMargin);
            if (length < minLength)
            {
                length = Math.Min(front.LengthMm * 0.48, front.LengthMm - 2.0 * 35.0);
            }

            var height = Math.Min(preferredHeight, Math.Max(18.0, front.WidthMm - 2.0 * minVerticalMargin));
            if (length < 80 || height < 14) return;

            var x = (front.LengthMm - length) / 2.0;
            var y = front.WidthMm - topOffset - height;
            y = Clamp(y, minVerticalMargin, Math.Max(minVerticalMargin, front.WidthMm - height - minVerticalMargin));

            var radius = height / 2.0;
            var centerLength = Math.Max(1.0, length - height);

            var skinDepth = Math.Max(0.1, MaterialThickness(front.Material) - 2.0);
            AddPocket(
                front,
                "Uitgefreesde handgreep midden tot 2mm restmateriaal",
                x + radius,
                y,
                centerLength,
                height,
                skinDepth,
                OperationFace.PositiveZ,
                "Capsule eerst uitruimen met 2mm restmateriaal voor vacuümbehoud");
            AddDrawerPullRoundEnd(front, x + radius, y + radius, height, skinDepth, "links");
            AddDrawerPullRoundEnd(front, x + length - radius, y + radius, height, skinDepth, "rechts");
            SheetOperations.AddThroughCutout(
                front,
                "Uitgefreesde handgreep afwerkcontour tabs 8x2 max70",
                x,
                y,
                length,
                height,
                OperationFace.CenterPlane,
                "Alleen capsule-binnencontour doorfrezen; tabs 8mm breed, 2mm hoog en maximaal 70mm uit elkaar");
        }

        private static void AddDrawerPullRoundEnd(SheetPart front, double x, double y, double diameter, double depthMm, string side)
        {
            if (front == null || diameter <= 0) return;
            front.Holes.Add(new SheetHole
            {
                Name = "Uitgefreesde handgreep " + side + " ronding tot 2mm restmateriaal",
                Xmm = Math.Round(x, 3),
                Ymm = Math.Round(y, 3),
                DiameterMm = Math.Round(diameter, 3),
                DepthMm = Math.Round(depthMm, 3),
                Face = OperationFace.PositiveZ,
                DepthMode = OperationDepthMode.PocketFromFace,
                Countersunk = false,
                SupportKind = SheetHoleSupportKind.MachiningCutout
            });
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

            if (config.IncludeBackPanel)
            {
                var backThickness = config.BackMaterial == null ? MaterialThickness(config.CarcassMaterial) : MaterialThickness(config.BackMaterial);
                var backLineY = Clamp(config.DepthMm + backThickness / 2.0, 12.0, Math.Max(12.0, worktop.WidthMm - 12.0));
                AddMountingLine(worktop, edgeInset, backLineY, worktop.LengthMm - edgeInset, backLineY, diameter, 220, "werkblad naar achterwand");
            }
        }

        private static void AddPlinthToUprightHoles(SheetPart plinth, CabinetConfig config)
        {
            if (plinth == null || config == null) return;
            var diameter = AssemblyHoleDiameter(config);
            var unitWidth = config.WidthMm / config.UnitCount;
            var sideT = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var grooveWidth = Math.Min(plinth.LengthMm - 2.0, sideT + AlignmentGrooveClearanceMm());
            var grooveDepth = AlignmentGrooveDepthMm(plinth);
            var ys = PatternPositions(plinth.WidthMm, 35, 150, 2);

            for (var i = 0; i <= config.UnitCount; i++)
            {
                var x = unitWidth * i;
                if (i == 0) x = sideT / 2.0;
                else if (i == config.UnitCount) x = config.WidthMm - sideT / 2.0;
                AddPocket(
                    plinth,
                    "Plint staander-positioneergroef " + i.ToString(CultureInfo.InvariantCulture),
                    Clamp(x - grooveWidth / 2.0, 0, Math.Max(0, plinth.LengthMm - grooveWidth)),
                    0,
                    grooveWidth,
                    plinth.WidthMm,
                    grooveDepth,
                    OperationFace.PositiveZ,
                    "3mm verdiepte groef aan achterzijde plint voor front-uitlijning van zijwand/tussenschot");

                foreach (var y in ys)
                {
                    AddUniqueCabinetHole(plinth, x, y, diameter, "plint naar staander " + i.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void AddBottomToUprightHoles(SheetPart panel, CabinetConfig config, int unitNumber)
        {
            if (panel == null || config == null || unitNumber < 1 || unitNumber > config.UnitCount) return;

            var diameter = AssemblyHoleDiameter(config);
            var y = config.PlinthHeightMm + MaterialThickness(config.CarcassMaterial) / 2.0;
            var positions = PatternPositions(panel.LengthMm, 45, 180, 3);
            for (var i = 0; i < positions.Count; i++)
            {
                AddCabinetHole(
                    panel,
                    positions[i],
                    y,
                    diameter,
                    "Montagegat bodem U" + unitNumber.ToString(CultureInfo.InvariantCulture) + " naar zijpaneel " + (i + 1).ToString(CultureInfo.InvariantCulture),
                SheetHoleSupportKind.PanelScrew);
            }
        }

        private static void AddBottomToPlinthHoles(SheetPart bottom, CabinetConfig config, int unitNumber)
        {
            if (bottom == null || config == null || unitNumber < 1 || unitNumber > config.UnitCount) return;
            if (config.UnitCount <= 1) return;

            var diameter = AssemblyHoleDiameter(config);
            var materialThickness = MaterialThickness(config.CarcassMaterial);
            var frontPlinthCenterY = config.PlinthDepthMm + materialThickness / 2.0;
            var y = Clamp(frontPlinthCenterY, 12.0, Math.Max(12.0, bottom.WidthMm - 12.0));
            var positions = PatternPositions(bottom.LengthMm, 80.0, 180.0, 2);

            for (var i = 0; i < positions.Count; i++)
            {
                AddCabinetHole(
                    bottom,
                    positions[i],
                    y,
                    diameter,
                    "Plintbevestigingsgat bodem U" + unitNumber.ToString(CultureInfo.InvariantCulture) + " " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    SheetHoleSupportKind.PanelScrew);
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
            var frontYs = PatternPositions(drawerFront.WidthMm, 32, 95, 2);
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
                    OperationFace.NegativeZ,
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

        private static void AddShelfBracketMountingHoles(SheetPart shelf, CabinetConfig config)
        {
            if (shelf == null || config == null || !config.IncludeAdjustableShelfHoles || config.ShelfSupport == null) return;

            const double sideInset = 10.0;
            const double rowOffset = 20.0;
            const double edgeSafety = 8.0;

            var support = config.ShelfSupport;
            var diameter = support.HoleDiameterMm > 0 ? support.HoleDiameterMm : 5.0;
            var sideXs = new[] { sideInset, shelf.LengthMm - sideInset };
            var rowCenters = new[]
            {
                support.FrontInsetMm,
                shelf.WidthMm - support.BackInsetMm
            };

            var index = 1;
            foreach (var xRaw in sideXs)
            {
                var x = Clamp(xRaw, edgeSafety, shelf.LengthMm - edgeSafety);
                foreach (var centerRaw in rowCenters)
                {
                    var center = Clamp(centerRaw, edgeSafety + rowOffset, shelf.WidthMm - edgeSafety - rowOffset);
                    AddShelfBracketHole(shelf, x, center - rowOffset, diameter, index++);
                    AddShelfBracketHole(shelf, x, center + rowOffset, diameter, index++);
                }
            }
        }

        private static void AddShelfBracketHole(SheetPart shelf, double x, double y, double diameter, int index)
        {
            if (shelf == null) return;
            x = Math.Round(Clamp(x, 6, shelf.LengthMm - 6), 3);
            y = Math.Round(Clamp(y, 6, shelf.WidthMm - 6), 3);
            shelf.Holes.Add(new SheetHole
            {
                Name = "Onderzijde legplank beugelgat " + index.ToString(CultureInfo.InvariantCulture),
                Xmm = x,
                Ymm = y,
                DiameterMm = diameter,
                DepthMm = ShelfBracketBlindHoleDepthMm(),
                Face = OperationFace.NegativeY,
                DepthMode = OperationDepthMode.BlindFromFace,
                Countersunk = false,
                SupportKind = SheetHoleSupportKind.ShelfSupport
            });
        }

        private static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, string note)
        {
            SheetOperations.AddPocket(sheet, name, x, y, length, width, depth, note);
        }

        private static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, OperationFace face, string note)
        {
            SheetOperations.AddPocket(sheet, name, x, y, length, width, depth, face, note);
        }

        private static OperationFace InnerPocketFaceForVerticalZPanel(SheetPart panel)
        {
            if (panel == null || panel.Name == null) return OperationFace.CenterPlane;
            if (panel.Name.StartsWith("Zijwand links", StringComparison.OrdinalIgnoreCase)) return OperationFace.PositiveX;
            if (panel.Name.StartsWith("Zijwand rechts", StringComparison.OrdinalIgnoreCase)) return OperationFace.NegativeX;
            return OperationFace.CenterPlane;
        }

        private static OperationFace DrawerPocketFace(SheetPart panel)
        {
            if (panel == null || panel.Name == null) return OperationFace.CenterPlane;
            if (panel.Name.StartsWith("Ladezijde links", StringComparison.OrdinalIgnoreCase) ||
                panel.Name.StartsWith("Bovenlade zijde links", StringComparison.OrdinalIgnoreCase)) return OperationFace.PositiveX;
            if (panel.Name.StartsWith("Ladezijde rechts", StringComparison.OrdinalIgnoreCase) ||
                panel.Name.StartsWith("Bovenlade zijde rechts", StringComparison.OrdinalIgnoreCase)) return OperationFace.NegativeX;
            if (panel.Name.StartsWith("Ladeachter", StringComparison.OrdinalIgnoreCase) ||
                panel.Name.StartsWith("Bovenlade achter", StringComparison.OrdinalIgnoreCase)) return OperationFace.NegativeZ;
            if (panel.Name.StartsWith("Ladefront", StringComparison.OrdinalIgnoreCase) ||
                panel.Name.StartsWith("Bovenlade front", StringComparison.OrdinalIgnoreCase)) return OperationFace.PositiveZ;
            return OperationFace.CenterPlane;
        }

        private static double AlignmentGrooveDepthMm(SheetPart sheet)
        {
            return AlignmentGrooveDepthMmForMaterial(sheet == null ? null : sheet.Material);
        }

        private static double AlignmentGrooveDepthMmForMaterial(Material material)
        {
            return ProductDrawingStrategy.GrooveDepthForMaterial(material);
        }

        private static double AlignmentGrooveClearanceMm()
        {
            return ProductDrawingStrategy.DefaultAlignmentGrooveClearanceMm;
        }

        private static double DrawerGrooveDepthMm()
        {
            return ProductDrawingStrategy.DefaultDrawerGrooveDepthMm;
        }

        private static double DrawerGrooveClearanceMm()
        {
            return ProductDrawingStrategy.DefaultDrawerGrooveClearanceMm;
        }

        private static double DrawerGrooveBottomOffsetMm()
        {
            return 0.0;
        }

        private static double BlindOutsidePanelHoleDepthMm()
        {
            return 12.0;
        }

        private static double ShelfBracketBlindHoleDepthMm()
        {
            return 8.0;
        }

        private static double AssemblyHoleDiameter(CabinetConfig config)
        {
            return config != null && config.SheetFastener != null && config.SheetFastener.ClearanceHoleDiameterMm > 0
                ? config.SheetFastener.ClearanceHoleDiameterMm
                : ProductDrawingStrategy.DefaultWoodScrewClearanceHoleDiameterMm;
        }

        private static List<double> PatternPositions(double length, double edgeInset, double maxSpacing, int minimumCount)
        {
            return SheetPatterns.EdgeDistributedPositions(length, edgeInset, maxSpacing, minimumCount);
        }

        private static void AddMountingLine(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing, string note)
        {
            SheetOperations.AddMountingLine(sheet, x1, y1, x2, y2, diameter, maxSpacing, "Montagegat " + note, SheetHoleSupportKind.PanelScrew);
        }

        private static void AddUniqueCabinetHole(SheetPart sheet, double x, double y, double diameter, string note)
        {
            AddUniqueCabinetHole(sheet, x, y, diameter, note, SheetHoleSupportKind.PanelScrew);
        }

        private static void AddUniqueCabinetHole(SheetPart sheet, double x, double y, double diameter, string note, SheetHoleSupportKind supportKind)
        {
            SheetOperations.AddUniqueThroughHole(sheet, x, y, diameter, "Montagegat " + note + " " + (sheet == null ? 1 : sheet.Holes.Count + 1), supportKind, 6);
        }

        private static void AddCabinetHole(SheetPart sheet, double x, double y, double diameter, string name, SheetHoleSupportKind supportKind)
        {
            if (sheet == null) return;
            x = Math.Round(Math.Max(6, Math.Min(sheet.LengthMm - 6, x)), 3);
            y = Math.Round(Math.Max(6, Math.Min(sheet.WidthMm - 6, y)), 3);
            sheet.Holes.Add(new SheetHole
            {
                Name = name,
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
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
        }

        private static CabinetUnitConfig GetUnit(CabinetConfig config, int unitNumber)
        {
            foreach (var unit in config.Units)
            {
                if (unit.UnitNumber == unitNumber) return unit;
            }

            return new CabinetUnitConfig { UnitNumber = unitNumber };
        }

        private static IEnumerable<double> ShelfHeights(CabinetUnitConfig unit, CabinetConfig config, double min, double max, bool forceEvenDistribution)
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
            if (!forceEvenDistribution && IsAnchoredShelfStart(config.ShelfStartMode))
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

        private sealed class VerticalZone
        {
            public VerticalZone(double minMm, double maxMm)
            {
                MinMm = minMm;
                MaxMm = maxMm;
            }

            public double MinMm { get; private set; }
            public double MaxMm { get; private set; }
        }

        private sealed class DoorZone
        {
            public DoorZone(double centerYmm, double heightMm)
            {
                CenterYmm = centerYmm;
                HeightMm = heightMm;
            }

            public double CenterYmm { get; private set; }
            public double HeightMm { get; private set; }
        }

        private sealed class UnitRange
        {
            public UnitRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; private set; }
            public int End { get; private set; }
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
            AddRailHoleLine(panel, rail.CabinetHolePositionsMm, rail.HoleCount, rail.FirstHoleOffsetMm, rail.HoleSpacingMm, rail.HoleDiameterMm, railY, "Bovenlade railgat U" + unitNumber, BlindDepthForOutsidePanel(panel), DrawerBoxFrontInset(config));
        }

        private static void AddRailHolesForAdjacentUnit(SheetPart panel, CabinetConfig config, int unitNumber, double bodyHeight)
        {
            if (unitNumber < 1 || unitNumber > config.UnitCount) return;
            var unit = GetUnit(config, unitNumber);
            if (unit.DrawerCount <= 0) return;

            var rail = config.DrawerRail;
            var drawerHeight = Math.Max(80, unit.DrawerHeightMm);
            var shelfZoneTop = DrawerShelfZoneTop(config, bodyHeight);
            var railXOffset = DrawerBoxFrontInset(config);
            for (var drawerIndex = 0; drawerIndex < unit.DrawerCount; drawerIndex++)
            {
                var bottomY = DrawerBottomY(config, drawerIndex, drawerHeight, shelfZoneTop);
                var railY = bottomY + rail.VerticalOffsetMm;
                if (railY <= 10 || railY >= bodyHeight - 10) continue;

                var positions = RailHolePositions(rail.CabinetHolePositionsMm, rail.HoleCount, rail.FirstHoleOffsetMm, rail.HoleSpacingMm);
                for (var holeIndex = 0; holeIndex < positions.Count; holeIndex++)
                {
                    var x = positions[holeIndex] + railXOffset;
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

            AddRailHoleLine(panel, rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm, rail.DrawerHoleDiameterMm, y, "Laderailgat", 0, Math.Max(0.0, rail.DrawerFrontInsertionCompensationMm));
        }

        private static void AddRailHoleLine(SheetPart panel, string explicitPositions, int count, double firstOffset, double spacing, double diameter, double y, string name, double depthMm, double xOffset)
        {
            y = Math.Round(y, 3);
            if (y <= 5 || y >= panel.WidthMm - 5) return;

            var positions = RailHolePositions(explicitPositions, count, firstOffset, spacing);
            for (var holeIndex = 0; holeIndex < positions.Count; holeIndex++)
            {
                var x = positions[holeIndex] + xOffset;
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

        private static double DrawerBoxFrontInset(CabinetConfig config)
        {
            var frontThickness = config == null || config.FrontMaterial == null ? 18.0 : config.FrontMaterial.ThicknessMm;
            return Math.Max(0, frontThickness - DrawerGrooveDepthMm());
        }

        private static bool HasHoleAt(SheetPart panel, double x, double y, double diameter)
        {
            return SheetOperations.HasHoleAt(panel, x, y, diameter);
        }

        private static void AddAdjustableShelfHolesForPanel(SheetPart panel, CabinetConfig config, int boundaryIndex, double bodyHeight, double shelfZoneTop)
        {
            var zones = new List<VerticalZone>();
            AddShelfHoleZoneForUnit(zones, config, boundaryIndex, shelfZoneTop);
            AddShelfHoleZoneForUnit(zones, config, boundaryIndex + 1, shelfZoneTop);
            var frontInset = config.ShelfFrontInsetMm;
            if (UnitHasSlidingDoors(config, boundaryIndex) || UnitHasSlidingDoors(config, boundaryIndex + 1))
            {
                frontInset = Math.Max(frontInset, SlidingDoorRequiredShelfInset(config));
            }

            AddAdjustableShelfHoles(panel, config, bodyHeight, zones, frontInset);
        }

        private static void AddShelfHoleZoneForUnit(List<VerticalZone> zones, CabinetConfig config, int unitNumber, double shelfZoneTop)
        {
            if (zones == null || config == null || unitNumber < 1 || unitNumber > config.UnitCount) return;
            var unit = GetUnit(config, unitNumber);
            var zone = ShelfZoneForUnit(unit, config, shelfZoneTop);
            if (zone.MaxMm > zone.MinMm)
            {
                zones.Add(zone);
            }
        }

        private static void AddAdjustableShelfHoles(SheetPart panel, CabinetConfig config, double usableHeight, List<VerticalZone> zones, double shelfFrontInset)
        {
            if (!config.IncludeAdjustableShelfHoles || config.ShelfSupport == null) return;
            var support = config.ShelfSupport;
            var spacing = Math.Max(1, support.HoleSpacingMm);
            var endY = Math.Min(panel.WidthMm - config.AdjustableShelfHoleEndMarginMm, usableHeight - config.AdjustableShelfHoleEndMarginMm);
            var frontX = support.FrontInsetMm + Clamp(shelfFrontInset, 0, Math.Max(0, panel.LengthMm - 80));
            var backX = panel.LengthMm - support.BackInsetMm;
            if (frontX <= 5 || backX >= panel.LengthMm - 5 || frontX >= backX) return;

            var index = 1;
            var shelfHoleYs = new List<double>();
            for (var y = support.FirstHoleHeightMm; y <= endY; y += spacing)
            {
                if (!IsInsideAnyShelfZone(y, zones)) continue;
                shelfHoleYs.Add(y);
            }

            foreach (var y in shelfHoleYs)
            {
                AddShelfSupportHole(panel, frontX, y, support.HoleDiameterMm, index++, BlindDepthForOutsidePanel(panel));
            }

            foreach (var y in shelfHoleYs)
            {
                AddShelfSupportHole(panel, backX, y, support.HoleDiameterMm, index++, BlindDepthForOutsidePanel(panel));
            }
        }

        private static bool IsInsideAnyShelfZone(double y, List<VerticalZone> zones)
        {
            if (zones == null || zones.Count == 0) return true;
            foreach (var zone in zones)
            {
                if (y >= zone.MinMm && y <= zone.MaxMm) return true;
            }

            return false;
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

        private sealed class HorizontalFit
        {
            public HorizontalFit(double centerXmm, double widthMm)
            {
                CenterXmm = centerXmm;
                WidthMm = widthMm;
            }

            public double CenterXmm { get; private set; }
            public double WidthMm { get; private set; }
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
