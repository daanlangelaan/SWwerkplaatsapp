using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class HeightAdjustableWorkbenchMasterDataSettings
    {
        private const string ProductId = "hoogteverstelbare_werktafel";

        public double MinimumWidthMm { get; private set; }
        public double MaximumWidthMm { get; private set; }
        public double MinimumDepthMm { get; private set; }
        public double MaximumDepthMm { get; private set; }
        public double MinimumHeightMm { get; private set; }
        public double MaximumHeightMm { get; private set; }
        public double DefaultWidthMm { get; private set; }
        public double DefaultDepthMm { get; private set; }
        public double DefaultHeightMm { get; private set; }
        public string[] TopFrameProfileIds { get; private set; }
        public string FootProfileId { get; private set; }
        public string[] WorktopMaterialIds { get; private set; }
        public string WorktopMaterialId { get; private set; }
        public string StabilizationMaterialId { get; private set; }

        public static HeightAdjustableWorkbenchMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var product = catalog.Records("products").SingleOrDefault(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductId, StringComparison.OrdinalIgnoreCase));
            if (product == null) throw new InvalidOperationException("Hoogteverstelbare werktafel ontbreekt in productmasterdata.");
            var rules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductId, StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var bounds = Split(RequiredRuleValue(rules, "Parametercontract")).Select(ParseBounds).ToArray();
            if (bounds.Length != 3) throw new InvalidOperationException("Hoogteverstelbare werktafel vereist drie parametergrenzen.");

            var profiles = RequiredIds(rules, "Profielkeuze");
            var footProfiles = RequiredIds(rules, "Onderstelprofiel");
            var worktops = RequiredIds(rules, "Werkbladmateriaal");
            var stabilizers = RequiredIds(rules, "Stabilisatieplaatmateriaal");
            if (profiles.Length != 2 || footProfiles.Length != 1 || worktops.Length == 0 || stabilizers.Length != 1)
                throw new InvalidOperationException("Hoogteverstelbare werktafel mist een geldige profiel- of plaatmateriaalkeuze.");

            return new HeightAdjustableWorkbenchMasterDataSettings
            {
                MinimumWidthMm = bounds[0].Item1,
                MaximumWidthMm = bounds[0].Item2,
                MinimumDepthMm = bounds[1].Item1,
                MaximumDepthMm = bounds[1].Item2,
                MinimumHeightMm = bounds[2].Item1,
                MaximumHeightMm = bounds[2].Item2,
                DefaultWidthMm = ParsePositive(MasterDataRuntimeCatalog.Value(product, "Breedte default mm"), "standaardbreedte"),
                DefaultDepthMm = ParsePositive(MasterDataRuntimeCatalog.Value(product, "Diepte default mm"), "standaarddiepte"),
                DefaultHeightMm = ParsePositive(MasterDataRuntimeCatalog.Value(product, "Hoogte default mm"), "standaardhoogte"),
                TopFrameProfileIds = profiles,
                FootProfileId = footProfiles[0],
                WorktopMaterialIds = worktops,
                WorktopMaterialId = worktops[0],
                StabilizationMaterialId = stabilizers[0]
            };
        }

        public string ResolveTopFrameProfile(string requested)
        {
            if (string.IsNullOrWhiteSpace(requested)) return TopFrameProfileIds[0];
            var resolved = TopFrameProfileIds.FirstOrDefault(id => string.Equals(id, requested.Trim(), StringComparison.OrdinalIgnoreCase));
            if (resolved == null) throw new InvalidOperationException("Niet-toegestaan bovenframeprofiel: " + requested + ".");
            return resolved;
        }

        public string ResolveWorktopMaterial(string requested)
        {
            if (string.IsNullOrWhiteSpace(requested)) return WorktopMaterialId;
            var resolved = WorktopMaterialIds.FirstOrDefault(id => string.Equals(id, requested.Trim(), StringComparison.OrdinalIgnoreCase));
            if (resolved == null) throw new InvalidOperationException("Niet-toegestaan werkbladmateriaal voor hoogteverstelbare werktafel: " + requested + ".");
            return resolved;
        }

        private static string RequiredRuleValue(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            return MasterDataRuntimeCatalog.Value(RequiredRule(rules, parameterType), "Waarde");
        }

        private static string[] RequiredIds(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            return Split(MasterDataRuntimeCatalog.Value(RequiredRule(rules, parameterType), "Referentie-ID(s)"));
        }

        private static Dictionary<string, string> RequiredRule(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var matches = rules.Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), parameterType, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Hoogteverstelbare werktafel vereist precies één regel voor " + parameterType + ".");
            return matches[0];
        }

        private static string[] Split(string value)
        {
            return (value ?? string.Empty).Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
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
                throw new InvalidOperationException("Ongeldige hoogteverstelbare-werktafelgrens: " + value + ".");
            return Tuple.Create(minimum, maximum);
        }

        private static double ParsePositive(string value, string label)
        {
            double result;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || result <= 0)
                throw new InvalidOperationException("Ongeldige " + label + " voor hoogteverstelbare werktafel.");
            return result;
        }
    }
}
