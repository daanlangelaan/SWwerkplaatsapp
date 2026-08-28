using System;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class HeightAdjustableWorkbenchEngine
    {
        public WorkbenchModel Build(HeightAdjustableWorkbenchConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel { ProjectName = config.ProjectName, SawingMode = config.SawingMode };
            foreach (var note in config.DesignNotes) model.DesignNotes.Add(note);
            AddProfiles(model, config);
            AddSheets(model, config);
            AddWorktopBrackets(model, config);
            AddLiftColumns(model, config);
            AddAdjustableFeet(model, config);
            AddEndCaps(model, config);
            BuildProfileOperations(model, config.SawingMode);
            AddHardware(model, config);
            AddFastenerCalculation(model, config);
            model.StructuralCalculation = new StructuralCalculationService().Calculate(
                "hoogteverstelbare_werktafel", config.TopFrameProfile.Id, config.WidthMm, 2);
            return model;
        }

        private static void AddWorktopBrackets(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var supports = model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile
                && (string.Equals(item.PartName, "Vast bovenframe links", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.PartName, "Vast bovenframe rechts", StringComparison.OrdinalIgnoreCase)
                    || item.PartName.StartsWith("Onderstelaansluiting bovenframe", StringComparison.OrdinalIgnoreCase))).ToArray();
            new WorktopBracketPlacementService().AddSymmetricPairs(
                model, "hoogteverstelbare_werktafel", supports, WorktopSupportAxis.Z,
                config.HeightMm - config.TopSheet.ThicknessMm, "Werkbladbeugel vast bovenframe");
        }

        private static void AddProfiles(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var footLength = FootProfileLength(config);
            AddProfile(model, "Voetprofiel", config.FootProfile, footLength, 2,
                "80x80 mm; as Z onder de twee HTE2-kolommen; koppen volledig afgedekt door de hoekadapters.");

            var frameHeight = TopFrameHeight(config);
            var frameThickness = TopFrameThickness(config);
            var innerDepth = config.DepthMm - 2.0 * frameThickness;
            AddProfile(model, "Vast bovenframe voor/achter", config.TopFrameProfile, config.WidthMm, 2,
                SectionLabel(config) + "; as X; Y=" + frameHeight.ToString("0.##") + " mm, Z=" + frameThickness.ToString("0.##") + " mm; 80-mm zijde bij 40x80 verticaal.");
            AddProfile(model, "Vast bovenframe links/rechts", config.TopFrameProfile, innerDepth, 2,
                SectionLabel(config) + "; as Z tussen voor- en achterligger; Y=" + frameHeight.ToString("0.##") + " mm, X=" + frameThickness.ToString("0.##") + " mm.");
            AddProfile(model, "Onderstelaansluiting bovenframe", config.TopFrameProfile, innerDepth, 2,
                "Precies twee profielen; as Z boven de HTE2-kolomharten; vlakcontact met de bovenplaten en met voor-/achterligger.");

            var footX = config.ColumnCenterDistanceMm / 2.0;
            AddPlacement(model, "Voetprofiel links", AssemblyComponentKind.Profile, 80, 80, footLength, -footX, 40, 0, "profile");
            AddPlacement(model, "Voetprofiel rechts", AssemblyComponentKind.Profile, 80, 80, footLength, footX, 40, 0, "profile");

            var frameY = config.HeightMm - config.TopSheet.ThicknessMm - frameHeight / 2.0;
            var edgeZ = (config.DepthMm - frameThickness) / 2.0;
            var sideX = (config.WidthMm - frameThickness) / 2.0;
            AddPlacement(model, "Vast bovenframe voor", AssemblyComponentKind.Profile, config.WidthMm, frameHeight, frameThickness, 0, frameY, -edgeZ, "profile");
            AddPlacement(model, "Vast bovenframe achter", AssemblyComponentKind.Profile, config.WidthMm, frameHeight, frameThickness, 0, frameY, edgeZ, "profile");
            AddPlacement(model, "Vast bovenframe links", AssemblyComponentKind.Profile, frameThickness, frameHeight, innerDepth, -sideX, frameY, 0, "profile");
            AddPlacement(model, "Vast bovenframe rechts", AssemblyComponentKind.Profile, frameThickness, frameHeight, innerDepth, sideX, frameY, 0, "profile");
            AddPlacement(model, "Onderstelaansluiting bovenframe links", AssemblyComponentKind.Profile, frameThickness, frameHeight, innerDepth, -footX, frameY, 0, "profile");
            AddPlacement(model, "Onderstelaansluiting bovenframe rechts", AssemblyComponentKind.Profile, frameThickness, frameHeight, innerDepth, footX, frameY, 0, "profile");
        }

        private static void AddSheets(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var top = new SheetPart
            {
                Name = "Vast werkblad",
                Material = config.TopSheet,
                LengthMm = config.WidthMm,
                WidthMm = config.DepthMm,
                Quantity = 1,
                CenterHeightMm = config.HeightMm - config.TopSheet.ThicknessMm / 2.0,
                BomStatus = "Vast blad zonder kogelpotgaten; via automatisch verdeelde TIN 100391-beugels aan het bovenframe."
            };
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
                CenterHeightMm = 80 + EffectiveColumnLength(config) / 2.0,
                BomStatus = "Zelfde vaste stabilisatieplaat als het Workstation-onderstel; bevestigingswijze blijft open."
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
        }

        private static void AddLiftColumns(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var column = config.LiftColumn;
            var installationLength = EffectiveColumnLength(config);
            var bodyLength = installationLength - 2.0 * column.EndPlateThicknessMm;
            var xOffset = config.ColumnCenterDistanceMm / 2.0;
            var frameBottom = config.HeightMm - config.TopSheet.ThicknessMm - TopFrameHeight(config);
            foreach (var x in new[] { -xOffset, xOffset })
            {
                AddPlacement(model, "HTE2 kolom", AssemblyComponentKind.Purchased, column.BodyDepthMm, bodyLength, column.BodyWidthMm,
                    x, 80 + column.EndPlateThicknessMm + bodyLength / 2.0, 0, "hardware");
                AddPlacement(model, "HTE2 O1 onderplaat 280x65", AssemblyComponentKind.Purchased, column.EndPlateWidthMm, column.EndPlateThicknessMm, column.EndPlateLengthMm,
                    x, 80 + column.EndPlateThicknessMm / 2.0, 0, "hardware");
                AddPlacement(model, "HTE2 O1 bovenplaat 280x65", AssemblyComponentKind.Purchased, column.EndPlateWidthMm, column.EndPlateThicknessMm, column.EndPlateLengthMm,
                    x, frameBottom - column.EndPlateThicknessMm / 2.0, 0, "hardware");
            }
        }

        private static void AddAdjustableFeet(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var adapter = config.LevelingFootCornerAdapter;
            var foot = config.LevelingFoot;
            var footX = config.ColumnCenterDistanceMm / 2.0;
            var profileHalfLength = FootProfileLength(config) / 2.0;
            foreach (var x in new[] { -footX, footX })
            foreach (var zDirection in new[] { -1.0, 1.0 })
            {
                var label = (x < 0 ? "links " : "rechts ") + (zDirection < 0 ? "voor" : "achter");
                var adapterCenterZ = zDirection * (profileHalfLength + adapter.ReachMm / 2.0);
                var footZ = zDirection * (profileHalfLength + adapter.FootAxisFromMountingFaceMm);
                AddPlacementWithShape(model, "Stelpoot hoekadapter ZI-1744 " + label, AssemblyComponentKind.Purchased,
                    adapter.WidthMm, adapter.MountingPlateHeightMm, adapter.ReachMm, x, adapter.MountingPlateHeightMm / 2.0, adapterCenterZ, "hardware-adapter", "leveling-foot-adapter");
                AddPlacementWithShape(model, "Stelvoet D80 schotel ZI-1415-S", AssemblyComponentKind.Purchased,
                    foot.ActualFootDiameterMm, foot.FootHeightMm, foot.ActualFootDiameterMm, x, foot.FootHeightMm / 2.0, footZ, "hardware", "cylinder");
                AddPlacementWithShape(model, "Stelvoet D80 zwenkkraag ZI-1415-S", AssemblyComponentKind.Purchased,
                    foot.NutAcrossFlatsMm, 14, foot.NutAcrossFlatsMm, x, foot.FootHeightMm + 7, footZ, "hardware", "cylinder");
                var threadBottom = foot.OverallHeightMm - foot.ThreadLengthMm;
                var visibleLength = Math.Max(0, adapter.MountingPlateHeightMm - threadBottom);
                AddPlacementWithShape(model, "Stelvoet M16x130 draadeind ZI-1415-S", AssemblyComponentKind.Purchased,
                    foot.ThreadDiameterMm, visibleLength, foot.ThreadDiameterMm, x, threadBottom + visibleLength / 2.0, footZ, "hardware", "cylinder");
                AddPlacementWithShape(model, "Stelvoet M16 stelmoer ZI-1415-S", AssemblyComponentKind.Purchased,
                    foot.NutAcrossFlatsMm, foot.NutHeightMm, foot.NutAcrossFlatsMm, x,
                    adapter.MountingPlateHeightMm - adapter.SupportArmThicknessMm - foot.NutHeightMm / 2.0, footZ, "hardware", "cylinder");
            }
        }

        private static void AddEndCaps(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            const double capThickness = 4;
            var frameHeight = TopFrameHeight(config);
            var frameThickness = TopFrameThickness(config);
            var frameY = config.HeightMm - config.TopSheet.ThicknessMm - frameHeight / 2.0;
            var edgeZ = (config.DepthMm - frameThickness) / 2.0;
            var capName = frameHeight > 40 ? "Afdekkap 8 80x40 zwart" : "Afdekkap 8 40x40 zwart";
            foreach (var z in new[] { -edgeZ, edgeZ })
            foreach (var direction in new[] { -1.0, 1.0 })
                AddPlacement(model, capName + " - vast bovenframe", AssemblyComponentKind.Purchased,
                    capThickness, frameHeight, frameThickness, direction * (config.WidthMm / 2.0 + capThickness / 2.0), frameY, z, "hardware");
        }

        private static void AddHardware(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var feet = CountPlacements(model, "Stelvoet D80 schotel ZI-1415-S");
            var adapters = CountPlacements(model, "Stelpoot hoekadapter ZI-1744");
            model.Hardware.Add(new HardwareItem { Name = "GeMinG HTE2 complete 2-koloms hefset O1, slag 400 mm", ArticleNumber = config.LiftColumn.Id + "_2COL_SET", Quantity = 1, Unit = "set", Note = "Zelfde elektrische tweekoloms-hefset als Workstation.", ModelStatus = "Kolommen en eindplaten in 3D-model", BomStatus = "Generieke leveranciersprijs voor alle toepassingen" });
            model.Hardware.Add(new HardwareItem { Name = "Maunsystem stelvoet D80 M16x130 zwart", ArticleNumber = config.LevelingFoot.ArticleNumber, Quantity = feet, Unit = "st", Note = "Zelfde voetketen als Workstation.", ModelStatus = "In 3D-model", BomStatus = "Actueel leveranciersdeel" });
            model.Hardware.Add(new HardwareItem { Name = "Maunsystem Stellfusssockel 8 D80 hoekadapter M16", ArticleNumber = config.LevelingFootCornerAdapter.ArticleNumber, Quantity = adapters, Unit = "st", Note = "Hoekadapter sluit iedere voetprofielkop volledig af.", ModelStatus = "In 3D-model", BomStatus = "Actueel leveranciersdeel" });
            model.Hardware.Add(new HardwareItem { Name = "Bevestigingsset Nut 8 voor ZI-1744 hoekadapter", ArticleNumber = "MAUNSYSTEM_NUT8_M8X22_SET", Quantity = adapters * 4, Unit = "st", Note = "Vier M8-posities per adapter; zelfde keten als Workstation.", ModelStatus = "Niet als losse bodies", BomStatus = "Aantal uit model" });
            model.Hardware.Add(new HardwareItem { Name = "HTE2 eindplaat M8 bout met inschuifmoer", ArticleNumber = "LEX_HTE2_ENDPLATE_M8_TNUT", Quantity = config.LiftColumn.Quantity * 2 * 2, Unit = "st", Note = "Twee verbindingen per onder- en bovenplaat per kolom.", ModelStatus = "Verbinding uit model", BomStatus = "Aantal uit HTE2-interface" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL standaardverbinder 8 40", ArticleNumber = "TIN 100342 / S208ZP", Quantity = 0, Unit = "st", Note = "Aantal wordt uit fysieke profielkop-op-sleufcontacten afgeleid.", ModelStatus = "Verbindingen uit model", BomStatus = "Actueel leveranciersartikel" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL bolkop-inbusbout ISO 7380 M8x25", ArticleNumber = "TIN 100673 / S208HS825", Quantity = 0, Unit = "st", Note = "Eén bout per standaardverbinder; aantal wordt gesynchroniseerd.", ModelStatus = "Niet als losse bodies", BomStatus = "Actueel leveranciersartikel" });
            var bracketCount = CountPlacements(model, "Werkbladbeugel vast bovenframe");
            model.Hardware.Add(new HardwareItem { Name = "TechXXL montagebeugel 40×40×20 ZN", ArticleNumber = WorktopBracketPlacementService.ArticleNumber, Quantity = bracketCount, Unit = "st", Note = "Twee symmetrische beugels per Z-draagprofiel; posities uit assemblygeometrie.", ModelStatus = "In 3D-model", BomStatus = "Actueel leveranciersartikel" });
            model.Hardware.Add(new HardwareItem { Name = "M6×12 verzonken inbusbout voor profielzijde werkbladbeugel", ArticleNumber = "TIN 100691 / S208SKS612V", Quantity = bracketCount, Unit = "st", Note = "Eén per TIN 100391-beugel naar groef-8-profiel.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen" });
            model.Hardware.Add(new HardwareItem { Name = "T-moer 8 met brug M6 voor werkbladbeugel", ArticleNumber = "TIN 100242 / S208NSMS6", Quantity = bracketCount, Unit = "st", Note = "Eén per TIN 100391-beugel.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen" });
            model.Hardware.Add(new HardwareItem { Name = "M6 werkblad-doorsteekset voor TIN 100391", ArticleNumber = "WORKTOP_M6_THROUGH_SET", Quantity = bracketCount, Unit = "st", Note = "M6 verzonken bout, ring en borgmoer; definitieve handelslengte volgt uit de gekozen werkbladdikte plus de nog te koppelen beugel- en moerstack.", ModelStatus = "Niet als losse body", BomStatus = "Aantal uit beugelplaatsingen; leveranciersartikel en lengteregel nog koppelen" });
            model.Hardware.Add(new HardwareItem { Name = "Bevestigingsset HPL-stabilisatieplaat 6 mm", ArticleNumber = "LEX_HPL_STABILIZER_FASTENERS_OPEN", Quantity = 1, Unit = "set", Note = "Zelfde open bevestigingspunt als Workstation.", ModelStatus = "Niet in 3D-model", BomStatus = "OPEN" });
            if (TopFrameHeight(config) > 40)
                model.Hardware.Add(new HardwareItem { Name = "Afdekkap 8 80x40 zwart", ArticleNumber = "TIN 100192 / S208AK8040", Quantity = CountPlacements(model, "Afdekkap 8 80x40 zwart"), Unit = "st", Note = "Vier vrije koppen van voor- en achterligger.", ModelStatus = "Leveranciersdata", BomStatus = "Inkoop" });
            else
                model.Hardware.Add(new HardwareItem { Name = "Afdekkap 8 40x40 zwart", ArticleNumber = "TIN 100184 / S208AK4040", Quantity = CountPlacements(model, "Afdekkap 8 40x40 zwart"), Unit = "st", Note = "Vier vrije koppen van voor- en achterligger.", ModelStatus = "In 3D-model", BomStatus = "Actueel leveranciersartikel" });
        }

        private static void AddFastenerCalculation(WorkbenchModel model, HeightAdjustableWorkbenchConfig config)
        {
            var thread = ProfileNutThreadZoneCatalog.LoadRequired().Required("techxxl_t_nut_8_m8", 8);
            var slotDepth = new ProfileSlotGeometryCatalog().FindRequired(config.FootProfile.Id).SlotCavityDepthMm;
            if (!slotDepth.HasValue) throw new InvalidOperationException("Exacte sleufbodemdiepte voor HTE2-onderstel ontbreekt.");
            model.ProfileFastenerCalculations.Add(new ProfileFastenerCalculation
            {
                CalculationId = "height-worktable-hte2-m8",
                HardwareArticleNumber = "LEX_HTE2_ENDPLATE_M8_TNUT",
                AttachmentKind = "component-plate-to-profile-click-nut",
                BoltFamily = new FastenerDefinition { Id = "M8_PROFILE_ATTACHMENT", NominalDiameterMm = 8, UsageKind = FastenerUsageKind.StructuralBolt, LengthMm = 25, AvailableLengthsMm = new[] { 10.0, 12.0, 16.0, 20.0, 25.0, 30.0, 35.0, 40.0 } },
                PassingStackMm = config.LiftColumn.EndPlateThicknessMm,
                MinimumThreadEngagementMm = 5,
                ReceivingThreadComponentId = thread.ComponentId,
                ReceivingThreadSource = thread.Source,
                AvailableThreadZoneMm = thread.UsableThreadZoneMm,
                ThreadInletOffsetMm = thread.ThreadInletOffsetMm,
                MaximumInsertionDepthMm = slotDepth.Value,
                ReceivingThreadThroughHole = thread.ThroughThread,
                BottomClearanceMm = 0.1
            });
        }

        private static void BuildProfileOperations(WorkbenchModel model, ProfileSawingMode mode)
        {
            foreach (var profile in model.Profiles)
                model.ProfileOperations.Add(new ProfileOperation { ProfileId = profile.Name.Replace(" ", "_") + "_" + profile.LengthMm.ToString("0.##") + "mm", PartName = profile.Name, Quantity = profile.Quantity, Material = profile.Material, ProfileLengthMm = profile.LengthMm, Sequence = 1, Kind = ProfileOperationKind.SawCut, SawAngleDeg = 90, WorkOrigin = "Kop A", MachineHint = mode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH", ExecutionParty = mode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER", Note = profile.OrientationNote });
        }

        private static void AddProfile(WorkbenchModel model, string name, Material material, double length, int quantity, string note)
        {
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = Math.Round(length, 2), Quantity = quantity, OrientationNote = note, BomStatus = "Actueel uit model" });
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind, double xSize, double ySize, double zSize, double x, double y, double z, string visualKind)
        {
            AddPlacementWithShape(model, name, kind, xSize, ySize, zSize, x, y, z, visualKind, "box");
        }

        private static void AddPlacementWithShape(WorkbenchModel model, string name, AssemblyComponentKind kind, double xSize, double ySize, double zSize, double x, double y, double z, string visualKind, string shape)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { Kind = kind, PartName = name, LengthMm = xSize, HeightMm = ySize, WidthMm = zSize, Xmm = x, Ymm = y, Zmm = z, Orientation = AssemblyOrientation.Default, VisualKind = visualKind, Shape = shape });
        }

        private static double EffectiveColumnLength(HeightAdjustableWorkbenchConfig config)
        {
            return config.HeightMm - config.FootProfile.HeightMm - TopFrameHeight(config) - config.TopSheet.ThicknessMm;
        }

        private static double FootProfileLength(HeightAdjustableWorkbenchConfig config)
        {
            return config.DepthMm - 2.0 * config.LevelingFootCornerAdapter.ReachMm;
        }

        private static double TopFrameHeight(HeightAdjustableWorkbenchConfig config)
        {
            return Math.Max(config.TopFrameProfile.WidthMm, config.TopFrameProfile.HeightMm);
        }

        private static double TopFrameThickness(HeightAdjustableWorkbenchConfig config)
        {
            return Math.Min(config.TopFrameProfile.WidthMm, config.TopFrameProfile.HeightMm);
        }

        private static string SectionLabel(HeightAdjustableWorkbenchConfig config)
        {
            return TopFrameHeight(config) > 40 ? "40x80 staand" : "40x40";
        }

        private static int CountPlacements(WorkbenchModel model, string prefix)
        {
            return model.AssemblyPlacements.Count(item => (item.PartName ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static void Validate(HeightAdjustableWorkbenchConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.FootProfile == null || config.TopFrameProfile == null || config.TopSheet == null || config.StabilizationSheet == null) throw new ArgumentException("Profiel- of plaatmateriaal ontbreekt.");
            if (config.LiftColumn == null || config.LevelingFootCornerAdapter == null || config.LevelingFoot == null) throw new ArgumentException("Workstation-onderstelcomponent ontbreekt.");
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.HeightMm <= 0) throw new ArgumentException("Werktafelmaten moeten positief zijn.");
            if (TopFrameThickness(config) != 40 || (TopFrameHeight(config) != 40 && TopFrameHeight(config) != 80)) throw new ArgumentException("Bovenframe vereist exact 40x40 of 40x80 serie-8-profiel.");
            var columnLength = EffectiveColumnLength(config);
            if (columnLength < config.LiftColumn.RetractedLengthMm - 0.001 || columnLength > config.LiftColumn.RetractedLengthMm + config.LiftColumn.StrokeMm + 0.001) throw new ArgumentException("Gevraagde werkhoogte valt buiten de HTE2-slag voor het gekozen bovenframeprofiel.");
            if (config.DepthMm <= 2.0 * TopFrameThickness(config)) throw new ArgumentException("Werktafeldiepte is te klein voor het vaste bovenframe.");
            if (config.WidthMm <= config.ColumnCenterDistanceMm + TopFrameThickness(config)) throw new ArgumentException("Werktafelbreedte is te klein voor de twee kolomaansluitprofielen.");
        }
    }
}
