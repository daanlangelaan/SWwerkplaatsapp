using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class HardwareLibraryData
    {
        public RailTemplate[] rails { get; set; }
        public ShelfSupportTemplate[] shelfSupports { get; set; }
    }

    public static class HardwareLibraryRepository
    {
        public static RailTemplate[] DrawerRails()
        {
            return MergeRails(LibraryCatalog.DrawerRails(), Load().rails);
        }

        public static ShelfSupportTemplate[] ShelfSupports()
        {
            return MergeShelfSupports(LibraryCatalog.ShelfSupports(), Load().shelfSupports);
        }

        public static HardwareLibraryData Load()
        {
            var path = ConfigPath(false);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return Empty();
            }

            try
            {
                var data = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<HardwareLibraryData>(File.ReadAllText(path));
                return data ?? Empty();
            }
            catch
            {
                return Empty();
            }
        }

        public static string Save(RailTemplate[] rails, ShelfSupportTemplate[] shelfSupports)
        {
            var path = ConfigPath(true);
            if (string.IsNullOrEmpty(path)) return "";

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var data = new HardwareLibraryData
            {
                rails = rails ?? new RailTemplate[0],
                shelfSupports = shelfSupports ?? new ShelfSupportTemplate[0]
            };
            File.WriteAllText(path, new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(data));
            return path;
        }

        private static HardwareLibraryData Empty()
        {
            return new HardwareLibraryData
            {
                rails = new RailTemplate[0],
                shelfSupports = new ShelfSupportTemplate[0]
            };
        }

        private static RailTemplate[] MergeRails(RailTemplate[] defaults, RailTemplate[] configured)
        {
            var merged = new List<RailTemplate>();
            var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AddRails(merged, indexById, defaults);
            AddRails(merged, indexById, configured);
            return merged.ToArray();
        }

        private static void AddRails(List<RailTemplate> merged, Dictionary<string, int> indexById, RailTemplate[] rails)
        {
            if (rails == null) return;
            foreach (var rail in rails)
            {
                if (rail == null || string.IsNullOrWhiteSpace(rail.Id) || string.IsNullOrWhiteSpace(rail.Name)) continue;
                var clone = CloneRail(rail);
                int existing;
                if (indexById.TryGetValue(clone.Id, out existing))
                {
                    merged[existing] = clone;
                }
                else
                {
                    indexById[clone.Id] = merged.Count;
                    merged.Add(clone);
                }
            }
        }

        private static ShelfSupportTemplate[] MergeShelfSupports(ShelfSupportTemplate[] defaults, ShelfSupportTemplate[] configured)
        {
            var merged = new List<ShelfSupportTemplate>();
            var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AddShelfSupports(merged, indexById, defaults);
            AddShelfSupports(merged, indexById, configured);
            return merged.ToArray();
        }

        private static void AddShelfSupports(List<ShelfSupportTemplate> merged, Dictionary<string, int> indexById, ShelfSupportTemplate[] supports)
        {
            if (supports == null) return;
            foreach (var support in supports)
            {
                if (support == null || string.IsNullOrWhiteSpace(support.Id) || string.IsNullOrWhiteSpace(support.Name)) continue;
                var clone = CloneShelfSupport(support);
                int existing;
                if (indexById.TryGetValue(clone.Id, out existing))
                {
                    merged[existing] = clone;
                }
                else
                {
                    indexById[clone.Id] = merged.Count;
                    merged.Add(clone);
                }
            }
        }

        private static RailTemplate CloneRail(RailTemplate rail)
        {
            return new RailTemplate
            {
                Id = rail.Id,
                Name = rail.Name,
                LengthMm = Positive(rail.LengthMm, 450),
                ThicknessMm = Positive(rail.ThicknessMm, 12.7),
                CabinetHoleCount = NonNegative(rail.CabinetHoleCount),
                CabinetFirstHoleOffsetMm = NonNegative(rail.CabinetFirstHoleOffsetMm),
                CabinetHoleSpacingMm = NonNegative(rail.CabinetHoleSpacingMm),
                CabinetHolePositionsMm = rail.CabinetHolePositionsMm ?? "",
                CabinetVerticalOffsetMm = NonNegative(rail.CabinetVerticalOffsetMm),
                CabinetHoleDiameterMm = Positive(rail.CabinetHoleDiameterMm, 5),
                DrawerHoleCount = NonNegative(rail.DrawerHoleCount),
                DrawerFirstHoleOffsetMm = NonNegative(rail.DrawerFirstHoleOffsetMm),
                DrawerHoleSpacingMm = NonNegative(rail.DrawerHoleSpacingMm),
                DrawerHolePositionsMm = rail.DrawerHolePositionsMm ?? "",
                DrawerVerticalOffsetMm = NonNegative(rail.DrawerVerticalOffsetMm),
                DrawerHoleDiameterMm = Positive(rail.DrawerHoleDiameterMm, 4.5),
                FastenerName = rail.FastenerName ?? ""
            };
        }

        private static ShelfSupportTemplate CloneShelfSupport(ShelfSupportTemplate support)
        {
            return new ShelfSupportTemplate
            {
                Id = support.Id,
                Name = support.Name,
                ThicknessMm = Positive(support.ThicknessMm, 5),
                HeightMm = Positive(support.HeightMm, 12),
                HoleDiameterMm = Positive(support.HoleDiameterMm, 5),
                HoleSpacingMm = Positive(support.HoleSpacingMm, 32),
                FrontInsetMm = NonNegative(support.FrontInsetMm),
                BackInsetMm = NonNegative(support.BackInsetMm),
                FirstHoleHeightMm = NonNegative(support.FirstHoleHeightMm)
            };
        }

        private static string ConfigPath(bool create)
        {
            var existingConfig = FindConfigFolder(AppDomain.CurrentDomain.BaseDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "hardware-library.json");

            existingConfig = FindConfigFolder(Environment.CurrentDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "hardware-library.json");

            if (!create) return null;
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config", "hardware-library.json"));
        }

        private static string FindConfigFolder(string startFolder)
        {
            if (string.IsNullOrEmpty(startFolder)) return null;

            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 6 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config");
                if (Directory.Exists(candidate)) return candidate;

                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }

            return null;
        }

        private static double Positive(double value, double fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static double NonNegative(double value)
        {
            return value >= 0 ? value : 0;
        }

        private static int NonNegative(int value)
        {
            return value >= 0 ? value : 0;
        }
    }
}
