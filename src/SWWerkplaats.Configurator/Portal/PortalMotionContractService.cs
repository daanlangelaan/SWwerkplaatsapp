using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalMotionContractService
    {
        public PortalMotionContract Build(WorkbenchModel model, PortalQuoteRequest request, IList<PortalAssemblyPart> parts)
        {
            if (IsHeightAdjustableWorkbench(request)) return BuildHeightAdjustableWorkbench(model, request, parts);
            if (!IsLex(request)) return null;
            if (model == null) throw new ArgumentNullException("model");

            var config = string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)
                ? new PortalConfigurationFactory().BuildLexRevolutionWorkbench(request)
                : new PortalConfigurationFactory().BuildLexWorkbench(request);
            var lockPositions = model.AssemblyPlacements
                .Where(item => item != null && (item.PartName ?? string.Empty).IndexOf("borgpositie", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.Xmm)
                .OrderBy(value => value)
                .ToArray();
            if (lockPositions.Length < 2)
                throw new InvalidOperationException("De horizontale bewegingsgrenzen ontbreken in de LEX-assemblydata.");

            var horizontalMin = lockPositions.First();
            var horizontalMax = lockPositions.Last();
            var minimumWorkHeight = ProductDefaults.LexWorkbenchHeightMm;
            var top = (parts ?? new List<PortalAssemblyPart>()).FirstOrDefault(item =>
                item != null && (item.Name ?? string.Empty).IndexOf("kogelpotblad", StringComparison.OrdinalIgnoreCase) >= 0);
            var supports = (parts ?? new List<PortalAssemblyPart>()).Where(item =>
                item != null && (item.Name ?? string.Empty).StartsWith("Voetprofiel ", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (top == null || supports.Length == 0)
                throw new InvalidOperationException("Werkblad of vaste pootprofielen ontbreken voor het LEX-bewegingscontract.");

            var supportMin = supports.Min(item => item.Xmm - item.SizeXmm / 2.0);
            var supportMax = supports.Max(item => item.Xmm + item.SizeXmm / 2.0);
            var maximumOverhang = Math.Max(
                supportMin - (top.Xmm + horizontalMin - top.SizeXmm / 2.0),
                top.Xmm + horizontalMax + top.SizeXmm / 2.0 - supportMax);

            return new PortalMotionContract
            {
                Horizontal = new PortalMotionAxis
                {
                    Id = "worktop-horizontal",
                    Label = "Bladpositie",
                    Unit = "mm",
                    Minimum = horizontalMin,
                    Maximum = horizontalMax,
                    DefaultValue = 0,
                    Step = Math.Max(1, (horizontalMax - horizontalMin) / 44.0),
                    ReferenceValueMm = 0
                },
                Vertical = new PortalMotionAxis
                {
                    Id = "work-height",
                    Label = "Werkhoogte",
                    Unit = "mm",
                    Minimum = minimumWorkHeight - config.HeightMm,
                    Maximum = minimumWorkHeight + config.LiftColumn.StrokeMm - config.HeightMm,
                    DefaultValue = 0,
                    Step = Math.Max(1, config.LiftColumn.StrokeMm / 80.0),
                    ReferenceValueMm = config.HeightMm
                },
                WorktopWidthMm = top.SizeXmm,
                FixedSupportOuterWidthMm = supportMax - supportMin,
                MaximumOverhangMm = maximumOverhang
            };
        }

        private static PortalMotionContract BuildHeightAdjustableWorkbench(WorkbenchModel model, PortalQuoteRequest request, IList<PortalAssemblyPart> parts)
        {
            if (model == null) throw new ArgumentNullException("model");
            var config = new PortalConfigurationFactory().BuildHeightAdjustableWorkbench(request);
            var master = HeightAdjustableWorkbenchMasterDataSettings.LoadRequired();
            var top = (parts ?? new List<PortalAssemblyPart>()).FirstOrDefault(item => item != null
                && string.Equals(item.Name, "Vast werkblad", StringComparison.OrdinalIgnoreCase));
            var supports = (parts ?? new List<PortalAssemblyPart>()).Where(item => item != null
                && (item.Name ?? string.Empty).StartsWith("Voetprofiel ", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (top == null || supports.Length == 0)
                throw new InvalidOperationException("Vast werkblad of voetprofielen ontbreken voor het hoogtecontract.");

            var supportMin = supports.Min(item => item.Xmm - item.SizeXmm / 2.0);
            var supportMax = supports.Max(item => item.Xmm + item.SizeXmm / 2.0);
            return new PortalMotionContract
            {
                Horizontal = null,
                Vertical = new PortalMotionAxis
                {
                    Id = "work-height",
                    Label = "Werkhoogte",
                    Unit = "mm",
                    Minimum = master.MinimumHeightMm - config.HeightMm,
                    Maximum = master.MaximumHeightMm - config.HeightMm,
                    DefaultValue = 0,
                    Step = Math.Max(1, (master.MaximumHeightMm - master.MinimumHeightMm) / 72.0),
                    ReferenceValueMm = config.HeightMm
                },
                WorktopWidthMm = top.SizeXmm,
                FixedSupportOuterWidthMm = supportMax - supportMin,
                MaximumOverhangMm = Math.Max(top.SizeXmm / 2.0 + supportMin, top.SizeXmm / 2.0 - supportMax)
            };
        }

        public static void ApplyPartMotionMetadata(IEnumerable<PortalAssemblyPart> parts)
        {
            foreach (var part in parts ?? Enumerable.Empty<PortalAssemblyPart>())
            {
                if (part == null) continue;
                var name = (part.Name ?? string.Empty).ToLowerInvariant();

                if (name == "hte2 kolom")
                {
                    part.MotionTranslateYPerMm = 0.5;
                    part.MotionSizeYPerMm = 1;
                }
                else if (!IsFixedLowerPart(name))
                {
                    part.MotionTranslateYPerMm = 1;
                }

                if (IsHorizontalStagePart(name, part.AppearanceRole))
                    // Het operator-vooraanzicht spiegelt model-X. Een negatieve modelsprong
                    // laat een negatieve sliderwaarde daarom zichtbaar naar links bewegen.
                    part.MotionTranslateXPerMm = -1;
            }
        }

        private static bool IsFixedLowerPart(string name)
        {
            return name.Contains("voetprofiel")
                || name.Contains("stelvoet")
                || name.Contains("stelpoot")
                || name.Contains("hoekadapter")
                || name.Contains("stellfußsockel")
                || name.Contains("onderplaat")
                || name.Contains("stabilisatieplaat");
        }

        private static bool IsHorizontalStagePart(string name, string appearanceRole)
        {
            return name.Contains("kogelpot")
                || name.Contains("bewegend buitenframe")
                || name.Contains("werkbladhouder")
                || name.StartsWith("hsr15 rail")
                || name.Contains("mechanische eindstop")
                || name.Contains("schwenkriegel")
                || name.Contains("afdekkap bewegend")
                || string.Equals(appearanceRole, "moving-frame", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLex(PortalQuoteRequest request)
        {
            return request != null &&
                (string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsHeightAdjustableWorkbench(PortalQuoteRequest request)
        {
            return request != null && string.Equals(request.Product, "hoogteverstelbare_werktafel", StringComparison.OrdinalIgnoreCase);
        }
    }
}
