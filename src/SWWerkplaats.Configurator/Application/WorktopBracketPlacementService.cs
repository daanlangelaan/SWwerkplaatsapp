using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public enum WorktopSupportAxis { X, Z }

    public sealed class WorktopBracketPlacementService
    {
        public const string ComponentId = "techxxl_mounting_bracket_8_40x40x20_zn";
        public const string ArticleNumber = "TIN 100391 / S200BW404020ZN";

        public int AddSymmetricPairs(
            WorkbenchModel model,
            string productId,
            IEnumerable<AssemblyPlacement> supports,
            WorktopSupportAxis axis,
            double worktopUndersideYmm,
            string placementNamePrefix)
        {
            if (model == null) throw new ArgumentNullException("model");
            var specification = LoadRequired(productId);
            var source = (supports ?? Enumerable.Empty<AssemblyPlacement>()).ToArray();
            if (source.Length == 0) throw new InvalidOperationException("Geen draagprofielen gevonden voor werkbladbeugels.");

            var index = 0;
            foreach (var support in source)
            {
                var length = axis == WorktopSupportAxis.X ? support.LengthMm : support.WidthMm;
                if (length < 2.0 * specification.LegLengthMm)
                    throw new InvalidOperationException("Draagprofiel is te kort voor twee symmetrische werkbladbeugels: " + support.PartName + ".");
                var offset = length / 2.0 - specification.LegLengthMm;
                foreach (var direction in new[] { -1.0, 1.0 })
                {
                    var x = support.Xmm + (axis == WorktopSupportAxis.X ? direction * offset : 0);
                    var z = support.Zmm + (axis == WorktopSupportAxis.Z ? direction * offset : 0);
                    model.AssemblyPlacements.Add(new AssemblyPlacement
                    {
                        Kind = AssemblyComponentKind.Purchased,
                        PartName = placementNamePrefix + " " + (++index).ToString(CultureInfo.InvariantCulture),
                        ComponentId = specification.ComponentId,
                        LengthMm = specification.LegLengthMm,
                        HeightMm = specification.LegLengthMm,
                        WidthMm = specification.WidthMm,
                        Xmm = x,
                        Ymm = worktopUndersideYmm - specification.LegThicknessMm / 2.0,
                        Zmm = z,
                        RotationYDeg = (axis == WorktopSupportAxis.Z ? 90 : 0) + (direction > 0 ? 180 : 0),
                        VisualKind = "hardware-bracket",
                        Shape = "component-primitives"
                    });
                }
            }
            return index;
        }

        private static WorktopBracketSpecification LoadRequired(string productId)
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var rule = catalog.Records("productRules").SingleOrDefault(row =>
                string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), productId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Werkbladbeugel", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase));
            if (rule == null) throw new InvalidOperationException("Werkbladbeugelregel ontbreekt voor " + productId + ".");
            var componentId = MasterDataRuntimeCatalog.Value(rule, "Referentie-ID(s)").Trim();
            var component = catalog.Records("components").SingleOrDefault(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Component-ID"), componentId, StringComparison.OrdinalIgnoreCase));
            if (component == null) throw new InvalidOperationException("Werkbladbeugel ontbreekt in componentmasterdata: " + componentId + ".");
            var render = new ComponentPrimitiveRenderContractService().BuildRequired(componentId);
            var thickness = render.Primitives.SelectMany(item => new[] { item.SizeXmm, item.SizeYmm, item.SizeZmm })
                .Where(value => value > 0).Min();
            return new WorktopBracketSpecification
            {
                ComponentId = componentId,
                LegLengthMm = Positive(component, "Renderlengte mm"),
                WidthMm = Positive(component, "Renderbreedte mm"),
                LegThicknessMm = thickness
            };
        }

        private static double Positive(Dictionary<string, string> row, string field)
        {
            double value;
            if (!double.TryParse(MasterDataRuntimeCatalog.Value(row, field), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Werkbladbeugel mist positieve masterdatawaarde: " + field + ".");
            return value;
        }

        private sealed class WorktopBracketSpecification
        {
            public string ComponentId { get; set; }
            public double LegLengthMm { get; set; }
            public double WidthMm { get; set; }
            public double LegThicknessMm { get; set; }
        }
    }
}
