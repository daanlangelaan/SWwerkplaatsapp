using System;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class LexWorkbenchEngine
    {
        private const double MechanicalStopLengthMm = 12.0;

        public WorkbenchModel Build(LexWorkbenchConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                SawingMode = config.SawingMode
            };
            foreach (var note in config.DesignNotes) model.DesignNotes.Add(note);

            AddProfiles(model, config);
            AddSheets(model, config);
            AddWorktopBrackets(model, config);
            AddLiftColumns(model, config);
            AddAdjustableFeet(model, config);
            AddLinearGuides(model, config);
            AddLocksAndStops(model, config);
            AddSwingLatches(model, config);
            AddExposedProfileEndCaps(model, config);
            BuildProfileOperations(model, config.SawingMode);
            AddHardware(model, config);
            AddFastenerCalculations(model, config);
            model.StructuralCalculation = new StructuralCalculationService().Calculate(
                "werktafel_lex", config.Profile40x40.Id, config.WidthMm, 5);
            return model;
        }

        private static void AddWorktopBrackets(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var supports = model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile
                && (item.PartName.StartsWith("Bewegend buitenframe voor", StringComparison.OrdinalIgnoreCase)
                    || item.PartName.StartsWith("Bewegend buitenframe achter", StringComparison.OrdinalIgnoreCase)
                    || item.PartName.StartsWith("Werkbladhouder", StringComparison.OrdinalIgnoreCase))).ToArray();
            new WorktopBracketPlacementService().AddSymmetricPairs(
                model, "werktafel_lex", supports, WorktopSupportAxis.X,
                config.HeightMm - config.TopSheet.ThicknessMm, "Werkbladhouder montagebeugel");
        }

        private static void AddProfiles(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var footProfileLength = FootProfileLength(config);
            var footProfile = AddProfile(model, config, "Voetprofiel", config.Profile80x80, footProfileLength, 2,
                "80x80 mm; langs Z onder beide HTE2-kolommen; tussen vier ZI-1744 hoekadapters, zodat de totale diepte inclusief adapters exact " + config.DepthMm.ToString("0.##") + " mm blijft");
            footProfile.BomStatus = "OPEN ONTWERPKEUZE - 80x80 is actueel gemodelleerd; 80x40 beoordelen op beenruimte en rondom zitten vóór vrijgave.";
            AddProfile(model, config, "Vast railframe voor/achter", config.Profile80x80, config.FixedRailFrameWidthMm, 2,
                "80x80; langs X onder de omgekeerde HSR15R-wagens; bovenvlak volledig onder 80x80-adapterplaten");
            AddProfile(model, config, "Vast railframe links/rechts", config.Profile80x80, config.FixedRailFrameDepthMm - 160, 2,
                "80x80; langs Z tussen voor- en achterprofiel; hart exact boven de HTE2-poot");
            AddProfile(model, config, "Bewegend buitenframe voor/achter", config.Profile40x40, config.WidthMm, 2,
                "40x40 mm; langs X als voor- en achterrand van de rechthoekige buitencontour");
            AddProfile(model, config, "Bewegend buitenframe links/rechts", config.Profile40x40, config.DepthMm - 80, 2,
                "40x40 mm; langs Z tussen voor- en achterprofiel; buitencontour blijft gesloten");
            AddProfile(model, config, "Bewegende werkbladhouder horizontaal", config.Profile40x40, config.WidthMm - 80, 3,
                "Drie 40x40-profielen langs X tussen de binnenvlakken van de 40 mm brede zijprofielen; buitenste twee op HSR15-railhart en middelste op Z=" + config.WorktopCenterSupportOffsetMm.ToString("0.##") + " mm tussen twee kogelpotrijen. Geen overlap met de zijprofielen en geen extra dwarsliggers langs Z.");

            var footX = config.ColumnCenterDistanceMm / 2.0;
            AddPlacement(model, "Voetprofiel links", AssemblyComponentKind.Profile, 80, 80, footProfileLength,
                -footX, 40, 0, "profile");
            AddPlacement(model, "Voetprofiel rechts", AssemblyComponentKind.Profile, 80, 80, footProfileLength,
                footX, 40, 0, "profile");

            var fixedFrameY = FixedFrameCenterY(config);
            var railZ = config.RailCenterDistanceMm / 2.0;
            var fixedSideX = config.ColumnCenterDistanceMm / 2.0;
            AddPlacement(model, "Vast railframe voor", AssemblyComponentKind.Profile, config.FixedRailFrameWidthMm, 80, 80,
                0, fixedFrameY, -railZ, "profile");
            AddPlacement(model, "Vast railframe achter", AssemblyComponentKind.Profile, config.FixedRailFrameWidthMm, 80, 80,
                0, fixedFrameY, railZ, "profile");
            AddPlacement(model, "Vast railframe links", AssemblyComponentKind.Profile, 80, 80, config.FixedRailFrameDepthMm - 160,
                -fixedSideX, fixedFrameY, 0, "profile");
            AddPlacement(model, "Vast railframe rechts", AssemblyComponentKind.Profile, 80, 80, config.FixedRailFrameDepthMm - 160,
                fixedSideX, fixedFrameY, 0, "profile");

            var movingFrameBottom = MovingFrameBottomY(config);
            var movingFrameY = movingFrameBottom + config.Profile40x40.HeightMm / 2.0;
            var frameZ = (config.DepthMm - 40) / 2.0;
            AddPlacement(model, "Bewegend buitenframe voor", AssemblyComponentKind.Profile, config.WidthMm, 40, 40,
                0, movingFrameY, -frameZ, "profile");
            AddPlacement(model, "Bewegend buitenframe achter", AssemblyComponentKind.Profile, config.WidthMm, 40, 40,
                0, movingFrameY, frameZ, "profile");
            AddPlacement(model, "Bewegend buitenframe links", AssemblyComponentKind.Profile, 40, 40, config.DepthMm - 80,
                -(config.WidthMm - 40) / 2.0, movingFrameY, 0, "profile");
            AddPlacement(model, "Bewegend buitenframe rechts", AssemblyComponentKind.Profile, 40, 40, config.DepthMm - 80,
                (config.WidthMm - 40) / 2.0, movingFrameY, 0, "profile");
            AddPlacement(model, "Werkbladhouder raildrager voor", AssemblyComponentKind.Profile, config.WidthMm - 80, 40, 40,
                0, movingFrameY, -railZ, "profile");
            AddPlacement(model, "Werkbladhouder middenligger", AssemblyComponentKind.Profile, config.WidthMm - 80, 40, 40,
                0, movingFrameY, config.WorktopCenterSupportOffsetMm, "profile");
            AddPlacement(model, "Werkbladhouder raildrager achter", AssemblyComponentKind.Profile, config.WidthMm - 80, 40, 40,
                0, movingFrameY, railZ, "profile");
        }

        private static void AddSheets(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var top = new SheetPart
            {
                Name = "Kogelpotblad HPL",
                Material = config.TopSheet,
                LengthMm = config.WidthMm,
                WidthMm = config.DepthMm,
                Quantity = 1,
                CenterHeightMm = config.HeightMm - config.TopSheet.ThicknessMm / 2.0,
                BomStatus = "Actueel uit model; via automatisch verdeelde TIN 100391-beugels aan het bewegende profielframe."
            };
            for (var row = 0; row < 5; row++)
            {
                var count = row % 2 == 0 ? 11 : 10;
                var startX = -(count - 1) * 140.0 / 2.0;
                var z = (row - 2) * 140.0;
                for (var column = 0; column < count; column++)
                {
                    top.Holes.Add(new SheetHole
                    {
                        Name = "Kogelpot " + (top.Holes.Count + 1),
                        Xmm = config.WidthMm / 2.0 + startX + column * 140.0,
                        Ymm = config.DepthMm / 2.0 + z,
                        DiameterMm = config.BallTransferHoleDiameterMm,
                        DepthMm = 0,
                        Face = OperationFace.CenterPlane,
                        DepthMode = OperationDepthMode.Through,
                        Countersunk = true,
                        CountersinkDiameterMm = config.BallTransferFlangeRecessDiameterMm,
                        CountersinkDepthMm = config.BallTransferFlangeRecessDepthMm,
                        SupportKind = SheetHoleSupportKind.MachiningCutout
                    });
                }
            }
            model.Sheets.Add(top);
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Sheet,
                PartName = top.Name,
                LengthMm = top.LengthMm,
                WidthMm = top.WidthMm,
                Xmm = 0,
                Ymm = top.CenterHeightMm,
                Zmm = 0,
                Orientation = AssemblyOrientation.SheetHorizontal
            });

            var stabilizer = new SheetPart
            {
                Name = "HPL stabilisatieplaat tussen kolommen",
                Material = config.StabilizationSheet,
                LengthMm = config.StabilizationPlateWidthMm,
                WidthMm = config.StabilizationPlateHeightMm,
                Quantity = 1,
                CenterHeightMm = config.ColumnBaseHeightMm + config.LiftColumn.RetractedLengthMm / 2.0,
                BomStatus = "Actueel eigen maakdeel in 6 mm HPL; niet inbegrepen bij de HTE2-set. Bevestigingswijze nog open."
            };
            model.Sheets.Add(stabilizer);
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Sheet,
                PartName = stabilizer.Name,
                LengthMm = stabilizer.LengthMm,
                WidthMm = stabilizer.WidthMm,
                Xmm = 0,
                Ymm = stabilizer.CenterHeightMm,
                Zmm = config.LiftColumn.BodyWidthMm / 2.0 + config.StabilizationSheet.ThicknessMm / 2.0,
                Orientation = AssemblyOrientation.SheetVerticalX
            });

            var adapter = BuildCarriageAdapter(config);
            model.Sheets.Add(adapter);
            var adapterY = FixedFrameTopY(config) + config.CarriageAdapterThicknessMm / 2.0;
            var railZ = config.RailCenterDistanceMm / 2.0;
            var carriageX = config.CarriageCenterDistanceMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                foreach (var x in new[] { -carriageX, carriageX })
                {
                    model.AssemblyPlacements.Add(new AssemblyPlacement
                    {
                        Kind = AssemblyComponentKind.Sheet,
                        PartName = adapter.Name,
                        LengthMm = adapter.LengthMm,
                        WidthMm = adapter.WidthMm,
                        Xmm = x,
                        Ymm = adapterY,
                        Zmm = z,
                        Orientation = AssemblyOrientation.SheetHorizontal
                    });
                }
            }
        }

        private static SheetPart BuildCarriageAdapter(LexWorkbenchConfig config)
        {
            var adapter = new SheetPart
            {
                Name = "HSR15R wagen adapterplaat naar vast frame",
                Material = config.CarriageAdapterSheet,
                LengthMm = config.CarriageAdapterLengthMm,
                WidthMm = config.CarriageAdapterWidthMm,
                Quantity = 4,
                UseTabs = false,
                BomStatus = "Actueel uit model; vier 3D-geprinte PA-CF-delen, 100% infill, met wagenpatroon en twee profielgroefsleuven."
            };

            var halfPitchX = config.LinearGuide.CarriageMountingPitchXmm / 2.0;
            var halfPitchZ = config.LinearGuide.CarriageMountingPitchZmm / 2.0;
            foreach (var xOffset in new[] { -halfPitchX, halfPitchX })
            {
                foreach (var zOffset in new[] { -halfPitchZ, halfPitchZ })
                {
                    adapter.Holes.Add(new SheetHole
                    {
                        Name = "HSR15R M4 verzonken doorgang",
                        Xmm = adapter.LengthMm / 2.0 + xOffset,
                        Ymm = adapter.WidthMm / 2.0 + zOffset,
                        DiameterMm = config.CarriageAdapterClearanceHoleDiameterMm,
                        DepthMode = OperationDepthMode.Through,
                        Countersunk = true,
                        CountersinkDiameterMm = 8.4,
                        CountersinkDepthMm = 2.1,
                        SupportKind = SheetHoleSupportKind.MachiningCutout
                    });
                }
            }

            var slotCenterLength = Math.Max(0.5, config.CarriageAdapterSlotLengthMm - config.CarriageAdapterSlotWidthMm);
            var firstSlotZ = config.CarriageAdapterProfileGrooveOffsetMm;
            var slotCenterX = adapter.LengthMm / 2.0;
            foreach (var slotCenterZ in new[] { firstSlotZ, firstSlotZ + config.CarriageAdapterSlotPitchMm })
            {
                var label = slotCenterZ < adapter.WidthMm / 2.0 ? "V" : "A";
                adapter.Pockets.Add(new SheetPocket
                {
                    Name = "80x80 profielgroef sleuf " + label + " midden",
                    Xmm = slotCenterX - slotCenterLength / 2.0,
                    Ymm = slotCenterZ - config.CarriageAdapterSlotWidthMm / 2.0,
                    LengthMm = slotCenterLength,
                    WidthMm = config.CarriageAdapterSlotWidthMm,
                    DepthMode = OperationDepthMode.Through,
                    Note = "Sleufgat voor M8-inschuifmoer; profielgroefhart 20 mm uit de 80 mm profielrand"
                });
                foreach (var endOffset in new[] { -slotCenterLength / 2.0, slotCenterLength / 2.0 })
                {
                    adapter.Holes.Add(new SheetHole
                    {
                        Name = "80x80 profielgroef sleuf " + label + " ronding",
                        Xmm = slotCenterX + endOffset,
                        Ymm = slotCenterZ,
                        DiameterMm = config.CarriageAdapterSlotWidthMm,
                        DepthMode = OperationDepthMode.Through,
                        SupportKind = SheetHoleSupportKind.ProfileNut
                    });
                }
            }
            return adapter;
        }

        private static void AddLiftColumns(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var column = config.LiftColumn;
            var installationLength = EffectiveColumnLength(config);
            var bodyLength = installationLength - 2.0 * column.EndPlateThicknessMm;
            var xOffset = config.ColumnCenterDistanceMm / 2.0;
            foreach (var x in new[] { -xOffset, xOffset })
            {
                AddPlacement(model, "HTE2 kolom", AssemblyComponentKind.Purchased,
                    column.BodyDepthMm, bodyLength, column.BodyWidthMm,
                    x, config.Profile80x80.HeightMm + column.EndPlateThicknessMm + bodyLength / 2.0, 0, "hardware");
                AddPlacement(model, "HTE2 O1 onderplaat 280x65", AssemblyComponentKind.Purchased,
                    column.EndPlateWidthMm, column.EndPlateThicknessMm, column.EndPlateLengthMm,
                    x, config.Profile80x80.HeightMm + column.EndPlateThicknessMm / 2.0, 0, "hardware");
                AddPlacement(model, "HTE2 O1 bovenplaat 280x65", AssemblyComponentKind.Purchased,
                    column.EndPlateWidthMm, column.EndPlateThicknessMm, column.EndPlateLengthMm,
                    x, FixedFrameBottomY(config) - column.EndPlateThicknessMm / 2.0, 0, "hardware");
            }
        }

        private static void AddAdjustableFeet(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var adapter = config.LevelingFootCornerAdapter;
            var foot = config.LevelingFoot;
            var footX = config.ColumnCenterDistanceMm / 2.0;
            var profileHalfLength = FootProfileLength(config) / 2.0;

            // De ZI-1744 wordt zoals in de leveranciers-toepassing 90 graden gedraaid:
            // het 80x80 montagevlak sluit de profielkop af en de afgeronde M16-arm wijst
            // naar buiten. Profiel + twee adapterbereiken blijft exact de blad-diepte.
            foreach (var x in new[] { -footX, footX })
            {
                foreach (var zDirection in new[] { -1.0, 1.0 })
                {
                    var label = (x < 0 ? "links " : "rechts ") + (zDirection < 0 ? "voor" : "achter");
                    var adapterCenterZ = zDirection * (profileHalfLength + adapter.ReachMm / 2.0);
                    var footZ = zDirection * (profileHalfLength + adapter.FootAxisFromMountingFaceMm);
                    AddPlacementWithShape(model, "Stelpoot hoekadapter ZI-1744 " + label, AssemblyComponentKind.Purchased,
                        adapter.WidthMm, adapter.MountingPlateHeightMm, adapter.ReachMm,
                        x, adapter.MountingPlateHeightMm / 2.0, adapterCenterZ, "hardware-adapter", "leveling-foot-adapter");
                    AddPlacementWithShape(model, "Stelvoet D80 schotel ZI-1415-S", AssemblyComponentKind.Purchased,
                        foot.ActualFootDiameterMm, foot.FootHeightMm, foot.ActualFootDiameterMm,
                        x, foot.FootHeightMm / 2.0, footZ, "hardware", "cylinder");
                    AddPlacementWithShape(model, "Stelvoet D80 zwenkkraag ZI-1415-S", AssemblyComponentKind.Purchased,
                        foot.NutAcrossFlatsMm, 14, foot.NutAcrossFlatsMm,
                        x, foot.FootHeightMm + 7, footZ, "hardware", "cylinder");
                    // Het inkoopdeel blijft een M16x130-stelvoet met 96 mm draad in de BOM.
                    // In de gemonteerde weergave tonen we alleen het deel tot aan de bovenzijde
                    // van de hoekadapter: het uitstekende draadeind wordt bij assemblage afgezaagd.
                    var visibleThreadBottomY = foot.OverallHeightMm - foot.ThreadLengthMm;
                    var visibleThreadTopY = adapter.MountingPlateHeightMm;
                    var visibleThreadLengthMm = Math.Max(0, visibleThreadTopY - visibleThreadBottomY);
                    AddPlacementWithShape(model, "Stelvoet M16x130 draadeind ZI-1415-S", AssemblyComponentKind.Purchased,
                        foot.ThreadDiameterMm, visibleThreadLengthMm, foot.ThreadDiameterMm,
                        x, visibleThreadBottomY + visibleThreadLengthMm / 2.0, footZ, "hardware", "cylinder");
                    AddPlacementWithShape(model, "Stelvoet M16 stelmoer ZI-1415-S", AssemblyComponentKind.Purchased,
                        foot.NutAcrossFlatsMm, foot.NutHeightMm, foot.NutAcrossFlatsMm,
                        x, adapter.MountingPlateHeightMm - adapter.SupportArmThicknessMm - foot.NutHeightMm / 2.0,
                        footZ, "hardware", "cylinder");
                }
            }
        }

        private static void AddLinearGuides(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var guide = config.LinearGuide;
            var cutLength = RequiredRailCutLengthMm(config);
            var adapterTop = FixedFrameTopY(config) + config.CarriageAdapterThicknessMm;
            var railMountingY = MovingFrameBottomY(config);
            var railCenterY = railMountingY - guide.RailHeightMm / 2.0;
            var railZ = config.RailCenterDistanceMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                AddPlacement(model, "HSR15 rail " + cutLength.ToString("0.##"), AssemblyComponentKind.Purchased,
                    cutLength, guide.RailHeightMm, guide.RailWidthMm,
                    0, railCenterY, z, "hardware");
                foreach (var x in new[] { -config.CarriageCenterDistanceMm / 2.0, config.CarriageCenterDistanceMm / 2.0 })
                {
                    AddPlacement(model, "HSR15R wagen", AssemblyComponentKind.Purchased,
                        guide.CarriageLengthMm, guide.AssemblyHeightMm, guide.CarriageWidthMm,
                        x, adapterTop + guide.AssemblyHeightMm / 2.0, z, "hardware");
                }
            }
        }

        private static void AddLocksAndStops(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var railZ = config.RailCenterDistanceMm / 2.0;
            var travel = MaximumHorizontalTravelMm(config);
            var stopCenter = config.CarriageCenterDistanceMm / 2.0 + travel
                + config.LinearGuide.CarriageLengthMm / 2.0 + MechanicalStopLengthMm / 2.0;
            var railCenterY = MovingFrameBottomY(config) - config.LinearGuide.RailHeightMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                foreach (var x in new[] { -stopCenter, stopCenter })
                {
                    AddPlacement(model, "HSR15 mechanische eindstop", AssemblyComponentKind.Purchased,
                        MechanicalStopLengthMm, 24, 30, x, railCenterY, z, "hardware");
                }
            }
            foreach (var x in config.HorizontalLockPositionsMm)
            {
                AddPlacement(model, "Plunjer borgpositie", AssemblyComponentKind.Purchased,
                    18, 35, 18, x, config.HeightMm - 95, -railZ - 55, "hardware");
            }
        }

        private static void AddSwingLatches(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var latch = config.SwingLatch;
            var pivotY = config.HeightMm - config.TopSheet.ThicknessMm - config.MovingFrameSlotAxisEdgeOffsetMm;
            var longSideX = config.WidthMm / 4.0;
            foreach (var x in new[] { -longSideX, longSideX })
            {
                AddSwingLatchPlacement(model, "voor " + (x < 0 ? "links" : "rechts"), latch, x, pivotY, -config.DepthMm / 2.0, 180);
                AddSwingLatchPlacement(model, "achter " + (x < 0 ? "links" : "rechts"), latch, x, pivotY, config.DepthMm / 2.0, 0);
            }
            AddSwingLatchPlacement(model, "zijde links", latch, -config.WidthMm / 2.0, pivotY, 0, -90);
            AddSwingLatchPlacement(model, "zijde rechts", latch, config.WidthMm / 2.0, pivotY, 0, 90);
        }

        private static void AddSwingLatchPlacement(WorkbenchModel model, string label, SwingLatchTemplate latch,
            double x, double y, double z, double rotationYDeg)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Purchased,
                PartName = latch.Name + " " + label,
                LengthMm = latch.WidthMm,
                HeightMm = latch.OverallLengthMm,
                WidthMm = latch.OverallProjectionMm,
                Xmm = x,
                Ymm = y,
                Zmm = z,
                Orientation = AssemblyOrientation.Default,
                VisualKind = "hardware-swing-latch",
                Shape = "swing-latch",
                RotationYDeg = rotationYDeg
            });
        }

        private static void AddExposedProfileEndCaps(WorkbenchModel model, LexWorkbenchConfig config)
        {
            // De leverancierkap is circa 12 mm diep, maar het grootste deel daarvan klemt
            // binnen in het profiel. Alleen de zichtbare buitenflens wordt hier gemodelleerd.
            const double visibleCapThickness = 4.0;
            var halfCapThickness = visibleCapThickness / 2.0;
            // De X-koppen van voor- en achterligger van het vaste 80x80-railframe.
            var fixedFrameY = FixedFrameCenterY(config);
            var railZ = config.RailCenterDistanceMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                foreach (var xDirection in new[] { -1.0, 1.0 })
                {
                    AddPlacement(model, "Afdekkap 8 80x80 zwart - vast railframe", AssemblyComponentKind.Purchased,
                        visibleCapThickness, 80, 80,
                        xDirection * (config.FixedRailFrameWidthMm / 2.0 + halfCapThickness), fixedFrameY, z, "hardware");
                }
            }

            // Alleen de vier vrije X-koppen van het bewegende buitenframe. De zijliggers
            // en drie binnenliggers sluiten tegen een ander profiel aan en krijgen geen kap.
            var movingFrameY = MovingFrameBottomY(config) + config.Profile40x40.HeightMm / 2.0;
            var movingFrameZ = (config.DepthMm - 40) / 2.0;
            foreach (var z in new[] { -movingFrameZ, movingFrameZ })
            {
                foreach (var xDirection in new[] { -1.0, 1.0 })
                {
                    AddPlacement(model, "Afdekkap 8 40x40 zwart - bewegend buitenframe", AssemblyComponentKind.Purchased,
                        visibleCapThickness, 40, 40,
                        xDirection * (config.WidthMm / 2.0 + halfCapThickness), movingFrameY, z, "hardware");
                }
            }
        }

        private static void AddHardware(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var adapter = model.Sheets.First(s => string.Equals(s.Name, "HSR15R wagen adapterplaat naar vast frame", StringComparison.OrdinalIgnoreCase));
            var top = model.Sheets.First(s => string.Equals(s.Name, "Kogelpotblad HPL", StringComparison.OrdinalIgnoreCase));
            var adjustableFootCount = CountPlacements(model, "Stelvoet D80 schotel ZI-1415-S");
            var footAdapterCount = CountPlacements(model, "Stelpoot hoekadapter ZI-1744");
            var swingLatchCount = CountPlacements(model, config.SwingLatch.Name);
            // Het definitieve aantal wordt na traceertoekenning uit fysieke profielcontacten afgeleid.
            // De tijdelijke nul wordt door ProfileConnectionHardwareSynchronizationService vervangen.
            const int standardConnectorCount = 0;
            var railCutLength = RequiredRailCutLengthMm(config);
            var railHoleCount = RailHoleCount(config, railCutLength);

            model.Hardware.Add(new HardwareItem
            {
                Name = "GeMinG HTE2 complete 2-koloms hefset O1, slag 400 mm",
                ArticleNumber = config.LiftColumn.Id + "_2COL_SET",
                Quantity = 1,
                Unit = "set",
                Note = "Set bevat 2x HTE2 O1-kolom, HTT2 tweekanaals regelkast en bedrade geheugenbediening. O1-platen 280x65x5 met twee sleuven per plaat, hartafstand 250 en sleuf 50x8,5 mm. De losse HPL-tussenplaat is een eigen maakdeel en zit niet bij deze set.",
                ModelStatus = "Deels in 3D-model: kolommen en eindplaten wel; regelkast en bediening niet",
                BomStatus = "Actueel als complete leveranciersset"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Maunsystem stelvoet D80 M16x130 zwart",
                ArticleNumber = config.LevelingFoot.ArticleNumber,
                Quantity = adjustableFootCount,
                Unit = "st",
                Note = "Vier ZI-1415-S stelvoeten met werkelijke schotel Ø" + config.LevelingFoot.ActualFootDiameterMm.ToString("0.##") + " mm, M16-draad en totale hoogte 130 mm. Voetharten liggen binnen de adaptercontour en steken niet buiten het 1000-mm blad.",
                ModelStatus = "In 3D-model als schotel, zwenkkraag, M16-draadeind en stelmoer",
                BomStatus = "Actueel volgens aangeleverde Maunsystem-keuze"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = config.SwingLatch.Name,
                ArticleNumber = config.SwingLatch.ArticleNumber,
                Quantity = swingLatchCount,
                Unit = "st",
                Note = "Zes draaibare serie-8 aanslagen: twee op de voorzijde, twee op de achterzijde en één gecentreerd op iedere korte zijde. In 90 graden raststanden bruikbaar als uitgeklapte werkstukaanslag of ingeklapte vrije rand; voorgemonteerde M6 Nut-8 bevestiging.",
                ModelStatus = "In 3D-model als montagevoet, draaibare grijze aanslag en rode indicatiekap",
                BomStatus = "Actueel volgens Item 0.0.700.81; interne renderdetails niet voor CAM"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Maunsystem Stellfusssockel 8 D80 hoekadapter M16",
                ArticleNumber = config.LevelingFootCornerAdapter.ArticleNumber,
                Quantity = footAdapterCount,
                Unit = "st",
                Note = "Vier ZI-1744 adapters 80x80x100 mm, 90 graden gedraaid op de koppen van de ingekorte 80x80-voetprofielen. Afgeronde draagarm, M16-doorvoer en montagegaten zijn in 3D opgenomen.",
                ModelStatus = "In 3D-model en als afzonderlijk SolidWorks-part",
                BomStatus = "Actueel volgens aangeleverde Maunsystem-keuze"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Bevestigingsset Nut 8 voor ZI-1744 hoekadapter",
                ArticleNumber = "MAUNSYSTEM_NUT8_M8X22_SET",
                Quantity = footAdapterCount * 4,
                Unit = "st",
                Note = "Vier M8 bevestigingsposities per adapter op raster 40 mm; definitieve keuze rond- of sleufgat volgt uit leverancier-STEP/proefpassing.",
                ModelStatus = "Gatposities in 3D-model; bouten niet als losse bodies",
                BomStatus = "Actueel aantal; bevestigingsset controleren bij bestelling"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = config.LinearGuide.Name,
                ArticleNumber = config.LinearGuide.Id,
                Quantity = 1,
                Unit = "set",
                Note = "2 voorraadrails van " + config.LinearGuide.RailLengthMm.ToString("0.##") + " mm en 4 wagens; beide rails symmetrisch afzagen tot " +
                    railCutLength.ToString("0.##") + "x15x15 mm voor de vrijgegeven slag. Na zagen blijven " + railHoleCount + " montagegaten op steek " +
                    config.LinearGuide.RailMountingPitchMm.ToString("0.##") + " mm, eindafstand " + config.LinearGuide.RailEndDistanceMm.ToString("0.##") +
                    " mm; gat Ø" + config.LinearGuide.RailHoleThroughDiameterMm.ToString("0.##") + "/Ø" +
                    config.LinearGuide.RailHoleCounterboreDiameterMm.ToString("0.##") + "x" + config.LinearGuide.RailHoleCounterboreDepthMm.ToString("0.##") +
                    " mm; wagen " + config.LinearGuide.CarriageWidthMm.ToString("0.##") + "x" + config.LinearGuide.CarriageLengthMm.ToString("0.##") +
                    "x" + config.LinearGuide.AssemblyHeightMm.ToString("0.##") + " mm, montage " + config.LinearGuide.CarriageMountingThread +
                    ". Rail ondersteboven onder het bewegende werkbladframe; wagens vast op adapterplaten. Eindstops staan op de afgeleide wagen-contactposities.",
                ModelStatus = "In 3D-model",
                BomStatus = "Actueel"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "HSR15R M4x12 verzonken wagenschroef",
                ArticleNumber = "LEX_HSR15_ADAPTER_M4X12_CSUNK_FABORY",
                Quantity = adapter.Quantity * 4,
                Unit = "st",
                Note = "Vier M4x12-verzonken schroeven per 80x80x10-adapterplaat. Effectieve doorvoer na 2,1 mm verzinking is 7,9 mm; de HSR15R-wagen heeft M4x5 blinde draad, zodat 12 mm binnen de geometrische draadzone blijft. Inkoop uit passend Fabory-doosje nog prijzen.",
                ModelStatus = "Verbinding uit model; schroeven niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Adapterplaat M8 bout met inschuifmoer",
                ArticleNumber = "LEX_HSR15_ADAPTER_M8_TNUT",
                Quantity = adapter.Quantity * 2,
                Unit = "st",
                Note = "Twee M8-verbindingen per PA-CF-adapterplaat naar de profielgroeven op 20 en 60 mm. Gebruik serie-8 inklikmoeren met verende kogel; boutlengte pas selecteren uit 10 mm doorvoerstack en de leveranciers-draadzone van de gekozen inklikmoer.",
                ModelStatus = "Verbinding uit model; bouten niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Railmontage met inschuifmoeren",
                ArticleNumber = "LEX_HSR15_RAIL_TNUT_M4",
                Quantity = config.LinearGuide.RailQuantity * railHoleCount,
                Unit = "st",
                Note = "M4-schuifmoer met passende schroef; één per railgat. De schroef passeert 9,7 mm railmateriaal onder de verzonken kop (15-5,3 mm). Definitieve lengte volgt uit deze doorvoer plus de draadzone van de gekozen schuifmoer en moet vóór de sleufbodem stoppen.",
                ModelStatus = "Verbinding uit railgatmodel; schroeven niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "TechXXL standaardverbinder 8 40",
                ArticleNumber = "TIN 100342 / S208ZP",
                Quantity = standardConnectorCount,
                Unit = "st",
                Note = "Aantal automatisch afgeleid uit de profielkoppen: twee verbinders per 80 mm kopvlak in het vaste frame en één per 40 mm kopvlak in het bewegende frame. Leveranciersgeometrie 35x17x10,2 mm.",
                ModelStatus = "Verbindingen afgeleid uit model; verbinders niet als losse bodies",
                BomStatus = "Actueel leveranciersartikel en modelaantal"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "TechXXL bolkop-inbusbout ISO 7380 M8x25",
                ArticleNumber = "TIN 100673 / S208HS825",
                Quantity = standardConnectorCount,
                Unit = "st",
                Note = "Eén bout per TIN 100342 standaardverbinder; schacht M8x25, kop Ø14x4 mm en inbus SW5 volgens leveranciersdata.",
                ModelStatus = "Bevestigers afgeleid uit verbinder-aantal; niet als losse bodies",
                BomStatus = "Actueel leveranciersartikel en modelaantal"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "HTE2 eindplaat M8 bout met inschuifmoer",
                ArticleNumber = "LEX_HTE2_ENDPLATE_M8_TNUT",
                Quantity = config.LiftColumn.Quantity * 2 * 2,
                Unit = "st",
                Note = "Twee sleufverbindingen per onder- en bovenplaat, twee kolommen. Gebruik M8-inklikmoeren met verende kogel; de GeMing O1-eindplaat is 5 mm. De bout gaat door de plaat, overbrugt de gemonteerde draadinlaat, grijpt in de doorlopende moerdraad en blijft vóór de profiel-sleufbodem.",
                ModelStatus = "Verbinding uit HTE2-model; bouten niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kogelpot / ball transfer unit",
                ArticleNumber = "1005008611039159 / VCN310-30",
                Quantity = top.Holes.Count,
                Unit = "st",
                Note = "Geselecteerde VCN310-30: huis D1 Ø" + config.BallTransferBodyDiameterMm.ToString("0.##") +
                    ", flens Ø" + config.BallTransferFlangeDiameterMm.ToString("0.##") +
                    "x" + config.BallTransferFlangeThicknessMm.ToString("0.##") +
                    ", insteeklengte " + config.BallTransferInsertionLengthMm.ToString("0.##") +
                    ", hoofdkogel Ø" + config.BallTransferBallDiameterMm.ToString("0.##") +
                    " en gewenste werkhoogte " + config.BallTransferWorkingHeightMm.ToString("0.##") +
                    " mm boven het HPL; toelaatbare leveranciersbelasting 412 N bij rechtop gebruik. Verzonken in vlakke zitting Ø" + config.BallTransferFlangeRecessDiameterMm.ToString("0.##") +
                    "x" + config.BallTransferFlangeRecessDepthMm.ToString("0.##") +
                    " mm; kraag draagt op de HPL-schouder. Patroon 11-10-11-10-11, raster 140 mm, verspringing 70 mm. Passing/borging met proefexemplaar vrijgeven.",
                ModelStatus = "In 3D-model; aantal uit bladgaten",
                BomStatus = "Actueel gemonteerd aantal en geselecteerd leveranciersartikel; passing nog proefondervindelijk vrijgeven"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kogelpot / ball transfer unit - reserve",
                ArticleNumber = "1005008611039159 / VCN310-30",
                Quantity = 4,
                Unit = "st",
                Note = "Apart bestellen als reserve bovenop het gemodelleerde aantal.",
                ModelStatus = "Niet in 3D-model (reserveonderdeel)",
                BomStatus = "Actueel - apart van 53 gemonteerde stuks"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "VCN226 indexeerplunjer M10 voor lineaire verschuiving",
                ArticleNumber = "836926179 / VCN226",
                Quantity = 3,
                Unit = "st",
                Note = "Drie plunjers voor de borgposities links, midden en rechts. Leveranciersselectie en prijs zijn bronvast; exacte penlengte, slag en opnameplaat blijven te valideren.",
                ModelStatus = "In 3D-model (vereenvoudigde vorm)",
                BomStatus = "Artikel geselecteerd; geometrie blijft voorlopig"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Mechanische eindstop HSR15",
                ArticleNumber = "LEX_HSR15_ENDSTOP_PA_CF_RUBBER",
                Quantity = CountPlacements(model, "HSR15 mechanische eindstop"),
                Unit = "st",
                Note = "Eén eigen PA-CF 3D-printdeel per railuiteinde met kleine vervangbare rubber buffer. Exacte bevestiging, buffermaat en proefbelasting blijven uit te werken.",
                ModelStatus = "In 3D-model (vereenvoudigde vorm)",
                BomStatus = "Actueel aantal; artikel te beheren"
            });
            var bracketCount = CountPlacements(model, "Werkbladhouder montagebeugel");
            model.Hardware.Add(new HardwareItem { Name = "TechXXL montagebeugel 40×40×20 ZN", ArticleNumber = WorktopBracketPlacementService.ArticleNumber, Quantity = bracketCount, Unit = "st", Note = "Twee symmetrische beugels op ieder van vijf X-draagprofielen.", ModelStatus = "In 3D-model", BomStatus = "Actueel leveranciersartikel" });
            model.Hardware.Add(new HardwareItem { Name = "M6×12 verzonken inbusbout voor profielzijde werkbladbeugel", ArticleNumber = "TIN 100691 / S208SKS612V", Quantity = bracketCount, Unit = "st", Note = "Eén per TIN 100391-beugel naar groef-8-profiel.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen" });
            model.Hardware.Add(new HardwareItem { Name = "T-moer 8 met brug M6 voor werkbladbeugel", ArticleNumber = "TIN 100242 / S208NSMS6", Quantity = bracketCount, Unit = "st", Note = "Eén per TIN 100391-beugel.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen" });
            model.Hardware.Add(new HardwareItem { Name = "M6 werkblad-doorsteekset voor TIN 100391", ArticleNumber = "WORKTOP_M6_THROUGH_SET", Quantity = bracketCount, Unit = "st", Note = "M6 verzonken bout, ring en borgmoer; handelslengte volgt uit 10 mm HPL plus 8 mm beugel en moerstack.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen; leveranciersartikel nog koppelen" });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Bevestigingsset HPL-stabilisatieplaat 6 mm",
                ArticleNumber = "LEX_HPL_STABILIZER_FASTENERS_OPEN",
                Quantity = 1,
                Unit = "set",
                Note = "Tussenplaat is een eigen 6 mm HPL-maakdeel en zit niet bij HTE2. Voorlopig concept: 8 bouten M6 of M8 volgens de nog te verifiëren HTE2-kolomsleuf, met passende inklikmoeren; bij afwijkende Chinese sleufgeometrie vierkante moeren valideren.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "OPEN - bevestigingsconcept bepalen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kabelmanagement en trekontlasting",
                ArticleNumber = "RS 879-3725 / 4X1M",
                Quantity = 1,
                Unit = "set",
                Note = "RS PRO VDR kabelgoot PVC grijs 40x40 mm, open sleuf, verpakking 4x1 m. Kabels worden in/aan de goot getyrapt, waarmee de route lokaal wordt gefixeerd; aansluitzijde van bewegende kabels afzonderlijk op buig- en trekbelasting controleren.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "Actueel leveranciersartikel en verpakkingshoeveelheid"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Typeplaat en veiligheidslabels",
                ArticleNumber = "LEX-LABEL-SET",
                Quantity = 1,
                Unit = "set",
                Note = "Typeplaat met machinegegevens en benodigde waarschuwingen.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "Actueel BOM-deel"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Afdekkap 8 80x80 zwart",
                ArticleNumber = "TECHXXL_SERIE8_CAP_80X80_BLACK_TBC",
                Quantity = CountPlacements(model, "Afdekkap 8 80x80 zwart"),
                Unit = "st",
                Note = "Zwart PA-GF; vrije koppen automatisch geteld uit de modelplaatsingen. Definitief leverancierartikel op gekozen 80x80-profielvariant afstemmen.",
                ModelStatus = "In 3D-model",
                BomStatus = "Actueel aantal; artikel te beheren"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Afdekkap 8 40x40 zwart",
                ArticleNumber = "TIN 100184 / S208AK4040",
                Quantity = CountPlacements(model, "Afdekkap 8 40x40 zwart"),
                Unit = "st",
                Note = "TechXXL serie groef 8 type I, 40x40x12 mm, zwart PA-GF; vrije koppen automatisch geteld uit de modelplaatsingen. Aangesloten profielkoppen blijven zonder kap.",
                ModelStatus = "In 3D-model",
                BomStatus = "Actueel leveranciersartikel en modelaantal"
            });
        }

        private static ProfilePart AddProfile(WorkbenchModel model, LexWorkbenchConfig config, string name, Material material,
            double length, int quantity, string note)
        {
            var profile = new ProfilePart
            {
                Name = name,
                Material = material,
                LengthMm = Math.Round(length, 2),
                Quantity = quantity,
                OrientationNote = note,
                BomStatus = "Actueel uit model"
            };
            model.Profiles.Add(profile);
            return profile;
        }

        private static int CountPlacements(WorkbenchModel model, string partNamePrefix)
        {
            return model.AssemblyPlacements.Count(p => p.PartName != null && p.PartName.StartsWith(partNamePrefix, StringComparison.OrdinalIgnoreCase));
        }

        private static void BuildProfileOperations(WorkbenchModel model, ProfileSawingMode sawingMode)
        {
            foreach (var profile in model.Profiles)
            {
                model.ProfileOperations.Add(new ProfileOperation
                {
                    ProfileId = profile.Name.Replace(" ", "_") + "_" + profile.LengthMm.ToString("0.##") + "mm",
                    PartName = profile.Name,
                    Quantity = profile.Quantity,
                    Material = profile.Material,
                    ProfileLengthMm = profile.LengthMm,
                    Sequence = 1,
                    Kind = ProfileOperationKind.SawCut,
                    SawAngleDeg = 90,
                    WorkOrigin = "Kop A",
                    MachineHint = sawingMode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH",
                    ExecutionParty = sawingMode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER",
                    Note = profile.OrientationNote
                });
            }
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind,
            double sizeX, double sizeY, double sizeZ, double x, double y, double z, string visualKind)
        {
            AddPlacementWithShape(model, name, kind, sizeX, sizeY, sizeZ, x, y, z, visualKind, "box");
        }

        private static void AddPlacementWithShape(WorkbenchModel model, string name, AssemblyComponentKind kind,
            double sizeX, double sizeY, double sizeZ, double x, double y, double z, string visualKind, string shape)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = kind,
                PartName = name,
                LengthMm = sizeX,
                WidthMm = sizeZ,
                HeightMm = sizeY,
                Xmm = x,
                Ymm = y,
                Zmm = z,
                Orientation = AssemblyOrientation.Default,
                VisualKind = visualKind,
                Shape = shape
            });
        }

        private static double EffectiveColumnLength(LexWorkbenchConfig config)
        {
            var retractedOverallHeight = config.Profile80x80.HeightMm + config.LiftColumn.RetractedLengthMm
                + config.Profile80x80.HeightMm + config.CarriageAdapterThicknessMm
                + config.LinearGuide.AssemblyHeightMm + config.Profile40x40.HeightMm
                + config.TopSheet.ThicknessMm;
            var extension = Math.Max(0, Math.Min(config.LiftColumn.StrokeMm,
                config.HeightMm - retractedOverallHeight));
            return config.LiftColumn.RetractedLengthMm + extension;
        }

        private static double FootProfileLength(LexWorkbenchConfig config)
        {
            return config.DepthMm - 2.0 * config.LevelingFootCornerAdapter.ReachMm;
        }

        private static double FixedFrameCenterY(LexWorkbenchConfig config)
        {
            return FixedFrameBottomY(config) + config.Profile80x80.HeightMm / 2.0;
        }

        private static double FixedFrameTopY(LexWorkbenchConfig config)
        {
            return FixedFrameCenterY(config) + config.Profile80x80.HeightMm / 2.0;
        }

        private static double FixedFrameBottomY(LexWorkbenchConfig config)
        {
            return config.Profile80x80.HeightMm + EffectiveColumnLength(config);
        }

        private static double MovingFrameBottomY(LexWorkbenchConfig config)
        {
            return FixedFrameTopY(config) + config.CarriageAdapterThicknessMm + config.LinearGuide.AssemblyHeightMm;
        }

        private static void AddFastenerCalculations(WorkbenchModel model, LexWorkbenchConfig config)
        {
            var threadZones = ProfileNutThreadZoneCatalog.LoadRequired();
            var m8ClickNut = threadZones.Required("techxxl_t_nut_8_m8", 8);
            var m4SlidingNut = threadZones.Required("techxxl_t_nut_8_sliding_m4", 4);
            var slotGeometry = new ProfileSlotGeometryCatalog();
            var profile40SlotDepth = slotGeometry.FindRequired(config.Profile40x40.Id).SlotCavityDepthMm;
            var profile80SlotDepth = slotGeometry.FindRequired(config.Profile80x80.Id).SlotCavityDepthMm;
            if (!profile40SlotDepth.HasValue || !profile80SlotDepth.HasValue)
                throw new InvalidOperationException("Exacte sleufbodemdiepte voor LEX-profielbevestigingen ontbreekt in masterdata.");
            var m4 = new FastenerDefinition
            {
                Id = "M4_PROFILE_ATTACHMENT",
                NominalDiameterMm = 4,
                UsageKind = FastenerUsageKind.StructuralBolt,
                LengthMm = 12,
                AvailableLengthsMm = new[] { 8.0, 10.0, 12.0, 16.0, 20.0 }
            };
            var m8 = new FastenerDefinition
            {
                Id = "M8_PROFILE_ATTACHMENT",
                NominalDiameterMm = 8,
                UsageKind = FastenerUsageKind.StructuralBolt,
                LengthMm = 25,
                AvailableLengthsMm = new[] { 10.0, 12.0, 16.0, 20.0, 25.0, 30.0, 35.0, 40.0 }
            };
            model.ProfileFastenerCalculations.Add(new ProfileFastenerCalculation
            {
                CalculationId = "lex-hsr15-carriage-m4",
                HardwareArticleNumber = "LEX_HSR15_ADAPTER_M4X12_CSUNK_FABORY",
                AttachmentKind = "component-to-printed-adapter",
                BoltFamily = m4,
                PassingStackMm = config.CarriageAdapterThicknessMm - 2.1,
                MinimumThreadEngagementMm = 4,
                AvailableThreadZoneMm = 5,
                ThreadInletOffsetMm = 0,
                MaximumInsertionDepthMm = 5,
                ReceivingThreadThroughHole = false,
                BottomClearanceMm = 0.1
            });
            model.ProfileFastenerCalculations.Add(ComponentCalculation("lex-adapter-m8", "LEX_HSR15_ADAPTER_M8_TNUT", "plate-to-profile-click-nut", m8,
                config.CarriageAdapterThicknessMm, 5, m8ClickNut, profile40SlotDepth.Value, 0.1));
            model.ProfileFastenerCalculations.Add(ComponentCalculation("lex-rail-m4", "LEX_HSR15_RAIL_TNUT_M4", "linear-rail-to-profile-sliding-nut", m4,
                config.LinearGuide.RailHeightMm - config.LinearGuide.RailHoleCounterboreDepthMm, 4, m4SlidingNut, profile40SlotDepth.Value, 0.5));
            model.ProfileFastenerCalculations.Add(ComponentCalculation("lex-hte2-m8", "LEX_HTE2_ENDPLATE_M8_TNUT", "component-plate-to-profile-click-nut", m8,
                config.LiftColumn.EndPlateThicknessMm, 5, m8ClickNut, profile80SlotDepth.Value, 0.1));
        }

        private static ProfileFastenerCalculation ComponentCalculation(string id, string article, string kind,
            FastenerDefinition bolt, double passingStackMm, double minimumEngagementMm,
            ProfileNutThreadZone receivingThread, double maximumInsertionDepthMm, double bottomClearanceMm)
        {
            return new ProfileFastenerCalculation
            {
                CalculationId = id,
                HardwareArticleNumber = article,
                AttachmentKind = kind,
                BoltFamily = bolt,
                PassingStackMm = passingStackMm,
                MinimumThreadEngagementMm = minimumEngagementMm,
                ReceivingThreadComponentId = receivingThread.ComponentId,
                ReceivingThreadSource = receivingThread.Source,
                AvailableThreadZoneMm = receivingThread.UsableThreadZoneMm,
                ThreadInletOffsetMm = receivingThread.ThreadInletOffsetMm,
                MaximumInsertionDepthMm = maximumInsertionDepthMm,
                ReceivingThreadThroughHole = receivingThread.ThroughThread,
                BottomClearanceMm = bottomClearanceMm
            };
        }

        private static double MaximumHorizontalTravelMm(LexWorkbenchConfig config)
        {
            return config.HorizontalLockPositionsMm.Max(value => Math.Abs(value));
        }

        private static double RequiredRailCutLengthMm(LexWorkbenchConfig config)
        {
            var guide = config.LinearGuide;
            var minimum = 2.0 * (config.CarriageCenterDistanceMm / 2.0
                + MaximumHorizontalTravelMm(config)
                + guide.CarriageLengthMm / 2.0
                + MechanicalStopLengthMm);
            var pitchCount = Math.Ceiling((minimum - 2.0 * guide.RailEndDistanceMm) / guide.RailMountingPitchMm);
            var cutLength = 2.0 * guide.RailEndDistanceMm + pitchCount * guide.RailMountingPitchMm;
            if (cutLength > guide.RailLengthMm)
                throw new ArgumentException("LEX-slag vereist een langere HSR15-rail dan de leverancierslengte.");
            return cutLength;
        }

        private static int RailHoleCount(LexWorkbenchConfig config, double railLengthMm)
        {
            return (int)Math.Floor((railLengthMm - 2.0 * config.LinearGuide.RailEndDistanceMm)
                / config.LinearGuide.RailMountingPitchMm) + 1;
        }

        private static void Validate(LexWorkbenchConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.LinearGuide == null) throw new ArgumentException("Lineaire geleiding ontbreekt.");
            if (config.LiftColumn == null) throw new ArgumentException("HTE2-hefkolomdata ontbreekt.");
            if (config.LevelingFootCornerAdapter == null) throw new ArgumentException("LEX-stelpoot hoekadapterdata ontbreekt.");
            if (config.LevelingFoot == null) throw new ArgumentException("LEX-stelpootdata ontbreekt.");
            if (config.SwingLatch == null) throw new ArgumentException("LEX-draaibare aanslagdata ontbreekt.");
            if (config.HorizontalLockPositionsMm == null || config.HorizontalLockPositionsMm.Count != 3
                || config.HorizontalLockPositionsMm[0] >= 0 || Math.Abs(config.HorizontalLockPositionsMm[1]) > 0.001
                || config.HorizontalLockPositionsMm[2] <= 0)
                throw new ArgumentException("LEX-borgposities links, midden en rechts ontbreken.");
            if (config.MovingFrameSlotAxisEdgeOffsetMm <= 0) throw new ArgumentException("LEX-sleufashoogte voor draaibare aanslagen ontbreekt.");
            if (Math.Abs(config.CarriageCenterDistanceMm - config.ColumnCenterDistanceMm) > 0.001)
                throw new ArgumentException("LEX-wagenharten moeten samenvallen met de HTE2-poot- en vaste zijframeharten.");
            if (config.CarriageCenterDistanceMm / 2.0 + config.LinearGuide.CarriageLengthMm / 2.0 > config.FixedRailFrameWidthMm / 2.0 + 0.001)
                throw new ArgumentException("LEX-wagens vallen buiten het vaste railframe.");
            if (config.Profile80x80 == null || config.Profile40x40 == null) throw new ArgumentException("LEX-profielmaterialen ontbreken.");
            if (config.TopSheet == null || config.StabilizationSheet == null || config.CarriageAdapterSheet == null) throw new ArgumentException("LEX-plaatmateriaal ontbreekt.");
            if (config.CarriageAdapterLengthMm <= 0 || config.CarriageAdapterWidthMm <= 0 || config.CarriageAdapterThicknessMm <= 0) throw new ArgumentException("LEX-adapterplaatafmetingen ontbreken.");
            if (config.CarriageAdapterProfileGrooveOffsetMm <= 0 || config.CarriageAdapterProfileGrooveOffsetMm >= config.CarriageAdapterWidthMm) throw new ArgumentException("LEX-profielgroefhart valt buiten de adapterplaat.");
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.HeightMm <= 0) throw new ArgumentException("LEX-afmetingen moeten groter zijn dan 0.");
            if (Math.Abs(config.WorktopCenterSupportOffsetMm) >= config.RailCenterDistanceMm / 2.0) throw new ArgumentException("LEX-middenligger moet tussen de twee raildragers liggen.");
            var requiredBallTransferClearance = 20.0 + config.BallTransferBodyDiameterMm / 2.0;
            for (var row = -2; row <= 2; row++)
            {
                if (Math.Abs(config.WorktopCenterSupportOffsetMm - row * 140.0) <= requiredBallTransferClearance)
                    throw new ArgumentException("LEX-middenligger botst met een kogelpotrij; kies een vrije positie tussen de rijen.");
            }
            if (FootProfileLength(config) <= 0) throw new ArgumentException("LEX-diepte is te klein voor twee hoekadapters.");
        }
    }
}
