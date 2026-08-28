using System;
using System.Collections.Generic;
using System.Globalization;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CamMasterSettings
    {
        public const string ThroughCutOvertravelKey = "cam_through_cut_overtravel_mm";
        public const string TabWidthKey = "cam_tab_width_mm";
        public const string TabHeightKey = "cam_tab_height_mm";
        public const string SafeTravelZKey = "cam_safe_travel_z_mm";
        public const string ContourOnionSkinKey = "cam_contour_onion_skin_mm";
        public const string FinalContourFeedRateKey = "cam_final_contour_feed_mm_min";
        public const string FinalContourRampLengthKey = "cam_final_contour_ramp_length_mm";

        public double ThroughCutOvertravelMm { get; private set; }
        public double TabWidthMm { get; private set; }
        public double TabHeightMm { get; private set; }
        public double SafeTravelZMm { get; private set; }
        public double ContourOnionSkinMm { get; private set; }
        public double FinalContourFeedRateMmMin { get; private set; }
        public double FinalContourRampLengthMm { get; private set; }
        public string SourcePath { get; private set; }

        public static CamMasterSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var values = ReadParameters(catalog);
            return new CamMasterSettings
            {
                ThroughCutOvertravelMm = RequiredValue(values, ThroughCutOvertravelKey, 0, 5),
                TabWidthMm = RequiredValue(values, TabWidthKey, 0.1, 100),
                TabHeightMm = RequiredValue(values, TabHeightKey, 0.1, 20),
                SafeTravelZMm = RequiredValue(values, SafeTravelZKey, 1, 100),
                ContourOnionSkinMm = RequiredValue(values, ContourOnionSkinKey, 0.1, 5),
                FinalContourFeedRateMmMin = RequiredValue(values, FinalContourFeedRateKey, 100, 10000),
                FinalContourRampLengthMm = RequiredValue(values, FinalContourRampLengthKey, 1, 500),
                SourcePath = catalog.SourcePath
            };
        }

        public void ApplyTo(Manufacturing.CamJobOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            options.ThroughCutOvertravelMm = ThroughCutOvertravelMm;
            options.TabWidthMm = TabWidthMm;
            options.TabHeightMm = TabHeightMm;
            options.SafeTravelZMm = SafeTravelZMm;
            options.ContourOnionSkinMm = ContourOnionSkinMm;
            options.FinalContourFeedRateMmMin = FinalContourFeedRateMmMin;
            options.FinalContourRampLengthMm = FinalContourRampLengthMm;
        }

        private static Dictionary<string, double> ReadParameters(MasterDataRuntimeCatalog catalog)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in catalog.Records("camParameters"))
            {
                var key = MasterDataRuntimeCatalog.Value(record, "Sleutel").Trim();
                var text = MasterDataRuntimeCatalog.Value(record, "Waarde");
                double value;
                if (!string.IsNullOrWhiteSpace(key) && TryDouble(text, out value)) result[key] = value;
            }
            return result;
        }

        private static double RequiredValue(Dictionary<string, double> values, string key, double minimum, double maximum)
        {
            double value;
            if (!values.TryGetValue(key, out value))
                throw new InvalidOperationException("CAM-masterparameter ontbreekt of is niet numeriek: " + key + ".");
            if (value < minimum || value > maximum)
                throw new InvalidOperationException("CAM-masterparameter " + key + " valt buiten het veilige bereik " + minimum.ToString(CultureInfo.InvariantCulture) + ".." + maximum.ToString(CultureInfo.InvariantCulture) + " mm.");
            return value;
        }

        private static bool TryDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                || double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("nl-NL"), out result);
        }
    }
}
