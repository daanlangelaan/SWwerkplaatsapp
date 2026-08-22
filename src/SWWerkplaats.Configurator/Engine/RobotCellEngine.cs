using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class RobotCellEngine
    {
        private const double FootVisibleHeightMm = 80.0;
        private const double FootDiameterMm = 80.0;
        private const double FootBaseHeightMm = 18.0;
        private const double FootStemDiameterMm = 16.0;
        private const double FootPlateThicknessMm = 15.0;
        private const double UprightSizeMm = 80.0;
        private const double BeamWidthMm = 40.0;
        private const double BeamHeightMm = 80.0;
        private const int LowerCrossBeamCount = 1;
        private const int RearRailEndCapCount = 2;

        private enum ModuleLane
        {
            OuterFlush,
            RecessedOneModule
        }

        public WorkbenchModel Build(RobotCellConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                LowerFrameHeightMm = FootVisibleHeightMm + FootPlateThicknessMm,
                MiddleLayerHeightMm = config.WorktopHeightMm,
                SawingMode = config.SawingMode
            };

            var worktopThickness = Math.Max(2.0, config.WorktopMaterial.ThicknessMm);
            var frameTop = config.WorktopHeightMm - worktopThickness;
            var frameBottom = frameTop - BeamHeightMm;
            var uprightBottom = FootVisibleHeightMm + FootPlateThicknessMm;
            var uprightLength = frameTop - uprightBottom;
            var x = (config.WidthMm - UprightSizeMm) / 2.0;
            var z = (config.DepthMm - UprightSizeMm) / 2.0;

            AddProfile(model, "Robotcel staander 80x80", config.UprightProfile, uprightLength, 4,
                "Staander tussen M16-voetplaat en 40x80-bladframe", config.SawingMode);
            foreach (var px in new[] { -x, x })
            foreach (var pz in new[] { -z, z })
            {
                AddPlacement(model, "Staander 80x80", AssemblyComponentKind.Profile, UprightSizeMm, UprightSizeMm, uprightLength,
                    px, uprightBottom + uprightLength / 2.0, pz, "profile", "box");
                AddPlacement(model, "TechXXL adapterplaat / voetplaat 10 80x80 M16", AssemblyComponentKind.Purchased, 80, 80, FootPlateThicknessMm,
                    px, FootVisibleHeightMm + FootPlateThicknessMm / 2.0, pz, "hardware-adapter", "box");
                AddPlacement(model, "TechXXL stelvoet D80 schotel", AssemblyComponentKind.Purchased,
                    FootDiameterMm, FootDiameterMm, FootBaseHeightMm, px, FootBaseHeightMm / 2.0, pz, "hardware-foot", "cylinder");
                AddPlacement(model, "M16 draadeind verstelbare voet", AssemblyComponentKind.Purchased,
                    FootStemDiameterMm, FootStemDiameterMm, FootVisibleHeightMm - FootBaseHeightMm,
                    px, FootBaseHeightMm + (FootVisibleHeightMm - FootBaseHeightMm) / 2.0, pz, "hardware", "cylinder");
            }

            // Reference topology: the 40x80 top beams terminate against the inside
            // faces of the 80x80 uprights and share the same top plane. They do not sit
            // on top of the uprights.
            var frontBackLength = config.WidthMm - 2.0 * UprightSizeMm;
            var sideLength = config.DepthMm - 2.0 * UprightSizeMm;
            AddProfile(model, "Robotcel bladframe voor/achter 40x80", config.FrameBeamProfile, frontBackLength, 2,
                "40x80 rondligger onder werkblad", config.SawingMode);
            AddProfile(model, "Robotcel bladframe links/rechts 40x80", config.FrameBeamProfile, sideLength, 2,
                "40x80 rondligger onder werkblad", config.SawingMode);
            var beamCenterY = frameBottom + BeamHeightMm / 2.0;
            var frontLaneZ = ModuleLaneCenter(-z, -1, ModuleLane.OuterFlush);
            var rearLaneZ = ModuleLaneCenter(z, 1, ModuleLane.OuterFlush);
            var leftLaneX = ModuleLaneCenter(-x, -1, ModuleLane.OuterFlush);
            var rightLaneX = ModuleLaneCenter(x, 1, ModuleLane.OuterFlush);
            AddPlacement(model, "Bladligger voor 40x80", AssemblyComponentKind.Profile, frontBackLength, BeamWidthMm, BeamHeightMm, 0, beamCenterY, frontLaneZ, "profile", "box");
            AddPlacement(model, "Bladligger achter 40x80", AssemblyComponentKind.Profile, frontBackLength, BeamWidthMm, BeamHeightMm, 0, beamCenterY, rearLaneZ, "profile", "box");
            AddPlacement(model, "Bladligger links 40x80", AssemblyComponentKind.Profile, BeamWidthMm, sideLength, BeamHeightMm, leftLaneX, beamCenterY, 0, "profile", "box");
            AddPlacement(model, "Bladligger rechts 40x80", AssemblyComponentKind.Profile, BeamWidthMm, sideLength, BeamHeightMm, rightLaneX, beamCenterY, 0, "profile", "box");

            var availableWidth = config.WidthMm - 2.0 * UprightSizeMm;
            var bayCount = Math.Max(1, (int)Math.Ceiling(availableWidth / config.IntermediateBeamMaxSpacingMm));
            var crossBeamCount = Math.Max(1, bayCount - 1);
            var crossBeamLength = Math.Abs(rearLaneZ - frontLaneZ) - BeamWidthMm;
            AddProfile(model, "Robotcel dwarsligger 40x80", config.FrameBeamProfile, crossBeamLength, crossBeamCount,
                "Dwarsliggers gelijkmatig onder blad", config.SawingMode);
            for (var i = 1; i <= crossBeamCount; i++)
            {
                var px = crossBeamCount == 1 ? 0 : -availableWidth / 2.0 + availableWidth * i / (crossBeamCount + 1.0);
                AddPlacement(model, "Dwarsligger 40x80 " + i, AssemblyComponentKind.Profile, BeamWidthMm, crossBeamLength, BeamHeightMm,
                    px, beamCenterY, 0, "profile", "box");
            }

            const double lowerBeamHeightMm = 80.0;
            const double lowerBeamWidthMm = 40.0;
            const double lowerFrameClearanceMm = 40.0;
            var lowerBeamCenterY = uprightBottom + lowerFrameClearanceMm + lowerBeamHeightMm / 2.0;
            var lowerFrontLaneZ = ModuleLaneCenter(-z, -1, ModuleLane.OuterFlush);
            var lowerRearLaneZ = ModuleLaneCenter(z, 1, ModuleLane.OuterFlush);
            var lowerLeftLaneX = ModuleLaneCenter(-x, -1, ModuleLane.OuterFlush);
            var lowerRightLaneX = ModuleLaneCenter(x, 1, ModuleLane.OuterFlush);
            AddProfile(model, "Robotcel onderframe voor/achter 40x80", config.FrameBeamProfile, frontBackLength, 2,
                "40x80 onderligger staand: 80 mm verticaal, 40 mm breed en buitenvlakken gelijk aan de 80x80-staanders", config.SawingMode);
            AddProfile(model, "Robotcel onderframe links/rechts 40x80", config.FrameBeamProfile, sideLength, 2,
                "40x80 onderligger staand: 80 mm verticaal, 40 mm breed en buitenvlakken gelijk aan de 80x80-staanders", config.SawingMode);
            AddPlacement(model, "Onderligger voor 40x80", AssemblyComponentKind.Profile, frontBackLength, lowerBeamWidthMm, lowerBeamHeightMm,
                0, lowerBeamCenterY, lowerFrontLaneZ, "profile", "box");
            AddPlacement(model, "Onderligger achter 40x80", AssemblyComponentKind.Profile, frontBackLength, lowerBeamWidthMm, lowerBeamHeightMm,
                0, lowerBeamCenterY, lowerRearLaneZ, "profile", "box");
            AddPlacement(model, "Onderligger links 40x80", AssemblyComponentKind.Profile, lowerBeamWidthMm, sideLength, lowerBeamHeightMm,
                lowerLeftLaneX, lowerBeamCenterY, 0, "profile", "box");
            AddPlacement(model, "Onderligger rechts 40x80", AssemblyComponentKind.Profile, lowerBeamWidthMm, sideLength, lowerBeamHeightMm,
                lowerRightLaneX, lowerBeamCenterY, 0, "profile", "box");

            var lowerCrossBeamLength = Math.Abs(lowerRearLaneZ - lowerFrontLaneZ) - lowerBeamWidthMm;
            AddProfile(model, "Robotcel onderste dwarsligger 40x80", config.FrameBeamProfile, lowerCrossBeamLength, LowerCrossBeamCount,
                "Exact één 40x80-dwarsligger in de onderste laag; staand gemonteerd met 80 mm verticaal en 40 mm breed", config.SawingMode);
            AddPlacement(model, "Onderste dwarsligger 40x80 1", AssemblyComponentKind.Profile, lowerBeamWidthMm, lowerCrossBeamLength, lowerBeamHeightMm,
                0, lowerBeamCenterY, 0, "profile", "box");

            var worktop = SheetDrawing.CreateSheet("Robotcel werkblad", config.WorktopMaterial, config.WidthMm, config.DepthMm);
            SheetDrawing.AddSheetToModel(model, worktop, 0, config.WorktopHeightMm - worktopThickness / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var rearRailLength = config.WidthMm;
            AddProfile(model, "Robotcel achterrail 40x160", config.RearRailProfile, rearRailLength, 1,
                "40x160 rail vlak op het blad aan de achterzijde", config.SawingMode);
            AddPlacement(model, "Achterrail 40x160 vlak", AssemblyComponentKind.Profile, rearRailLength, 160, 40, 0,
                config.WorktopHeightMm + 20, config.DepthMm / 2.0 - 80, "profile", "box");

            const double visibleEndCapThicknessMm = 4.0;
            var rearRailCenterY = config.WorktopHeightMm + 20;
            var rearRailCenterZ = config.DepthMm / 2.0 - 80;
            AddPlacement(model, "Zwarte eindkap 8 160x40 achterrail links", AssemblyComponentKind.Purchased,
                visibleEndCapThicknessMm, 160, 40, -rearRailLength / 2.0 - visibleEndCapThicknessMm / 2.0,
                rearRailCenterY, rearRailCenterZ, "hardware-cap", "box");
            AddPlacement(model, "Zwarte eindkap 8 160x40 achterrail rechts", AssemblyComponentKind.Purchased,
                visibleEndCapThicknessMm, 160, 40, rearRailLength / 2.0 + visibleEndCapThicknessMm / 2.0,
                rearRailCenterY, rearRailCenterZ, "hardware-cap", "box");

            var bracketCount = 16 + crossBeamCount * 2 + LowerCrossBeamCount * 2 + 2;
            model.Hardware.Add(new HardwareItem { Name = "TechXXL verstelbare voet D80 M16x150 met schroefgaten", ArticleNumber = "TIN 101219", Quantity = 4, Unit = "st", Note = "Serie 10; centrale leverancierscatalogus", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL voetplaat 10 80x80 M16", ArticleNumber = "TIN 101445", Quantity = 4, Unit = "st", Note = "80x80x15; centraal M16; raster 40", ModelStatus = "Maatmodel opgenomen", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL hoekbeugelset 10 40x80 met bevestigingen", ArticleNumber = "TIN 101245", Quantity = bracketCount, Unit = "set", Note = "Inclusief vier bolkopschroeven en bandmoeren", ModelStatus = "Functioneel gemodelleerd", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL afdekkap 8 160x40 zwart", ArticleNumber = "TIN 100199 / S208AK16040", Quantity = RearRailEndCapCount, Unit = "st", Note = "Groef 8, I-type, 160x40x12 mm; uitsluitend combineren met de groef-8-achterrail", ModelStatus = "Maatmodel en positie opgenomen", BomStatus = "Inkoop" });
            model.DesignNotes.Add("Robotcel laagopbouw en controletelling: voetlaag 4 M16-stelvoeten + 4 voetplaten; staanderlaag 4x 80x80; onderste laag 4 staande omtrekliggers + exact 1 staande dwarsligger 40x80; bladlaag 4 staande omtrekliggers + " + crossBeamCount + " staande dwarsligger(s) 40x80; blad 1; achterrail 1x 40x160; eindafwerking 2 passende zwarte afdekkappen. Robot niet opgenomen.");
            model.DesignNotes.Add("Een staand 40x80-profiel bezet bij aansluiting op een 80x80-profiel altijd één volledige 40-mm moduulbaan: buiten/front gelijk of één moduul (40 mm) terug. Hart-op-hart centreren is niet toegestaan, omdat de T-sleufhartlijnen dan niet uitlijnen.");
            model.DesignNotes.Add("Profielen en profieltoebehoren worden productonafhankelijk via de centrale leveranciersvoorkeur geselecteerd; TechXXL heeft rang 1 zolang een passende aanbieding beschikbaar is.");
            ValidateAssemblyManifest(model, crossBeamCount,
                leftLaneX, rightLaneX, frontLaneZ, rearLaneZ,
                lowerLeftLaneX, lowerRightLaneX, lowerFrontLaneZ, lowerRearLaneZ, x, z);
            return model;
        }

        private static double ModuleLaneCenter(double uprightCenter, double outwardDirection, ModuleLane lane)
        {
            if (outwardDirection != -1 && outwardDirection != 1) throw new ArgumentOutOfRangeException("outwardDirection");
            var laneCenterDistance = (UprightSizeMm - BeamWidthMm) / 2.0;
            return uprightCenter + outwardDirection * (lane == ModuleLane.OuterFlush ? laneCenterDistance : -laneCenterDistance);
        }

        private static void ValidateAssemblyManifest(WorkbenchModel model, int topCrossBeamCount,
            double leftLaneX, double rightLaneX, double frontLaneZ, double rearLaneZ,
            double lowerLeftLaneX, double lowerRightLaneX, double lowerFrontLaneZ, double lowerRearLaneZ,
            double uprightX, double uprightZ)
        {
            RequirePlacementCount(model, "Staander 80x80", 4);
            RequirePlacementCount(model, "Bladligger ", 4);
            RequirePlacementCount(model, "Dwarsligger 40x80 ", topCrossBeamCount);
            RequirePlacementCount(model, "Onderligger ", 4);
            RequirePlacementCount(model, "Onderste dwarsligger 40x80 ", LowerCrossBeamCount);
            RequirePlacementCount(model, "Achterrail 40x160", 1);
            RequirePlacementCount(model, "Zwarte eindkap 8 160x40", RearRailEndCapCount);
            RequireStanding40x80(model, "Onderligger ");
            RequireStanding40x80(model, "Onderste dwarsligger 40x80 ");

            RequireModuleLane(leftLaneX, -uprightX, "bladligger links");
            RequireModuleLane(rightLaneX, uprightX, "bladligger rechts");
            RequireModuleLane(frontLaneZ, -uprightZ, "bladligger voor");
            RequireModuleLane(rearLaneZ, uprightZ, "bladligger achter");
            RequireModuleLane(lowerLeftLaneX, -uprightX, "onderligger links");
            RequireModuleLane(lowerRightLaneX, uprightX, "onderligger rechts");
            RequireModuleLane(lowerFrontLaneZ, -uprightZ, "onderligger voor");
            RequireModuleLane(lowerRearLaneZ, uprightZ, "onderligger achter");
        }

        private static void RequireStanding40x80(WorkbenchModel model, string namePrefix)
        {
            var placements = model.AssemblyPlacements
                .Where(p => p.PartName != null && p.PartName.StartsWith(namePrefix, StringComparison.Ordinal))
                .ToArray();
            foreach (var placement in placements)
            {
                var hasFortyMillimetreHorizontalFace = Math.Abs(placement.LengthMm - BeamWidthMm) <= 0.001 ||
                                                       Math.Abs(placement.WidthMm - BeamWidthMm) <= 0.001;
                if (Math.Abs(placement.HeightMm - BeamHeightMm) > 0.001 || !hasFortyMillimetreHorizontalFace)
                {
                    throw new InvalidOperationException("Ongeldige doorsnede-oriëntatie voor " + placement.PartName +
                        ": een staand 40x80-profiel moet Y=80 mm en de dwarse horizontale maat 40 mm hebben.");
                }
            }
        }

        private static void RequirePlacementCount(WorkbenchModel model, string namePrefix, int expected)
        {
            var actual = model.AssemblyPlacements.Count(p => p.PartName != null && p.PartName.StartsWith(namePrefix, StringComparison.Ordinal));
            if (actual != expected)
            {
                throw new InvalidOperationException("Robotcel bouwlaagcontrole mislukt voor '" + namePrefix + "': verwacht " + expected + ", gevonden " + actual + ".");
            }
        }

        private static void RequireModuleLane(double beamCenter, double uprightCenter, string role)
        {
            var centreDifference = Math.Abs(beamCenter - uprightCenter);
            var expected = (UprightSizeMm - BeamWidthMm) / 2.0;
            if (Math.Abs(centreDifference - expected) > 0.001)
            {
                throw new InvalidOperationException("Ongeldige T-slot-moduulbaan voor " + role + ": 40x80 mag op een 80x80-aansluiting niet gecentreerd worden.");
            }
        }

        private static void AddProfile(WorkbenchModel model, string name, Material material, double lengthMm, int quantity, string note, ProfileSawingMode sawingMode)
        {
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = lengthMm, Quantity = quantity, OrientationNote = note, BomStatus = "Robotcel frame" });
            model.ProfileOperations.Add(new ProfileOperation { ProfileId = material.Id + "_" + name.Replace(' ', '_'), PartName = name, Quantity = quantity, Material = material, ProfileLengthMm = lengthMm, Sequence = 1, Kind = ProfileOperationKind.SawCut, SawAngleDeg = 90, WorkOrigin = "Kop A", MachineHint = sawingMode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH", ExecutionParty = sawingMode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER", Note = note });
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind, double length, double width, double height, double x, double y, double z, string visualKind, string shape)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { Kind = kind, PartName = name, LengthMm = length, WidthMm = width, HeightMm = height, Xmm = x, Ymm = y, Zmm = z, Orientation = AssemblyOrientation.Default, VisualKind = visualKind, Shape = shape });
        }

        private static void Validate(RobotCellConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.WidthMm < 600 || config.WidthMm > 6000) throw new ArgumentException("Robotcel breedte moet tussen 600 en 6000 mm liggen.");
            if (config.DepthMm < 500 || config.DepthMm > 2000) throw new ArgumentException("Robotcel diepte moet tussen 500 en 2000 mm liggen.");
            if (config.WorktopHeightMm < 650 || config.WorktopHeightMm > 1200) throw new ArgumentException("Robotcel werkbladhoogte moet tussen 650 en 1200 mm liggen.");
            if (config.WorktopHeightMm <= FootVisibleHeightMm + FootPlateThicknessMm + BeamHeightMm + 100) throw new ArgumentException("Robotcel is te laag voor stelvoet, voetplaat, staander en bladframe.");
            if (config.UprightProfile == null || config.FrameBeamProfile == null || config.RearRailProfile == null || config.WorktopMaterial == null) throw new ArgumentException("Robotcel materiaalkeuze is onvolledig.");
        }
    }
}
