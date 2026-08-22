using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class WorkbenchCabinetEngine
    {
        private const double AlignmentGrooveClearanceMm = 1.0;
        private const double HingeCupDiameterMm = 35.0;
        private const double HingeCupDepthMm = 13.0;
        private const double HingeCupEdgeOffsetMm = 23.0;
        private const double HingeDoorHoleDiameterMm = 4.5;
        private const double HingeDoorHoleDepthMm = 12.0;
        private const double HingeDoorHoleSpacingMm = 45.0;
        private const double HingeDoorHoleLineOffsetMm = 10.0;
        private const double HingePlateHoleDiameterMm = 4.5;
        private const double HingePlateHoleSpacingMm = 32.0;
        private const double HingePlateFrontInsetMm = 30.0;
        private const double HingePlateHoleDepthMm = 10.0;
        private const double HingeVerticalInsetMm = 110.0;
        private const double DoorOverlayMm = 16.5;
        private const double DrawerFrontTopGapMm = 3.0;
        private const double DrawerVerticalFastenerMaxSpacingMm = 160.0;
        private const double DrawerBottomFastenerMaxSpacingMm = 260.0;
        private const double OuterWallRailHoleDepthMm = 12.0;
        private const double DoorStopShelfClearanceMm = 1.0;

        public WorkbenchModel Build(WorkbenchCabinetConfig config)
        {
            Validate(config);

            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                SheetFastener = config.SheetFastener
            };

            var carcass = config.CarcassMaterial;
            var worktopMaterial = config.WorktopMaterial ?? carcass;
            var frontMaterial = config.FrontMaterial ?? carcass;
            var drawerMaterial = config.DrawerMaterial ?? carcass;
            var backMaterial = config.BackMaterial ?? carcass;
            var t = MaterialThickness(carcass);
            var topT = MaterialThickness(worktopMaterial);
            var frontT = MaterialThickness(frontMaterial);
            var backT = config.IncludeBackPanel ? MaterialThickness(backMaterial) : 0.0;
            var grooveDepth = ProductDrawingStrategy.GrooveDepthForMaterial(carcass);
            var bottomTopY = config.PlinthHeightMm + t;
            var worktopBottomY = config.WorktopHeightMm - topT;
            var clearBodyHeight = worktopBottomY - bottomTopY;
            var insertedPanelHeight = clearBodyHeight + 2.0 * grooveDepth;
            var panelCenterY = (bottomTopY + worktopBottomY) / 2.0;
            var frontZ = -config.DepthMm / 2.0;
            // De achterzijde van een gesloten draaideur blijft vrij van de kopse
            // kastkanten. Dit is een assembly-offset en staat los van de diepte
            // van de scharnierpot- en schroefboringen in het deurblad.
            var doorCenterZ = frontZ - config.DoorToCarcassClearanceMm - frontT / 2.0;
            var unitWidth = config.WidthMm / config.UnitCount;
            var topDrawerHeight = config.IncludeTopDrawer
                ? Math.Min(config.TopDrawerHeightMm, Math.Max(100.0, clearBodyHeight - 180.0))
                : 0.0;
            // Het ladefront valt voor de werkbladrand en eindigt 3 mm onder het
            // bovenvlak. De ladebak zelf blijft volledig onder het werkblad.
            var drawerBoxTopY = worktopBottomY - DrawerFrontTopGapMm;
            var drawerBoxHeight = topDrawerHeight > 0 ? Math.Max(80.0, topDrawerHeight - config.DoorGapMm) : 0.0;
            var drawerFrontBottomY = drawerBoxTopY - drawerBoxHeight;
            var drawerFrontTopY = topDrawerHeight > 0
                ? config.WorktopHeightMm - DrawerFrontTopGapMm
                : 0.0;
            var doorTopLimitY = topDrawerHeight > 0
                ? drawerFrontBottomY - config.DoorGapMm
                : worktopBottomY - config.DoorGapMm;
            var drawerRailWorldY = topDrawerHeight > 0 && config.DrawerRail != null
                ? drawerFrontBottomY + config.DrawerRail.VerticalOffsetMm
                : 0.0;

            var worktop = Sheet("Werkblad werkbankkast", worktopMaterial, config.WidthMm, config.DepthMm);
            var bottom = Sheet("Doorlopende bodemplaat", carcass, config.WidthMm, config.DepthMm);
            AddAdjustableFootHoles(bottom, config, unitWidth);

            var boundaries = new List<BoundaryPanel>();
            var leftSide = Sheet("Werkbank zijwand links", carcass, config.DepthMm, insertedPanelHeight);
            var leftX = -config.WidthMm / 2.0 + t / 2.0;
            boundaries.Add(new BoundaryPanel(0, leftX, leftSide));
            AddFullDepthSupportGrooves(bottom, worktop, config, leftX, t, grooveDepth, "zijwand links");
            AddSheet(model, leftSide, leftX, panelCenterY, 0, AssemblyOrientation.SheetVerticalZ);

            var groupCount = (config.UnitCount + 1) / 2;
            for (var unitBoundary = 1; unitBoundary < config.UnitCount; unitBoundary++)
            {
                var nominalX = -config.WidthMm / 2.0 + unitBoundary * unitWidth;
                var separatesDoorPairs = unitBoundary % 2 == 0;
                var x = nominalX - (separatesDoorPairs ? t / 2.0 : 0.0);
                var frontTrim = separatesDoorPairs ? 0.0 : Math.Max(0.0, t - grooveDepth);
                var dividerDepth = config.DepthMm - backT - frontTrim;
                var dividerCenterZ = frontZ + frontTrim + dividerDepth / 2.0;
                var divider = Sheet(
                    separatesDoorPairs
                        ? "Volledig tussenschot dubbel links U" + unitBoundary.ToString(CultureInfo.InvariantCulture)
                        : "Volledig tussenschot T U" + unitBoundary.ToString(CultureInfo.InvariantCulture),
                    carcass,
                    dividerDepth,
                    insertedPanelHeight);
                var boundary = new BoundaryPanel(unitBoundary, x, divider);
                boundary.FrontDepthTrimMm = frontTrim;
                boundaries.Add(boundary);
                AddSupportGrooves(bottom, worktop, config, x, t, frontTrim, dividerDepth, grooveDepth, "tussenschot U" + unitBoundary.ToString(CultureInfo.InvariantCulture));
                AddSheet(model, divider, x, panelCenterY, dividerCenterZ, AssemblyOrientation.SheetVerticalZ);

                if (separatesDoorPairs)
                {
                    AddSecondFullDepthDivider(model, bottom, worktop, config, carcass, boundary, nominalX + t / 2.0, dividerDepth, dividerCenterZ, insertedPanelHeight, panelCenterY, grooveDepth);
                }
                else
                {
                    AddDoorStopToDivider(model, bottom, worktop, config, carcass, nominalX, insertedPanelHeight, panelCenterY, bottomTopY, doorTopLimitY, grooveDepth);
                }
            }

            var rightSide = Sheet("Werkbank zijwand rechts", carcass, config.DepthMm, insertedPanelHeight);
            var rightX = config.WidthMm / 2.0 - t / 2.0;
            boundaries.Add(new BoundaryPanel(config.UnitCount, rightX, rightSide));
            AddFullDepthSupportGrooves(bottom, worktop, config, rightX, t, grooveDepth, "zijwand rechts");
            AddSheet(model, rightSide, rightX, panelCenterY, 0, AssemblyOrientation.SheetVerticalZ);

            AddBoundaryShelfSupportRows(boundaries, config, grooveDepth, backT, doorTopLimitY, panelCenterY, insertedPanelHeight);
            AddBoundaryDrawerRailHoles(boundaries, config, drawerRailWorldY, panelCenterY, insertedPanelHeight);

            var doorCount = 0;
            var hingeCount = 0;
            for (var group = 0; group < groupCount; group++)
            {
                var firstUnit = group * 2;
                var unitsInGroup = Math.Min(2, config.UnitCount - firstUnit);
                var leftBoundary = boundaries[firstUnit];
                var rightBoundary = boundaries[firstUnit + unitsInGroup];
                var openingLeftX = leftBoundary.Xmm + t / 2.0 + leftBoundary.PositiveThicknessMm;
                var openingRightX = rightBoundary.Xmm - t / 2.0;
                var openingCenterX = -config.WidthMm / 2.0 + (firstUnit + 1) * unitWidth;

                AddUnitShelves(
                    model,
                    config,
                    carcass,
                    t,
                    firstUnit,
                    unitsInGroup,
                    openingLeftX,
                    openingRightX,
                    openingCenterX,
                    bottomTopY,
                    doorTopLimitY,
                    backT);

                // De deur dekt ook de voorzijde van de doorlopende bodemplaat af.
                // De onderkant blijft alleen de ingestelde deurspleet boven de onderzijde van de bodemplaat.
                var doorBottomY = config.PlinthHeightMm + config.DoorGapMm;
                var doorTopY = doorTopLimitY;
                var doorHeight = doorTopY - doorBottomY;
                var doorCenterY = (doorBottomY + doorTopY) / 2.0;
                var hingeWorldYs = HingeWorldPositions(doorBottomY, doorTopY);
                var leftHingeWorldYs = hingeWorldYs;

                if (unitsInGroup == 2)
                {
                    var leftDoorLeftX = openingLeftX - DoorOverlayMm;
                    var leftDoorRightX = openingCenterX - config.DoorGapMm / 2.0;
                    var rightDoorLeftX = openingCenterX + config.DoorGapMm / 2.0;
                    var rightDoorRightX = openingRightX + DoorOverlayMm;

                    if (config.IncludeTopDrawer)
                    {
                        BuildTopDrawer(
                            model, config, carcass, drawerMaterial, frontMaterial, firstUnit + 1,
                            openingLeftX, openingCenterX - t / 2.0,
                            leftDoorLeftX, leftDoorRightX,
                            drawerFrontBottomY, drawerFrontTopY, frontZ, backT);
                        BuildTopDrawer(
                            model, config, carcass, drawerMaterial, frontMaterial, firstUnit + 2,
                            openingCenterX + t / 2.0, openingRightX,
                            rightDoorLeftX, rightDoorRightX,
                            drawerFrontBottomY, drawerFrontTopY, frontZ, backT);
                    }

                    var leftDoor = BuildDoor(
                        "Draaideur links paar " + (group + 1).ToString(CultureInfo.InvariantCulture),
                        frontMaterial,
                        leftDoorRightX - leftDoorLeftX,
                        doorHeight,
                        true,
                        leftHingeWorldYs,
                        doorBottomY,
                        config.FrontPanelCornerRadiusMm);
                    AddSheet(model, leftDoor, (leftDoorLeftX + leftDoorRightX) / 2.0, doorCenterY, doorCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddHingePlateHoles(leftBoundary.PositiveHingeSheet ?? leftBoundary.Sheet, OperationFace.PositiveX, leftHingeWorldYs, panelCenterY, insertedPanelHeight, t);

                    var rightDoor = BuildDoor(
                        "Draaideur rechts paar " + (group + 1).ToString(CultureInfo.InvariantCulture),
                        frontMaterial,
                        rightDoorRightX - rightDoorLeftX,
                        doorHeight,
                        false,
                        hingeWorldYs,
                        doorBottomY,
                        config.FrontPanelCornerRadiusMm);
                    AddSheet(model, rightDoor, (rightDoorLeftX + rightDoorRightX) / 2.0, doorCenterY, doorCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddHingePlateHoles(rightBoundary.Sheet, OperationFace.NegativeX, hingeWorldYs, panelCenterY, insertedPanelHeight, t);

                    doorCount += 2;
                    hingeCount += leftHingeWorldYs.Count + hingeWorldYs.Count;
                }
                else
                {
                    var doorLeftX = openingLeftX - DoorOverlayMm;
                    var doorRightX = openingRightX + DoorOverlayMm;
                    if (config.IncludeTopDrawer)
                    {
                        BuildTopDrawer(
                            model, config, carcass, drawerMaterial, frontMaterial, firstUnit + 1,
                            openingLeftX, openingRightX,
                            doorLeftX, doorRightX,
                            drawerFrontBottomY, drawerFrontTopY, frontZ, backT);
                    }
                    var door = BuildDoor(
                        "Draaideur links enkel " + (group + 1).ToString(CultureInfo.InvariantCulture),
                        frontMaterial,
                        doorRightX - doorLeftX,
                        doorHeight,
                        true,
                        leftHingeWorldYs,
                        doorBottomY,
                        config.FrontPanelCornerRadiusMm);
                    AddSheet(model, door, (doorLeftX + doorRightX) / 2.0, doorCenterY, doorCenterZ, AssemblyOrientation.SheetVerticalX);
                    AddHingePlateHoles(leftBoundary.PositiveHingeSheet ?? leftBoundary.Sheet, OperationFace.PositiveX, leftHingeWorldYs, panelCenterY, insertedPanelHeight, t);
                    doorCount++;
                    hingeCount += leftHingeWorldYs.Count;
                }
            }

            AddSheet(model, bottom, 0, config.PlinthHeightMm + t / 2.0, 0, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, worktop, 0, config.WorktopHeightMm - topT / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            if (config.IncludeBackPanel)
            {
                var back = Sheet("Achterwand werkbankkast", backMaterial, Math.Max(100, config.WidthMm - 2.0 * t), clearBodyHeight);
                AddSheet(model, back, 0, panelCenterY, config.DepthMm / 2.0 - backT / 2.0, AssemblyOrientation.SheetVerticalX);
            }

            var plinthHeight = Math.Max(40, config.PlinthHeightMm - config.PlinthFloorClearanceMm);
            // Bij zijplinten loopt de voorplint tot de buitenste kastmaat door.
            // De zijplint begint achter de voorplint, zodat de voorplint de kopse
            // kant afdekt en er van voren geen kopshout zichtbaar blijft.
            var frontPlinthLeftX = -config.WidthMm / 2.0 + (config.IncludeLeftSidePlinth ? 0.0 : config.DoorGapMm);
            var frontPlinthRightX = config.WidthMm / 2.0 - (config.IncludeRightSidePlinth ? 0.0 : config.DoorGapMm);
            var frontPlinthLength = frontPlinthRightX - frontPlinthLeftX;
            var plinth = Sheet("Losse voorzetplint", carcass, Math.Max(100, frontPlinthLength), plinthHeight);
            var frontPlinthClipCount = config.UnitCount + 1;
            var sidePlinthClipCount = (config.IncludeLeftSidePlinth ? 2 : 0) + (config.IncludeRightSidePlinth ? 2 : 0);
            AddFrontPlinthAdapterMountingMarks(plinth, config, frontPlinthLeftX, plinthHeight, unitWidth, t);
            AddSheet(
                model,
                plinth,
                (frontPlinthLeftX + frontPlinthRightX) / 2.0,
                config.PlinthFloorClearanceMm + plinthHeight / 2.0,
                frontZ + config.PlinthSetbackMm + t / 2.0,
                AssemblyOrientation.SheetVerticalX);

            var sidePlinthFrontZ = frontZ + config.PlinthSetbackMm + t;
            var sidePlinthBackZ = config.DepthMm / 2.0 - config.DoorGapMm;
            var sidePlinthLength = Math.Max(100, sidePlinthBackZ - sidePlinthFrontZ);
            var sidePlinthCenterZ = (sidePlinthFrontZ + sidePlinthBackZ) / 2.0;
            if (config.IncludeLeftSidePlinth)
            {
                var leftSidePlinth = Sheet("Zijplint links", carcass, sidePlinthLength, plinthHeight);
                AddSidePlinthAdapterMountingMarks(leftSidePlinth, config, plinthHeight, sidePlinthFrontZ, true);
                AddSheet(
                    model,
                    leftSidePlinth,
                    -config.WidthMm / 2.0 + t / 2.0,
                    config.PlinthFloorClearanceMm + plinthHeight / 2.0,
                    sidePlinthCenterZ,
                    AssemblyOrientation.SheetVerticalZ);
            }
            if (config.IncludeRightSidePlinth)
            {
                var rightSidePlinth = Sheet("Zijplint rechts", carcass, sidePlinthLength, plinthHeight);
                AddSidePlinthAdapterMountingMarks(rightSidePlinth, config, plinthHeight, sidePlinthFrontZ, false);
                AddSheet(
                    model,
                    rightSidePlinth,
                    config.WidthMm / 2.0 - t / 2.0,
                    config.PlinthFloorClearanceMm + plinthHeight / 2.0,
                    sidePlinthCenterZ,
                    AssemblyOrientation.SheetVerticalZ);
            }

            AddHardware(model, config, doorCount, hingeCount, frontPlinthClipCount, sidePlinthClipCount);
            return model;
        }

        private static void AddFrontPlinthAdapterMountingMarks(
            SheetPart plinth,
            WorkbenchCabinetConfig config,
            double frontPlinthLeftX,
            double plinthHeight,
            double unitWidth,
            double materialThickness)
        {
            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            var inset = Math.Max(config.AdjustableFootInsetMm, foot.FootDiameterMm / 2.0 + 1.0);
            var sideInset = ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
            var leftInset = config.IncludeLeftSidePlinth ? sideInset : inset;
            var rightInset = config.IncludeRightSidePlinth ? sideInset : inset;
            var clipCenterY = plinthHeight / 2.0 - 3.5;
            var screwOffsetY = adapter.MountingHoleSpacingMm / 2.0;

            for (var boundary = 0; boundary <= config.UnitCount; boundary++)
            {
                var fromOuterLeft = boundary * unitWidth;
                if (boundary == 0) fromOuterLeft = leftInset;
                else if (boundary == config.UnitCount) fromOuterLeft = config.WidthMm - rightInset;
                var worldX = -config.WidthMm / 2.0 + fromOuterLeft;
                var sheetX = worldX - frontPlinthLeftX;
                var wingSign = boundary == config.UnitCount ? -1.0 : 1.0;
                AddPlinthAdapterMountingMark(plinth, adapter, sheetX, clipCenterY - screwOffsetY, OperationFace.PositiveZ, "voor grens " + boundary.ToString(CultureInfo.InvariantCulture) + " onder");
                AddPlinthAdapterMountingMark(plinth, adapter, sheetX + wingSign * adapter.UpperMountingHoleHorizontalOffsetMm, clipCenterY + screwOffsetY, OperationFace.PositiveZ, "voor grens " + boundary.ToString(CultureInfo.InvariantCulture) + " boven op montagevleugel");
            }
        }

        private static void AddSidePlinthAdapterMountingMarks(
            SheetPart plinth,
            WorkbenchCabinetConfig config,
            double plinthHeight,
            double sidePlinthFrontZ,
            bool left)
        {
            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            var inset = Math.Max(config.AdjustableFootInsetMm, foot.FootDiameterMm / 2.0 + 1.0);
            var frontFootZ = -config.DepthMm / 2.0 + ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
            var backFootZ = config.DepthMm / 2.0 - inset;
            var clipCenterY = plinthHeight / 2.0 + 3.5;
            var screwOffsetY = adapter.MountingHoleSpacingMm / 2.0;
            var face = left ? OperationFace.PositiveX : OperationFace.NegativeX;
            var label = left ? "zijde links" : "zijde rechts";
            var positions = new[]
            {
                new { Name = label + " voor", X = frontFootZ - sidePlinthFrontZ, WingSign = 1.0 },
                new { Name = label + " achter", X = backFootZ - sidePlinthFrontZ, WingSign = -1.0 }
            };

            foreach (var position in positions)
            {
                AddPlinthAdapterMountingMark(plinth, adapter, position.X, clipCenterY - screwOffsetY, face, position.Name + " onder");
                AddPlinthAdapterMountingMark(plinth, adapter, position.X + position.WingSign * adapter.UpperMountingHoleHorizontalOffsetMm, clipCenterY + screwOffsetY, face, position.Name + " boven op montagevleugel");
            }
        }

        private static void AddPlinthAdapterMountingMark(
            SheetPart plinth,
            PlinthClipAdapterTemplate adapter,
            double x,
            double y,
            OperationFace face,
            string position)
        {
            AddBlindHole(
                plinth,
                x,
                y,
                adapter.PlinthCenterMarkDiameterMm,
                adapter.PlinthCenterMarkDepthMm,
                face,
                "Blind centreergat plintclip-adapter " + position,
                SheetHoleSupportKind.PlinthClip);
        }

        private static void AddSecondFullDepthDivider(
            WorkbenchModel model,
            SheetPart bottom,
            SheetPart worktop,
            WorkbenchCabinetConfig config,
            Material material,
            BoundaryPanel boundary,
            double x,
            double dividerDepth,
            double dividerCenterZ,
            double panelHeight,
            double panelCenterY,
            double grooveDepth)
        {
            var t = MaterialThickness(material);
            var divider = Sheet("Volledig tussenschot dubbel rechts U" + boundary.UnitBoundary.ToString(CultureInfo.InvariantCulture), material, dividerDepth, panelHeight);
            AddSupportGrooves(bottom, worktop, config, x, t, 0, dividerDepth, grooveDepth, "dubbel middenschot rechts U" + boundary.UnitBoundary.ToString(CultureInfo.InvariantCulture));
            AddSheet(model, divider, x, panelCenterY, dividerCenterZ, AssemblyOrientation.SheetVerticalZ);
            boundary.PositiveThicknessMm = t;
            boundary.PositiveHingeSheet = divider;
        }

        private static void AddDoorStopToDivider(
            WorkbenchModel model,
            SheetPart bottom,
            SheetPart worktop,
            WorkbenchCabinetConfig config,
            Material material,
            double x,
            double panelHeight,
            double panelCenterY,
            double bottomTopY,
            double doorTopLimitY,
            double grooveDepth)
        {
            var t = MaterialThickness(material);
            var stopBottomY = bottomTopY - grooveDepth;
            var stopTopY = config.IncludeTopDrawer ? doorTopLimitY : stopBottomY + panelHeight;
            var stopHeight = Math.Max(80.0, stopTopY - stopBottomY);
            var stopCenterY = stopBottomY + stopHeight / 2.0;
            var stop = Sheet("T-stijl deuraanslag X" + x.ToString("0", CultureInfo.InvariantCulture), material, config.DoorStopWidthMm, stopHeight);

            var alignmentSlotWidth = t + AlignmentGrooveClearanceMm;
            SheetOperations.AddPocket(
                stop,
                "Centreersleuf middenstaander achterzijde",
                (stop.LengthMm - alignmentSlotWidth) / 2.0,
                0,
                alignmentSlotWidth,
                stop.WidthMm,
                grooveDepth,
                OperationFace.PositiveZ,
                "Verticale centreersleuf over de voorkant van het volledige tussenschot.");

            var fastenerDiameter = config.SheetFastener == null || config.SheetFastener.ClearanceHoleDiameterMm <= 0
                ? ProductDrawingStrategy.DefaultWoodScrewClearanceHoleDiameterMm
                : config.SheetFastener.ClearanceHoleDiameterMm;
            var mountingYs = SheetPatterns.EdgeDistributedPositions(stop.WidthMm, 90.0, 260.0, 3);
            foreach (var mountingY in mountingYs)
            {
                AddThroughHole(
                    stop,
                    stop.LengthMm / 2.0,
                    mountingY,
                    fastenerDiameter,
                    "T-stijl naar middenstaander",
                    SheetHoleSupportKind.PanelScrew,
                    OperationFace.CenterPlane);
            }

            AddSupportGroove(bottom, x, 0, config.DoorStopWidthMm, t, grooveDepth, OperationFace.PositiveY, "T-stijl deuraanslag");
            if (!config.IncludeTopDrawer)
            {
                AddSupportGroove(worktop, x, 0, config.DoorStopWidthMm, t, grooveDepth, OperationFace.NegativeY, "T-stijl deuraanslag");
            }
            AddSheet(model, stop, x, stopCenterY, -config.DepthMm / 2.0 + t / 2.0, AssemblyOrientation.SheetVerticalX);
        }

        private static void AddBoundaryShelfSupportRows(List<BoundaryPanel> boundaries, WorkbenchCabinetConfig config, double grooveDepth, double backThickness, double shelfZoneTopY, double panelCenterY, double panelHeight)
        {
            if (!config.IncludeAdjustableShelfHoles || config.ShelfSupport == null) return;

            var frontRowX = config.ShelfSupport.FrontInsetMm + config.ShelfFrontInsetMm;
            var backRowX = config.DepthMm - backThickness - config.ShelfSupport.BackInsetMm;
            var panelBottomY = panelCenterY - panelHeight / 2.0;
            foreach (var boundary in boundaries)
            {
                if (boundary.UnitBoundary == 0)
                {
                    AddShelfSupportRow(boundary.Sheet, config, frontRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.PositiveX, "linker zijwand voor");
                    AddShelfSupportRow(boundary.Sheet, config, backRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.PositiveX, "linker zijwand achter");
                }
                else if (boundary.UnitBoundary == config.UnitCount)
                {
                    AddShelfSupportRow(boundary.Sheet, config, frontRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.NegativeX, "rechter zijwand voor");
                    AddShelfSupportRow(boundary.Sheet, config, backRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.NegativeX, "rechter zijwand achter");
                }
                else
                {
                    if (boundary.PositiveHingeSheet == null)
                    {
                        AddThroughShelfSupportRow(boundary.Sheet, config, frontRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, "gedeeld tussenschot voor");
                        AddThroughShelfSupportRow(boundary.Sheet, config, backRowX - boundary.FrontDepthTrimMm, grooveDepth, shelfZoneTopY, panelBottomY, "gedeeld tussenschot achter");
                    }
                    else
                    {
                        AddShelfSupportRow(boundary.Sheet, config, frontRowX, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.NegativeX, "dubbel middenschot linker unit voor");
                        AddShelfSupportRow(boundary.Sheet, config, backRowX, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.NegativeX, "dubbel middenschot linker unit achter");
                        AddShelfSupportRow(boundary.PositiveHingeSheet, config, frontRowX, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.PositiveX, "dubbel middenschot rechter unit voor");
                        AddShelfSupportRow(boundary.PositiveHingeSheet, config, backRowX, grooveDepth, shelfZoneTopY, panelBottomY, OperationFace.PositiveX, "dubbel middenschot rechter unit achter");
                    }
                }
            }
        }

        private static void AddShelfSupportRow(SheetPart panel, WorkbenchCabinetConfig config, double rowX, double grooveDepth, double shelfZoneTopY, double panelBottomY, OperationFace face, string label)
        {
            if (!config.IncludeAdjustableShelfHoles || panel == null || config.ShelfSupport == null) return;

            var support = config.ShelfSupport;
            var depth = Math.Min(Math.Max(0.1, support.HeightMm), MaterialThickness(panel.Material) - 0.1);
            var index = 1;
            foreach (var y in AdjustableShelfLocalPositions(panel, config, grooveDepth, shelfZoneTopY, panelBottomY))
            {
                AddBlindHole(
                    panel,
                    rowX,
                    y,
                    support.HoleDiameterMm,
                    depth,
                    face,
                    "Legplankdrager " + label + " " + index.ToString(CultureInfo.InvariantCulture),
                    SheetHoleSupportKind.ShelfSupport);
                index++;
            }
        }

        private static void AddThroughShelfSupportRow(SheetPart panel, WorkbenchCabinetConfig config, double rowX, double grooveDepth, double shelfZoneTopY, double panelBottomY, string label)
        {
            if (!config.IncludeAdjustableShelfHoles || panel == null || config.ShelfSupport == null) return;

            var index = 1;
            foreach (var y in AdjustableShelfLocalPositions(panel, config, grooveDepth, shelfZoneTopY, panelBottomY))
            {
                AddThroughHole(
                    panel,
                    rowX,
                    y,
                    config.ShelfSupport.HoleDiameterMm,
                    "Legplankdrager " + label + " " + index.ToString(CultureInfo.InvariantCulture),
                    SheetHoleSupportKind.ShelfSupport,
                    OperationFace.CenterPlane);
                index++;
            }
        }

        private static List<double> AdjustableShelfLocalPositions(SheetPart panel, WorkbenchCabinetConfig config, double grooveDepth, double shelfZoneTopY, double panelBottomY)
        {
            var result = new List<double>();
            if (panel == null || config == null || config.ShelfSupport == null) return result;

            var support = config.ShelfSupport;
            var spacing = Math.Max(1.0, support.HoleSpacingMm);
            var firstY = grooveDepth + support.FirstHoleHeightMm;
            var lastY = Math.Min(panel.WidthMm - grooveDepth, shelfZoneTopY - panelBottomY) - config.AdjustableShelfHoleEndMarginMm;
            var available = new List<double>();
            for (var y = firstY; y <= lastY + 0.01; y += spacing) available.Add(Math.Round(y, 3));
            if (available.Count == 0) return result;

            var count = Math.Min(Math.Max(1, config.AdjustableShelfPositionCount), available.Count);
            for (var i = 0; i < count; i++)
            {
                var index = count == 1
                    ? available.Count / 2
                    : (int)Math.Round((available.Count - 1.0) * i / (count - 1.0));
                if (result.Count == 0 || Math.Abs(result[result.Count - 1] - available[index]) > 0.01)
                {
                    result.Add(available[index]);
                }
            }

            return result;
        }

        private static void AddBoundaryDrawerRailHoles(List<BoundaryPanel> boundaries, WorkbenchCabinetConfig config, double railWorldY, double panelCenterY, double panelHeight)
        {
            if (!config.IncludeTopDrawer || config.DrawerRail == null) return;
            var panelBottomY = panelCenterY - panelHeight / 2.0;
            foreach (var boundary in boundaries)
            {
                if (boundary.UnitBoundary == 0)
                {
                    AddDrawerRailHolesForSegment(boundary.Sheet, config, boundary.FrontDepthTrimMm, railWorldY, panelBottomY, "linker zijwand", OperationFace.PositiveX, false, false);
                }
                else if (boundary.UnitBoundary == config.UnitCount)
                {
                    AddDrawerRailHolesForSegment(boundary.Sheet, config, boundary.FrontDepthTrimMm, railWorldY, panelBottomY, "rechter zijwand", OperationFace.NegativeX, false, false);
                }
                else
                {
                    if (boundary.PositiveHingeSheet != null)
                    {
                        AddDrawerRailHolesForSegment(boundary.Sheet, config, boundary.FrontDepthTrimMm, railWorldY, panelBottomY, "dubbel middenschot links", OperationFace.CenterPlane, true, false);
                        AddDrawerRailHolesForSegment(boundary.PositiveHingeSheet, config, 0.0, railWorldY, panelBottomY, "dubbel middenschot rechts", OperationFace.CenterPlane, true, false);
                    }
                    else
                    {
                        AddDrawerRailHolesForSegment(boundary.Sheet, config, boundary.FrontDepthTrimMm, railWorldY, panelBottomY, "tussenschot gedeeld versprongen", OperationFace.CenterPlane, true, true);
                    }
                }
            }
        }

        private static void AddDrawerRailHolesForSegment(SheetPart panel, WorkbenchCabinetConfig config, double segmentStartDepth, double railWorldY, double panelBottomY, string label, OperationFace face, bool through, bool includeOppositePattern)
        {
            if (!config.IncludeTopDrawer || panel == null || config.DrawerRail == null) return;
            var rail = config.DrawerRail;
            var positions = RailHolePositions(rail.CabinetHolePositionsMm, rail.CabinetHoleCount, rail.CabinetFirstHoleOffsetMm, rail.CabinetHoleSpacingMm);
            if (includeOppositePattern)
            {
                var opposite = RailHolePositions(
                    string.IsNullOrWhiteSpace(rail.CabinetOppositeHolePositionsMm) ? rail.CabinetHolePositionsMm : rail.CabinetOppositeHolePositionsMm,
                    rail.CabinetHoleCount,
                    rail.CabinetFirstHoleOffsetMm,
                    rail.CabinetHoleSpacingMm);
                positions = MergeRailHolePositions(positions, opposite);
            }
            var frontInset = Math.Max(0.0, MaterialThickness(config.FrontMaterial) - ProductDrawingStrategy.DefaultDrawerGrooveDepthMm);
            var localY = railWorldY - panelBottomY;
            foreach (var position in positions)
            {
                var worldDepth = frontInset + position;
                var localX = worldDepth - segmentStartDepth;
                if (localX <= 5.0 || localX >= panel.LengthMm - 5.0) continue;
                var name = "Bovenlade railgat " + label + " D" + worldDepth.ToString("0", CultureInfo.InvariantCulture);
                if (through)
                {
                    AddThroughHole(panel, localX, localY, RailPilotHoleDiameterMm(rail, rail.CabinetHoleDiameterMm), name, SheetHoleSupportKind.DrawerRail, OperationFace.CenterPlane);
                }
                else
                {
                    AddBlindHole(
                        panel,
                        localX,
                        localY,
                        RailPilotHoleDiameterMm(rail, rail.CabinetHoleDiameterMm),
                        Math.Min(OuterWallRailHoleDepthMm, MaterialThickness(panel.Material) - 0.1),
                        face,
                        name,
                        SheetHoleSupportKind.DrawerRail);
                }
            }
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
                    if (double.TryParse(part.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) positions.Add(value);
                }
            }

            if (positions.Count > 0) return positions;
            for (var i = 0; i < count; i++) positions.Add(firstOffset + i * spacing);
            return positions;
        }

        private static List<double> MergeRailHolePositions(List<double> first, List<double> second)
        {
            var merged = new List<double>();
            foreach (var position in first) AddUniqueRailPosition(merged, position);
            foreach (var position in second) AddUniqueRailPosition(merged, position);
            merged.Sort();
            return merged;
        }

        private static void AddUniqueRailPosition(List<double> positions, double candidate)
        {
            foreach (var position in positions)
            {
                if (Math.Abs(position - candidate) <= 0.05) return;
            }
            positions.Add(candidate);
        }

        private static double RailPilotHoleDiameterMm(RailTemplate rail, double configuredDiameter)
        {
            return configuredDiameter;
        }

        private static void AddUnitShelves(
            WorkbenchModel model,
            WorkbenchCabinetConfig config,
            Material material,
            double thickness,
            int firstUnit,
            int unitsInGroup,
            double openingLeftX,
            double openingRightX,
            double openingCenterX,
            double bottomTopY,
            double worktopBottomY,
            double backThickness)
        {
            if (config.ShelfCountPerUnit <= 0) return;

            var shelfDepth = config.DepthMm - backThickness - config.ShelfClearanceMm - config.ShelfFrontInsetMm;
            if (shelfDepth <= 40.0) return;
            var shelfCenterZ = -config.DepthMm / 2.0 + config.ShelfFrontInsetMm + shelfDepth / 2.0;
            var supportHeights = ShelfSupportWorldHeights(config, bottomTopY, worktopBottomY);
            for (var localUnit = 0; localUnit < unitsInGroup; localUnit++)
            {
                var leftX = unitsInGroup == 2 && localUnit == 1
                    ? openingCenterX + thickness / 2.0
                    : openingLeftX;
                var rightX = unitsInGroup == 2 && localUnit == 0
                    ? openingCenterX - thickness / 2.0
                    : openingRightX;
                var shelfWidth = rightX - leftX - config.ShelfClearanceMm;
                if (shelfWidth <= 40.0) continue;

                var unitNumber = firstUnit + localUnit + 1;
                for (var shelfIndex = 0; shelfIndex < supportHeights.Count; shelfIndex++)
                {
                    var shelf = Sheet(
                        "Legplank unit " + unitNumber.ToString(CultureInfo.InvariantCulture) + " niveau " + (shelfIndex + 1).ToString(CultureInfo.InvariantCulture),
                        material,
                        shelfWidth,
                        shelfDepth);
                    if (unitsInGroup == 2)
                    {
                        AddDoorStopShelfNotch(shelf, config, thickness, localUnit == 0);
                    }
                    AddSheet(
                        model,
                        shelf,
                        (leftX + rightX) / 2.0,
                        supportHeights[shelfIndex] + thickness / 2.0,
                        shelfCenterZ,
                        AssemblyOrientation.SheetHorizontal);
                }
            }
        }

        private static void AddDoorStopShelfNotch(SheetPart shelf, WorkbenchCabinetConfig config, double dividerThickness, bool atRightEdge)
        {
            if (shelf == null) return;
            var notchWidth = Math.Max(0.0, (config.DoorStopWidthMm - dividerThickness) / 2.0 + DoorStopShelfClearanceMm);
            var notchDepth = Math.Max(0.0, dividerThickness + DoorStopShelfClearanceMm - config.ShelfFrontInsetMm);
            if (notchWidth <= 0.01 || notchDepth <= 0.01) return;

            SheetOperations.AddThroughCutout(
                shelf,
                atRightEdge ? "Uitsparing T-stijl rechtsvoor" : "Uitsparing T-stijl linksvoor",
                atRightEdge ? shelf.LengthMm - notchWidth : 0,
                0,
                notchWidth,
                Math.Min(shelf.WidthMm, notchDepth),
                OperationFace.CenterPlane,
                "Vrijloop rond de voorste T-vormige deuraanslag.");
        }

        private static List<double> ShelfSupportWorldHeights(WorkbenchCabinetConfig config, double bottomTopY, double worktopBottomY)
        {
            var result = new List<double>();
            if (config.ShelfCountPerUnit <= 0 || config.ShelfSupport == null) return result;

            var support = config.ShelfSupport;
            var virtualPanel = new SheetPart
            {
                Material = config.CarcassMaterial,
                LengthMm = config.DepthMm,
                WidthMm = Math.Max(1.0, worktopBottomY - bottomTopY)
            };
            var localPositions = AdjustableShelfLocalPositions(virtualPanel, config, 0.0, worktopBottomY, bottomTopY);
            var available = new List<double>();
            foreach (var localY in localPositions) available.Add(Math.Round(bottomTopY + localY, 3));
            if (available.Count == 0) return result;

            var count = Math.Min(config.ShelfCountPerUnit, available.Count);
            var startTop = string.Equals(config.ShelfStartMode, "top", StringComparison.OrdinalIgnoreCase);
            var selected = new List<double>();
            for (var i = 0; i < count; i++)
            {
                var index = count == 1
                    ? (startTop ? available.Count - 1 : 0)
                    : (int)Math.Round((available.Count - 1.0) * i / (count - 1.0));
                if (selected.Count == 0 || Math.Abs(selected[selected.Count - 1] - available[index]) > 0.01)
                {
                    selected.Add(available[index]);
                }
            }

            if (startTop && selected.Count < count)
            {
                for (var i = available.Count - 1; i >= 0 && selected.Count < count; i--)
                {
                    if (!selected.Contains(available[i])) selected.Add(available[i]);
                }
                selected.Sort();
            }

            result.AddRange(selected);

            return result;
        }

        private static void BuildTopDrawer(
            WorkbenchModel model,
            WorkbenchCabinetConfig config,
            Material carcassMaterial,
            Material drawerMaterial,
            Material frontMaterial,
            int unitNumber,
            double bayLeftX,
            double bayRightX,
            double frontLeftX,
            double frontRightX,
            double frontBottomY,
            double frontTopY,
            double frontZ,
            double backThickness)
        {
            if (!config.IncludeTopDrawer || config.DrawerRail == null) return;

            var drawerT = MaterialThickness(drawerMaterial);
            var frontHeight = frontTopY - frontBottomY;
            var frontWidth = frontRightX - frontLeftX;
            var bayWidth = bayRightX - bayLeftX;
            if (frontHeight < 80.0 || frontWidth < 100.0 || bayWidth < 120.0) return;

            var rail = config.DrawerRail;
            var boxWidth = Math.Max(80.0, bayWidth - 2.0 * config.DrawerSideClearanceMm);
            var availableDepth = Math.Max(120.0, config.DepthMm - backThickness - config.DrawerBackClearanceMm);
            var boxDepth = rail.LengthMm > 0 ? Math.Min(availableDepth, rail.LengthMm) : availableDepth;
            var grooveDepth = ProductDrawingStrategy.DefaultDrawerGrooveDepthMm;
            var sideLength = boxDepth + grooveDepth;
            var bottomWidth = Math.Max(60.0, boxWidth - 2.0 * drawerT + 2.0 * grooveDepth);
            var bottomDepth = Math.Max(80.0, boxDepth - drawerT + 2.0 * grooveDepth);
            var centerX = (bayLeftX + bayRightX) / 2.0;
            var frontCenterX = (frontLeftX + frontRightX) / 2.0;
            var frontCenterY = (frontBottomY + frontTopY) / 2.0;
            var worktopThickness = MaterialThickness(config.WorktopMaterial ?? carcassMaterial);
            var boxTopY = Math.Min(frontTopY, config.WorktopHeightMm - worktopThickness - DrawerFrontTopGapMm);
            var boxHeight = boxTopY - frontBottomY;
            var boxCenterY = (frontBottomY + boxTopY) / 2.0;
            if (boxHeight < 60.0) return;
            var boxCenterZ = frontZ - grooveDepth + sideLength / 2.0;
            var bottomCenterZ = frontZ - grooveDepth + bottomDepth / 2.0;
            var backCenterZ = frontZ + boxDepth - drawerT / 2.0;

            var drawerFront = Sheet("Bovenlade front U" + unitNumber.ToString(CultureInfo.InvariantCulture), frontMaterial, frontWidth, frontHeight);
            drawerFront.CornerRadiusMm = Math.Min(config.FrontPanelCornerRadiusMm, Math.Min(frontWidth, frontHeight) / 2.0);
            AddDrawerFrontGrooves(drawerFront, boxWidth, boxHeight, drawerMaterial);
            AddDrawerPullCutout(drawerFront, config);

            var drawerBottom = Sheet("Bovenlade bodem U" + unitNumber.ToString(CultureInfo.InvariantCulture), drawerMaterial, bottomWidth, bottomDepth);
            var drawerSideLeft = Sheet("Bovenlade zijde links U" + unitNumber.ToString(CultureInfo.InvariantCulture), drawerMaterial, sideLength, boxHeight);
            var drawerSideRight = Sheet("Bovenlade zijde rechts U" + unitNumber.ToString(CultureInfo.InvariantCulture), drawerMaterial, sideLength, boxHeight);
            drawerSideRight.MirrorInNestingX = true;
            var drawerBack = Sheet("Bovenlade achter U" + unitNumber.ToString(CultureInfo.InvariantCulture), drawerMaterial, bottomWidth, boxHeight);

            AddDrawerBottomGroove(drawerSideLeft, drawerMaterial, OperationFace.PositiveX);
            AddDrawerBottomGroove(drawerSideRight, drawerMaterial, OperationFace.NegativeX);
            AddDrawerBackGroove(drawerSideLeft, drawerMaterial, OperationFace.PositiveX);
            AddDrawerBackGroove(drawerSideRight, drawerMaterial, OperationFace.NegativeX);
            AddDrawerBottomGroove(drawerBack, drawerMaterial, OperationFace.NegativeZ);
            AddDrawerAssemblyHoles(config, drawerFront, drawerSideLeft, drawerSideRight, drawerBack, boxWidth, drawerT);
            AddDrawerSideRailHoles(drawerSideLeft, config);
            AddDrawerSideRailHoles(drawerSideRight, config);

            AddSheet(model, drawerFront, frontCenterX, frontCenterY, frontZ - MaterialThickness(frontMaterial) / 2.0, AssemblyOrientation.SheetVerticalX);
            AddSheet(model, drawerBottom, centerX, frontBottomY + drawerT / 2.0, bottomCenterZ, AssemblyOrientation.SheetHorizontal);
            AddSheet(model, drawerSideLeft, centerX - boxWidth / 2.0 + drawerT / 2.0, boxCenterY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerSideRight, centerX + boxWidth / 2.0 - drawerT / 2.0, boxCenterY, boxCenterZ, AssemblyOrientation.SheetVerticalZ);
            AddSheet(model, drawerBack, centerX, boxCenterY, backCenterZ, AssemblyOrientation.SheetVerticalX);
        }

        private static void AddDrawerBottomGroove(SheetPart panel, Material drawerMaterial, OperationFace face)
        {
            if (panel == null || drawerMaterial == null) return;
            var grooveHeight = Math.Min(panel.WidthMm - 2.0, MaterialThickness(drawerMaterial) + ProductDrawingStrategy.DefaultDrawerGrooveClearanceMm);
            if (grooveHeight <= 0) return;
            SheetOperations.AddPocket(panel, "Ladebodem rabat", 0, 0, panel.LengthMm, grooveHeight, ProductDrawingStrategy.DefaultDrawerGrooveDepthMm, face, "Rabat voor de ladebodem.");
        }

        private static void AddDrawerBackGroove(SheetPart sidePanel, Material drawerMaterial, OperationFace face)
        {
            if (sidePanel == null || drawerMaterial == null) return;
            var grooveWidth = Math.Min(sidePanel.LengthMm - 2.0, MaterialThickness(drawerMaterial) + ProductDrawingStrategy.DefaultDrawerGrooveClearanceMm);
            if (grooveWidth <= 0) return;
            SheetOperations.AddPocket(sidePanel, "Ladeachter rabat", sidePanel.LengthMm - grooveWidth, 0, grooveWidth, sidePanel.WidthMm, ProductDrawingStrategy.DefaultDrawerGrooveDepthMm, face, "Rabat voor de ladeachterzijde.");
        }

        private static void AddDrawerFrontGrooves(SheetPart front, double boxWidth, double boxHeight, Material drawerMaterial)
        {
            if (front == null || drawerMaterial == null) return;
            var grooveWidth = Math.Min(front.LengthMm / 3.0, MaterialThickness(drawerMaterial) + ProductDrawingStrategy.DefaultDrawerGrooveClearanceMm);
            var grooveHeight = Math.Min(front.WidthMm, MaterialThickness(drawerMaterial) + ProductDrawingStrategy.DefaultDrawerGrooveClearanceMm);
            var sideGrooveHeight = Math.Min(front.WidthMm - 1.0, Math.Max(10.0, boxHeight + ProductDrawingStrategy.DefaultDrawerGrooveDepthMm));
            var sideInset = Math.Max(0, (front.LengthMm - boxWidth) / 2.0);
            SheetOperations.AddPocket(front, "Ladefront linker zij-rabat", sideInset, 0, grooveWidth, sideGrooveHeight, ProductDrawingStrategy.DefaultDrawerGrooveDepthMm, OperationFace.PositiveZ, "Blind eindigende rabat voor linker ladezijde; bovenrand van het front blijft gesloten.");
            SheetOperations.AddPocket(front, "Ladefront rechter zij-rabat", front.LengthMm - sideInset - grooveWidth, 0, grooveWidth, sideGrooveHeight, ProductDrawingStrategy.DefaultDrawerGrooveDepthMm, OperationFace.PositiveZ, "Blind eindigende rabat voor rechter ladezijde; bovenrand van het front blijft gesloten.");
            SheetOperations.AddPocket(front, "Ladefront bodem-rabat", sideInset, 0, Math.Max(10, front.LengthMm - 2.0 * sideInset), grooveHeight, ProductDrawingStrategy.DefaultDrawerGrooveDepthMm, OperationFace.PositiveZ, "Rabat voor ladebodem.");
        }

        private static void AddDrawerPullCutout(SheetPart front, WorkbenchCabinetConfig config)
        {
            if (front == null || config == null || !config.IncludeDrawerPullCutouts) return;
            var length = Math.Min(140.0, front.LengthMm - 120.0);
            var height = Math.Min(32.0, front.WidthMm - 52.0);
            if (length < 80.0 || height < 14.0) return;
            var x = (front.LengthMm - length) / 2.0;
            var y = Math.Max(26.0, front.WidthMm - 30.0 - height);
            var radius = height / 2.0;
            var skinDepth = Math.Max(0.1, MaterialThickness(front.Material) - 2.0);
            SheetOperations.AddPocket(front, "Uitgefreesde handgreep midden tot 2mm restmateriaal", x + radius, y, Math.Max(1.0, length - height), height, skinDepth, OperationFace.PositiveZ, "Capsule eerst uitruimen met 2mm restmateriaal voor vacuümbehoud.");
            AddBlindHole(front, x + radius, y + radius, height, skinDepth, OperationFace.PositiveZ, "Uitgefreesde handgreep ronding links tot 2mm restmateriaal", SheetHoleSupportKind.MachiningCutout);
            AddBlindHole(front, x + length - radius, y + radius, height, skinDepth, OperationFace.PositiveZ, "Uitgefreesde handgreep ronding rechts tot 2mm restmateriaal", SheetHoleSupportKind.MachiningCutout);
            SheetOperations.AddThroughCutout(front, "Uitgefreesde handgreep afwerkcontour tabs 8x2 max70", x, y, length, height, OperationFace.CenterPlane, "Alleen capsule-binnencontour doorfrezen; tabs 8mm breed, 2mm hoog en maximaal 70mm uit elkaar.");
        }

        private static void AddDrawerAssemblyHoles(WorkbenchCabinetConfig config, SheetPart drawerFront, SheetPart drawerSideLeft, SheetPart drawerSideRight, SheetPart drawerBack, double boxWidth, double drawerThickness)
        {
            var diameter = config.SheetFastener == null || config.SheetFastener.ClearanceHoleDiameterMm <= 0
                ? ProductDrawingStrategy.DefaultWoodScrewClearanceHoleDiameterMm
                : config.SheetFastener.ClearanceHoleDiameterMm;
            var frontSideInset = Math.Max(0, (drawerFront.LengthMm - boxWidth) / 2.0);
            var leftSideX = frontSideInset + drawerThickness / 2.0;
            var rightSideX = drawerFront.LengthMm - frontSideInset - drawerThickness / 2.0;
            foreach (var y in SheetPatterns.EdgeDistributedPositions(drawerFront.WidthMm, 32, DrawerVerticalFastenerMaxSpacingMm, 2))
            {
                AddThroughHole(drawerFront, leftSideX, y, diameter, "Ladefront naar linker zijkant", SheetHoleSupportKind.PanelScrew, OperationFace.CenterPlane);
                AddThroughHole(drawerFront, rightSideX, y, diameter, "Ladefront naar rechter zijkant", SheetHoleSupportKind.PanelScrew, OperationFace.CenterPlane);
            }

            var sideYs = SheetPatterns.EdgeDistributedPositions(drawerSideLeft.WidthMm, 32, DrawerVerticalFastenerMaxSpacingMm, 2);
            var backX = Math.Max(20, drawerSideLeft.LengthMm - drawerThickness / 2.0);
            foreach (var y in sideYs)
            {
                AddThroughHole(drawerSideLeft, backX, y, diameter, "Ladezijde links naar achterzijde", SheetHoleSupportKind.PanelScrew, OperationFace.CenterPlane);
                AddThroughHole(drawerSideRight, backX, y, diameter, "Ladezijde rechts naar achterzijde", SheetHoleSupportKind.PanelScrew, OperationFace.CenterPlane);
            }

            var bottomY = drawerThickness / 2.0;
            var bottomInset = Math.Max(30, drawerThickness + 14);
            SheetOperations.AddMountingLine(drawerFront, frontSideInset + bottomInset, bottomY, drawerFront.LengthMm - frontSideInset - bottomInset, bottomY, diameter, DrawerBottomFastenerMaxSpacingMm, "Montagegat ladefront naar ladebodem", SheetHoleSupportKind.PanelScrew);
            SheetOperations.AddMountingLine(drawerBack, bottomInset, bottomY, drawerBack.LengthMm - bottomInset, bottomY, diameter, DrawerBottomFastenerMaxSpacingMm, "Montagegat ladeachter naar ladebodem", SheetHoleSupportKind.PanelScrew);
            SheetOperations.AddMountingLine(drawerSideLeft, bottomInset, bottomY, drawerSideLeft.LengthMm - bottomInset, bottomY, diameter, DrawerBottomFastenerMaxSpacingMm, "Montagegat linker ladezijde naar ladebodem", SheetHoleSupportKind.PanelScrew);
            SheetOperations.AddMountingLine(drawerSideRight, bottomInset, bottomY, drawerSideRight.LengthMm - bottomInset, bottomY, diameter, DrawerBottomFastenerMaxSpacingMm, "Montagegat rechter ladezijde naar ladebodem", SheetHoleSupportKind.PanelScrew);
        }

        private static void AddDrawerSideRailHoles(SheetPart side, WorkbenchCabinetConfig config)
        {
            if (side == null || config.DrawerRail == null) return;
            var rail = config.DrawerRail;
            var positions = RailHolePositions(rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm);
            var frontInsert = Math.Max(0.0, rail.DrawerFrontInsertionCompensationMm);
            foreach (var measuredFromRailFront in positions)
            {
                var x = measuredFromRailFront + frontInsert;
                if (x <= 5 || x >= side.LengthMm - 5) continue;
                AddThroughHole(side, x, rail.DrawerVerticalOffsetMm, RailPilotHoleDiameterMm(rail, rail.DrawerHoleDiameterMm), "Laderailgat ladezijde", SheetHoleSupportKind.DrawerRail, OperationFace.CenterPlane);
            }
        }

        private static SheetPart BuildDoor(string name, Material material, double width, double height, bool hingeLeft, List<double> hingeWorldYs, double doorBottomY, double cornerRadiusMm)
        {
            var door = Sheet(name, material, width, height);
            door.CornerRadiusMm = Math.Min(cornerRadiusMm, Math.Min(width, height) / 2.0);
            var cupX = hingeLeft ? HingeCupEdgeOffsetMm : width - HingeCupEdgeOffsetMm;
            var screwX = hingeLeft
                ? cupX + HingeDoorHoleLineOffsetMm
                : cupX - HingeDoorHoleLineOffsetMm;
            foreach (var worldY in hingeWorldYs)
            {
                var y = worldY - doorBottomY;
                AddBlindHole(door, cupX, y, HingeCupDiameterMm, HingeCupDepthMm, OperationFace.PositiveZ, "KOMPLEMENT potgat 35x13mm", SheetHoleSupportKind.HingeCup);
                AddBlindHole(door, screwX, y - HingeDoorHoleSpacingMm / 2.0, HingeDoorHoleDiameterMm, HingeDoorHoleDepthMm, OperationFace.PositiveZ, "KOMPLEMENT deurgat 4,5x12 A", SheetHoleSupportKind.HingeScrew);
                AddBlindHole(door, screwX, y + HingeDoorHoleSpacingMm / 2.0, HingeDoorHoleDiameterMm, HingeDoorHoleDepthMm, OperationFace.PositiveZ, "KOMPLEMENT deurgat 4,5x12 B", SheetHoleSupportKind.HingeScrew);
            }

            return door;
        }

        private static void AddHingePlateHoles(SheetPart panel, OperationFace face, List<double> hingeWorldYs, double panelCenterY, double panelHeight, double thickness)
        {
            var panelBottomY = panelCenterY - panelHeight / 2.0;
            foreach (var worldY in hingeWorldYs)
            {
                var y = worldY - panelBottomY;
                var depth = Math.Min(HingePlateHoleDepthMm, thickness - 0.1);
                AddBlindHole(panel, HingePlateFrontInsetMm, y - HingePlateHoleSpacingMm / 2.0, HingePlateHoleDiameterMm, depth, face, "KOMPLEMENT montageplaatgat 4,5x10 A", SheetHoleSupportKind.HingePlate);
                AddBlindHole(panel, HingePlateFrontInsetMm, y + HingePlateHoleSpacingMm / 2.0, HingePlateHoleDiameterMm, depth, face, "KOMPLEMENT montageplaatgat 4,5x10 B", SheetHoleSupportKind.HingePlate);
            }
        }

        private static List<double> HingeWorldPositions(double doorBottomY, double doorTopY)
        {
            var height = doorTopY - doorBottomY;
            var inset = Math.Min(HingeVerticalInsetMm, height / 3.0);
            var positions = new List<double> { doorBottomY + inset, doorTopY - inset };
            if (height >= 900.0) positions.Insert(1, (doorBottomY + doorTopY) / 2.0);
            return positions;
        }

        private static void AddFullDepthSupportGrooves(SheetPart bottom, SheetPart worktop, WorkbenchCabinetConfig config, double x, double thickness, double grooveDepth, string label)
        {
            AddSupportGrooves(bottom, worktop, config, x, thickness, 0, config.DepthMm, grooveDepth, label);
        }

        private static void AddSupportGrooves(SheetPart bottom, SheetPart worktop, WorkbenchCabinetConfig config, double x, double thickness, double depthStart, double depth, double grooveDepth, string label)
        {
            AddSupportGroove(bottom, x, depthStart, thickness, depth, grooveDepth, OperationFace.PositiveY, label);
            AddSupportGroove(worktop, x, depthStart, thickness, depth, grooveDepth, OperationFace.NegativeY, label);
            var firstDepth = depthStart + Math.Min(120.0, depth / 3.0);
            var secondDepth = depthStart + Math.Max(Math.Min(120.0, depth / 3.0), depth - 120.0);
            AddHorizontalSupportMountingHoles(bottom, worktop, config, x, firstDepth, secondDepth, label);
        }

        private static void AddSupportGroove(SheetPart sheet, double worldX, double depthStart, double width, double depth, double grooveDepth, OperationFace face, string label)
        {
            var grooveWidth = width + AlignmentGrooveClearanceMm;
            var localX = worldX + sheet.LengthMm / 2.0 - grooveWidth / 2.0;
            SheetOperations.AddPocket(sheet, "Positioneergroef " + label, localX, depthStart, grooveWidth, depth, grooveDepth, face, "Positionering van dragend deel in doorlopende plaat.");
        }

        private static void AddHorizontalSupportMountingHoles(SheetPart bottom, SheetPart worktop, WorkbenchCabinetConfig config, double worldX, double firstDepth, double secondDepth, string label)
        {
            var diameter = config.SheetFastener == null || config.SheetFastener.ClearanceHoleDiameterMm <= 0
                ? ProductDrawingStrategy.DefaultWoodScrewClearanceHoleDiameterMm
                : config.SheetFastener.ClearanceHoleDiameterMm;
            var localX = worldX + config.WidthMm / 2.0;
            AddThroughHole(bottom, localX, firstDepth, diameter, "Bodemmontage " + label + " voor", SheetHoleSupportKind.PanelScrew, OperationFace.NegativeY);
            AddThroughHole(bottom, localX, secondDepth, diameter, "Bodemmontage " + label + " achter", SheetHoleSupportKind.PanelScrew, OperationFace.NegativeY);
            AddThroughHole(worktop, localX, firstDepth, diameter, "Werkbladmontage " + label + " voor", SheetHoleSupportKind.PanelScrew, OperationFace.PositiveY);
            AddThroughHole(worktop, localX, secondDepth, diameter, "Werkbladmontage " + label + " achter", SheetHoleSupportKind.PanelScrew, OperationFace.PositiveY);
        }

        private static void AddWorktopSupportMountingHoles(SheetPart worktop, WorkbenchCabinetConfig config, double worldX, double depthStart, double depth, string label)
        {
            if (worktop == null || depth <= 0) return;
            var diameter = config.SheetFastener == null || config.SheetFastener.ClearanceHoleDiameterMm <= 0
                ? ProductDrawingStrategy.DefaultWoodScrewClearanceHoleDiameterMm
                : config.SheetFastener.ClearanceHoleDiameterMm;
            var localX = worldX + config.WidthMm / 2.0;
            var firstDepth = depthStart + Math.Min(35.0, depth / 3.0);
            var secondDepth = depthStart + Math.Max(35.0, depth - 35.0);
            AddThroughHole(worktop, localX, firstDepth, diameter, "Werkbladmontage " + label + " voor", SheetHoleSupportKind.PanelScrew, OperationFace.PositiveY);
            AddThroughHole(worktop, localX, secondDepth, diameter, "Werkbladmontage " + label + " achter", SheetHoleSupportKind.PanelScrew, OperationFace.PositiveY);
        }

        private static void AddAdjustableFootHoles(SheetPart bottom, WorkbenchCabinetConfig config, double unitWidth)
        {
            var foot = config.AdjustableFoot;
            if (foot == null || !foot.MountingPatternVerified || foot.MountingHoles == null || foot.MountingHoles.Length == 0) return;

            var frontY = ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
            var backY = config.DepthMm - config.AdjustableFootInsetMm;
            var sidePlinthFootInset = ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
            var leftInset = config.IncludeLeftSidePlinth ? sidePlinthFootInset : config.AdjustableFootInsetMm;
            var rightInset = config.IncludeRightSidePlinth ? sidePlinthFootInset : config.AdjustableFootInsetMm;
            for (var boundary = 0; boundary <= config.UnitCount; boundary++)
            {
                var x = boundary * unitWidth;
                if (boundary == 0) x = leftInset;
                else if (boundary == config.UnitCount) x = config.WidthMm - rightInset;

                AddAdjustableFootPattern(bottom, config, x, frontY, false, "voor grens " + boundary.ToString(CultureInfo.InvariantCulture));
                AddAdjustableFootPattern(bottom, config, x, backY, true, "achter grens " + boundary.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AddAdjustableFootPattern(SheetPart bottom, WorkbenchCabinetConfig config, double centerX, double centerY, bool rotate180, string position)
        {
            var foot = config.AdjustableFoot;
            foreach (var mountingHole in foot.MountingHoles)
            {
                if (mountingHole == null || mountingHole.DiameterMm <= 0) continue;
                var name = "SEKTION poot " + position + " " + (mountingHole.Name ?? "montagegat");
                var localX = mountingHole.XOffsetMm;
                var localY = mountingHole.YOffsetMm;
                if (rotate180)
                {
                    localX = -localX;
                    localY = -localY;
                }
                var x = centerX + localX;
                var y = centerY + localY;

                if (mountingHole.Through)
                {
                    AddThroughHole(bottom, x, y, mountingHole.DiameterMm, name, SheetHoleSupportKind.AdjustableFoot, OperationFace.CenterPlane);
                }
                else
                {
                    AddBlindHole(bottom, x, y, mountingHole.DiameterMm, mountingHole.DepthMm, OperationFace.NegativeY, name, SheetHoleSupportKind.AdjustableFoot);
                }
            }
        }

        private static void AddHardware(WorkbenchModel model, WorkbenchCabinetConfig config, int doorCount, int hingeCount, int frontPlinthClipCount, int sidePlinthClipCount)
        {
            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var footCount = 2 * (config.UnitCount + 1);
            var packQuantity = Math.Max(1, foot.PackQuantity);
            var footPackCount = (footCount + packQuantity - 1) / packQuantity;
            var mountingNote = foot.MountingPatternVerified
                ? "Het ingemeten montagepatroon staat in de doorlopende bodemplaat."
                : "LET OP: IKEA publiceert het pen-/klikpatroon niet; daarom staan er nog geen stelpootgaten in het CNC-bestand. Eerst een exemplaar inmeten en het template vrijgeven.";
            var totalPlinthClipCount = frontPlinthClipCount + sidePlinthClipCount;
            var includedPlinthClipCount = foot.IncludesPlinthClips
                ? footPackCount * Math.Max(0, foot.PlinthClipQuantityPerPack)
                : 0;
            var additionalPlinthClipCount = Math.Max(0, totalPlinthClipCount - includedPlinthClipCount);
            var clipNote = foot.IncludesPlinthClips
                ? totalPlinthClipCount.ToString(CultureInfo.InvariantCulture) + " clips worden gebruikt: "
                    + frontPlinthClipCount.ToString(CultureInfo.InvariantCulture) + " voor de voorplint"
                    + (sidePlinthClipCount > 0 ? " en " + sidePlinthClipCount.ToString(CultureInfo.InvariantCulture) + " voor de gekozen zijplint(en). Op een gedeelde voorhoek wordt de zijclip omgekeerd gemonteerd om botsing met de voorclip te voorkomen." : ".")
                    + (additionalPlinthClipCount > 0 ? " De pootsets leveren er " + includedPlinthClipCount.ToString(CultureInfo.InvariantCulture) + "; " + additionalPlinthClipCount.ToString(CultureInfo.InvariantCulture) + " extra clip(s) zijn nodig." : " De clips van de niet voor de plint gebruikte achterpoten worden voor de zijplint hergebruikt.")
                : "Plintclips afzonderlijk bestellen.";
            model.Hardware.Add(new HardwareItem
            {
                Name = foot.Name + " (" + packQuantity.ToString(CultureInfo.InvariantCulture) + "-pack)",
                ArticleNumber = foot.ArticleNumber,
                Quantity = footPackCount,
                Unit = "verpakking",
                Note = footCount.ToString(CultureInfo.InvariantCulture) + " poten nodig: voor en achter onder iedere dragende unitgrens; ingesteld op "
                    + config.PlinthHeightMm.ToString("0.#", CultureInfo.InvariantCulture) + " mm, verstelbereik "
                    + foot.MinHeightMm.ToString("0.#", CultureInfo.InvariantCulture) + "-" + foot.MaxHeightMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + " mm, maximaal " + foot.MaxLoadKgPerFoot.ToString("0.#", CultureInfo.InvariantCulture) + " kg per poot volgens fabrikant. "
                    + clipNote + " " + mountingNote
                    + " Hart voorpoten ligt "
                    + ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config).ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm achter het kastfront; de rechthoekige montagevoet houdt minimaal "
                    + ProductDefaults.WorkbenchCabinetFootToPlinthClearanceMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm vrij van de plint."
            });
            if (additionalPlinthClipCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "Aanvullende C-plintclip passend op IKEA SEKTION-poot",
                    ArticleNumber = "SEKTION_PLINTH_CLIP_SPARE_TBD",
                    Quantity = additionalPlinthClipCount,
                    Unit = "st",
                    Note = "Alleen nodig wanneer het aantal voor- en zijplintverbindingen groter is dan het aantal clips dat met de gekozen pootsets wordt geleverd. Beschikbaarheid/bron nog vastleggen."
                });
            }
            var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            model.Hardware.Add(new HardwareItem
            {
                Name = "3D-geprinte plintclip-adapter kort",
                ArticleNumber = "CUSTOM_SEKTION_PLINTH_ADAPTER_FRONT_V2",
                Quantity = frontPlinthClipCount,
                Unit = "st",
                Note = "Voor de houten voorplint. Inschuifprofiel "
                    + adapter.TongueWidthMm.ToString("0.#", CultureInfo.InvariantCulture) + "x"
                    + adapter.TongueHeightMm.ToString("0.#", CultureInfo.InvariantCulture) + "x"
                    + adapter.TongueThicknessMm.ToString("0.#", CultureInfo.InvariantCulture) + "mm met "
                    + adapter.PrintClearancePerSideMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm printspeling per zijde. Uitstand "
                    + ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config).ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm. Cliptongpassing is fysiek bevestigd; bovenste schroef ligt op een gespiegelde montagevleugel buiten de inschuifbaan."
            });
            if (sidePlinthClipCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "3D-geprinte plintclip-adapter verlengd",
                    ArticleNumber = "CUSTOM_SEKTION_PLINTH_ADAPTER_SIDE_V2",
                    Quantity = sidePlinthClipCount,
                    Unit = "st",
                    Note = "Voor de gekozen houten zijplint(en). Zelfde inschuifhouder met verstevigde uitstand van "
                        + ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config).ToString("0.#", CultureInfo.InvariantCulture)
                        + "mm. Op de gedeelde voorhoek staat de zijclip 7mm hoger dan de voorclip; de montagevleugel wijst altijd van de hoek af."
                });
            }
            model.Hardware.Add(new HardwareItem
            {
                Name = "Spaanplaatschroef verzonken kop Ø4x" + adapter.FrontScrewLengthMm.ToString("0", CultureInfo.InvariantCulture) + " voor korte plintclip-adapter",
                ArticleNumber = "PLINTH_ADAPTER_SCREW_4X" + adapter.FrontScrewLengthMm.ToString("0", CultureInfo.InvariantCulture),
                Quantity = 2 * frontPlinthClipCount,
                Unit = "st",
                Note = "Handmatig vanaf de binnenzijde door de twee Ø"
                    + adapter.MountingHoleDiameterMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm adaptergaten met conische kopzitting Ø"
                    + adapter.MountingCountersinkDiameterMm.ToString("0.#", CultureInfo.InvariantCulture) + "x"
                    + adapter.MountingCountersinkDepthMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm in de 18mm houten voorplint schroeven; niet door de zichtzijde."
            });
            if (sidePlinthClipCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "Spaanplaatschroef verzonken kop Ø4x" + adapter.SideScrewLengthMm.ToString("0", CultureInfo.InvariantCulture) + " voor verlengde plintclip-adapter",
                    ArticleNumber = "PLINTH_ADAPTER_SCREW_4X" + adapter.SideScrewLengthMm.ToString("0", CultureInfo.InvariantCulture),
                    Quantity = 2 * sidePlinthClipCount,
                    Unit = "st",
                    Note = "Door de verlengde adapter in de 18mm zijplint. Maximaal circa 12mm inschroefdiepte in het hout aanhouden."
                });
            }
            model.Hardware.Add(new HardwareItem
            {
                Name = "Korte houtschroef Ø4x" + foot.CentralFastenerLengthMm.ToString("0", CultureInfo.InvariantCulture) + " voor SEKTION montagevoet",
                ArticleNumber = "SEKTION_FOOT_SCREW_4X" + foot.CentralFastenerLengthMm.ToString("0", CultureInfo.InvariantCulture),
                Quantity = footCount,
                Unit = "st",
                Note = "Handmatig door het Ø" + foot.CentralFastenerClearanceDiameterMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm kunststofgat met 2mm materiaalweg in de bodemplaat schroeven. Geen CNC-voorboring: zo blijft schroefgrip behouden. De automatische lengteselectie houdt minimaal "
                    + config.SheetFastener.MinimumTipClearanceMm.ToString("0.#", CultureInfo.InvariantCulture)
                    + "mm hout aan de zichtzijde over."
            });
            var hingePackCount = (hingeCount + 3) / 4;
            model.Hardware.Add(new HardwareItem
            {
                Name = "IKEA KOMPLEMENT zachtsluitend scharnier, 4-pack",
                ArticleNumber = "302.145.04",
                Quantity = hingePackCount,
                Unit = "set",
                Note = hingeCount.ToString(CultureInfo.InvariantCulture) + " scharnieren nodig voor " + doorCount.ToString(CultureInfo.InvariantCulture) + " deurbladen; per set twee gedempt en twee ongedempt, inclusief montageplaten en schroeven."
            });

            if (config.IncludeAdjustableShelfHoles && config.ShelfCountPerUnit > 0 && config.ShelfSupport != null)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = config.ShelfSupport.Name,
                    ArticleNumber = config.ShelfSupport.Id,
                    Quantity = config.UnitCount * config.ShelfCountPerUnit * 4,
                    Unit = "st",
                    Note = "Vier legplankdragers per afzonderlijke unitlegplank."
                });
            }

            if (config.IncludeTopDrawer && config.DrawerRail != null)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = config.DrawerRail.Name,
                    ArticleNumber = config.DrawerRail.Id,
                    Quantity = config.UnitCount * 2,
                    Unit = "st",
                    Note = "Eén paar ladegeleiders per bovenlade."
                });
                model.Hardware.Add(new HardwareItem
                {
                    Name = config.DrawerRail.FastenerName + " voor ladegeleider aan kast",
                    ArticleNumber = RailCabinetFastenerArticle(config.DrawerRail),
                    Quantity = config.UnitCount * 2 * Math.Max(0, config.DrawerRail.CabinetHoleCount),
                    Unit = "st",
                    Note = "Bevestiging van de geleiders aan de kast. Lengte en materiaalweg zijn dezelfde waarden als in de botsingscontrole. Doorlopende pilotgaten in een enkel T-tussenschot mogen pas worden vrijgegeven nadat een versprongen gatpatroon of afzonderlijk doorgaand bevestigingsconcept is vastgelegd."
                });
                model.Hardware.Add(new HardwareItem
                {
                    Name = config.DrawerRail.FastenerName,
                    ArticleNumber = "RAIL_DRAWER_SCREW",
                    Quantity = config.UnitCount * 2 * Math.Max(0, config.DrawerRail.DrawerHoleCount),
                    Unit = "st",
                    Note = "Bevestiging van de geleiders aan de ladezijden."
                });
            }

            var panelScrewCount = CountHoles(model, SheetHoleSupportKind.PanelScrew);
            if (panelScrewCount > 0)
            {
                var panelScrew = config.SheetFastener;
                model.Hardware.Add(new HardwareItem
                {
                    Name = panelScrew.Name + " 4x" + panelScrew.LengthMm.ToString("0", CultureInfo.InvariantCulture),
                    ArticleNumber = panelScrew.Id + "_L" + panelScrew.LengthMm.ToString("0", CultureInfo.InvariantCulture),
                    Quantity = panelScrewCount,
                    Unit = "st",
                    Note = "Productstandaard hout-op-hout: CNC-gat Ø" + panelScrew.ClearanceHoleDiameterMm.ToString("0.#", CultureInfo.InvariantCulture)
                        + "mm; minimaal " + panelScrew.MinimumEdgePenetrationMm.ToString("0", CultureInfo.InvariantCulture)
                        + "mm in de kopse houtkant en nooit door het ontvangende deel."
                });
            }
        }

        private static int CountHoles(WorkbenchModel model, SheetHoleSupportKind supportKind)
        {
            var count = 0;
            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    if (hole.SupportKind == supportKind) count++;
                }
            }

            return count;
        }

        private static SheetPart Sheet(string name, Material material, double length, double width)
        {
            return SheetDrawing.CreateSheet(name, material, Math.Max(1, length), Math.Max(1, width));
        }

        private static void AddSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
        }

        private static void AddThroughHole(SheetPart sheet, double x, double y, double diameter, string name, SheetHoleSupportKind supportKind, OperationFace face)
        {
            AddHole(sheet, x, y, diameter, 0, face, OperationDepthMode.Through, name, supportKind);
        }

        private static void AddBlindHole(SheetPart sheet, double x, double y, double diameter, double depth, OperationFace face, string name, SheetHoleSupportKind supportKind)
        {
            AddHole(sheet, x, y, diameter, depth, face, OperationDepthMode.BlindFromFace, name, supportKind);
        }

        private static void AddHole(SheetPart sheet, double x, double y, double diameter, double depth, OperationFace face, OperationDepthMode depthMode, string name, SheetHoleSupportKind supportKind)
        {
            if (sheet == null || diameter <= 0) return;
            var edge = Math.Max(3.0, diameter / 2.0 + 0.5);
            x = Math.Round(Math.Max(edge, Math.Min(sheet.LengthMm - edge, x)), 3);
            y = Math.Round(Math.Max(edge, Math.Min(sheet.WidthMm - edge, y)), 3);
            foreach (var existing in sheet.Holes)
            {
                if (Math.Abs(existing.Xmm - x) < 0.01 && Math.Abs(existing.Ymm - y) < 0.01 && Math.Abs(existing.DiameterMm - diameter) < 0.01 && existing.Face == face) return;
            }

            sheet.Holes.Add(new SheetHole
            {
                Name = name,
                Xmm = x,
                Ymm = y,
                DiameterMm = diameter,
                DepthMm = depth,
                Face = face,
                DepthMode = depthMode,
                Countersunk = false,
                SupportKind = supportKind
            });
        }

        private static double MaterialThickness(Material material)
        {
            return material == null || material.ThicknessMm <= 0 ? 18.0 : material.ThicknessMm;
        }

        private static void Validate(WorkbenchCabinetConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.CarcassMaterial == null) throw new ArgumentException("Plaatmateriaal voor de werkbankkast ontbreekt.");
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.WorktopHeightMm <= 0) throw new ArgumentException("Werkbankkast-afmetingen moeten groter zijn dan 0.");
            if (config.UnitCount < 1 || config.UnitCount > 12) throw new ArgumentException("Aantal deurposities moet tussen 1 en 12 liggen.");
            if (config.WidthMm / config.UnitCount < 180.0) throw new ArgumentException("Een deurpositie moet minimaal 180mm breed zijn. Verminder het aantal deurposities of vergroot de werkbank.");
            if (config.AdjustableFoot == null) config.AdjustableFoot = ProductDefaults.WorkbenchCabinetAdjustableFoot();
            if (config.PlinthHeightMm < config.AdjustableFoot.MinHeightMm || config.PlinthHeightMm > config.AdjustableFoot.MaxHeightMm)
            {
                throw new ArgumentException("Ingestelde poothoogte moet binnen het verstelbereik "
                    + config.AdjustableFoot.MinHeightMm.ToString("0.#", CultureInfo.InvariantCulture) + "-"
                    + config.AdjustableFoot.MaxHeightMm.ToString("0.#", CultureInfo.InvariantCulture) + "mm liggen.");
            }
            if (config.PlinthSetbackMm < 0 || config.PlinthSetbackMm >= config.DepthMm / 2.0) throw new ArgumentException("Terugligging van de voorzetplint is ongeldig.");
            var minimumFootInset = Math.Max(
                config.AdjustableFoot.FootDiameterMm / 2.0 + 1.0,
                config.AdjustableFoot.MountingBlockWidthMm / 2.0 + 1.0);
            if (config.AdjustableFootInsetMm < minimumFootInset || config.AdjustableFootInsetMm >= Math.Min(config.WidthMm, config.DepthMm) / 2.0) throw new ArgumentException("Inset van de stelpoten is ongeldig.");
            if (config.PlinthClipCenterBehindBackFaceMm <= 0) throw new ArgumentException("Clipmaat van plintbinnenvlak tot pootas moet groter zijn dan 0.");
            var frontFootCenter = ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
            if (frontFootCenter < minimumFootInset || frontFootCenter >= config.DepthMm - minimumFootInset)
                throw new ArgumentException("De berekende voorpootpositie past niet binnen de bodemplaat.");
            var sideFootInset = ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
            if ((config.IncludeLeftSidePlinth || config.IncludeRightSidePlinth)
                && (sideFootInset < minimumFootInset || sideFootInset >= config.WidthMm / 2.0))
                throw new ArgumentException("De berekende hoekpootpositie voor de zijplint past niet binnen de bodemplaat.");
            if (config.DoorStopWidthMm < MaterialThickness(config.CarcassMaterial) + 4.0) throw new ArgumentException("Aanslagstrook van de T-stijl is te smal.");
            if (config.DoorToCarcassClearanceMm < 0) throw new ArgumentException("Afstand tussen draaideur en kopse kastkant mag niet negatief zijn.");
            if (config.DrawerMaterial == null) config.DrawerMaterial = config.CarcassMaterial;
            if (config.IncludeTopDrawer && config.DrawerRail == null) throw new ArgumentException("Rail-template voor de bovenlades ontbreekt.");
            if (config.TopDrawerHeightMm <= 0) config.TopDrawerHeightMm = 160.0;
            if (config.DrawerSideClearanceMm <= 0 && config.DrawerRail != null) config.DrawerSideClearanceMm = Math.Max(13.0, config.DrawerRail.ThicknessMm);
            if (config.DrawerBackClearanceMm <= 0) config.DrawerBackClearanceMm = 30.0;
            if (config.ShelfSupport == null)
            {
                config.ShelfSupport = new ShelfSupportTemplate
                {
                    Id = "shelf_pin_5mm_32",
                    Name = "Legplankdrager pin 5mm, systeem 32",
                    ThicknessMm = 5,
                    HeightMm = 12,
                    HoleDiameterMm = 5,
                    HoleSpacingMm = 32,
                    FrontInsetMm = 50,
                    BackInsetMm = 50,
                    FirstHoleHeightMm = 160
                };
            }
            if (config.ShelfCountPerUnit < 0) config.ShelfCountPerUnit = 0;
            if (config.AdjustableShelfPositionCount <= 0) config.AdjustableShelfPositionCount = Math.Max(config.ShelfCountPerUnit, ProductDefaults.WorkbenchCabinetAdjustableShelfPositionCount);
            if (config.AdjustableShelfPositionCount < config.ShelfCountPerUnit) config.AdjustableShelfPositionCount = config.ShelfCountPerUnit;
            if (config.AdjustableShelfPositionCount > 20) config.AdjustableShelfPositionCount = 20;
            if (config.ShelfClearanceMm < 0) config.ShelfClearanceMm = 2.0;
            var shelfBackThickness = config.IncludeBackPanel ? MaterialThickness(config.BackMaterial ?? config.CarcassMaterial) : 0.0;
            var maxShelfFrontInset = Math.Max(0.0, config.DepthMm - shelfBackThickness - config.ShelfSupport.BackInsetMm - config.ShelfSupport.FrontInsetMm - 6.0);
            if (config.ShelfFrontInsetMm < 0) config.ShelfFrontInsetMm = 0;
            if (config.ShelfFrontInsetMm > maxShelfFrontInset) config.ShelfFrontInsetMm = maxShelfFrontInset;
            if (config.AdjustableShelfHoleEndMarginMm <= 0) config.AdjustableShelfHoleEndMarginMm = 80.0;
            if (string.IsNullOrWhiteSpace(config.ShelfStartMode)) config.ShelfStartMode = "bottom";

            var worktopT = MaterialThickness(config.WorktopMaterial ?? config.CarcassMaterial);
            var bodyHeight = config.WorktopHeightMm - worktopT - config.PlinthHeightMm - MaterialThickness(config.CarcassMaterial);
            if (bodyHeight < 240) throw new ArgumentException("Werkhoogte is te laag voor plint, doorlopende bodem en deuren.");
            if (config.IncludeTopDrawer && config.TopDrawerHeightMm > bodyHeight - 160.0) throw new ArgumentException("De bovenlade is te hoog; er blijft onvoldoende deurhoogte over.");

            if (!FitsSingleSheet(config.CarcassMaterial, config.WidthMm, config.DepthMm))
            {
                throw new ArgumentException("De doorlopende bodemplaat past niet uit een plaat " + config.CarcassMaterial.SheetLengthMm.ToString("0") + "x" + config.CarcassMaterial.SheetWidthMm.ToString("0") + "mm. Kies een kleinere werkbank of groter plaatmateriaal.");
            }
        }

        private static bool FitsSingleSheet(Material material, double length, double width)
        {
            if (material == null || material.SheetLengthMm <= 0 || material.SheetWidthMm <= 0) return true;
            return (length <= material.SheetLengthMm && width <= material.SheetWidthMm)
                || (length <= material.SheetWidthMm && width <= material.SheetLengthMm);
        }

        private static string RailCabinetFastenerArticle(RailTemplate rail)
        {
            var diameter = rail.CabinetFastenerDiameterMm.ToString("0.#", CultureInfo.InvariantCulture);
            var length = rail.CabinetFastenerLengthMm.ToString("0.#", CultureInfo.InvariantCulture);
            var headStyle = new string((rail.CabinetFastenerHeadStyle ?? "").Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
            return "RAIL_CABINET_SCREW_" + diameter + "X" + length
                + (headStyle.Length == 0 ? "" : "_" + headStyle);
        }

        private sealed class BoundaryPanel
        {
            public BoundaryPanel(int unitBoundary, double xmm, SheetPart sheet)
            {
                UnitBoundary = unitBoundary;
                Xmm = xmm;
                Sheet = sheet;
            }

            public int UnitBoundary { get; private set; }
            public double Xmm { get; private set; }
            public SheetPart Sheet { get; private set; }
            public double FrontDepthTrimMm { get; set; }
            public double PositiveThicknessMm { get; set; }
            public SheetPart PositiveHingeSheet { get; set; }
        }
    }
}
