using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class LinearRobotCellEngine
    {
        public WorkbenchModel Build(LinearRobotCellConfig config)
        {
            Validate(config);
            var uprightSize = SquareSize(config.UprightProfile, "staander");
            var beamVertical = Math.Max(config.FrameBeamProfile.WidthMm, config.FrameBeamProfile.HeightMm);
            var beamTransverse = Math.Min(config.FrameBeamProfile.WidthMm, config.FrameBeamProfile.HeightMm);
            var guardSize = SquareSize(config.GuardProfile, "topframe");
            var railCarrierSize = SquareSize(config.RailCarrierProfile, "raildrager");
            var totalDepth = config.RailZoneWidthMm + config.WorktopSideCount * config.WorktopDepthMm;
            var worktopThickness = config.WorktopMaterial.ThicknessMm;
            var frameTop = config.WorktopHeightMm - worktopThickness;
            var uprightBottom = config.FootVisibleHeightMm + config.FootPlateThicknessMm;
            var uprightLength = frameTop - uprightBottom;
            var guardTop = config.WorktopHeightMm + config.GuardHeightAboveWorktopMm;
            var cornerUprightLength = guardTop - uprightBottom;
            var stationXs = SupportStations(config.LengthMm, uprightSize, config.IntermediateSupportMaxSpacingMm);
            var outerPostZ = totalDepth / 2.0 - uprightSize / 2.0;
            var beamLaneZ = totalDepth / 2.0 - beamTransverse / 2.0;
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                LowerFrameHeightMm = uprightBottom,
                MiddleLayerHeightMm = config.WorktopHeightMm,
                SawingMode = config.SawingMode
            };

            AddSupports(model, config, stationXs, outerPostZ, uprightSize, uprightLength, cornerUprightLength, uprightBottom);
            AddFrameLayer(model, config, stationXs, outerPostZ, beamLaneZ, uprightSize, beamTransverse, beamVertical,
                uprightBottom + beamVertical / 2.0, "onderframe", config.LowerFrameEndCrossmembersUseOuterLane);
            AddFrameLayer(model, config, stationXs, outerPostZ, beamLaneZ, uprightSize, beamTransverse, beamVertical,
                frameTop - beamVertical / 2.0, "werkbladframe", false);
            if (config.WorktopSideCount == 2)
                AddCenterSupports(model, config, stationXs, uprightSize, beamVertical, uprightBottom, frameTop);
            AddWorktops(model, config, totalDepth, worktopThickness);
            AddLinearAxis(model, config, uprightSize, railCarrierSize, frameTop);
            AddGuard(model, config, totalDepth, guardSize, uprightSize, frameTop);

            model.DesignNotes.Add("Robotkeuze: FAIRINO FR5, 5 kg nominale payload (7 kg maximum), 922 mm bereik, circa 22 kg, voet Ø149 mm met 4x Ø9 op steekcirkel Ø132 mm en positioneergat Ø8 H7 x 10 mm volgens de officiële FAIRINO-tekeningen.");
            model.DesignNotes.Add("Lineaire geleiding: twee HIWIN HGR20-rails met vier HGH20CA-wagens. Officiële maatdata: rail 20x17,5 mm; wagen 77,5x44 mm; totale montagehoogte 30 mm; wagenpatroon 32x36 mm met 4x M5x6; railgatsteek 60 mm voor M5x16. Voorspanning, nauwkeurigheidsklasse, afdichting, raillengte-uitvoering en eindafstanden blijven te selecteren. Productie-export is daarom geblokkeerd.");
            model.DesignNotes.Add("De lineaire geleiding wordt niet door het werkblad gedragen. Twee doorlopende 80x80-raildragers rusten rechtstreeks op de dwarsliggers van het werkbladframe; het HPL vormt alleen de werkzone ernaast.");
            model.DesignNotes.Add("Primaire staanders en raildragers zijn 80x80. Onder- en werkbladframe zijn 40x80 staand; het lichte afschermframe is 40x40. De berekende tussensteunen beperken de vrije vaklengte, maar definitieve dynamische stijfheid en vloerverankering vereisen robotmassa, payload, bereik en versnellingsprofiel.");
            model.DesignNotes.Add(config.WorktopSideCount == 1
                ? "Eenzijdige uitvoering: één werkblad aan operatorzijde, de complete lineaire as in de achterste railzone, één Type-4-lichtscherm aan de lange voorzijde en helder acrylaat aan achterzijde en beide kopse zijden."
                : "Tweezijdige uitvoering: werkblad aan beide zijden van de centrale robotas, Type-4-lichtschermen aan beide lange operatorzijden en helder acrylaat uitsluitend aan beide kopse zijden; iedere kopwand is door een 40x40-middenstaander in twee velden verdeeld. Een centrale rij 80x80-steunen met eigen voetplaten en stelvoeten draagt de railzone via de frame-dwarsliggers rechtstreeks naar de vloer af.");
            ValidateManifest(model, stationXs.Length, config);
            return model;
        }

        private static void AddSupports(WorkbenchModel model, LinearRobotCellConfig config, double[] stationXs, double outerPostZ,
            double uprightSize, double uprightLength, double cornerUprightLength, double uprightBottom)
        {
            var regularCount = Math.Max(0, (stationXs.Length - 2) * 2);
            AddProfile(model, "Lineaire robotcel doorlopende hoekstaander 80x80", config.UprightProfile, cornerUprightLength,
                config.ThroughCornerUprightCount, "Ononderbroken van M16-voetplaat tot bovenzijde afschermframe", config.SawingMode);
            if (regularCount > 0)
                AddProfile(model, "Lineaire robotcel tussenstaander 80x80", config.UprightProfile, uprightLength, regularCount,
                    "Primaire tussenstaander tussen M16-voetplaat en werkbladframe", config.SawingMode);
            var memberIndex = 0;
            for (var xIndex = 0; xIndex < stationXs.Length; xIndex++)
            foreach (var z in new[] { -outerPostZ, outerPostZ })
            {
                memberIndex++;
                var isCorner = xIndex == 0 || xIndex == stationXs.Length - 1;
                var placementHeight = isCorner ? cornerUprightLength : uprightLength;
                AddPlacement(model, "LRC-UP-" + memberIndex.ToString("00"), "Staander 80x80 " + memberIndex.ToString("00"),
                    AssemblyComponentKind.Profile, uprightSize, uprightSize, placementHeight, stationXs[xIndex],
                    uprightBottom + placementHeight / 2.0, z, "profile", "box", null);
                AddPlacement(model, "LRC-FP-" + memberIndex.ToString("00"), "Voetplaat 80x80 M16 " + memberIndex.ToString("00"),
                    AssemblyComponentKind.Purchased, uprightSize, uprightSize, config.FootPlateThicknessMm, stationXs[xIndex],
                    config.FootVisibleHeightMm + config.FootPlateThicknessMm / 2.0, z, "hardware-adapter", "box",
                    "techxxl_footplate_10_80x80_m16");
                AddPlacement(model, "LRC-FT-" + memberIndex.ToString("00"), "Stelvoet D80 M16 " + memberIndex.ToString("00"),
                    AssemblyComponentKind.Purchased, config.FootDiameterMm, config.FootDiameterMm, config.FootVisibleHeightMm, stationXs[xIndex],
                    config.FootVisibleHeightMm / 2.0, z, "hardware-foot", "cylinder",
                    "techxxl_leveling_foot_d80_m16x150");
            }
            model.Hardware.Add(new HardwareItem { Name = "TechXXL voetplaat 80x80 M16", ArticleNumber = "TIN 101445", Quantity = stationXs.Length * 2, Unit = "st", Note = "Bestaand leveranciersrecord; vloerverankering aanvullend bepalen", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL stelvoet D80 M16x150", ArticleNumber = "TIN 101219", Quantity = stationXs.Length * 2, Unit = "st", Note = "Stelmiddel, niet de definitieve dynamische vloerverankering", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
        }

        private static void AddCenterSupports(WorkbenchModel model, LinearRobotCellConfig config, double[] stationXs,
            double uprightSize, double beamVertical, double uprightBottom, double frameTop)
        {
            var lowerBeamTop = uprightBottom + beamVertical;
            var upperBeamBottom = frameTop - beamVertical;
            var supportLength = upperBeamBottom - lowerBeamTop;
            var supportCount = stationXs.Length * config.TwoSidedCenterSupportRowCount;
            AddProfile(model, "LRC tweezijdige middensteun 80x80", config.UprightProfile, supportLength, supportCount,
                "Centrale railondersteuning tussen bovenkant onderframe en onderkant werkbladframe", config.SawingMode);
            var index = 0;
            foreach (var x in stationXs)
            for (var row = 0; row < config.TwoSidedCenterSupportRowCount; row++)
            {
                index++;
                var z = config.TwoSidedCenterSupportRowCount == 1
                    ? 0.0
                    : -config.RailZoneWidthMm / 2.0 + config.RailZoneWidthMm * (row + 1.0) / (config.TwoSidedCenterSupportRowCount + 1.0);
                AddPlacement(model, "LRC-CENTER-SUPPORT-" + index.ToString("00"), "Middensteun 80x80 " + index.ToString("00"),
                    AssemblyComponentKind.Profile, uprightSize, uprightSize, supportLength, x,
                    lowerBeamTop + supportLength / 2.0, z, "profile", "box", null);
                AddPlacement(model, "LRC-CENTER-FP-" + index.ToString("00"), "Middensteun voetplaat 80x80 M16 " + index.ToString("00"),
                    AssemblyComponentKind.Purchased, uprightSize, uprightSize, config.FootPlateThicknessMm, x,
                    config.FootVisibleHeightMm + config.FootPlateThicknessMm / 2.0, z, "hardware-adapter", "box",
                    "techxxl_footplate_10_80x80_m16");
                AddPlacement(model, "LRC-CENTER-FT-" + index.ToString("00"), "Middensteun stelvoet D80 M16 " + index.ToString("00"),
                    AssemblyComponentKind.Purchased, config.FootDiameterMm, config.FootDiameterMm, config.FootVisibleHeightMm, x,
                    config.FootVisibleHeightMm / 2.0, z, "hardware-foot", "cylinder",
                    "techxxl_leveling_foot_d80_m16x150");
            }
            model.Hardware.Add(new HardwareItem { Name = "TechXXL voetplaat 80x80 M16 middensteunrij", ArticleNumber = "TIN 101445", Quantity = supportCount, Unit = "st", Note = "Ondersteunt de centrale railzone via de onderste dwarsliggers; vloerverankering aanvullend bepalen", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL stelvoet D80 M16x150 middensteunrij", ArticleNumber = "TIN 101219", Quantity = supportCount, Unit = "st", Note = "Stelmiddel onder centrale railsteun; niet de definitieve dynamische vloerverankering", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
        }

        private static void AddFrameLayer(WorkbenchModel model, LinearRobotCellConfig config, double[] stationXs, double outerPostZ,
            double beamLaneZ, double uprightSize, double beamTransverse, double beamVertical, double centerY, string layer,
            bool outerEndCrossmemberLane)
        {
            var longitudinalQuantity = 2 * (stationXs.Length - 1);
            var crossQuantity = stationXs.Length;
            for (var i = 0; i < stationXs.Length - 1; i++)
            {
                var length = stationXs[i + 1] - stationXs[i] - uprightSize;
                AddProfile(model, "LRC " + layer + " langsligger 40x80 vak " + (i + 1), config.FrameBeamProfile, length, 2,
                    "40x80 staand; 80 mm verticaal en buitenvlak gelijk met staanders", config.SawingMode);
                foreach (var side in new[] { -1.0, 1.0 })
                {
                    AddPlacement(model, "LRC-" + layer + "-LONG-" + (i + 1) + (side < 0 ? "-F" : "-R"),
                        Capitalize(layer) + " langsligger " + (i + 1) + (side < 0 ? " voor" : " achter"),
                        AssemblyComponentKind.Profile, length, beamTransverse, beamVertical,
                        (stationXs[i] + stationXs[i + 1]) / 2.0, centerY, side * beamLaneZ, "profile", "box", null);
                }
            }
            var crossLength = 2.0 * outerPostZ - uprightSize;
            AddProfile(model, "LRC " + layer + " dwarsligger 40x80", config.FrameBeamProfile, crossLength, crossQuantity,
                "40x80 staand; hart op geldige 40-mm moduulbaan van de 80x80-staander", config.SawingMode);
            for (var i = 0; i < stationXs.Length; i++)
            {
                var isFirst = i == 0;
                var isLast = i == stationXs.Length - 1;
                var laneOffset = outerEndCrossmemberLane && isFirst
                    ? -beamTransverse / 2.0
                    : outerEndCrossmemberLane && isLast
                        ? beamTransverse / 2.0
                        : isLast ? -beamTransverse / 2.0 : beamTransverse / 2.0;
                AddPlacement(model, "LRC-" + layer + "-CROSS-" + (i + 1), Capitalize(layer) + " dwarsligger " + (i + 1),
                    AssemblyComponentKind.Profile, beamTransverse, crossLength, beamVertical,
                    stationXs[i] + laneOffset, centerY, 0, "profile", "box", null);
            }
            if (longitudinalQuantity <= 0 || crossQuantity < 2) throw new InvalidOperationException("Lineaire robotcel heeft onvoldoende framevakken.");
        }

        private static void AddWorktops(WorkbenchModel model, LinearRobotCellConfig config, double totalDepth, double thickness)
        {
            var railHalf = config.RailZoneWidthMm / 2.0;
            var centers = config.WorktopSideCount == 1
                ? new[] { -totalDepth / 2.0 + config.WorktopDepthMm / 2.0 }
                : new[] { -(railHalf + config.WorktopDepthMm / 2.0), railHalf + config.WorktopDepthMm / 2.0 };
            for (var i = 0; i < centers.Length; i++)
            {
                var sheet = SheetDrawing.CreateSheet("LRC werkblad zijde " + (i + 1), config.WorktopMaterial, config.LengthMm, config.WorktopDepthMm);
                SheetDrawing.AddSheetToModel(model, sheet, 0, config.WorktopHeightMm - thickness / 2.0, centers[i], AssemblyOrientation.SheetHorizontal);
            }
        }

        private static void AddLinearAxis(WorkbenchModel model, LinearRobotCellConfig config, double uprightSize, double carrierSize, double frameTop)
        {
            var axisLength = config.LengthMm - 2.0 * uprightSize;
            var carrierCenterY = frameTop + carrierSize / 2.0;
            var railZoneCenterZ = config.WorktopSideCount == 1 ? config.WorktopDepthMm / 2.0 : 0;
            AddProfile(model, "LRC raildrager 80x80", config.RailCarrierProfile, axisLength, 2,
                "Doorlopende raildragers rechtstreeks op de werkblad-dwarsliggers", config.SawingMode);
            foreach (var side in new[] { -1.0, 1.0 })
            {
                var z = railZoneCenterZ + side * config.RailCenterSpacingMm / 2.0;
                AddPlacement(model, "LRC-RAIL-CARRIER-" + (side < 0 ? "F" : "R"), "Raildrager 80x80 " + (side < 0 ? "voor" : "achter"),
                    AssemblyComponentKind.Profile, axisLength, carrierSize, carrierSize, 0, carrierCenterY, z, "profile", "box", null);
                AddPlacement(model, "LRC-LINEAR-RAIL-" + (side < 0 ? "F" : "R"), "HIWIN HGR20 rail " + (side < 0 ? "voor" : "achter"),
                    AssemblyComponentKind.Purchased, axisLength, config.RailWidthMm, config.RailHeightMm, 0,
                    frameTop + carrierSize + config.RailHeightMm / 2.0, z, "hardware-rail", "box", "hiwin_hgr20_rail");
            }

            var carriageY = frameTop + carrierSize + config.RailHeightMm + config.CarriageHeightMm / 2.0;
            var carriageOffsetX = config.RobotAdapterLengthMm / 4.0;
            var carriageIndex = 0;
            foreach (var x in new[] { -carriageOffsetX, carriageOffsetX })
            foreach (var z in new[] { railZoneCenterZ - config.RailCenterSpacingMm / 2.0, railZoneCenterZ + config.RailCenterSpacingMm / 2.0 })
            {
                carriageIndex++;
                AddPlacement(model, "LRC-CARRIAGE-" + carriageIndex, "HIWIN HGH20CA wagen " + carriageIndex,
                    AssemblyComponentKind.Purchased, config.CarriageLengthMm, config.CarriageWidthMm, config.CarriageHeightMm,
                    x, carriageY, z, "hardware-carriage", "box", "hiwin_hgh20ca_block");
            }

            var plateY = carriageY + config.CarriageHeightMm / 2.0 + config.RobotAdapterThicknessMm / 2.0;
            AddPlacement(model, "LRC-ROBOT-ADAPTER", "Robotadapterplaat FAIRINO FR5 - voorlopige envelop",
                AssemblyComponentKind.Purchased, config.RobotAdapterLengthMm, config.RobotAdapterWidthMm, config.RobotAdapterThicknessMm,
                0, plateY, railZoneCenterZ, "hardware-adapter", "box", "linear_robot_adapter_plate_provisional");
            AddPlacement(model, "LRC-MOTOR-ADAPTER", "Motoradapterplaat aan robotadapter - voorlopige envelop",
                AssemblyComponentKind.Purchased, config.MotorAdapterThicknessMm, config.MotorAdapterLengthMm, config.MotorAdapterHeightMm,
                config.RobotAdapterLengthMm / 2.0 + config.MotorAdapterThicknessMm / 2.0,
                plateY - config.MotorAdapterHeightMm / 2.0, railZoneCenterZ, "hardware-adapter", "box", "linear_motor_adapter_plate_provisional");
            AddPlacement(model, "LRC-RACK", "Voorlopige tandheugel langs raildrager",
                AssemblyComponentKind.Purchased, axisLength, config.RackWidthMm, config.RackHeightMm, 0,
                frameTop + carrierSize - config.RackHeightMm / 2.0,
                railZoneCenterZ + config.RailCenterSpacingMm / 2.0 + carrierSize / 2.0 + config.RackWidthMm / 2.0,
                "hardware-rack", "box", "rack_pinion_drive_provisional");

            model.Hardware.Add(new HardwareItem { Name = "HIWIN HGR20 rails", Quantity = 2, Unit = "st", Note = "Lengte parametrisch; 20x17,5 mm, gatsteek 60 mm en M5x16 volgens HIWIN. Exacte raillengte-uitvoering, eindafstanden en eventuele deling nog selecteren", ModelStatus = "Leveranciersmaatmodel", BomStatus = "Niet vrijgegeven" });
            model.Hardware.Add(new HardwareItem { Name = "HIWIN HGH20CA wagen", Quantity = 4, Unit = "st", Note = "77,5x44 mm, montagehoogte met rail 30 mm, patroon 32x36 mm met 4x M5x6; volledige bestelcode voor voorspanning, nauwkeurigheid en afdichting nog selecteren", ModelStatus = "Leveranciersmaatmodel", BomStatus = "Niet vrijgegeven" });
            model.Hardware.Add(new HardwareItem { Name = "Robotadapterplaat FAIRINO FR5 naar vier railwagens", Quantity = 1, Unit = "st", Note = "FR5-interface bekend: voet Ø149, 4x Ø9 op steekcirkel Ø132 en positioneergat Ø8 H7 x 10; plaatmateriaal, dikte, railwageninterface en berekening open", ModelStatus = "ProvisionalRenderEnvelope", BomStatus = "Niet vrijgegeven" });
            model.Hardware.Add(new HardwareItem { Name = "Motoradapterplaat gekoppeld aan robotadapterplaat", Quantity = 1, Unit = "st", Note = "Motor, reductor, boutpatroon, riem/tandwiel en reactiekrachten open", ModelStatus = "ProvisionalRenderEnvelope", BomStatus = "Niet vrijgegeven" });
            model.Hardware.Add(new HardwareItem { Name = "Tandheugel met pignon voor lineaire robotas", Quantity = 1, Unit = "set", Note = "Module, kwaliteit, voorspanning, smering en leverancier open", ModelStatus = "ProvisionalRenderEnvelope", BomStatus = "Niet vrijgegeven" });
        }

        private static void AddGuard(WorkbenchModel model, LinearRobotCellConfig config, double totalDepth, double guardSize,
            double uprightSize, double frameTop)
        {
            var guardBottom = config.WorktopHeightMm;
            var guardTop = guardBottom + config.GuardHeightAboveWorktopMm;
            var guardPostLength = guardTop - guardBottom;
            var intermediatePostLength = guardTop - guardSize - frameTop;
            var cornerPostZ = totalDepth / 2.0 - uprightSize / 2.0;
            var guardLaneZ = totalDepth / 2.0 - guardSize / 2.0;
            var cornerPostX = config.LengthMm / 2.0 - uprightSize / 2.0;
            var endX = config.LengthMm / 2.0 - guardSize / 2.0;
            var postXs = new List<double> { -endX, endX };
            var rearStationXs = config.WorktopSideCount == 1
                ? SupportStations(config.LengthMm, uprightSize, config.IntermediateSupportMaxSpacingMm)
                : new double[0];
            var rearIntermediateXs = rearStationXs.Length > 2
                ? rearStationXs.Skip(1).Take(rearStationXs.Length - 2).ToArray()
                : new double[0];
            var endWallIntermediateZs = config.WorktopSideCount == 2
                ? Enumerable.Range(1, config.TwoSidedEndWallIntermediatePostCount)
                    .Select(index => -cornerPostZ + 2.0 * cornerPostZ * index / (config.TwoSidedEndWallIntermediatePostCount + 1.0)).ToArray()
                : new double[0];

            if (rearIntermediateXs.Length > 0)
                AddProfile(model, "LRC achterwand tussenstaander 40x40", config.GuardProfile, intermediatePostLength,
                    rearIntermediateXs.Length, "Tussenstaanders lopen vanaf het werkbladframe tot de onderzijde van de bovenligger", config.SawingMode);
            for (var i = 0; i < rearIntermediateXs.Length; i++)
            {
                AddPlacement(model, "LRC-GUARD-REAR-POST-" + (i + 1), "Topframe achterstaander " + (i + 1),
                    AssemblyComponentKind.Profile, guardSize, guardSize, intermediatePostLength, rearIntermediateXs[i],
                    frameTop + intermediatePostLength / 2.0, guardLaneZ, "profile", "box", null);
            }

            if (endWallIntermediateZs.Length > 0)
            {
                var intermediateLength = intermediatePostLength;
                AddProfile(model, "LRC tweezijdige kopwand middenstaander 40x40", config.GuardProfile, intermediateLength,
                    endWallIntermediateZs.Length * 2, "Middenstaanders lopen vanaf het werkbladframe tot de onderzijde van de kopse bovenligger", config.SawingMode);
                foreach (var x in postXs)
                for (var i = 0; i < endWallIntermediateZs.Length; i++)
                    AddPlacement(model, "LRC-GUARD-END-CENTER-" + (x < 0 ? "L" : "R") + "-" + (i + 1),
                        "Topframe kopwand middenstaander " + (x < 0 ? "links " : "rechts ") + (i + 1),
                        AssemblyComponentKind.Profile, guardSize, guardSize, intermediateLength, x,
                        frameTop + intermediateLength / 2.0, endWallIntermediateZs[i], "profile", "box", null);
            }

            var longLength = config.LengthMm - 2.0 * uprightSize;
            AddProfile(model, "LRC topframe langsligger 40x40", config.GuardProfile, longLength, 2,
                "Licht topframe tussen de binnenvlakken van de doorlopende 80x80-hoekstaanders", config.SawingMode);
            AddPlacement(model, "LRC-GUARD-TOP-FRONT", "Topframe langsligger voor", AssemblyComponentKind.Profile,
                longLength, guardSize, guardSize, 0, guardTop - guardSize / 2.0, -guardLaneZ, "profile", "box", null);
            AddPlacement(model, "LRC-GUARD-TOP-REAR", "Topframe langsligger achter", AssemblyComponentKind.Profile,
                longLength, guardSize, guardSize, 0, guardTop - guardSize / 2.0, guardLaneZ, "profile", "box", null);
            var endLength = totalDepth - 2.0 * uprightSize;
            AddProfile(model, "LRC topframe kopligger 40x40", config.GuardProfile, endLength, 2,
                "Kopse topframeprofielen tussen de binnenvlakken van de doorlopende 80x80-hoekstaanders", config.SawingMode);
            AddPlacement(model, "LRC-GUARD-TOP-LEFT", "Topframe kopligger links", AssemblyComponentKind.Profile,
                guardSize, endLength, guardSize, -endX, guardTop - guardSize / 2.0, 0, "profile", "box", null);
            AddPlacement(model, "LRC-GUARD-TOP-RIGHT", "Topframe kopligger rechts", AssemblyComponentKind.Profile,
                guardSize, endLength, guardSize, endX, guardTop - guardSize / 2.0, 0, "profile", "box", null);

            AddEndPanels(model, config, "links", -1, endX, cornerPostZ, uprightSize, guardSize, guardPostLength, guardBottom, endWallIntermediateZs);
            AddEndPanels(model, config, "rechts", 1, endX, cornerPostZ, uprightSize, guardSize, guardPostLength, guardBottom, endWallIntermediateZs);
            if (config.WorktopSideCount == 1)
                AddRearPanels(model, config, rearStationXs, guardLaneZ, uprightSize, guardSize, guardPostLength, guardBottom);
            AddLightCurtain(model, config, "voor", -guardLaneZ, cornerPostX, uprightSize, guardPostLength, guardBottom);
            if (config.WorktopSideCount == 2)
                AddLightCurtain(model, config, "achter", guardLaneZ, cornerPostX, uprightSize, guardPostLength, guardBottom);
        }

        private static void AddEndPanels(WorkbenchModel model, LinearRobotCellConfig config, string sideName, int side,
            double endX, double cornerPostZ, double uprightSize, double guardSize, double height, double bottom,
            double[] intermediateZs)
        {
            var clearHeight = height - guardSize;
            var stations = new[] { -cornerPostZ }.Concat(intermediateZs).Concat(new[] { cornerPostZ }).ToArray();
            for (var i = 0; i < stations.Length - 1; i++)
            {
                var leftWidth = i == 0 ? uprightSize : guardSize;
                var rightWidth = i == stations.Length - 2 ? uprightSize : guardSize;
                var clearDepth = stations[i + 1] - stations[i] - (leftWidth + rightWidth) / 2.0;
                var suffix = intermediateZs.Length == 0 ? string.Empty : " vak " + (i + 1);
                var sheet = SheetDrawing.CreateSheet("LRC acrylaat kopwand " + sideName + suffix, config.GuardPanelMaterial, clearDepth, clearHeight);
                AddAcrylicSheet(model, sheet, side * endX, bottom + clearHeight / 2.0,
                    (stations[i] + stations[i + 1]) / 2.0, AssemblyOrientation.SheetVerticalZ);
            }
        }

        private static void AddRearPanels(WorkbenchModel model, LinearRobotCellConfig config, double[] stationXs, double outerZ,
            double uprightSize, double guardSize, double height, double bottom)
        {
            var clearHeight = height - guardSize;
            for (var i = 0; i < stationXs.Length - 1; i++)
            {
                var leftWidth = i == 0 ? uprightSize : guardSize;
                var rightWidth = i == stationXs.Length - 2 ? uprightSize : guardSize;
                var clearWidth = stationXs[i + 1] - stationXs[i] - (leftWidth + rightWidth) / 2.0;
                var sheet = SheetDrawing.CreateSheet("LRC acrylaat achterwand vak " + (i + 1), config.GuardPanelMaterial, clearWidth, clearHeight);
                AddAcrylicSheet(model, sheet, (stationXs[i] + stationXs[i + 1]) / 2.0, bottom + clearHeight / 2.0,
                    outerZ, AssemblyOrientation.SheetVerticalX);
            }
        }

        private static void AddLightCurtain(WorkbenchModel model, LinearRobotCellConfig config, string sideName, double z,
            double cornerPostX, double uprightSize, double height, double bottom)
        {
            var innerFaceX = cornerPostX - uprightSize / 2.0;
            var curtainCenterX = innerFaceX - config.LightCurtainWidthMm / 2.0;
            var centerY = bottom + height / 2.0;
            AddPlacement(model, "LRC-LIGHT-CURTAIN-" + sideName.ToUpperInvariant() + "-TX", "Veiligheidslichtscherm " + sideName + " zender",
                AssemblyComponentKind.Purchased, config.LightCurtainWidthMm, config.LightCurtainDepthMm, config.LightCurtainOverallHeightMm,
                -curtainCenterX, centerY, z, "safety-light-curtain", "box", config.LightCurtainEmitterComponentId, 0, 0, 0);
            AddPlacement(model, "LRC-LIGHT-CURTAIN-" + sideName.ToUpperInvariant() + "-RX", "Veiligheidslichtscherm " + sideName + " ontvanger",
                AssemblyComponentKind.Purchased, config.LightCurtainWidthMm, config.LightCurtainDepthMm, config.LightCurtainOverallHeightMm,
                curtainCenterX, centerY, z, "safety-light-curtain", "box", config.LightCurtainReceiverComponentId, 0, 180, 0);
            model.Hardware.Add(new HardwareItem { Name = config.LightCurtainDisplayName + " " + sideName, ArticleNumber = config.LightCurtainArticleNumber, PricingCategory = "Veiligheid", PricingKey = config.LightCurtainSetComponentId, Quantity = 1, Unit = "set", Note = "Zender, ontvanger en standaardbeugels; 30 mm resolutie en " + config.LightCurtainProtectedHeightMm.ToString("0", CultureInfo.InvariantCulture) + " mm beschermd veld. Veiligheidsafstand, resetconcept en vereiste PLr blijven projectspecifiek te valideren.", ModelStatus = "Leveranciersmaatmodel", BomStatus = "Veiligheidsvalidatie vereist" });
        }

        private static void AddAcrylicSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
            model.AssemblyPlacements[model.AssemblyPlacements.Count - 1].Shape = "acrylic-panel";
        }

        private static double[] SupportStations(double length, double uprightSize, double maxSpacing)
        {
            var usable = length - uprightSize;
            var bayCount = Math.Max(1, (int)Math.Ceiling(usable / maxSpacing));
            var start = -length / 2.0 + uprightSize / 2.0;
            return Enumerable.Range(0, bayCount + 1).Select(i => start + usable * i / bayCount).ToArray();
        }

        private static double SquareSize(Material material, string role)
        {
            if (material == null || material.WidthMm <= 0 || material.HeightMm <= 0 || Math.Abs(material.WidthMm - material.HeightMm) > 0.001)
                throw new ArgumentException("Lineaire robotcel vereist een vierkant profiel voor " + role + ".");
            return material.WidthMm;
        }

        private static void ValidateManifest(WorkbenchModel model, int stationCount, LinearRobotCellConfig config)
        {
            RequireCount(model, "Staander 80x80 ", stationCount * 2);
            RequireCount(model, "Middensteun 80x80 ", config.WorktopSideCount == 2
                ? stationCount * config.TwoSidedCenterSupportRowCount
                : 0);
            RequireCount(model, "Middensteun voetplaat 80x80 M16 ", config.WorktopSideCount == 2
                ? stationCount * config.TwoSidedCenterSupportRowCount
                : 0);
            RequireCount(model, "Middensteun stelvoet D80 M16 ", config.WorktopSideCount == 2
                ? stationCount * config.TwoSidedCenterSupportRowCount
                : 0);
            RequireCount(model, "HIWIN HGR20 rail ", 2);
            RequireCount(model, "HIWIN HGH20CA wagen ", 4);
            RequireCount(model, "Robotadapterplaat FAIRINO FR5", 1);
            RequireCount(model, "Motoradapterplaat aan robotadapter", 1);
            RequireCount(model, "Veiligheidslichtscherm ", config.WorktopSideCount * 2);
            RequireCount(model, "LRC werkblad zijde ", config.WorktopSideCount);
            RequireCount(model, "LRC acrylaat kopwand ", config.WorktopSideCount == 2
                ? 2 * (config.TwoSidedEndWallIntermediatePostCount + 1)
                : 2);
            RequireCount(model, "Topframe kopwand middenstaander ", config.WorktopSideCount == 2
                ? 2 * config.TwoSidedEndWallIntermediatePostCount
                : 0);
            RequireCount(model, "Topframe achterstaander ", config.WorktopSideCount == 1
                ? Math.Max(0, stationCount - 2)
                : 0);
            RequireCount(model, "Topframe voorstaander ", 0);
        }

        private static void RequireCount(WorkbenchModel model, string prefix, int expected)
        {
            var actual = model.AssemblyPlacements.Count(item => (item.PartName ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal));
            if (actual != expected) throw new InvalidOperationException("Lineaire robotcel manifestfout voor " + prefix + ": verwacht " + expected + ", gevonden " + actual + ".");
        }

        private static void AddProfile(WorkbenchModel model, string name, Material material, double length, int quantity, string note, ProfileSawingMode mode)
        {
            if (length <= 0 || quantity <= 0) throw new InvalidOperationException("Ongeldig profielrecord voor " + name + ".");
            if (material.StockLengthMm > 0 && length > material.StockLengthMm + 0.001)
                throw new InvalidOperationException(name + " overschrijdt handelslengte " + material.StockLengthMm.ToString("0.##") + " mm.");
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = length, Quantity = quantity, OrientationNote = note, BomStatus = "Lineaire robotcel frame" });
            model.ProfileOperations.Add(new ProfileOperation { ProfileId = material.Id + "_" + name.Replace(' ', '_'), PartName = name, Quantity = quantity, Material = material, ProfileLengthMm = length, Sequence = 1, Kind = ProfileOperationKind.SawCut, SawAngleDeg = 90, WorkOrigin = "Kop A", MachineHint = mode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH", ExecutionParty = mode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER", Note = note });
        }

        private static void AddPlacement(WorkbenchModel model, string memberId, string name, AssemblyComponentKind kind,
            double length, double width, double height, double x, double y, double z, string visualKind, string shape, string componentId,
            double rotationXDeg = 0, double rotationYDeg = 0, double rotationZDeg = 0)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { MemberId = memberId, PartName = name, ComponentId = componentId, Kind = kind,
                LengthMm = length, WidthMm = width, HeightMm = height, Xmm = x, Ymm = y, Zmm = z,
                Orientation = AssemblyOrientation.Default, VisualKind = visualKind, Shape = shape,
                RotationXDeg = rotationXDeg, RotationYDeg = rotationYDeg, RotationZDeg = rotationZDeg });
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static void Validate(LinearRobotCellConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.WorktopSideCount != 1 && config.WorktopSideCount != 2) throw new ArgumentException("Werkbladzijde moet één- of tweezijdig zijn.");
            if (config.LengthMm <= 0 || config.WorktopDepthMm <= 0 || config.WorktopHeightMm <= 0 || config.GuardHeightAboveWorktopMm <= 0)
                throw new ArgumentException("Lineaire robotcel bevat ongeldige hoofdmaten.");
            if (config.UprightProfile == null || config.FrameBeamProfile == null || config.RailCarrierProfile == null || config.GuardProfile == null
                || config.WorktopMaterial == null || config.GuardPanelMaterial == null) throw new ArgumentException("Lineaire robotcel materiaalcontract is onvolledig.");
            if (!config.LowerFrameEndCrossmembersUseOuterLane || config.TwoSidedEndWallIntermediatePostCount <= 0
                || config.ThroughCornerUprightCount != 4 || config.TwoSidedCenterSupportRowCount != 1)
                throw new ArgumentException("Lineaire robotcel mist de masterdataregels voor onderframebaan, doorlopende hoekstaanders, tweezijdige kopwandstaanders of middensteunrij.");
            if (config.RailCenterSpacingMm + config.RailWidthMm > config.RailZoneWidthMm)
                throw new ArgumentException("Lineaire rails passen niet binnen de railzone.");
            if (config.RobotAdapterWidthMm > config.RailZoneWidthMm)
                throw new ArgumentException("Voorlopige robotadapter valt buiten de railzone.");
            if (string.IsNullOrWhiteSpace(config.LightCurtainSetComponentId) || string.IsNullOrWhiteSpace(config.LightCurtainEmitterComponentId)
                || string.IsNullOrWhiteSpace(config.LightCurtainReceiverComponentId) || string.IsNullOrWhiteSpace(config.LightCurtainDisplayName)
                || string.IsNullOrWhiteSpace(config.LightCurtainArticleNumber) || config.LightCurtainProtectedHeightMm <= 0 || config.LightCurtainWidthMm <= 0
                || config.LightCurtainOverallHeightMm <= 0 || config.LightCurtainDepthMm <= 0)
                throw new ArgumentException("Lineaire robotcel mist het getypeerde lichtschermcontract.");
            if (config.GuardHeightAboveWorktopMm + 0.001 < config.LightCurtainOverallHeightMm)
                throw new ArgumentException("Afschermhoogte is lager dan het geselecteerde lichtschermhuis.");
            if (config.WorktopHeightMm <= config.FootVisibleHeightMm + config.FootPlateThicknessMm + Math.Max(config.FrameBeamProfile.WidthMm, config.FrameBeamProfile.HeightMm))
                throw new ArgumentException("Werkbladhoogte is te laag voor voet-, staander- en frameketen.");
            if (config.WorktopSideCount == 2
                && config.WorktopHeightMm <= config.FootVisibleHeightMm + config.FootPlateThicknessMm
                    + 2.0 * Math.Max(config.FrameBeamProfile.WidthMm, config.FrameBeamProfile.HeightMm))
                throw new ArgumentException("Werkbladhoogte is te laag voor de tweezijdige middensteun tussen onder- en werkbladframe.");
        }
    }
}
