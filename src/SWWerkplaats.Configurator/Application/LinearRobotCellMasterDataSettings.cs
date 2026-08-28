using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class LinearRobotCellMasterDataSettings
    {
        public double MinimumLengthMm { get; private set; }
        public double MaximumLengthMm { get; private set; }
        public double MinimumWorktopDepthMm { get; private set; }
        public double MaximumWorktopDepthMm { get; private set; }
        public double MinimumWorktopHeightMm { get; private set; }
        public double MaximumWorktopHeightMm { get; private set; }
        public double DefaultLengthMm { get; private set; }
        public double DefaultWorktopDepthMm { get; private set; }
        public double DefaultWorktopHeightMm { get; private set; }
        public int DefaultWorktopSideCount { get; private set; }
        public double DefaultGuardHeightMm { get; private set; }
        public double MinimumGuardHeightMm { get; private set; }
        public double MaximumGuardHeightMm { get; private set; }
        public double DefaultSupportSpacingMm { get; private set; }
        public double MinimumSupportSpacingMm { get; private set; }
        public double MaximumSupportSpacingMm { get; private set; }
        public string UprightProfileId { get; private set; }
        public string FrameBeamProfileId { get; private set; }
        public string RailCarrierProfileId { get; private set; }
        public string GuardProfileId { get; private set; }
        public string WorktopMaterialId { get; private set; }
        public string GuardPanelMaterialId { get; private set; }
        public double[] RailEnvelope { get; private set; }
        public double[] CarriageEnvelope { get; private set; }
        public double[] RobotAdapterEnvelope { get; private set; }
        public double[] MotorAdapterEnvelope { get; private set; }
        public double[] RackEnvelope { get; private set; }
        public double[] SupportEnvelope { get; private set; }
        public double RailZoneWidthMm { get; private set; }
        public double RailCenterSpacingMm { get; private set; }
        public double LightCurtainMountingClearanceMm { get; private set; }
        public bool LowerFrameEndCrossmembersUseOuterLane { get; private set; }
        public int TwoSidedEndWallIntermediatePostCount { get; private set; }
        public int ThroughCornerUprightCount { get; private set; }
        public int TwoSidedCenterSupportRowCount { get; private set; }
        public IReadOnlyList<LightCurtainVariant> LightCurtainVariants { get; private set; }

        public sealed class LightCurtainVariant
        {
            public string SetComponentId { get; set; }
            public string EmitterComponentId { get; set; }
            public string ReceiverComponentId { get; set; }
            public string DisplayName { get; set; }
            public string ArticleNumber { get; set; }
            public double ProtectedHeightMm { get; set; }
            public double OverallHeightMm { get; set; }
            public double WidthMm { get; set; }
            public double DepthMm { get; set; }
        }

        public static LinearRobotCellMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var product = catalog.Records("products").Single(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), "lineaire_robotcel", StringComparison.OrdinalIgnoreCase));
            var rules = catalog.Records("productRules").Where(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), "lineaire_robotcel", StringComparison.OrdinalIgnoreCase)).ToArray();
            var components = catalog.Records("components").ToArray();
            var inputs = catalog.Records("productInputContracts").Where(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), "lineaire_robotcel", StringComparison.OrdinalIgnoreCase)).ToArray();
            var bounds = Split(RequiredRuleValue(rules, "Parametercontract")).Select(ParseBounds).ToArray();
            if (bounds.Length != 3) throw new InvalidOperationException("Lineaire robotcel vereist drie parametergrenzen voor lengte, werkbladdiepte en werkbladhoogte.");

            var guardInput = RequiredInput(inputs, "LinearRobotCellGuardHeightAboveWorktopMm");
            var supportInput = RequiredInput(inputs, "LinearRobotCellIntermediateSupportMaxSpacingMm");
            var sideInput = RequiredInput(inputs, "LinearRobotCellWorktopSideCount");
            return new LinearRobotCellMasterDataSettings
            {
                MinimumLengthMm = bounds[0].Item1,
                MaximumLengthMm = bounds[0].Item2,
                MinimumWorktopDepthMm = bounds[1].Item1,
                MaximumWorktopDepthMm = bounds[1].Item2,
                MinimumWorktopHeightMm = bounds[2].Item1,
                MaximumWorktopHeightMm = bounds[2].Item2,
                DefaultLengthMm = RequiredDouble(product, "Breedte default mm"),
                DefaultWorktopDepthMm = RequiredDouble(product, "Diepte default mm"),
                DefaultWorktopHeightMm = RequiredDouble(product, "Hoogte default mm"),
                DefaultWorktopSideCount = (int)RequiredDouble(sideInput, "Standaardwaarde"),
                DefaultGuardHeightMm = RequiredDouble(guardInput, "Standaardwaarde"),
                MinimumGuardHeightMm = RequiredDouble(guardInput, "Minimum"),
                MaximumGuardHeightMm = RequiredDouble(guardInput, "Maximum"),
                DefaultSupportSpacingMm = RequiredDouble(supportInput, "Standaardwaarde"),
                MinimumSupportSpacingMm = RequiredDouble(supportInput, "Minimum"),
                MaximumSupportSpacingMm = RequiredDouble(supportInput, "Maximum"),
                UprightProfileId = RequiredSingleId(rules, "Staanderprofiel"),
                FrameBeamProfileId = RequiredSingleId(rules, "Frameprofiel"),
                RailCarrierProfileId = RequiredSingleId(rules, "Raildragerprofiel"),
                GuardProfileId = RequiredSingleId(rules, "Topframeprofiel"),
                WorktopMaterialId = RequiredSingleId(rules, "Werkbladmateriaal"),
                GuardPanelMaterialId = RequiredSingleId(rules, "Afschermmateriaal"),
                RailZoneWidthMm = RequiredScalar(rules, "Railzonebreedte"),
                RailCenterSpacingMm = RequiredScalar(rules, "Railhartafstand"),
                RailEnvelope = RequiredTuple(rules, "RailRenderEnvelope", 2),
                CarriageEnvelope = RequiredTuple(rules, "RailwagenRenderEnvelope", 3),
                RobotAdapterEnvelope = RequiredTuple(rules, "RobotAdapterRenderEnvelope", 3),
                MotorAdapterEnvelope = RequiredTuple(rules, "MotorAdapterRenderEnvelope", 3),
                RackEnvelope = RequiredTuple(rules, "TandheugelRenderEnvelope", 2),
                SupportEnvelope = RequiredTuple(rules, "VoetRenderEnvelope", 3),
                LightCurtainMountingClearanceMm = RequiredScalar(rules, "LichtschermMontagespeling"),
                LowerFrameEndCrossmembersUseOuterLane = RequiredOuterLane(rules, "OnderframeKopseDwarsliggerBaan"),
                TwoSidedEndWallIntermediatePostCount = RequiredPositiveInteger(rules, "TweezijdigeKopwandMiddenstaanders"),
                ThroughCornerUprightCount = RequiredPositiveInteger(rules, "DoorlopendeHoekstaanders80x80"),
                TwoSidedCenterSupportRowCount = RequiredPositiveInteger(rules, "TweezijdigeMiddensteunrijen"),
                LightCurtainVariants = RequiredLightCurtainVariants(rules, components)
            };
        }

        public LightCurtainVariant SelectLightCurtain(double guardHeightMm)
        {
            var availableHeight = guardHeightMm - LightCurtainMountingClearanceMm;
            var selected = LightCurtainVariants
                .Where(item => item.OverallHeightMm <= availableHeight + 0.001)
                .OrderByDescending(item => item.ProtectedHeightMm)
                .FirstOrDefault();
            if (selected == null)
                throw new InvalidOperationException("Geen lichtschermvariant past binnen de afschermhoogte en masterdata-montagespeling.");
            return selected;
        }

        private static IReadOnlyList<LightCurtainVariant> RequiredLightCurtainVariants(
            IEnumerable<Dictionary<string, string>> rules, IEnumerable<Dictionary<string, string>> components)
        {
            var result = new List<LightCurtainVariant>();
            foreach (var row in rules.Where(item => string.Equals(MasterDataRuntimeCatalog.Value(item, "Parametertype"), "Lichtschermvariant", StringComparison.OrdinalIgnoreCase)))
            {
                var ids = Split(MasterDataRuntimeCatalog.Value(row, "Referentie-ID(s)")).ToArray();
                var values = Split(MasterDataRuntimeCatalog.Value(row, "Waarde")).Select(value => ParsePositive(value, "Lichtschermvariant")).ToArray();
                if (ids.Length != 3 || values.Length != 4)
                    throw new InvalidOperationException("Iedere Lichtschermvariant vereist set-, zender- en ontvanger-ID plus beschermd veld, huishoogte, breedte en diepte.");
                var set = components.SingleOrDefault(item => string.Equals(MasterDataRuntimeCatalog.Value(item, "Component-ID"), ids[0], StringComparison.OrdinalIgnoreCase));
                if (set == null) throw new InvalidOperationException("Lichtschermvariant verwijst naar ontbrekende component " + ids[0] + ".");
                result.Add(new LightCurtainVariant
                {
                    SetComponentId = ids[0], EmitterComponentId = ids[1], ReceiverComponentId = ids[2],
                    DisplayName = MasterDataRuntimeCatalog.Value(set, "Naam"), ArticleNumber = MasterDataRuntimeCatalog.Value(set, "Norm/artikel"),
                    ProtectedHeightMm = values[0], OverallHeightMm = values[1], WidthMm = values[2], DepthMm = values[3]
                });
            }
            if (result.Count == 0) throw new InvalidOperationException("Lineaire robotcel mist Lichtschermvariant-regels.");
            return result.OrderBy(item => item.ProtectedHeightMm).ToArray();
        }

        private static Dictionary<string, string> RequiredInput(IEnumerable<Dictionary<string, string>> inputs, string requestField)
        {
            var result = inputs.SingleOrDefault(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Request-veld"), requestField, StringComparison.OrdinalIgnoreCase));
            if (result == null) throw new InvalidOperationException("Lineaire robotcel mist invoercontract " + requestField + ".");
            return result;
        }

        private static string RequiredRuleValue(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var row = rules.SingleOrDefault(item => string.Equals(MasterDataRuntimeCatalog.Value(item, "Parametertype"), parameterType, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Lineaire robotcel mist productregel " + parameterType + ".");
            var value = MasterDataRuntimeCatalog.Value(row, "Waarde").Trim();
            if (value.Length == 0) throw new InvalidOperationException("Lineaire robotcel productregel " + parameterType + " mist een waarde.");
            return value;
        }

        private static string RequiredSingleId(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var row = rules.SingleOrDefault(item => string.Equals(MasterDataRuntimeCatalog.Value(item, "Parametertype"), parameterType, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Lineaire robotcel mist materiaalregel " + parameterType + ".");
            var ids = Split(MasterDataRuntimeCatalog.Value(row, "Referentie-ID(s)")).ToArray();
            if (ids.Length != 1) throw new InvalidOperationException("Lineaire robotcel vereist precies één materiaal voor " + parameterType + ".");
            return ids[0];
        }

        private static double RequiredScalar(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            return ParsePositive(RequiredRuleValue(rules, parameterType), parameterType);
        }

        private static double[] RequiredTuple(IEnumerable<Dictionary<string, string>> rules, string parameterType, int count)
        {
            var values = Split(RequiredRuleValue(rules, parameterType)).Select(value => ParsePositive(value, parameterType)).ToArray();
            if (values.Length != count) throw new InvalidOperationException("Lineaire robotcel productregel " + parameterType + " vereist " + count + " waarden.");
            return values;
        }

        private static bool RequiredOuterLane(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var value = RequiredRuleValue(rules, parameterType);
            if (!string.Equals(value, "buiten", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Lineaire robotcel productregel " + parameterType + " vereist waarde 'buiten'.");
            return true;
        }

        private static int RequiredPositiveInteger(IEnumerable<Dictionary<string, string>> rules, string parameterType)
        {
            var value = RequiredScalar(rules, parameterType);
            var integer = (int)Math.Round(value);
            if (Math.Abs(value - integer) > 0.001)
                throw new InvalidOperationException("Lineaire robotcel productregel " + parameterType + " vereist een geheel aantal.");
            return integer;
        }

        private static IEnumerable<string> Split(string value)
        {
            return (value ?? string.Empty).Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).Where(item => item.Length > 0);
        }

        private static Tuple<double, double> ParseBounds(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { ".." }, StringSplitOptions.None);
            if (parts.Length != 2) throw new InvalidOperationException("Ongeldige grens voor lineaire robotcel: " + value + ".");
            var minimum = ParsePositive(parts[0], "minimum");
            var maximum = ParsePositive(parts[1], "maximum");
            if (maximum < minimum) throw new InvalidOperationException("Omgekeerde grens voor lineaire robotcel: " + value + ".");
            return Tuple.Create(minimum, maximum);
        }

        private static double RequiredDouble(Dictionary<string, string> row, string field)
        {
            return ParsePositive(MasterDataRuntimeCatalog.Value(row, field), field);
        }

        private static double ParsePositive(string raw, string field)
        {
            double result;
            if (!double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || result <= 0)
                throw new InvalidOperationException("Lineaire robotcel bevat geen positieve waarde voor " + field + ".");
            return result;
        }
    }
}
