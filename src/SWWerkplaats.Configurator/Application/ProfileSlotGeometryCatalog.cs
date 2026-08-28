using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileSlotGeometryCatalog
    {
        private readonly MasterDataRuntimeCatalog masterData;

        public ProfileSlotGeometryCatalog()
            : this(MasterDataRuntimeCatalog.LoadRequired())
        {
        }

        internal ProfileSlotGeometryCatalog(MasterDataRuntimeCatalog masterData)
        {
            this.masterData = masterData ?? throw new ArgumentNullException("masterData");
        }

        public ProfileSlotGeometry FindRequired(string materialId)
        {
            var row = masterData.Records("materials").FirstOrDefault(record =>
                string.Equals(MasterDataRuntimeCatalog.Value(record, "Materiaal-ID"), materialId, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Profielmateriaal ontbreekt in masterdata: " + materialId);

            var series = MasterDataRuntimeCatalog.Value(row, "Profielserie").Trim();
            if (series.Length == 0)
                throw new InvalidOperationException("Profielmateriaal " + materialId + " heeft geen vrijgegeven sleufasgeometrie.");

            var width = RequiredDouble(row, "Breedte mm");
            var height = RequiredDouble(row, "Hoogte mm");
            var edgeOffset = RequiredDouble(row, "Sleufas-randafstand mm");
            var pitch = RequiredDouble(row, "Sleufas-raster mm");
            var geometry = new ProfileSlotGeometry
            {
                MaterialId = materialId,
                ProfileSeries = series,
                SlotWidthMm = RequiredDouble(row, "Sleufmaat mm"),
                EdgeOffsetMm = edgeOffset,
                PitchMm = pitch,
                ExpectedPerimeterSlotCount = RequiredInt(row, "Sleufassen rondom"),
                ExpectedCoreHoleCountPerEnd = RequiredInt(row, "Kernboringen per kop"),
                EndTapThread = MasterDataRuntimeCatalog.Value(row, "Kopse tapdraad"),
                Status = MasterDataRuntimeCatalog.Value(row, "Profielgeometrie-status"),
                SlotMouthWidthMm = OptionalDouble(row, "Sleufmondbreedte mm"),
                SlotMouthDepthMm = OptionalDouble(row, "Sleufmond-diepte mm"),
                SlotCavityWidthMm = OptionalDouble(row, "Sleufkamerbreedte mm"),
                SlotCavityDepthMm = OptionalDouble(row, "Sleufkamerdiepte mm"),
                OutsideCornerRadiusMm = OptionalDouble(row, "Buitenradius mm"),
                CoreHoleDiameterMm = OptionalDouble(row, "Kernboringdiameter mm"),
                CoreHoleContour = MasterDataRuntimeCatalog.Value(row, "Kernboringcontour"),
                GeometrySource = MasterDataRuntimeCatalog.Value(row, "Profielgeometrie-bron"),
                OpenGeometryData = MasterDataRuntimeCatalog.Value(row, "Open profielgeometrie"),
                WidthFaceAxisOffsetsMm = AxisOffsets(width, edgeOffset, pitch),
                HeightFaceAxisOffsetsMm = AxisOffsets(height, edgeOffset, pitch)
            };
            if (geometry.CalculatedPerimeterSlotCount != geometry.ExpectedPerimeterSlotCount)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Profielmateriaal {0}: berekend {1} sleufassen rondom, masterdata verwacht {2}.",
                    materialId, geometry.CalculatedPerimeterSlotCount, geometry.ExpectedPerimeterSlotCount));
            if (geometry.CalculatedCoreHoleCountPerEnd != geometry.ExpectedCoreHoleCountPerEnd)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Profielmateriaal {0}: berekend {1} kernboringen per kop, masterdata verwacht {2}.",
                    materialId, geometry.CalculatedCoreHoleCountPerEnd, geometry.ExpectedCoreHoleCountPerEnd));
            return geometry;
        }

        private static IList<double> AxisOffsets(double dimensionMm, double edgeOffsetMm, double pitchMm)
        {
            if (dimensionMm <= 0 || edgeOffsetMm <= 0 || pitchMm <= 0 || dimensionMm + 0.001 < 2 * edgeOffsetMm)
                throw new InvalidOperationException("Ongeldige profielmaat, randafstand of sleufasraster in masterdata.");
            var result = new List<double>();
            for (var offset = edgeOffsetMm; offset <= dimensionMm - edgeOffsetMm + 0.001; offset += pitchMm)
                result.Add(Math.Round(offset, 3));
            return result;
        }

        private static double RequiredDouble(Dictionary<string, string> row, string field)
        {
            double value;
            if (!double.TryParse(MasterDataRuntimeCatalog.Value(row, field), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Masterdata mist geldige numerieke waarde voor " + field + ".");
            return value;
        }

        private static int RequiredInt(Dictionary<string, string> row, string field)
        {
            int value;
            if (!int.TryParse(MasterDataRuntimeCatalog.Value(row, field), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Masterdata mist geldige gehele waarde voor " + field + ".");
            return value;
        }

        private static double? OptionalDouble(Dictionary<string, string> row, string field)
        {
            var raw = MasterDataRuntimeCatalog.Value(row, field);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            double value;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Masterdata bevat geen geldige positieve waarde voor " + field + ".");
            return value;
        }
    }
}
