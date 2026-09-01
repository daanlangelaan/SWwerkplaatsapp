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
            if (IsFoldingWorkbench(request)) return BuildFoldingWorkbench(model, request, parts);
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

        private static PortalMotionContract BuildFoldingWorkbench(WorkbenchModel model, PortalQuoteRequest request, IList<PortalAssemblyPart> parts)
        {
            if (model == null) throw new ArgumentNullException("model");
            var config = new PortalConfigurationFactory().BuildFoldingWorkbench(request);
            var top = (parts ?? new List<PortalAssemblyPart>()).FirstOrDefault(item => item != null
                && string.Equals(item.Name, "Uitneembaar werkblad", StringComparison.OrdinalIgnoreCase));
            if (top == null) throw new InvalidOperationException("Uitneembaar werkblad ontbreekt voor het vouwbewegingscontract.");

            return new PortalMotionContract
            {
                Horizontal = new PortalMotionAxis
                {
                    Id = "underframe-fold",
                    Label = "Onderstel uitvouwen",
                    Unit = "%",
                    Minimum = 0,
                    Maximum = 1,
                    DefaultValue = 1,
                    Step = 0.01,
                    ReferenceValueMm = 0,
                    DisplayKind = "fold-fraction"
                },
                Vertical = new PortalMotionAxis
                {
                    Id = "worktop-drop",
                    Label = "Blad laten zakken",
                    Unit = "mm",
                    Minimum = 0,
                    Maximum = config.WorktopFloatMm,
                    DefaultValue = config.WorktopFloatMm,
                    Step = Math.Max(1, config.WorktopFloatMm / 60.0),
                    ReferenceValueMm = 0,
                    DisplayKind = "clearance"
                },
                WorktopWidthMm = top.SizeXmm,
                FixedSupportOuterWidthMm = config.LengthMm - 2.0 * config.UnderframeInsetShortEdgeMm,
                MaximumOverhangMm = config.UnderframeInsetShortEdgeMm
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
            var all = (parts ?? Enumerable.Empty<PortalAssemblyPart>()).Where(item => item != null).ToArray();
            if (all.Any(item => string.Equals(item.Name, "Uitneembaar werkblad", StringComparison.OrdinalIgnoreCase)))
            {
                ApplyFoldingWorkbenchMotion(all);
                return;
            }
            foreach (var part in all)
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

        private static void ApplyFoldingWorkbenchMotion(IEnumerable<PortalAssemblyPart> parts)
        {
            var master = FoldingWorkbenchMasterDataSettings.LoadRequired();
            var frontLongPanel = parts.Single(item => string.Equals(item.Name, "Langspaneel voor", StringComparison.OrdinalIgnoreCase));
            var rearLongPanel = parts.Single(item => string.Equals(item.Name, "Langspaneel achter", StringComparison.OrdinalIgnoreCase));
            var top = parts.Single(item => string.Equals(item.Name, "Uitneembaar werkblad", StringComparison.OrdinalIgnoreCase));
            var span = rearLongPanel.Zmm - frontLongPanel.Zmm;
            var thickness = top.SizeYmm;
            var closedGap = Math.Min(span - 1.0, 4.0 * thickness + master.FoldedClearanceMm);
            var maximumAngle = Math.Acos(closedGap / span);
            var half = span / 2.0;
            var frontZ = frontLongPanel.Zmm;

            foreach (var part in parts)
            {
                var name = (part.Name ?? string.Empty).ToLowerInvariant();
                if (name == "uitneembaar werkblad")
                {
                    part.MotionTranslateYPerMm = 1;
                    continue;
                }
                if (name.StartsWith("scharnier")) continue;

                var isLeft = name.Contains(" links ") || name.StartsWith("vouwpaneel links") || name.StartsWith("scharnier links");
                var isRight = name.Contains(" rechts ") || name.StartsWith("vouwpaneel rechts") || name.StartsWith("scharnier rechts");
                if (!isLeft && !isRight && name != "langspaneel achter") continue;
                // Beide korte zijden vouwen naar binnen, zodat de vier paneelhelften
                // binnen de tafellengte als een vlak pakket tussen de langspanelen vallen.
                var direction = isLeft ? 1.0 : -1.0;
                var baseX = part.Xmm;

                for (var index = 0; index <= 20; index++)
                {
                    var value = index / 20.0;
                    var angle = (1.0 - value) * maximumAngle;
                    var sin = Math.Sin(angle);
                    var cos = Math.Cos(angle);
                    var rearZ = frontZ + span * cos;
                    var frame = new PortalAssemblyMotionKeyframe
                    {
                        Value = value,
                        Xmm = part.Xmm,
                        Ymm = part.Ymm,
                        Zmm = part.Zmm,
                        RotationXDeg = part.RotationXDeg,
                        RotationYDeg = part.RotationYDeg,
                        RotationZDeg = part.RotationZDeg
                    };

                    if (name == "langspaneel achter") frame.Zmm = rearZ;
                    else if (name.StartsWith("vouwpaneel") && name.EndsWith(" voor"))
                    {
                        frame.Xmm = baseX + direction * half * sin / 2.0;
                        frame.Zmm = frontZ + half * cos / 2.0;
                        frame.RotationYDeg = direction * angle * 180.0 / Math.PI;
                    }
                    else if (name.StartsWith("vouwpaneel") && name.EndsWith(" achter"))
                    {
                        frame.Xmm = baseX + direction * half * sin / 2.0;
                        frame.Zmm = frontZ + 3.0 * half * cos / 2.0;
                        frame.RotationYDeg = -direction * angle * 180.0 / Math.PI;
                    }
                    else continue;

                    part.HorizontalMotionKeyframes.Add(frame);
                }
            }

            ApplyRigidComponentMotion(parts);
        }

        private static void ApplyRigidComponentMotion(IEnumerable<PortalAssemblyPart> parts)
        {
            var all = parts.ToArray();
            foreach (var part in all.Where(value => !string.IsNullOrWhiteSpace(value.RigidMotionDriverName)))
            {
                var driver = all.Single(value => string.Equals(value.Name, part.RigidMotionDriverName, StringComparison.OrdinalIgnoreCase));
                if (driver.HorizontalMotionKeyframes.Count == 0) continue;
                var baseOffsetX = part.Xmm - driver.Xmm;
                var baseOffsetZ = part.Zmm - driver.Zmm;
                foreach (var driverFrame in driver.HorizontalMotionKeyframes)
                {
                    var delta = (driverFrame.RotationYDeg - driver.RotationYDeg) * Math.PI / 180.0;
                    var cos = Math.Cos(delta);
                    var sin = Math.Sin(delta);
                    part.HorizontalMotionKeyframes.Add(new PortalAssemblyMotionKeyframe
                    {
                        Value = driverFrame.Value,
                        Xmm = driverFrame.Xmm + baseOffsetX * cos + baseOffsetZ * sin,
                        Ymm = driverFrame.Ymm + (part.Ymm - driver.Ymm),
                        Zmm = driverFrame.Zmm - baseOffsetX * sin + baseOffsetZ * cos,
                        RotationXDeg = part.RotationXDeg + (driverFrame.RotationXDeg - driver.RotationXDeg),
                        RotationYDeg = part.RotationYDeg + (driverFrame.RotationYDeg - driver.RotationYDeg),
                        RotationZDeg = part.RotationZDeg + (driverFrame.RotationZDeg - driver.RotationZDeg)
                    });
                }
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

        private static bool IsFoldingWorkbench(PortalQuoteRequest request)
        {
            return request != null && string.Equals(request.Product, "opvouwbare_werktafel", StringComparison.OrdinalIgnoreCase);
        }
    }
}
