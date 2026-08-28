using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class SwingLatchCatalog
    {
        private const string ComponentId = "item_schwenkriegel_8_pi_54_pa_70081";
        private readonly MasterDataRuntimeCatalog masterData;

        public SwingLatchCatalog() : this(MasterDataRuntimeCatalog.LoadRequired()) { }

        internal SwingLatchCatalog(MasterDataRuntimeCatalog masterData)
        {
            this.masterData = masterData ?? throw new ArgumentNullException("masterData");
        }

        public SwingLatchTemplate LoadRequired()
        {
            var row = masterData.Records("components").FirstOrDefault(record =>
                string.Equals(MasterDataRuntimeCatalog.Value(record, "Component-ID"), ComponentId, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Draaibare aanslag ontbreekt in runtime-masterdata: " + ComponentId);
            var status = MasterDataRuntimeCatalog.Value(row, "Renderstatus").Trim();
            var openData = MasterDataRuntimeCatalog.Value(row, "Open renderdata").Trim();
            if (status.Length == 0) throw new InvalidOperationException("Renderstatus ontbreekt voor " + ComponentId + ".");
            if (status.IndexOf("ProvisionalRenderEnvelope", StringComparison.OrdinalIgnoreCase) >= 0 && openData.Length == 0)
                throw new InvalidOperationException("Open renderdata ontbreekt voor voorlopige renderenvelop " + ComponentId + ".");
            return new SwingLatchTemplate
            {
                Id = ComponentId,
                Name = RequiredText(row, "Naam"),
                ArticleNumber = RequiredText(row, "Norm/artikel"),
                SupplierUrl = RequiredText(row, "Bron"),
                OverallLengthMm = RequiredDouble(row, "Renderlengte mm"),
                WidthMm = RequiredDouble(row, "Renderbreedte mm"),
                OverallProjectionMm = RequiredDouble(row, "Renderprojectie mm"),
                MountingBaseDiameterMm = RequiredDouble(row, "Montagevoet Ø mm"),
                BaseProjectionMm = RequiredDouble(row, "Basisprojectie mm"),
                ThreadDiameterMm = RequiredDouble(row, "Draad Ø mm"),
                RotationStepDeg = RequiredDouble(row, "Draaistap °"),
                HexKeyAcrossFlatsMm = RequiredDouble(row, "Inbus SW mm"),
                WeightGrams = RequiredDouble(row, "Gewicht g"),
                RenderStatus = status,
                OpenRenderData = openData
            };
        }

        private static string RequiredText(Dictionary<string, string> row, string field)
        {
            var value = MasterDataRuntimeCatalog.Value(row, field).Trim();
            if (value.Length == 0) throw new InvalidOperationException("Masterdata mist " + field + " voor " + ComponentId + ".");
            return value;
        }

        private static double RequiredDouble(Dictionary<string, string> row, string field)
        {
            double value;
            if (!double.TryParse(MasterDataRuntimeCatalog.Value(row, field), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Masterdata mist een positieve numerieke waarde voor " + field + " bij " + ComponentId + ".");
            return value;
        }
    }
}
