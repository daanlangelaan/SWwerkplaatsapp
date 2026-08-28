using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class MachineBaseMasterDataSettings
    {
        public double MinimumWidthMm { get; private set; }
        public double MaximumWidthMm { get; private set; }
        public double MinimumDepthMm { get; private set; }
        public double MaximumDepthMm { get; private set; }
        public double MinimumHeightMm { get; private set; }
        public double MaximumHeightMm { get; private set; }
        public string PrimaryProfileId { get; private set; }
        public string[] LowerBeamProfileIds { get; private set; }
        public string TopFrameProfileId { get; private set; }
        public string[] WorktopMaterialIds { get; private set; }
        public string LowerPanelMaterialId { get; private set; }
        public string UpperPanelMaterialId { get; private set; }

        public static MachineBaseMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var rules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), "machinebasis", StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Open", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var dimensions = RequiredRule(rules, "Parametercontract");
            var bounds = MasterDataRuntimeCatalog.Value(dimensions, "Waarde")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseBounds).ToArray();
            if (bounds.Length != 3)
                throw new InvalidOperationException("Machinebasis-masterdata vereist precies drie parametergrenzen voor breedte, diepte en hoogte.");

            var primaryProfiles = RequiredIds(rules, "Profielkeuze");
            var lowerProfiles = RequiredIds(rules, "Onderliggerprofiel");
            var topProfiles = RequiredIds(rules, "Bovenframeprofiel");
            var worktops = RequiredIds(rules, "Werkbladmateriaal");
            var lowerPanels = RequiredIds(rules, "Onderpaneelmateriaal");
            var upperPanels = RequiredIds(rules, "Bovenpaneelmateriaal");
            if (primaryProfiles.Length != 1 || topProfiles.Length != 1 || lowerPanels.Length != 1 || upperPanels.Length != 1)
                throw new InvalidOperationException("Machinebasis-masterdata vereist één primair profiel, bovenframeprofiel en onder-/bovenpaneelmateriaal.");

            return new MachineBaseMasterDataSettings
            {
                MinimumWidthMm = bounds[0].Item1,
                MaximumWidthMm = bounds[0].Item2,
                MinimumDepthMm = bounds[1].Item1,
                MaximumDepthMm = bounds[1].Item2,
                MinimumHeightMm = bounds[2].Item1,
                MaximumHeightMm = bounds[2].Item2,
                PrimaryProfileId = primaryProfiles[0],
                LowerBeamProfileIds = lowerProfiles,
                TopFrameProfileId = topProfiles[0],
                WorktopMaterialIds = worktops,
                LowerPanelMaterialId = lowerPanels[0],
                UpperPanelMaterialId = upperPanels[0]
            };
        }

        public string ResolveWorktopMaterial(string requested)
        {
            return ResolveChoice(WorktopMaterialIds, requested, "werkbladmateriaal");
        }

        public string ResolveLowerBeamProfile(string requested)
        {
            return ResolveChoice(LowerBeamProfileIds, requested, "onderliggerprofiel");
        }

        private static string ResolveChoice(IEnumerable<string> allowed, string requested, string label)
        {
            var values = allowed.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (values.Length == 0) throw new InvalidOperationException("Machinebasis mist toegestane keuzes voor " + label + ".");
            if (string.IsNullOrWhiteSpace(requested)) return values[0];
            var match = values.FirstOrDefault(value => string.Equals(value, requested.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null) throw new InvalidOperationException("Niet-toegestane machinebasiskeuze voor " + label + ": " + requested + ".");
            return match;
        }

        private static Dictionary<string, string> RequiredRule(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var matches = rules.Where(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Parametertype"), parameterType, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Machinebasis-masterdata vereist precies één productregel voor " + parameterType + ".");
            return matches[0];
        }

        private static string[] RequiredIds(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            return MasterDataRuntimeCatalog.Value(RequiredRule(rules, parameterType), "Referentie-ID(s)")
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim()).Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static Tuple<double, double> ParseBounds(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { ".." }, StringSplitOptions.None);
            double minimum;
            double maximum;
            if (parts.Length != 2
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minimum)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out maximum)
                || minimum <= 0 || maximum < minimum)
                throw new InvalidOperationException("Ongeldige machinebasis-parametergrens: " + value + ".");
            return Tuple.Create(minimum, maximum);
        }
    }
}
