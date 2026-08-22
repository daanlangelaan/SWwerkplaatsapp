using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Domain
{
    public static class LibraryCatalog
    {
        public static Material[] Profiles()
        {
            return MergeConfiguredMaterials(DefaultProfiles(), MaterialKind.Profile);
        }

        public static Material[] Sheets()
        {
            return MergeConfiguredMaterials(DefaultSheets(), MaterialKind.Sheet);
        }

        private static Material[] DefaultProfiles()
        {
            return new[]
            {
                new Material { Id = "alu_profile_30x30", Name = "Alu profiel 30x30", Kind = MaterialKind.Profile, WidthMm = 30, HeightMm = 30, StockLengthMm = 6000 },
                new Material { Id = "alu_profile_40x40", Name = "Alu profiel 40x40", Kind = MaterialKind.Profile, WidthMm = 40, HeightMm = 40, StockLengthMm = 6000 },
                new Material { Id = "alu_profile_45x45", Name = "Alu profiel 45x45", Kind = MaterialKind.Profile, WidthMm = 45, HeightMm = 45, StockLengthMm = 6000 },
                new Material { Id = "alu_system_40x40", Name = "Geanodiseerd aluminium systeemprofiel 40x40", Kind = MaterialKind.Profile, WidthMm = 40, HeightMm = 40, StockLengthMm = 6000 },
                new Material { Id = "alu_system_80x40", Name = "Geanodiseerd aluminium systeemprofiel 80x40", Kind = MaterialKind.Profile, WidthMm = 80, HeightMm = 40, StockLengthMm = 6000 },
                new Material { Id = "alu_system_80x80", Name = "Geanodiseerd aluminium systeemprofiel 80x80", Kind = MaterialKind.Profile, WidthMm = 80, HeightMm = 80, StockLengthMm = 6000 },
                new Material { Id = "alu_system_160x40", Name = "Geanodiseerd aluminium systeemprofiel 160x40", Kind = MaterialKind.Profile, WidthMm = 160, HeightMm = 40, StockLengthMm = 6000 },
                new Material { Id = "steel_tube_40x40", Name = "Stalen koker 40x40", Kind = MaterialKind.Profile, WidthMm = 40, HeightMm = 40, StockLengthMm = 6000 }
            };
        }

        private static Material[] DefaultSheets()
        {
            return new[]
            {
                new Material { Id = "osb_18", Name = "OSB 18mm", Kind = MaterialKind.Sheet, ThicknessMm = 18, SheetLengthMm = 2440, SheetWidthMm = 1220 },
                new Material { Id = "betonplex_12", Name = "Betonplex 12mm", Kind = MaterialKind.Sheet, ThicknessMm = 12, SheetLengthMm = 2500, SheetWidthMm = 1250 },
                new Material { Id = "betonplex_18", Name = "Betonplex 18mm", Kind = MaterialKind.Sheet, ThicknessMm = 18, SheetLengthMm = 2500, SheetWidthMm = 1250 },
                new Material { Id = "hpl_10_lex", Name = "HPL plaat wit 10 mm", Kind = MaterialKind.Sheet, ThicknessMm = 10, SheetLengthMm = 3050, SheetWidthMm = 1300 },
                new Material { Id = "hpl_12_machinebase", Name = "HPL plaat wit 12 mm", Kind = MaterialKind.Sheet, ThicknessMm = 12, SheetLengthMm = 3050, SheetWidthMm = 1300 },
                new Material { Id = "hpl_6_lex_stabilizer", Name = "HPL stabilisatieplaat wit 6 mm", Kind = MaterialKind.Sheet, ThicknessMm = 6, SheetLengthMm = 3050, SheetWidthMm = 1300 },
                new Material { Id = "acrylic_clear_6", Name = "Acrylaat helder kleurloos 6 mm", Kind = MaterialKind.Sheet, ThicknessMm = 6, SheetLengthMm = 3050, SheetWidthMm = 2050 },
                new Material { Id = "alu_6082_10_adapter", Name = "Aluminium EN AW-6082 plaat 10 mm", Kind = MaterialKind.Sheet, ThicknessMm = 10, SheetLengthMm = 2500, SheetWidthMm = 1250 },
                new Material { Id = "multiplex_15", Name = "Multiplex 15mm", Kind = MaterialKind.Sheet, ThicknessMm = 15, SheetLengthMm = 2500, SheetWidthMm = 1250 }
            };
        }

        private static Material[] MergeConfiguredMaterials(Material[] defaults, MaterialKind kind)
        {
            var merged = new List<Material>();
            var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < defaults.Length; i++)
            {
                merged.Add(Clone(defaults[i]));
                if (!string.IsNullOrEmpty(defaults[i].Id)) indexById[defaults[i].Id] = i;
            }

            foreach (var material in ConfiguredMaterials(kind))
            {
                int existingIndex;
                if (!string.IsNullOrEmpty(material.Id) && indexById.TryGetValue(material.Id, out existingIndex))
                {
                    merged[existingIndex] = material;
                }
                else
                {
                    indexById[material.Id] = merged.Count;
                    merged.Add(material);
                }
            }

            return merged.ToArray();
        }

        private static IEnumerable<Material> ConfiguredMaterials(MaterialKind kind)
        {
            var path = MaterialsConfigPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) yield break;

            MaterialConfig config;
            try
            {
                config = new JavaScriptSerializer().Deserialize<MaterialConfig>(File.ReadAllText(path));
            }
            catch
            {
                yield break;
            }

            if (config == null || config.materials == null) yield break;
            foreach (var item in config.materials)
            {
                var material = ToMaterial(item);
                if (material != null && material.Kind == kind) yield return material;
            }
        }

        private static string MaterialsConfigPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "config", "materials.json"),
                Path.Combine(baseDir, "..", "config", "materials.json"),
                Path.Combine(baseDir, "..", "..", "config", "materials.json")
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath)) return fullPath;
            }

            return null;
        }

        private static Material ToMaterial(MaterialConfigItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id)) return null;
            MaterialKind kind;
            if (string.Equals(item.kind, "profile", StringComparison.OrdinalIgnoreCase))
            {
                kind = MaterialKind.Profile;
            }
            else if (string.Equals(item.kind, "sheet", StringComparison.OrdinalIgnoreCase))
            {
                kind = MaterialKind.Sheet;
            }
            else
            {
                return null;
            }

            return new Material
            {
                Id = item.id,
                Name = string.IsNullOrWhiteSpace(item.name) ? item.id : item.name,
                Kind = kind,
                ThicknessMm = item.thicknessMm,
                WidthMm = item.widthMm,
                HeightMm = item.heightMm,
                StockLengthMm = item.stockLengthMm,
                SheetLengthMm = item.sheetLengthMm,
                SheetWidthMm = item.sheetWidthMm
            };
        }

        private static Material Clone(Material material)
        {
            return new Material
            {
                Id = material.Id,
                Name = material.Name,
                Kind = material.Kind,
                ThicknessMm = material.ThicknessMm,
                WidthMm = material.WidthMm,
                HeightMm = material.HeightMm,
                StockLengthMm = material.StockLengthMm,
                SheetLengthMm = material.SheetLengthMm,
                SheetWidthMm = material.SheetWidthMm
            };
        }

        public static FastenerDefinition[] SheetFasteners()
        {
            return new[]
            {
                new FastenerDefinition
                {
                    Id = "M8_ISO4762",
                    Name = "M8 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 8,
                    ClearanceHoleDiameterMm = 8,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 13,
                    HeadHeightMm = 8,
                    HeadClearanceMm = 1,
                    UsageKind = FastenerUsageKind.StructuralBolt
                },
                new FastenerDefinition
                {
                    Id = "M6_ISO4762",
                    Name = "M6 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 6,
                    ClearanceHoleDiameterMm = 6.5,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 10,
                    HeadHeightMm = 6,
                    HeadClearanceMm = 1,
                    UsageKind = FastenerUsageKind.StructuralBolt
                },
                new FastenerDefinition
                {
                    Id = "M10_ISO4762",
                    Name = "M10 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 10,
                    ClearanceHoleDiameterMm = 10.5,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 16,
                    HeadHeightMm = 10,
                    HeadClearanceMm = 1,
                    UsageKind = FastenerUsageKind.StructuralBolt
                },
                new FastenerDefinition
                {
                    Id = "WOODSCREW_4",
                    Name = "Houtschroef verzonken kop Ø4",
                    Standard = "SW hout-op-hout; getest als 4x40 in CNC-gat Ø4",
                    NominalDiameterMm = 4,
                    ClearanceHoleDiameterMm = 4,
                    ReceivingPilotHoleDiameterMm = 3,
                    HeadKind = FastenerHeadKind.Countersunk,
                    HeadDiameterMm = 8,
                    HeadHeightMm = 3.2,
                    HeadClearanceMm = 0,
                    UsageKind = FastenerUsageKind.WoodScrew,
                    LengthMm = 40,
                    AvailableLengthsMm = new double[] { 12, 16, 20, 25, 30, 35, 40, 45, 50, 55, 60 },
                    MinimumEdgePenetrationMm = 20,
                    MinimumTipClearanceMm = 2
                }
            };
        }

        public static RailTemplate[] DrawerRails()
        {
            return new[]
            {
                new RailTemplate
                {
                    Id = "generic_350",
                    Name = "AliExpress ladegeleider 350mm basis",
                    LengthMm = 350,
                    ThicknessMm = 12.7,
                    CabinetHoleCount = 4,
                    CabinetFirstHoleOffsetMm = 37,
                    CabinetHoleSpacingMm = 96,
                    CabinetHolePositionsMm = "",
                    CabinetOppositeHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 4,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef",
                    CabinetFastenerDiameterMm = 4,
                    CabinetFastenerLengthMm = 16,
                    CabinetFastenerPassingStackMm = 0,
                    CabinetFastenerHeadStyle = "bolkop",
                    CabinetOpposingFitVerificationSignature = ""
                },
                new RailTemplate
                {
                    Id = "generic_450",
                    Name = "AliExpress ladegeleider 450mm basis",
                    LengthMm = 450,
                    ThicknessMm = 12.7,
                    CabinetHoleCount = 5,
                    CabinetFirstHoleOffsetMm = 37,
                    CabinetHoleSpacingMm = 96,
                    CabinetHolePositionsMm = "",
                    CabinetOppositeHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 5,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef",
                    CabinetFastenerDiameterMm = 4,
                    CabinetFastenerLengthMm = 16,
                    CabinetFastenerPassingStackMm = 0,
                    CabinetFastenerHeadStyle = "bolkop",
                    CabinetOpposingFitVerificationSignature = ""
                },
                new RailTemplate
                {
                    Id = "measured_500",
                    Name = "Gemeten ladegeleider 500mm",
                    LengthMm = 500,
                    ThicknessMm = 13,
                    CabinetHoleCount = 5,
                    CabinetFirstHoleOffsetMm = 34,
                    CabinetHoleSpacingMm = 0,
                    CabinetHolePositionsMm = "34;98;226;354;418",
                    CabinetOppositeHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 21,
                    CabinetHoleDiameterMm = 3,
                    DrawerHoleCount = 3,
                    DrawerFirstHoleOffsetMm = 29,
                    DrawerHoleSpacingMm = 0,
                    DrawerHolePositionsMm = "29;220;453",
                    DrawerVerticalOffsetMm = 21,
                    DrawerHoleDiameterMm = 3,
                    FastenerName = "4,2x9,5 plaatkopschroef",
                    CabinetFastenerDiameterMm = 4.2,
                    CabinetFastenerLengthMm = 9.5,
                    CabinetFastenerPassingStackMm = 0.5,
                    CabinetFastenerHeadStyle = "plaatkop",
                    CabinetOpposingFitVerificationSignature = "4.2x9.5|0.5|PLAATKOP|18"
                },
                new RailTemplate
                {
                    Id = "measured_500_r2",
                    Name = "Gemeten ladegeleider 500mm R2",
                    LengthMm = 500,
                    ThicknessMm = 13,
                    CabinetHoleCount = 5,
                    CabinetFirstHoleOffsetMm = 34,
                    CabinetHoleSpacingMm = 0,
                    CabinetHolePositionsMm = "34;98;226;354;418",
                    CabinetOppositeHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 21,
                    CabinetHoleDiameterMm = 3,
                    DrawerHoleCount = 3,
                    DrawerFirstHoleOffsetMm = 27,
                    DrawerHoleSpacingMm = 0,
                    DrawerHolePositionsMm = "27;218;451",
                    DrawerVerticalOffsetMm = 21,
                    DrawerHoleDiameterMm = 3,
                    DrawerFrontInsertionCompensationMm = 3,
                    FastenerName = "4,2x9,5 plaatkopschroef",
                    CabinetFastenerDiameterMm = 4.2,
                    CabinetFastenerLengthMm = 9.5,
                    CabinetFastenerPassingStackMm = 0.5,
                    CabinetFastenerHeadStyle = "plaatkop",
                    CabinetOpposingFitVerificationSignature = "4.2x9.5|0.5|PLAATKOP|18"
                }
            };
        }

        public static ShelfSupportTemplate[] ShelfSupports()
        {
            return new[]
            {
                new ShelfSupportTemplate
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
                },
                new ShelfSupportTemplate
                {
                    Id = "shelf_pin_5mm_64",
                    Name = "Legplankdrager pin 5mm, grove stap 64",
                    ThicknessMm = 5,
                    HeightMm = 12,
                    HoleDiameterMm = 5,
                    HoleSpacingMm = 64,
                    FrontInsetMm = 50,
                    BackInsetMm = 50,
                    FirstHoleHeightMm = 160
                }
            };
        }

        public static ToolDefinition DefaultEndMill(double diameterMm, double passDepthMm)
        {
            var isTestedSixMillimeterFlat = Math.Abs(diameterMm - 6) < 0.001;
            var isSafeThreeMillimeterTwoFlute = Math.Abs(diameterMm - 3) < 0.001;
            var isSafeFourMillimeterFlat = Math.Abs(diameterMm - 4) < 0.001;
            return new ToolDefinition
            {
                Id = "endmill_" + diameterMm.ToString("0.##").Replace(",", ".") + "mm",
                Name = isSafeThreeMillimeterTwoFlute ? "Frees 3mm 2-fluit carbide" : "Frees " + diameterMm.ToString("0.##") + "mm",
                Kind = ToolKind.EndMill,
                DiameterMm = diameterMm,
                FeedRateMmMin = isTestedSixMillimeterFlat ? 3200 : (isSafeThreeMillimeterTwoFlute ? 1600 : (isSafeFourMillimeterFlat ? 2200 : 1800)),
                PlungeRateMmMin = isTestedSixMillimeterFlat ? 800 : (isSafeThreeMillimeterTwoFlute ? 300 : (isSafeFourMillimeterFlat ? 600 : 400)),
                SpindleRpm = isTestedSixMillimeterFlat ? 20500 : 18000,
                PassDepthMm = passDepthMm
            };
        }

        public static ToolDefinition WorkbenchCabinetVBit()
        {
            return new ToolDefinition
            {
                Id = "vbit_90deg_8mm_shank_6_35",
                Name = "V-frees 90° Ø8 / schacht Ø6,35",
                Kind = ToolKind.VBit,
                DiameterMm = 8.0,
                FeedRateMmMin = 600.0,
                PlungeRateMmMin = 150.0,
                SpindleRpm = 18000.0,
                PassDepthMm = 1.0,
                FluteCount = 2,
                Rotation = "Rechts",
                IncludedAngleDeg = 90.0,
                TipDiameterMm = 0.0,
                ShankDiameterMm = 6.35,
                ConeLengthMm = 4.0,
                CylindricalCutLengthMm = 4.5,
                MaximumCutDepthMm = 8.5
            };
        }

        private sealed class MaterialConfig
        {
            public MaterialConfigItem[] materials { get; set; }
        }

        private sealed class MaterialConfigItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public string kind { get; set; }
            public double thicknessMm { get; set; }
            public double widthMm { get; set; }
            public double heightMm { get; set; }
            public double stockLengthMm { get; set; }
            public double sheetLengthMm { get; set; }
            public double sheetWidthMm { get; set; }
        }
    }
}
