using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class WorkbenchCabinetBackPanelSettings
    {
        public const string GrooveRuleId = "PRD-WBK-012";
        public const string FastenerRuleId = "PRD-WBK-013";

        public double GrooveDepthMm { get; private set; }
        public double GrooveClearanceMm { get; private set; }
        public double FastenerEndInsetMm { get; private set; }
        public double FastenerMaxSpacingMm { get; private set; }
        public string SourcePath { get; private set; }

        public static WorkbenchCabinetBackPanelSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var rules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), "werkbankkast", StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Open", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var groove = RequiredRule(rules, GrooveRuleId);
            var fastener = RequiredRule(rules, FastenerRuleId);
            var grooveValues = RequiredNumbers(groove, GrooveRuleId, 2);
            var fastenerValues = RequiredNumbers(fastener, FastenerRuleId, 2);

            if (!string.Equals(MasterDataRuntimeCatalog.Value(fastener, "Recept-ID"), "CON_WOOD_EDGE", StringComparison.OrdinalIgnoreCase)
                || !MasterDataRuntimeCatalog.Value(fastener, "Referentie-ID(s)").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(value => string.Equals(value.Trim(), "WOODSCREW_4", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(FastenerRuleId + " moet naar CON_WOOD_EDGE en WOODSCREW_4 verwijzen.");
            if (grooveValues[0] <= 0 || grooveValues[1] < 0)
                throw new InvalidOperationException(GrooveRuleId + " bevat een ongeldige groefdiepte of passingsruimte.");
            if (fastenerValues[0] <= 0 || fastenerValues[1] <= 0)
                throw new InvalidOperationException(FastenerRuleId + " bevat een ongeldige eindinset of maximale gatafstand.");

            return new WorkbenchCabinetBackPanelSettings
            {
                GrooveDepthMm = grooveValues[0],
                GrooveClearanceMm = grooveValues[1],
                FastenerEndInsetMm = fastenerValues[0],
                FastenerMaxSpacingMm = fastenerValues[1],
                SourcePath = catalog.SourcePath
            };
        }

        private static Dictionary<string, string> RequiredRule(IEnumerable<Dictionary<string, string>> rules, string ruleId)
        {
            var matches = rules.Where(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Regel-ID"), ruleId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Werkbankkast-masterdata vereist precies één actieve productregel " + ruleId + ".");
            return matches[0];
        }

        private static double[] RequiredNumbers(Dictionary<string, string> rule, string ruleId, int count)
        {
            var values = MasterDataRuntimeCatalog.Value(rule, "Waarde")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => ParseNumber(value, ruleId)).ToArray();
            if (values.Length != count)
                throw new InvalidOperationException(ruleId + " vereist precies " + count.ToString(CultureInfo.InvariantCulture) + " numerieke waarden.");
            return values;
        }

        private static double ParseNumber(string text, string ruleId)
        {
            double value;
            if (!double.TryParse((text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !double.TryParse((text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.GetCultureInfo("nl-NL"), out value))
                throw new InvalidOperationException(ruleId + " bevat een ongeldige numerieke waarde: " + text + ".");
            return value;
        }
    }
}
