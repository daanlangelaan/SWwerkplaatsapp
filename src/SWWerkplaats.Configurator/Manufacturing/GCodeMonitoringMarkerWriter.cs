using System;
using System.Globalization;
using System.Text;

namespace SWWerkplaats.Configurator.Manufacturing
{
    internal static class GCodeMonitoringMarkerWriter
    {
        public const int ContractVersion = 1;

        public static void AppendProgramMetadata(
            StringBuilder sb,
            bool enabled,
            string project,
            int? plateNumber,
            int? plateCount,
            double safeZmm)
        {
            if (!enabled) return;

            AppendMeta(sb, "CONTRACT", ContractVersion.ToString(CultureInfo.InvariantCulture));
            AppendMeta(sb, "PROJECT", project);
            if (plateNumber.HasValue) AppendMeta(sb, "PLATE", plateNumber.Value.ToString(CultureInfo.InvariantCulture));
            if (plateCount.HasValue) AppendMeta(sb, "PLATE_COUNT", plateCount.Value.ToString(CultureInfo.InvariantCulture));
            AppendMeta(sb, "SAFE_Z", safeZmm.ToString("0.###", CultureInfo.InvariantCulture));
        }

        public static void AppendStep(StringBuilder sb, bool enabled, int number, string name)
        {
            if (!enabled) return;
            sb.AppendLine("(RD_STEP: " + number.ToString("00", CultureInfo.InvariantCulture) + " " + Sanitize(name) + ")");
        }

        public static void AppendEvent(StringBuilder sb, bool enabled, string name)
        {
            if (!enabled) return;
            sb.AppendLine("(RD_EVENT: " + Sanitize(name) + ")");
        }

        public static void AppendToolChangeEvent(StringBuilder sb, bool enabled, int toolNumber, string label)
        {
            if (!enabled) return;
            sb.AppendLine("(RD_EVENT: TOOL_CHANGE; TOOL="
                + toolNumber.ToString(CultureInfo.InvariantCulture)
                + "; LABEL=" + Sanitize(label) + ")");
        }

        private static void AppendMeta(StringBuilder sb, string key, string value)
        {
            sb.AppendLine("(RD_META: " + key + "=" + Sanitize(value) + ")");
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "ONBEKEND";

            var result = value
                .Replace('(', ' ')
                .Replace(')', ' ')
                .Replace(';', ',')
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            while (result.Contains("  ")) result = result.Replace("  ", " ");
            return result.Length == 0 ? "ONBEKEND" : result;
        }
    }
}
