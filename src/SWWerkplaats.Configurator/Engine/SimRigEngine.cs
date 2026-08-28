using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class SimRigEngine
    {
        private const double ModuleMm = 40;
        private const double ProfileWideMm = 80;
        private const double PlateThicknessMm = 10;

        public WorkbenchModel Build(SimRigConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                LowerFrameHeightMm = ModuleMm,
                MiddleLayerHeightMm = config.SteeringBridgeHeightMm,
                SawingMode = config.SawingMode
            };

            var railX = config.OutsideWidthMm / 2.0 - ProfileWideMm / 2.0;
            var innerSpan = config.OutsideWidthMm - 2.0 * ProfileWideMm;
            var uprightZ = -config.LengthMm / 2.0 + config.SteeringBridgePositionMm;
            var uprightLength = config.SteeringBridgeHeightMm - ModuleMm;

            AddProfile(model, "Basislangsligger 40x80 vlak", config.Profile4080, config.LengthMm, 2,
                "As Z; doorsnede X=80, Y=40; buitenvlakken bepalen totale breedte", config.SawingMode);
            foreach (var x in new[] { -railX, railX })
                AddPlacement(model, "Basislangsligger 40x80 vlak", AssemblyComponentKind.Profile,
                    ProfileWideMm, config.LengthMm, ModuleMm, x, ModuleMm / 2.0, 0, "profile", "box");

            var crossZ = new[] { -config.LengthMm / 2.0 + ModuleMm, uprightZ + 250, config.LengthMm / 2.0 - ModuleMm };
            AddProfile(model, "Basisdwarsligger 40x80 vlak", config.Profile4080, innerSpan, crossZ.Length,
                "As X; doorsnede Y=40, Z=80; eindigt tegen binnenvlakken basislangsliggers", config.SawingMode);
            for (var i = 0; i < crossZ.Length; i++)
                AddPlacement(model, "Basisdwarsligger 40x80 vlak " + (i + 1), AssemblyComponentKind.Profile,
                    innerSpan, ProfileWideMm, ModuleMm, 0, ModuleMm / 2.0, crossZ[i], "profile", "box");

            AddProfile(model, "Stuurstaander 40x80", config.Profile4080, uprightLength, 2,
                "As Y; doorsnede X=40, Z=80; buitenvlak gelijk aan hartbaan basislangsligger", config.SawingMode);
            foreach (var x in new[] { -railX, railX })
                AddPlacement(model, "Stuurstaander 40x80", AssemblyComponentKind.Profile,
                    ModuleMm, ProfileWideMm, uprightLength, x, ModuleMm + uprightLength / 2.0, uprightZ, "profile", "box");

            AddProfile(model, "Stuurbrug 40x80 staand", config.Profile4080, innerSpan + ModuleMm, 1,
                "As X; doorsnede Y=80, Z=40; montage tussen vereenvoudigde zijplaten", config.SawingMode);
            AddPlacement(model, "Stuurbrug 40x80 staand", AssemblyComponentKind.Profile,
                innerSpan + ModuleMm, ModuleMm, ProfileWideMm, 0, config.SteeringBridgeHeightMm - ProfileWideMm / 2.0, uprightZ, "profile", "box");

            AddSteeringSidePlates(model, config, railX, uprightZ);
            AddUprightBasePlates(model, config, railX, uprightZ);
            AddPedalDeck(model, config, innerSpan, railX);
            AddEndCaps(model, config, railX);

            model.Hardware.Add(new HardwareItem { Name = "M8 profielbout + groefmoer set", ArticleNumber = "M8_PROFILE_SET", Quantity = 46, Unit = "set", Note = "Frame, T-platen en pedaalverstelling", ModelStatus = "Functioneel gemodelleerd", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "M6x20 10.9 wielbasis montagebout", ArticleNumber = "M6X20_10.9", Quantity = string.Equals(config.WheelMountPattern, "blank", StringComparison.OrdinalIgnoreCase) ? 0 : 4, Unit = "st", Note = "10 mm plaat + maximaal 10 mm inschroefdiepte CSL DD", ModelStatus = "Gatpatroon gemodelleerd", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "Zwarte eindkap 40x80 groef 8", ArticleNumber = "CAP_4080_G8", Quantity = 6, Unit = "st", Note = "Vier basisuiteinden en twee bovenzijden stuurstaanders", ModelStatus = "Posities gemodelleerd", BomStatus = "Inkoop" });

            model.DesignNotes.Add("Referentiehypothese R1: 2 vlakke 40x80-basislangsliggers, 3 vlakke dwarsliggers, 2 verticale 40x80-stuurstaanders, 1 staande 40x80-stuurbrug en 3 vlakke 40x80-pedaalprofielen.");
            model.DesignNotes.Add("Custom platen zijn functioneel vereenvoudigd: twee stuurzijplaten met profielslots en CSL-DD M6-interface, twee T-platen aan de staandervoet en twee pedaalhoekplaten met draaipunt plus gebogen functie benaderd door een recht capsuleslot.");
            model.DesignNotes.Add("Stuurwielbasis, pedalen en stoel zijn klantapparatuur en vallen buiten de BOM. De officiële referentiemaat is 1350x680x660 mm; varianten blijven binnen het portalcontract.");
            ValidateContract(model, config);
            return model;
        }

        private static void AddSteeringSidePlates(WorkbenchModel model, SimRigConfig config, double railX, double uprightZ)
        {
            var plate = CustomPlate("Custom stuurzijplaat CSL DD vereenvoudigd", config.AdapterPlateMaterial, 150, 150, 2);
            plate.CustomContour.Add(new SheetContourPoint(0, 0));
            plate.CustomContour.Add(new SheetContourPoint(150, 0));
            plate.CustomContour.Add(new SheetContourPoint(150, 70));
            plate.CustomContour.Add(new SheetContourPoint(92, 150));
            plate.CustomContour.Add(new SheetContourPoint(0, 150));
            AddCapsule(plate, "Hoogte-instelling profiel", 18, 25, 14, 70);
            AddCapsule(plate, "Hoek-instelling stuurbrug", 112, 74, 14, 55);
            if (!string.Equals(config.WheelMountPattern, "blank", StringComparison.OrdinalIgnoreCase))
            {
                AddHole(plate, "CSL DD zijmontage M6 boven", 72, 110, 6.5);
                AddHole(plate, "CSL DD zijmontage M6 onder", 72, 40, 6.5);
            }
            model.Sheets.Add(plate);
            foreach (var side in new[] { -1.0, 1.0 })
                AddPlacement(model, plate.Name, AssemblyComponentKind.Sheet, plate.LengthMm, plate.WidthMm, PlateThicknessMm,
                    side * (railX - ModuleMm / 2.0 - PlateThicknessMm / 2.0), config.SteeringBridgeHeightMm - 75, uprightZ, "sheet", "box", AssemblyOrientation.SheetVerticalZ);
        }

        private static void AddUprightBasePlates(WorkbenchModel model, SimRigConfig config, double railX, double uprightZ)
        {
            var plate = CustomPlate("Custom T-verbindingsplaat staandervoet vereenvoudigd", config.AdapterPlateMaterial, 180, 150, 2);
            plate.CustomContour.Add(new SheetContourPoint(0, 0));
            plate.CustomContour.Add(new SheetContourPoint(180, 0));
            plate.CustomContour.Add(new SheetContourPoint(180, 55));
            plate.CustomContour.Add(new SheetContourPoint(110, 55));
            plate.CustomContour.Add(new SheetContourPoint(110, 150));
            plate.CustomContour.Add(new SheetContourPoint(70, 150));
            plate.CustomContour.Add(new SheetContourPoint(70, 55));
            plate.CustomContour.Add(new SheetContourPoint(0, 55));
            AddCapsule(plate, "Basisrail bevestiging links", 24, 20, 45, 14);
            AddCapsule(plate, "Basisrail bevestiging rechts", 111, 20, 45, 14);
            AddCapsule(plate, "Staander bevestiging", 83, 68, 14, 60);
            model.Sheets.Add(plate);
            foreach (var side in new[] { -1.0, 1.0 })
                AddPlacement(model, plate.Name, AssemblyComponentKind.Sheet, plate.LengthMm, plate.WidthMm, PlateThicknessMm,
                    side * (railX + ModuleMm / 2.0 + PlateThicknessMm / 2.0), 75, uprightZ, "sheet", "box", AssemblyOrientation.SheetVerticalZ);
        }

        private static void AddPedalDeck(WorkbenchModel model, SimRigConfig config, double innerSpan, double railX)
        {
            var pedalZ = -config.LengthMm / 2.0 + config.PedalDeckPositionMm;
            var angleRad = config.PedalAngleDeg * Math.PI / 180.0;
            var deckLength = 3 * ProfileWideMm;
            var deckCenterY = 115 + Math.Sin(angleRad) * deckLength / 2.0;
            AddProfile(model, "Pedaalplatform profiel 40x80 vlak", config.Profile4080, innerSpan, 3,
                "As X; drie fysieke profielen; gezamenlijk platform met instelbare hoek", config.SawingMode);
            for (var i = -1; i <= 1; i++)
            {
                var dz = i * ProfileWideMm * Math.Cos(angleRad);
                var dy = i * ProfileWideMm * Math.Sin(angleRad);
                AddPlacement(model, "Pedaalplatform profiel 40x80 vlak " + (i + 2), AssemblyComponentKind.Profile,
                    innerSpan, ProfileWideMm, ModuleMm, 0, deckCenterY + dy, pedalZ + dz, "profile", "sim-rig-pedal-profile", AssemblyOrientation.Default, config.PedalAngleDeg);
            }

            var plate = CustomPlate("Custom pedaalhoekplaat vereenvoudigd", config.AdapterPlateMaterial, 300, 170, 2);
            plate.CustomContour.Add(new SheetContourPoint(0, 0));
            plate.CustomContour.Add(new SheetContourPoint(300, 0));
            plate.CustomContour.Add(new SheetContourPoint(285, 90));
            plate.CustomContour.Add(new SheetContourPoint(90, 170));
            plate.CustomContour.Add(new SheetContourPoint(0, 55));
            AddHole(plate, "Draaipunt pedaalplaat M8", 35, 35, 8.5);
            AddCapsule(plate, "Pedaalhoek instelling", 72, 92, 150, 14);
            AddHole(plate, "Pedaalplatform achter M8", 250, 70, 8.5);
            model.Sheets.Add(plate);
            foreach (var side in new[] { -1.0, 1.0 })
                AddPlacement(model, plate.Name, AssemblyComponentKind.Sheet, plate.LengthMm, plate.WidthMm, PlateThicknessMm,
                    side * (railX - ProfileWideMm / 2.0 - PlateThicknessMm / 2.0), 100, pedalZ, "sheet", "box", AssemblyOrientation.SheetVerticalZ);
        }

        private static void AddEndCaps(WorkbenchModel model, SimRigConfig config, double railX)
        {
            var z = config.LengthMm / 2.0;
            foreach (var x in new[] { -railX, railX })
            foreach (var end in new[] { -1.0, 1.0 })
                AddPlacement(model, "Zwarte eindkap 40x80 basis", AssemblyComponentKind.Purchased, ProfileWideMm, 3, ModuleMm, x, ModuleMm / 2.0, end * (z + 1.5), "hardware-cap", "box");
            foreach (var x in new[] { -railX, railX })
                AddPlacement(model, "Zwarte eindkap 40x80 staander", AssemblyComponentKind.Purchased, ModuleMm, ProfileWideMm, 3, x, config.SteeringBridgeHeightMm + 1.5, -config.LengthMm / 2.0 + config.SteeringBridgePositionMm, "hardware-cap", "box");
        }

        private static SheetPart CustomPlate(string name, Material material, double length, double width, int quantity)
        {
            return new SheetPart { Name = name, Material = material, LengthMm = length, WidthMm = width, Quantity = quantity, UseTabs = true, CustomContourCornerRadiusMm = 6, BomStatus = "Custom CNC-plaat" };
        }

        private static void AddHole(SheetPart plate, string name, double x, double y, double diameter)
        {
            plate.Holes.Add(new SheetHole { Name = name, Xmm = x, Ymm = y, DiameterMm = diameter, DepthMm = PlateThicknessMm, Face = OperationFace.CenterPlane, DepthMode = OperationDepthMode.Through, SupportKind = SheetHoleSupportKind.ProfileNut });
        }

        private static void AddCapsule(SheetPart plate, string name, double x, double y, double length, double width)
        {
            plate.Pockets.Add(new SheetPocket { Name = name, Shape = "capsule", Xmm = x, Ymm = y, LengthMm = length, WidthMm = width, DepthMm = PlateThicknessMm, Face = OperationFace.CenterPlane, DepthMode = OperationDepthMode.Through, Note = "Functionele verstelruimte; decoratieve uitsparingen bewust weggelaten." });
        }

        private static void AddProfile(WorkbenchModel model, string name, Material material, double lengthMm, int quantity, string note, ProfileSawingMode sawingMode)
        {
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = lengthMm, Quantity = quantity, OrientationNote = note, BomStatus = "Sim-rig frame" });
            model.ProfileOperations.Add(new ProfileOperation { ProfileId = material.Id + "_" + name.Replace(' ', '_'), PartName = name, Quantity = quantity, Material = material, ProfileLengthMm = lengthMm, Sequence = 1, Kind = ProfileOperationKind.SawCut, SawAngleDeg = 90, WorkOrigin = "Kop A", MachineHint = sawingMode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH", ExecutionParty = sawingMode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER", Note = note });
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind, double length, double width, double height, double x, double y, double z, string visualKind, string shape, AssemblyOrientation orientation = AssemblyOrientation.Default, double rotationXDeg = 0)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { Kind = kind, PartName = name, LengthMm = length, WidthMm = width, HeightMm = height, Xmm = x, Ymm = y, Zmm = z, Orientation = orientation, VisualKind = visualKind, Shape = shape, RotationXDeg = rotationXDeg });
        }

        private static void ValidateContract(WorkbenchModel model, SimRigConfig config)
        {
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Basislangsligger", StringComparison.Ordinal)) == 2, "verwacht exact 2 basislangsliggers");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Basisdwarsligger", StringComparison.Ordinal)) == 3, "verwacht exact 3 basisdwarsliggers");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Stuurstaander", StringComparison.Ordinal)) == 2, "verwacht exact 2 stuurstaanders");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Stuurbrug", StringComparison.Ordinal)) == 1, "verwacht exact 1 stuurbrug");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Pedaalplatform profiel", StringComparison.Ordinal)) == 3, "verwacht exact 3 pedaalprofielen");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Custom ", StringComparison.Ordinal)) == 6, "verwacht exact 6 custom adapterplaten");
            Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Zwarte eindkap", StringComparison.Ordinal)) == 6, "verwacht exact 6 eindkappen");
            Require(model.Sheets.Where(s => s.Name.StartsWith("Custom ", StringComparison.Ordinal)).All(s => s.CustomContour.Count >= 5), "iedere custom plaat heeft een produceerbare vereenvoudigde contour");
            Require(model.Sheets.Where(s => s.Name.StartsWith("Custom ", StringComparison.Ordinal)).All(s => s.Holes.Count + s.Pockets.Count >= 2), "iedere custom plaat heeft een functioneel gaten- of sleuvenpatroon");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Sim-rig bouwcontract: " + message + ".");
        }

        private static void Validate(SimRigConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.OutsideWidthMm < 600 || config.OutsideWidthMm > 800) throw new ArgumentException("Buitenbreedte sim-rig moet tussen 600 en 800 mm liggen.");
            if (config.LengthMm < 1200 || config.LengthMm > 1800) throw new ArgumentException("Lengte sim-rig moet tussen 1200 en 1800 mm liggen.");
            if (config.SteeringBridgeHeightMm < 550 || config.SteeringBridgeHeightMm > 850) throw new ArgumentException("Hoogte stuurbrug moet tussen 550 en 850 mm liggen.");
            if (config.SteeringBridgePositionMm < 350 || config.SteeringBridgePositionMm > config.LengthMm - 350) throw new ArgumentException("Positie stuurbrug valt buiten het bruikbare frame.");
            if (config.PedalDeckPositionMm < 180 || config.PedalDeckPositionMm > config.SteeringBridgePositionMm - 180) throw new ArgumentException("Positie pedaalplatform moet vóór de stuurbrug liggen.");
            if (config.PedalAngleDeg < 0 || config.PedalAngleDeg > 25) throw new ArgumentException("Pedaalhoek moet tussen 0 en 25 graden liggen.");
            if (config.Profile4080 == null || config.AdapterPlateMaterial == null) throw new ArgumentException("Profiel- en plaatmateriaal zijn verplicht.");
        }
    }
}
