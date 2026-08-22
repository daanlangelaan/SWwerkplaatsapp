using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class LexWorkbenchEngine
    {
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
            AddLiftColumns(model, config);
            AddAdjustableFeet(model, config);
            AddLinearGuides(model, config);
            AddLocksAndStops(model, config);
            AddExposedProfileEndCaps(model, config);
            BuildProfileOperations(model, config.SawingMode);
            AddHardware(model, config);
            return model;
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
            AddProfile(model, config, "Bewegend buitenframe voor/achter", config.Profile80x40, config.WidthMm, 2,
                "80 mm verticaal; langs X als voor- en achterrand van de rechthoekige buitencontour");
            AddProfile(model, config, "Bewegend buitenframe links/rechts", config.Profile80x40, config.DepthMm - 80, 2,
                "80 mm verticaal; langs Z tussen voor- en achterprofiel; buitencontour blijft gesloten");
            AddProfile(model, config, "Bewegende werkbladhouder horizontaal", config.Profile80x40, config.WidthMm - 80, 3,
                "Drie 80x40-profielen langs X tussen de binnenvlakken van de 40 mm brede zijprofielen, 80 mm verticaal; buitenste twee op HSR15-railhart en middelste op Z=" + config.WorktopCenterSupportOffsetMm.ToString("0.##") + " mm tussen twee kogelpotrijen. Geen overlap met de zijprofielen en geen extra dwarsliggers langs Z.");

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
            var movingFrameY = movingFrameBottom + 40;
            var frameZ = (config.DepthMm - 40) / 2.0;
            AddPlacement(model, "Bewegend buitenframe voor", AssemblyComponentKind.Profile, config.WidthMm, 80, 40,
                0, movingFrameY, -frameZ, "profile");
            AddPlacement(model, "Bewegend buitenframe achter", AssemblyComponentKind.Profile, config.WidthMm, 80, 40,
                0, movingFrameY, frameZ, "profile");
            AddPlacement(model, "Bewegend buitenframe links", AssemblyComponentKind.Profile, 40, 80, config.DepthMm - 80,
                -(config.WidthMm - 40) / 2.0, movingFrameY, 0, "profile");
            AddPlacement(model, "Bewegend buitenframe rechts", AssemblyComponentKind.Profile, 40, 80, config.DepthMm - 80,
                (config.WidthMm - 40) / 2.0, movingFrameY, 0, "profile");
            AddPlacement(model, "Werkbladhouder raildrager voor", AssemblyComponentKind.Profile, config.WidthMm - 80, 80, 40,
                0, movingFrameY, -railZ, "profile");
            AddPlacement(model, "Werkbladhouder middenligger", AssemblyComponentKind.Profile, config.WidthMm - 80, 80, 40,
                0, movingFrameY, config.WorktopCenterSupportOffsetMm, "profile");
            AddPlacement(model, "Werkbladhouder raildrager achter", AssemblyComponentKind.Profile, config.WidthMm - 80, 80, 40,
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
                BomStatus = "Actueel uit model; bevestiging aan bewegend profielframe staat als open BOM-regel."
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
                CenterHeightMm = config.ColumnBaseHeightMm + EffectiveColumnLength(config) / 2.0,
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
                BomStatus = "Actueel uit model; vier stuks met wagenpatroon en twee profielgroefsleuven."
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
            var columnLength = EffectiveColumnLength(config);
            var xOffset = config.ColumnCenterDistanceMm / 2.0;
            foreach (var x in new[] { -xOffset, xOffset })
            {
                AddPlacement(model, "HTE2 kolom", AssemblyComponentKind.Purchased,
                    column.BodyDepthMm, columnLength, column.BodyWidthMm,
                    x, config.ColumnBaseHeightMm + columnLength / 2.0, 0, "hardware");
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
            var adapterTop = FixedFrameTopY(config) + config.CarriageAdapterThicknessMm;
            var railMountingY = MovingFrameBottomY(config);
            var railCenterY = railMountingY - guide.RailHeightMm / 2.0;
            var railZ = config.RailCenterDistanceMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                AddPlacement(model, "HSR15 rail 1500", AssemblyComponentKind.Purchased,
                    guide.RailLengthMm, guide.RailHeightMm, guide.RailWidthMm,
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
            const double stopLength = 12.0;
            var railCenterY = MovingFrameBottomY(config) - config.LinearGuide.RailHeightMm / 2.0;
            foreach (var z in new[] { -railZ, railZ })
            {
                foreach (var x in new[] { -config.LinearGuide.RailLengthMm / 2.0 + stopLength / 2.0, config.LinearGuide.RailLengthMm / 2.0 - stopLength / 2.0 })
                {
                    AddPlacement(model, "HSR15 mechanische eindstop", AssemblyComponentKind.Purchased,
                        stopLength, 24, 30, x, railCenterY, z, "hardware");
                }
            }
            foreach (var x in new[] { -220.0, 0.0, 220.0 })
            {
                AddPlacement(model, "Plunjer borgpositie", AssemblyComponentKind.Purchased,
                    18, 35, 18, x, config.HeightMm - 95, -railZ - 55, "hardware");
            }
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
            var movingFrameY = MovingFrameBottomY(config) + 40.0;
            var movingFrameZ = (config.DepthMm - 40) / 2.0;
            foreach (var z in new[] { -movingFrameZ, movingFrameZ })
            {
                foreach (var xDirection in new[] { -1.0, 1.0 })
                {
                    AddPlacement(model, "Afdekkap 8 80x40 zwart - bewegend buitenframe", AssemblyComponentKind.Purchased,
                        visibleCapThickness, 80, 40,
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
            var standardConnectorCount = ModelDerivedStandardConnectorCount(model);

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
                Note = "2 rails en 4 wagens; rail 1500x15x15, " + config.LinearGuide.RailHoleCount + " montagegaten op steek " +
                    config.LinearGuide.RailMountingPitchMm.ToString("0.##") + " mm, eindafstand " + config.LinearGuide.RailEndDistanceMm.ToString("0.##") +
                    " mm; gat Ø" + config.LinearGuide.RailHoleThroughDiameterMm.ToString("0.##") + "/Ø" +
                    config.LinearGuide.RailHoleCounterboreDiameterMm.ToString("0.##") + "x" + config.LinearGuide.RailHoleCounterboreDepthMm.ToString("0.##") +
                    " mm; wagen " + config.LinearGuide.CarriageWidthMm.ToString("0.##") + "x" + config.LinearGuide.CarriageLengthMm.ToString("0.##") +
                    "x" + config.LinearGuide.AssemblyHeightMm.ToString("0.##") + " mm, montage " + config.LinearGuide.CarriageMountingThread +
                    ". Rail ondersteboven onder het bewegende werkbladframe; wagens vast op adapterplaten.",
                ModelStatus = "In 3D-model",
                BomStatus = "Actueel"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "HSR15R M4 verzonken wagenschroef",
                ArticleNumber = "LEX_HSR15_ADAPTER_M4_CSUNK",
                Quantity = adapter.Quantity * 4,
                Unit = "st",
                Note = "Vier M4-verzonken schroeven per 80x80x10-adapterplaat, afgeleid van het wagenpatroon in het plaatmodel.",
                ModelStatus = "Verbinding uit model; schroeven niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Adapterplaat M8 bout met inschuifmoer",
                ArticleNumber = "LEX_HSR15_ADAPTER_M8_TNUT",
                Quantity = adapter.Quantity * 2,
                Unit = "st",
                Note = "Twee M8-verbindingen per adapterplaat naar de twee profielgroeven op 20 en 60 mm.",
                ModelStatus = "Verbinding uit model; bouten niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Railmontage met inschuifmoeren",
                ArticleNumber = "LEX_HSR15_RAIL_TNUT_M4",
                Quantity = config.LinearGuide.RailQuantity * config.LinearGuide.RailHoleCount,
                Unit = "st",
                Note = "M4-inschuifmoer met passende cilinderkopschroef; één per railgat, railgatpatroon gecentreerd op de onderste profielgroef van de bewegende raildrager.",
                ModelStatus = "Verbinding uit railgatmodel; schroeven niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Standaard profielverbinder serie 8 inclusief bout",
                ArticleNumber = "TECHXXL_SERIE8_STANDARD_CONNECTOR_TBC",
                Quantity = standardConnectorCount,
                Unit = "st",
                Note = "Aantal automatisch afgeleid van 80 mm profielverbindingen in het model: twee verbinders per aangesloten 80 mm profielzijde. Omvat vast railframe, bewegende buitencontour en de drie horizontale werkbladhouders.",
                ModelStatus = "Verbindingen afgeleid uit model; clips/bouten niet als losse bodies",
                BomStatus = "Actueel aantal; definitief artikel bevestigen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "HTE2 eindplaat M8 bout met inschuifmoer",
                ArticleNumber = "LEX_HTE2_ENDPLATE_M8_TNUT",
                Quantity = config.LiftColumn.Quantity * 2 * 2,
                Unit = "st",
                Note = "Twee sleufverbindingen per onder- en bovenplaat, twee kolommen; aantal afgeleid van de HTE2-template.",
                ModelStatus = "Verbinding uit HTE2-model; bouten niet als losse bodies",
                BomStatus = "Actueel uit model"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kogelpot / ball transfer unit",
                ArticleNumber = "LEX_KOGELPOT_H5_PROVISIONAL",
                Quantity = top.Holes.Count,
                Unit = "st",
                Note = "Voorlopige maatvoering: huis Ø" + config.BallTransferBodyDiameterMm.ToString("0.##") +
                    ", flens Ø" + config.BallTransferFlangeDiameterMm.ToString("0.##") +
                    "x" + config.BallTransferFlangeThicknessMm.ToString("0.##") +
                    ", insteeklengte " + config.BallTransferInsertionLengthMm.ToString("0.##") +
                    ", hoofdkogel Ø" + config.BallTransferBallDiameterMm.ToString("0.##") +
                    " en gewenste werkhoogte " + config.BallTransferWorkingHeightMm.ToString("0.##") +
                    " mm boven het HPL. Verzonken in vlakke zitting Ø" + config.BallTransferFlangeRecessDiameterMm.ToString("0.##") +
                    "x" + config.BallTransferFlangeRecessDepthMm.ToString("0.##") +
                    " mm; kraag draagt op de HPL-schouder. Patroon 11-10-11-10-11, raster 140 mm, verspringing 70 mm; kies een inkooptype dat deze uitsteking haalt en geef passing/borging met proefexemplaar vrij.",
                ModelStatus = "In 3D-model; aantal uit bladgaten",
                BomStatus = "Actueel gemonteerd aantal; inkooptype nog bepalen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kogelpot / ball transfer unit - reserve",
                ArticleNumber = "LEX_KOGELPOT_H5_PROVISIONAL",
                Quantity = 4,
                Unit = "st",
                Note = "Apart bestellen als reserve bovenop het gemodelleerde aantal.",
                ModelStatus = "Niet in 3D-model (reserveonderdeel)",
                BomStatus = "Actueel - apart van 53 gemonteerde stuks"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Plunjerborging lineaire verschuiving",
                ArticleNumber = "LEX_PLUNJER_TBD",
                Quantity = 3,
                Unit = "positie",
                Note = "Drie borgposities; VCN226 is kandidaat, definitieve slagposities en exact plunjertype nog bepalen.",
                ModelStatus = "In 3D-model (vereenvoudigde vorm)",
                BomStatus = "OPEN - artikel en slag nog bepalen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Mechanische eindstop HSR15",
                ArticleNumber = "LEX_HSR15_ENDSTOP_TBD",
                Quantity = CountPlacements(model, "HSR15 mechanische eindstop"),
                Unit = "st",
                Note = "Eén eindstop per railuiteinde; definitieve leverancierkeuze wordt door documentbeheer ingevuld.",
                ModelStatus = "In 3D-model (vereenvoudigde vorm)",
                BomStatus = "Actueel aantal; artikel te beheren"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Bevestigingsset HPL-kogelpotblad aan bewegend frame",
                ArticleNumber = "LEX_HPL_TOP_FASTENERS_OPEN",
                Quantity = 1,
                Unit = "set",
                Note = "Bevestigingswijze, gatpatroon en aantallen nog constructief bepalen; bewust zichtbaar als open BOM-punt.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "OPEN - bevestigingsconcept bepalen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Bevestigingsset HPL-stabilisatieplaat 6 mm",
                ArticleNumber = "LEX_HPL_STABILIZER_FASTENERS_OPEN",
                Quantity = 1,
                Unit = "set",
                Note = "Tussenplaat is een eigen 6 mm HPL-maakdeel en zit niet bij HTE2; bevestigingswijze en gaten nog bepalen.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "OPEN - bevestigingsconcept bepalen"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Kabelmanagement en trekontlasting",
                ArticleNumber = "LEX_CABLE_MANAGEMENT_TBD",
                Quantity = 1,
                Unit = "set",
                Note = "Voedings- en motorkabels, kabelklemmen en trekontlasting; voorlopig niet geometrisch gemodelleerd.",
                ModelStatus = "Niet in 3D-model",
                BomStatus = "In BOM houden; later detailleren"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Typeplaat en veiligheidslabels",
                ArticleNumber = "LEX_LABEL_SET",
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
                Name = "Afdekkap 8 80x40 zwart",
                ArticleNumber = "TECHXXL_SERIE8_CAP_80X40_BLACK_TBC",
                Quantity = CountPlacements(model, "Afdekkap 8 80x40 zwart"),
                Unit = "st",
                Note = "Zwart PA-GF; vrije koppen automatisch geteld uit de modelplaatsingen. Aangesloten profielkoppen blijven zonder kap.",
                ModelStatus = "In 3D-model",
                BomStatus = "Actueel aantal; artikel te beheren"
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

        private static int ModelDerivedStandardConnectorCount(WorkbenchModel model)
        {
            var fixedFrameEndJoints = ProfileQuantity(model, "Vast railframe links/rechts") * 2;
            var movingOuterEndJoints = ProfileQuantity(model, "Bewegend buitenframe links/rechts") * 2;
            var movingHolderEndJoints = ProfileQuantity(model, "Bewegende werkbladhouder horizontaal") * 2;
            const int connectorsPer80MmFace = 2;
            return (fixedFrameEndJoints + movingOuterEndJoints + movingHolderEndJoints) * connectorsPer80MmFace;
        }

        private static int ProfileQuantity(WorkbenchModel model, string name)
        {
            var profile = model.Profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            return profile == null ? 0 : profile.Quantity;
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
            var retractedOverallHeight = config.ColumnBaseHeightMm + config.LiftColumn.RetractedLengthMm +
                40 + config.LinearGuide.AssemblyHeightMm + 80 + config.TopSheet.ThicknessMm;
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
            return config.ColumnBaseHeightMm + EffectiveColumnLength(config) - config.CarriageAdapterThicknessMm;
        }

        private static double FixedFrameTopY(LexWorkbenchConfig config)
        {
            return FixedFrameCenterY(config) + 40.0;
        }

        private static double FixedFrameBottomY(LexWorkbenchConfig config)
        {
            return FixedFrameCenterY(config) - 40.0;
        }

        private static double MovingFrameBottomY(LexWorkbenchConfig config)
        {
            return FixedFrameTopY(config) + config.CarriageAdapterThicknessMm + config.LinearGuide.AssemblyHeightMm;
        }

        private static void Validate(LexWorkbenchConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.LinearGuide == null) throw new ArgumentException("Lineaire geleiding ontbreekt.");
            if (config.LiftColumn == null) throw new ArgumentException("HTE2-hefkolomdata ontbreekt.");
            if (config.LevelingFootCornerAdapter == null) throw new ArgumentException("LEX-stelpoot hoekadapterdata ontbreekt.");
            if (config.LevelingFoot == null) throw new ArgumentException("LEX-stelpootdata ontbreekt.");
            if (config.Profile80x40 == null || config.Profile80x80 == null || config.Profile40x40 == null) throw new ArgumentException("LEX-profielmaterialen ontbreken.");
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
