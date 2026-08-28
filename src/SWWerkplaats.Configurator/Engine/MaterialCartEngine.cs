using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class MaterialCartEngine
    {
        private const double ProfileSizeMm = 40;
        private const double CasterHeightMm = 132;
        private const double CasterDiameterMm = 100;
        private const double CasterPlateThicknessMm = 8;
        private const double HandleRiseMm = 220;

        public WorkbenchModel Build(MaterialCartConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                LowerFrameHeightMm = CasterHeightMm,
                MiddleLayerHeightMm = config.TopShelfHeightMm,
                SawingMode = config.SawingMode
            };

            var sheetThickness = Math.Max(2, config.ShelfMaterial.ThicknessMm);
            var clearWidth = config.WidthMm - 2 * ProfileSizeMm;
            var clearDepth = config.DepthMm - 2 * ProfileSizeMm;
            var topFrameCenterY = config.TopShelfHeightMm - sheetThickness - ProfileSizeMm / 2;
            var uprightBottom = CasterHeightMm + CasterPlateThicknessMm;
            var uprightTop = config.TopShelfHeightMm - sheetThickness;
            var uprightLength = uprightTop - uprightBottom;
            var x = config.WidthMm / 2 - ProfileSizeMm / 2;
            var z = config.DepthMm / 2 - ProfileSizeMm / 2;
            var crossmemberCount = clearWidth >= 920 ? 1 : 0;

            AddProfile(model, "Materiaalwagen hoekstaander 40x40", config.FrameProfile, uprightLength, 4,
                "Vier doorlopende hoekstaanders tussen wielplaat en bovenste legbordlaag", config.SawingMode);
            foreach (var px in new[] { -x, x })
            foreach (var pz in new[] { -z, z })
            {
                AddPlacement(model, "Hoekstaander 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, ProfileSizeMm, uprightLength,
                    px, uprightBottom + uprightLength / 2, pz, "profile", "box");
                AddPlacement(model, "Wielmontageplaat 40x40", AssemblyComponentKind.Purchased, ProfileSizeMm, ProfileSizeMm, CasterPlateThicknessMm,
                    px, CasterHeightMm + CasterPlateThicknessMm / 2, pz, "hardware-adapter", "box");
            }

            var bottomSurface = CasterHeightMm + CasterPlateThicknessMm + ProfileSizeMm + sheetThickness;
            for (var layer = 0; layer < config.ShelfCount; layer++)
            {
                var surfaceY = config.ShelfCount == 1
                    ? config.TopShelfHeightMm
                    : bottomSurface + (config.TopShelfHeightMm - bottomSurface) * layer / (config.ShelfCount - 1.0);
                AddShelfLayer(model, config, layer + 1, surfaceY, clearWidth, clearDepth, x, z, sheetThickness, crossmemberCount);
            }

            AddCasters(model, config.SteeringMode, x, z);
            AddHandle(model, config, x, z, topFrameCenterY);

            var cornerBracketCount = config.ShelfCount * 8;
            var crossmemberBracketCount = config.ShelfCount * crossmemberCount * 2;
            var handleBracketCount = string.Equals(config.HandleSide, "none", StringComparison.OrdinalIgnoreCase) ? 0 : 6;
            model.Hardware.Add(new HardwareItem { Name = "TechXXL hoekbeugelset 8 40x40 ZN", ArticleNumber = "TIN 100360", Quantity = cornerBracketCount + crossmemberBracketCount + handleBracketCount, Unit = "set", Note = "Complete 90°-set voor groef-8 profiel, raster 40", ModelStatus = "Functioneel gemodelleerd", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL zwenkwiel D100 dubbele rem", ArticleNumber = "TIN 101076", Quantity = 2, Unit = "st", Note = "Bouwhoogte 132 mm; geremd", ModelStatus = "Maat-envelop opgenomen", BomStatus = "Inkoop" });
            if (string.Equals(config.SteeringMode, "four-swivel", StringComparison.OrdinalIgnoreCase))
                model.Hardware.Add(new HardwareItem { Name = "TechXXL zwenkwiel D100", ArticleNumber = "TIN 101074", Quantity = 2, Unit = "st", Note = "Vrij zwenkend", ModelStatus = "Maat-envelop opgenomen", BomStatus = "Inkoop" });
            else
                model.Hardware.Add(new HardwareItem { Name = "TechXXL zwenkwiel vast D100", ArticleNumber = "TIN 101068", Quantity = 2, Unit = "st", Note = "Vaste rijrichting", ModelStatus = "Maat-envelop opgenomen", BomStatus = "Inkoop" });

            model.DesignNotes.Add("Bouwcontract materiaalwagen: 4 doorlopende 40x40-staanders; " + config.ShelfCount + " complete legbordlagen; per laag 4 omtrekliggers" + (crossmemberCount == 1 ? " + exact 1 middenligger" : " zonder middenligger") + "; 4 wielplaten en 4 D100-wielen.");
            model.DesignNotes.Add("Alle laagprofielen staan en liggen als 40x40 vierkant op één 40-mm T-slot-raster. Liggers eindigen tegen de binnenvlakken van de hoekstaanders; middenliggers eindigen tegen de binnenvlakken van voor- en achterligger.");
            model.DesignNotes.Add("Geen losse profieleindkappen: staanderkoppen zijn door het bovenblad afgedekt, staanderonderzijden door wielplaten en alle liggerkoppen door profielverbindingen.");
            ValidateContract(model, config.ShelfCount, crossmemberCount, string.Equals(config.HandleSide, "none", StringComparison.OrdinalIgnoreCase) ? 0 : 3);
            return model;
        }

        private static void AddShelfLayer(WorkbenchModel model, MaterialCartConfig config, int layer, double surfaceY,
            double clearWidth, double clearDepth, double x, double z, double sheetThickness, int crossmemberCount)
        {
            var beamY = surfaceY - sheetThickness - ProfileSizeMm / 2;
            AddProfile(model, "Legbordlaag " + layer + " voor/achter 40x40", config.FrameProfile, clearWidth, 2, "Omtrekligger tussen hoekstaanders", config.SawingMode);
            AddProfile(model, "Legbordlaag " + layer + " links/rechts 40x40", config.FrameProfile, clearDepth, 2, "Omtrekligger tussen hoekstaanders", config.SawingMode);
            AddPlacement(model, "Legbordlaag " + layer + " voorligger 40x40", AssemblyComponentKind.Profile, clearWidth, ProfileSizeMm, ProfileSizeMm, 0, beamY, -z, "profile", "box");
            AddPlacement(model, "Legbordlaag " + layer + " achterligger 40x40", AssemblyComponentKind.Profile, clearWidth, ProfileSizeMm, ProfileSizeMm, 0, beamY, z, "profile", "box");
            AddPlacement(model, "Legbordlaag " + layer + " linker ligger 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, clearDepth, ProfileSizeMm, -x, beamY, 0, "profile", "box");
            AddPlacement(model, "Legbordlaag " + layer + " rechter ligger 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, clearDepth, ProfileSizeMm, x, beamY, 0, "profile", "box");
            if (crossmemberCount == 1)
            {
                AddProfile(model, "Legbordlaag " + layer + " middenligger 40x40", config.FrameProfile, clearDepth, 1, "Automatische middensteun bij wagenbreedte vanaf 1000 mm", config.SawingMode);
                AddPlacement(model, "Legbordlaag " + layer + " middenligger 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, clearDepth, ProfileSizeMm, 0, beamY, 0, "profile", "box");
            }
            var shelf = SheetDrawing.CreateSheet("Legbord " + layer, config.ShelfMaterial, clearWidth, clearDepth);
            SheetDrawing.AddSheetToModel(model, shelf, 0, surfaceY - sheetThickness / 2, 0, AssemblyOrientation.SheetHorizontal);
        }

        private static void AddCasters(WorkbenchModel model, string steeringMode, double x, double z)
        {
            foreach (var px in new[] { -x, x })
            foreach (var pz in new[] { -z, z })
            {
                var isBrake = pz > 0;
                var fixedWheel = !string.Equals(steeringMode, "four-swivel", StringComparison.OrdinalIgnoreCase) && pz < 0;
                var label = isBrake ? "Zwenkwiel D100 dubbele rem" : fixedWheel ? "Vast wiel D100" : "Zwenkwiel D100";
                AddPlacement(model, label, AssemblyComponentKind.Purchased, CasterDiameterMm, 44, CasterHeightMm,
                    px, CasterHeightMm / 2, pz, "hardware-wheel", "swivel-caster");
            }
        }

        private static void AddHandle(WorkbenchModel model, MaterialCartConfig config, double x, double z, double topFrameCenterY)
        {
            if (string.Equals(config.HandleSide, "none", StringComparison.OrdinalIgnoreCase)) return;
            var px = string.Equals(config.HandleSide, "left", StringComparison.OrdinalIgnoreCase) ? -x : x;
            AddProfile(model, "Duwbeugel staander 40x40", config.FrameProfile, HandleRiseMm, 2, "Opbouw op gekozen smalle zijde", config.SawingMode);
            AddProfile(model, "Duwbeugel greep 40x40", config.FrameProfile, config.DepthMm - 2 * ProfileSizeMm, 1, "Horizontale greep tussen beugelstaanders", config.SawingMode);
            foreach (var pz in new[] { -z, z })
                AddPlacement(model, "Duwbeugel staander 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, ProfileSizeMm, HandleRiseMm, px, topFrameCenterY + ProfileSizeMm / 2 + HandleRiseMm / 2, pz, "profile", "box");
            AddPlacement(model, "Duwbeugel greep 40x40", AssemblyComponentKind.Profile, ProfileSizeMm, config.DepthMm - 2 * ProfileSizeMm, ProfileSizeMm, px, topFrameCenterY + ProfileSizeMm + HandleRiseMm, 0, "profile", "box");
        }

        private static void ValidateContract(WorkbenchModel model, int shelfCount, int crossmemberCount, int handleProfileCount)
        {
            RequireCount(model, "Hoekstaander 40x40", 4);
            RequireCount(model, "Legbordlaag ", shelfCount * (4 + crossmemberCount));
            RequireCount(model, "Legbord ", shelfCount);
            RequireCount(model, "Wielmontageplaat 40x40", 4);
            RequireCount(model, "Zwenkwiel D100", 4, true);
            var actualHandle = model.AssemblyPlacements.Count(p => p.PartName != null && p.PartName.StartsWith("Duwbeugel ", StringComparison.Ordinal));
            if (actualHandle != handleProfileCount) throw new InvalidOperationException("Materiaalwagen duwbeugelcontract: verwacht " + handleProfileCount + ", gevonden " + actualHandle + ".");
        }

        private static void RequireCount(WorkbenchModel model, string prefix, int expected, bool wheels = false)
        {
            var actual = model.AssemblyPlacements.Count(p => p.PartName != null && (wheels ? p.VisualKind == "hardware-wheel" : p.PartName.StartsWith(prefix, StringComparison.Ordinal)));
            if (actual != expected) throw new InvalidOperationException("Materiaalwagen bouwcontract voor '" + prefix + "': verwacht " + expected + ", gevonden " + actual + ".");
        }

        private static void AddProfile(WorkbenchModel model, string name, Material material, double lengthMm, int quantity, string note, ProfileSawingMode sawingMode)
        {
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = lengthMm, Quantity = quantity, OrientationNote = note, BomStatus = "Materiaalwagen frame" });
            model.ProfileOperations.Add(new ProfileOperation { ProfileId = material.Id + "_" + name.Replace(' ', '_'), PartName = name, Quantity = quantity, Material = material, ProfileLengthMm = lengthMm, Sequence = 1, Kind = ProfileOperationKind.SawCut, SawAngleDeg = 90, WorkOrigin = "Kop A", MachineHint = sawingMode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH", ExecutionParty = sawingMode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER", Note = note });
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind, double length, double width, double height, double x, double y, double z, string visualKind, string shape)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { Kind = kind, PartName = name, LengthMm = length, WidthMm = width, HeightMm = height, Xmm = x, Ymm = y, Zmm = z, Orientation = AssemblyOrientation.Default, VisualKind = visualKind, Shape = shape });
        }

        private static void Validate(MaterialCartConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.WidthMm < 600 || config.WidthMm > 1800) throw new ArgumentException("Wagenbreedte moet tussen 600 en 1800 mm liggen.");
            if (config.DepthMm < 450 || config.DepthMm > 1000) throw new ArgumentException("Wagendiepte moet tussen 450 en 1000 mm liggen.");
            if (config.TopShelfHeightMm < 700 || config.TopShelfHeightMm > 1200) throw new ArgumentException("Hoogte van het bovenblad moet tussen 700 en 1200 mm liggen.");
            if (config.ShelfCount < 2 || config.ShelfCount > 4) throw new ArgumentException("Kies 2, 3 of 4 legbordlagen.");
            if (config.FrameProfile == null || config.ShelfMaterial == null) throw new ArgumentException("Profiel- en legbordmateriaal zijn verplicht.");
        }
    }
}
