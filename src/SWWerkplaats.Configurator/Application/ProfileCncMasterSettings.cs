using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public static class ProfileCncMasterSettings
    {
        public const string ContractIdKey = "profile_cnc_contract_id";
        public const string SpindleRpmKey = "profile_cnc_spindle_rpm";
        public const string SpindleSpinUpSecondsKey = "profile_cnc_spindle_spinup_seconds";
        public const string SafeParkZKey = "profile_cnc_safe_park_z_mm";
        public const string SafeParkYKey = "profile_cnc_safe_park_y_mm";
        public const string ClearanceKey = "profile_cnc_clearance_above_profile_mm";
        public const string SurfaceBreakthroughKey = "profile_cnc_surface_breakthrough_mm";
        public const string ThroughOvertravelKey = "profile_cnc_through_overtravel_mm";
        public const string SurfaceFeedKey = "profile_cnc_surface_feed_mm_min";
        public const string DrillFeedKey = "profile_cnc_drill_feed_mm_min";
        public const string ValidatedProfileTypesKey = "profile_cnc_validated_profile_types";
        public const string X0AnchorRuleKey = "profile_cnc_x0_anchor_rule";
        public const string RollDirectionKey = "profile_cnc_roll_direction";

        public static ProfileCncMachineSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var records = catalog.Records("camParameters")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Actief", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(row => MasterDataRuntimeCatalog.Value(row, "Sleutel").Trim(), StringComparer.OrdinalIgnoreCase);
            var result = new ProfileCncMachineSettings
            {
                ContractId = Text(records, ContractIdKey),
                SpindleRpm = Number(records, SpindleRpmKey, 100, 50000),
                SpindleSpinUpSeconds = Number(records, SpindleSpinUpSecondsKey, 0.1, 300),
                SafeParkZMm = Number(records, SafeParkZKey, 1, 1000),
                SafeParkYMm = Number(records, SafeParkYKey, 1, 5000),
                ClearanceAboveProfileMm = Number(records, ClearanceKey, 0.1, 500),
                SurfaceBreakthroughMm = Number(records, SurfaceBreakthroughKey, 0.1, 50),
                ThroughOvertravelMm = Number(records, ThroughOvertravelKey, 0.1, 20),
                SurfaceFeedMmMin = Number(records, SurfaceFeedKey, 1, 10000),
                DrillFeedMmMin = Number(records, DrillFeedKey, 1, 10000),
                X0AnchorRule = Text(records, X0AnchorRuleKey),
                RollDirectionRule = Text(records, RollDirectionKey),
                SourcePath = catalog.SourcePath
            };
            foreach (var profileType in Text(records, ValidatedProfileTypesKey)
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim().ToUpperInvariant()).Where(value => value.Length > 0).Distinct())
                result.ValidatedProfileTypes.Add(profileType);
            result.EnsureValid();
            return result;
        }

        private static string Text(IDictionary<string, Dictionary<string, string>> records, string key)
        {
            Dictionary<string, string> row;
            if (!records.TryGetValue(key, out row))
                throw new InvalidOperationException("Actieve CAM-masterparameter ontbreekt: " + key + ".");
            var value = MasterDataRuntimeCatalog.Value(row, "Waarde").Trim();
            if (value.Length == 0) throw new InvalidOperationException("CAM-masterparameter heeft geen waarde: " + key + ".");
            return value;
        }

        private static double Number(IDictionary<string, Dictionary<string, string>> records, string key, double minimum, double maximum)
        {
            var text = Text(records, key);
            double value;
            if ((!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                    && !double.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("nl-NL"), out value))
                || value < minimum || value > maximum)
                throw new InvalidOperationException("CAM-masterparameter " + key + " valt buiten het veilige bereik "
                    + minimum.ToString(CultureInfo.InvariantCulture) + ".." + maximum.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }
    }
}
