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
                    HeadClearanceMm = 1
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
                    HeadClearanceMm = 1
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
                    HeadClearanceMm = 1
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
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 4,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
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
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 5,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
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
                    CabinetVerticalOffsetMm = 21,
                    CabinetHoleDiameterMm = 5,
                    DrawerHoleCount = 3,
                    DrawerFirstHoleOffsetMm = 29,
                    DrawerHoleSpacingMm = 0,
                    DrawerHolePositionsMm = "29;220;453",
                    DrawerVerticalOffsetMm = 21,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
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
            return new ToolDefinition
            {
                Id = "endmill_" + diameterMm.ToString("0.##").Replace(",", ".") + "mm",
                Name = "Frees " + diameterMm.ToString("0.##") + "mm",
                Kind = ToolKind.EndMill,
                DiameterMm = diameterMm,
                FeedRateMmMin = 1800,
                PlungeRateMmMin = 400,
                SpindleRpm = 18000,
                PassDepthMm = passDepthMm
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
