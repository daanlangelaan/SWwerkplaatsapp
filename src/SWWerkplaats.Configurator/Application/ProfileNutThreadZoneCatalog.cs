using System;
using System.Collections.Generic;
using System.Globalization;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>Typed access to supplier-confirmed receiving thread zones in runtime masterdata.</summary>
    public sealed class ProfileNutThreadZoneCatalog
    {
        private readonly Dictionary<string, ProfileNutThreadZone> values;

        private ProfileNutThreadZoneCatalog(Dictionary<string, ProfileNutThreadZone> values)
        {
            this.values = values;
        }

        public static ProfileNutThreadZoneCatalog LoadRequired()
        {
            var runtime = MasterDataRuntimeCatalog.LoadRequired();
            var values = new Dictionary<string, ProfileNutThreadZone>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in runtime.Records("components"))
            {
                var componentId = MasterDataRuntimeCatalog.Value(record, "Component-ID").Trim();
                if (string.IsNullOrWhiteSpace(componentId)) continue;

                double diameter;
                double usableZone;
                double threadInletOffset;
                if (!TryParsePositive(MasterDataRuntimeCatalog.Value(record, "Draad Ø mm"), out diameter)
                    || !TryParsePositive(MasterDataRuntimeCatalog.Value(record, "Bruikbare draadzone mm"), out usableZone)
                    || !TryParseNonNegative(MasterDataRuntimeCatalog.Value(record, "Draadinlaat vanaf profielvlak mm"), out threadInletOffset))
                    continue;

                var source = MasterDataRuntimeCatalog.Value(record, "Draadzone-bron").Trim();
                if (string.IsNullOrWhiteSpace(source))
                    throw new InvalidOperationException("Component " + componentId + " heeft een bruikbare draadzone zonder bronverwijzing.");

                values[componentId] = new ProfileNutThreadZone
                {
                    ComponentId = componentId,
                    ThreadDiameterMm = diameter,
                    UsableThreadZoneMm = usableZone,
                    ThreadInletOffsetMm = threadInletOffset,
                    ThroughThread = RequiredBoolean(record, componentId, "Draadgat doorlopend"),
                    Source = source
                };
            }
            return new ProfileNutThreadZoneCatalog(values);
        }

        public ProfileNutThreadZone Required(string componentId, double expectedThreadDiameterMm)
        {
            ProfileNutThreadZone value;
            if (!values.TryGetValue(componentId ?? string.Empty, out value))
                throw new InvalidOperationException("Bruikbare draadzone ontbreekt in runtime-masterdata voor component " + componentId + ".");
            if (Math.Abs(value.ThreadDiameterMm - expectedThreadDiameterMm) > 0.001)
                throw new InvalidOperationException("Draadmaat van component " + componentId + " is "
                    + value.ThreadDiameterMm.ToString("0.###", CultureInfo.InvariantCulture) + " mm; verwacht "
                    + expectedThreadDiameterMm.ToString("0.###", CultureInfo.InvariantCulture) + " mm.");
            return value;
        }

        private static bool TryParsePositive(string raw, out double value)
        {
            raw = (raw ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;
        }

        private static bool TryParseNonNegative(string raw, out double value)
        {
            raw = (raw ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0;
        }

        private static bool RequiredBoolean(Dictionary<string, string> record, string componentId, string field)
        {
            var raw = MasterDataRuntimeCatalog.Value(record, field).Trim();
            if (string.Equals(raw, "Ja", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(raw, "Nee", StringComparison.OrdinalIgnoreCase)) return false;
            throw new InvalidOperationException("Component " + componentId + " mist Ja/Nee in " + field + ".");
        }
    }

    public sealed class ProfileNutThreadZone
    {
        public string ComponentId { get; set; }
        public double ThreadDiameterMm { get; set; }
        public double UsableThreadZoneMm { get; set; }
        public double ThreadInletOffsetMm { get; set; }
        public bool ThroughThread { get; set; }
        public string Source { get; set; }
    }
}
