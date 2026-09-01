using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class FoldingWorkbenchMasterDataSettings
    {
        private const string ProductIdValue = "opvouwbare_werktafel";

        public double MinimumLengthMm { get; private set; }
        public double MaximumLengthMm { get; private set; }
        public double MinimumWidthMm { get; private set; }
        public double MaximumWidthMm { get; private set; }
        public double MinimumHeightMm { get; private set; }
        public double MaximumHeightMm { get; private set; }
        public double DefaultLengthMm { get; private set; }
        public double DefaultWidthMm { get; private set; }
        public double DefaultHeightMm { get; private set; }
        public string MaterialId { get; private set; }
        public double StockAllowanceMm { get; private set; }
        public double DefaultUnderframeInsetLongEdgeMm { get; private set; }
        public double MinimumUnderframeInsetLongEdgeMm { get; private set; }
        public double MaximumUnderframeInsetLongEdgeMm { get; private set; }
        public double DefaultUnderframeInsetShortEdgeMm { get; private set; }
        public double MinimumUnderframeInsetShortEdgeMm { get; private set; }
        public double MaximumUnderframeInsetShortEdgeMm { get; private set; }
        public double JointClearanceMm { get; private set; }
        public double TabWidthMm { get; private set; }
        public double FrameStileWidthMm { get; private set; }
        public double TopRailHeightMm { get; private set; }
        public double BottomRailHeightMm { get; private set; }
        public double IntegratedFootWidthMm { get; private set; }
        public double IntegratedFootReliefHeightMm { get; private set; }
        public double CornerRadiusMm { get; private set; }
        public double WorktopFloatMm { get; private set; }
        public double FoldedClearanceMm { get; private set; }
        public int TabsPerLongPanel { get; private set; }
        public int TabsPerShortPanelHalf { get; private set; }
        public double HingeHeightMm { get; private set; }
        public double HingeOpenWidthMm { get; private set; }
        public double HingeLeafThicknessMm { get; private set; }
        public double HingeBarrelDiameterMm { get; private set; }
        public double HingeGapMm { get; private set; }
        public string HingeComponentId { get; private set; }
        public string HingeArticleNumber { get; private set; }
        public double DogboneToolDiameterMm { get; private set; }

        public static FoldingWorkbenchMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var product = Single(catalog.Records("products"), "Product-ID", ProductIdValue, "productrecord");
            var rules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductIdValue, StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var inputs = catalog.Records("productInputContracts")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductIdValue, StringComparison.OrdinalIgnoreCase))
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Actief"), "Ja", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var parameter = SingleRule(rules, "Parametercontract");
            var bounds = MasterDataRuntimeCatalog.Value(parameter, "Waarde").Split(';').Select(ParseBounds).ToArray();
            if (bounds.Length != 3) throw new InvalidOperationException("Opvouwbare werktafel vereist drie parametergrenzen.");

            var materialRule = SingleRule(rules, "Materiaalkeuze");
            var geometry = ParseKeyValues(MasterDataRuntimeCatalog.Value(SingleRule(rules, "Geometriecontract"), "Waarde"));
            var hingeRule = SingleRule(rules, "Vouwscharnier");
            var hingeId = MasterDataRuntimeCatalog.Value(hingeRule, "Referentie-ID(s)").Trim();
            var hinge = Single(catalog.Records("components"), "Component-ID", hingeId, "scharniercomponent");
            var toolId = MasterDataRuntimeCatalog.Value(SingleRule(rules, "Dogbonegereedschap"), "Referentie-ID(s)").Trim();
            var tool = Single(catalog.Records("tools"), "Gereedschap-ID", toolId, "dogbonegereedschap");
            var longEdgeInsetInput = SingleInput(inputs, "FoldingWorkbenchUnderframeInsetLongEdgeMm");
            var shortEdgeInsetInput = SingleInput(inputs, "FoldingWorkbenchUnderframeInsetShortEdgeMm");

            return new FoldingWorkbenchMasterDataSettings
            {
                MinimumLengthMm = bounds[0].Item1,
                MaximumLengthMm = bounds[0].Item2,
                MinimumWidthMm = bounds[1].Item1,
                MaximumWidthMm = bounds[1].Item2,
                MinimumHeightMm = bounds[2].Item1,
                MaximumHeightMm = bounds[2].Item2,
                DefaultLengthMm = Positive(product, "Breedte default mm"),
                DefaultWidthMm = Positive(product, "Diepte default mm"),
                DefaultHeightMm = Positive(product, "Hoogte default mm"),
                MaterialId = Required(MasterDataRuntimeCatalog.Value(materialRule, "Referentie-ID(s)"), "materiaal-ID"),
                StockAllowanceMm = NonNegative(MasterDataRuntimeCatalog.Value(SingleRule(rules, "Plaatmarge"), "Waarde"), "plaatmarge"),
                DefaultUnderframeInsetLongEdgeMm = Positive(longEdgeInsetInput, "Standaardwaarde"),
                MinimumUnderframeInsetLongEdgeMm = Positive(longEdgeInsetInput, "Minimum"),
                MaximumUnderframeInsetLongEdgeMm = Positive(longEdgeInsetInput, "Maximum"),
                DefaultUnderframeInsetShortEdgeMm = Positive(shortEdgeInsetInput, "Standaardwaarde"),
                MinimumUnderframeInsetShortEdgeMm = Positive(shortEdgeInsetInput, "Minimum"),
                MaximumUnderframeInsetShortEdgeMm = Positive(shortEdgeInsetInput, "Maximum"),
                JointClearanceMm = Geometry(geometry, "jointClearance"),
                TabWidthMm = Geometry(geometry, "tabWidth"),
                FrameStileWidthMm = Geometry(geometry, "frameStileWidth"),
                TopRailHeightMm = Geometry(geometry, "topRailHeight"),
                BottomRailHeightMm = Geometry(geometry, "bottomRailHeight"),
                IntegratedFootWidthMm = Geometry(geometry, "integratedFootWidth"),
                IntegratedFootReliefHeightMm = Geometry(geometry, "integratedFootReliefHeight"),
                CornerRadiusMm = Geometry(geometry, "cornerRadius"),
                WorktopFloatMm = Geometry(geometry, "worktopFloat"),
                FoldedClearanceMm = Geometry(geometry, "foldedClearance"),
                TabsPerLongPanel = Convert.ToInt32(Geometry(geometry, "tabsPerLongPanel"), CultureInfo.InvariantCulture),
                TabsPerShortPanelHalf = Convert.ToInt32(Geometry(geometry, "tabsPerShortPanelHalf"), CultureInfo.InvariantCulture),
                HingeHeightMm = Geometry(geometry, "hingeHeight"),
                HingeOpenWidthMm = Geometry(geometry, "hingeOpenWidth"),
                HingeLeafThicknessMm = Geometry(geometry, "hingeLeafThickness"),
                HingeBarrelDiameterMm = Geometry(geometry, "hingeBarrelDiameter"),
                HingeGapMm = Geometry(geometry, "hingeGap"),
                HingeComponentId = Required(hingeId, "scharniercomponent-ID"),
                HingeArticleNumber = Required(MasterDataRuntimeCatalog.Value(hinge, "Norm/artikel"), "scharnierartikel"),
                DogboneToolDiameterMm = Positive(tool, "Snijdiameter mm")
            };
        }

        private static Dictionary<string, string> Single(IEnumerable<Dictionary<string, string>> rows, string key, string value, string label)
        {
            var matches = rows.Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, key), value, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Opvouwbare werktafel vereist precies één " + label + ": " + value + ".");
            return matches[0];
        }

        private static Dictionary<string, string> SingleRule(IEnumerable<Dictionary<string, string>> rules, string type)
        {
            var matches = rules.Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), type, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Opvouwbare werktafel vereist precies één regel van type " + type + ".");
            return matches[0];
        }

        private static Dictionary<string, string> SingleInput(IEnumerable<Dictionary<string, string>> inputs, string requestField)
        {
            var matches = inputs.Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Request-veld"), requestField, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Opvouwbare werktafel vereist precies één invoercontract voor " + requestField + ".");
            return matches[0];
        }

        private static Tuple<double, double> ParseBounds(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { ".." }, StringSplitOptions.None);
            if (parts.Length != 2) throw new InvalidOperationException("Ongeldige parametergrens: " + value + ".");
            var minimum = Parse(parts[0], "parameterminimum");
            var maximum = Parse(parts[1], "parametermaximum");
            if (minimum <= 0 || maximum < minimum) throw new InvalidOperationException("Ongeldige parametergrens: " + value + ".");
            return Tuple.Create(minimum, maximum);
        }

        private static Dictionary<string, string> ParseKeyValues(string value)
        {
            return (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Split(new[] { '=' }, 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static double Geometry(IDictionary<string, string> values, string key)
        {
            string value;
            if (!values.TryGetValue(key, out value)) throw new InvalidOperationException("Geometrieveld ontbreekt voor opvouwbare werktafel: " + key + ".");
            return NonNegative(value, key);
        }

        private static double Positive(Dictionary<string, string> row, string key)
        {
            var value = Parse(MasterDataRuntimeCatalog.Value(row, key), key);
            if (value <= 0) throw new InvalidOperationException("Ongeldige positieve waarde voor " + key + ".");
            return value;
        }

        private static double NonNegative(string value, string key)
        {
            var parsed = Parse(value, key);
            if (parsed < 0) throw new InvalidOperationException("Ongeldige niet-negatieve waarde voor " + key + ".");
            return parsed;
        }

        private static double Parse(string value, string key)
        {
            double parsed;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidOperationException("Ongeldige numerieke masterdata voor " + key + ": " + value + ".");
            return parsed;
        }

        private static string Required(string value, string key)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Masterdata ontbreekt voor " + key + ".");
            return value.Trim();
        }
    }
}
