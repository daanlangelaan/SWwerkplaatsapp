using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksAuditedAssemblyResult
    {
        public int ContractVersion { get; set; }
        public bool Ok { get; set; }
        public bool GeometryAuditPassed { get; set; }
        public bool SourceGeometryAuditPassed { get; set; }
        public bool BodyRoundtripAuditPassed { get; set; }
        public bool InterferenceAuditPassed { get; set; }
        public bool ReleaseEligible { get; set; }
        public string Status { get; set; }
        public string FailureStage { get; set; }
        public string Error { get; set; }
        public string AssemblyPath { get; set; }
        public string AuditPath { get; set; }
        public int ExpectedComponentCount { get; set; }
        public int ReopenedComponentCount { get; set; }
        public double TransformToleranceMm { get; set; }
        public double MaximumTransformDeltaMm { get; set; }
        public double MaximumRotationDelta { get; set; }
        public int CheckedHoleCount { get; set; }
        public int CheckedPocketCount { get; set; }
        public int CheckedThicknessCount { get; set; }
        public int CheckedFitCount { get; set; }
        public int InterferenceCount { get; set; }
        public List<string> OpenData { get; private set; }
        public List<SolidWorksGeometryAuditFinding> GeometryFindings { get; private set; }
        public List<SolidWorksPocketFitAuditItem> PocketFits { get; private set; }
        public List<SolidWorksInterferenceAuditItem> Interferences { get; private set; }
        public List<SolidWorksAssemblyAuditItem> Components { get; private set; }

        public SolidWorksAuditedAssemblyResult()
        {
            OpenData = new List<string>();
            GeometryFindings = new List<SolidWorksGeometryAuditFinding>();
            PocketFits = new List<SolidWorksPocketFitAuditItem>();
            Interferences = new List<SolidWorksInterferenceAuditItem>();
            Components = new List<SolidWorksAssemblyAuditItem>();
        }
    }

    public sealed class SolidWorksAssemblyAuditItem
    {
        public string AuditId { get; set; }
        public string SourceName { get; set; }
        public string AssemblyInstanceId { get; set; }
        public string SolidWorksComponentName { get; set; }
        public string TraceId { get; set; }
        public string MemberId { get; set; }
        public string ComponentId { get; set; }
        public string SourceStatus { get; set; }
        public string PartPath { get; set; }
        public double[] ExpectedTransform { get; set; }
        public double[] ObservedTransform { get; set; }
        public double[] ExpectedWorldBoundsMm { get; set; }
        public double[] ObservedWorldBoundsMm { get; set; }
        public double MaximumTranslationDeltaMm { get; set; }
        public double MaximumRotationDelta { get; set; }
        public double MaximumBoundsDeltaMm { get; set; }
        public double[] CreatedBodyMassProperties { get; set; }
        public double[] ReopenedBodyMassProperties { get; set; }
        public double MaximumBodySignatureDelta { get; set; }
        public bool BodySignaturePassed { get; set; }
        public bool Passed { get; set; }
        public string Error { get; set; }
    }

    public sealed class SolidWorksGeometryAuditFinding
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string PartName { get; set; }
        public string OperationName { get; set; }
        public string Message { get; set; }
    }

    public sealed class SolidWorksPocketFitAuditItem
    {
        public string ContractId { get; set; }
        public string HostPartName { get; set; }
        public string PocketName { get; set; }
        public double RequiredOccupancyRatio { get; set; }
        public double ObservedOccupancyRatio { get; set; }
        public string[] OccupantNames { get; set; }
        public bool Passed { get; set; }
        public string Error { get; set; }
    }

    public sealed class SolidWorksInterferenceAuditItem
    {
        public string[] ComponentNames { get; set; }
        public double VolumeMm3 { get; set; }
        public bool IsPossibleInterference { get; set; }
        public bool IsFastener { get; set; }
        public string Disposition { get; set; }
        public bool Passed { get; set; }
        public string Error { get; set; }
    }

    public sealed class SolidWorksSourceGeometryAuditSummary
    {
        public bool Passed { get { return Findings.All(value => !string.Equals(value.Severity, "Error", StringComparison.OrdinalIgnoreCase)); } }
        public int HoleCount { get; set; }
        public int PocketCount { get; set; }
        public int ThicknessCount { get; set; }
        public int FitCount { get; set; }
        public List<SolidWorksGeometryAuditFinding> Findings { get; private set; }
        public List<SolidWorksPocketFitAuditItem> PocketFits { get; private set; }

        public SolidWorksSourceGeometryAuditSummary()
        {
            Findings = new List<SolidWorksGeometryAuditFinding>();
            PocketFits = new List<SolidWorksPocketFitAuditItem>();
        }
    }

    public static class SolidWorksSourceGeometryAudit
    {
        private const double ToleranceMm = 0.05;

        public static SolidWorksSourceGeometryAuditSummary Audit(WorkbenchModel model, IList<PortalAssemblyPart> visuals)
        {
            var result = new SolidWorksSourceGeometryAuditSummary();
            if (model == null) { Error(result, "MODEL_MISSING", null, null, "Bronmodel ontbreekt."); return result; }
            if (visuals == null) { Error(result, "VISUALS_MISSING", null, null, "SolidWorks-geometriecontract ontbreekt."); return result; }

            foreach (var duplicate in model.Sheets.GroupBy(value => value.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).Where(value => value.Count() > 1))
                Error(result, "DUPLICATE_SHEET", duplicate.Key, null, "Plaatnaam is niet uniek in het bronmodel.");

            foreach (var placement in model.AssemblyPlacements.Where(value => value.Kind == AssemblyComponentKind.Sheet))
            {
                var sheet = model.Sheets.FirstOrDefault(value => string.Equals(value.Name, placement.PartName, StringComparison.OrdinalIgnoreCase));
                if (sheet == null)
                {
                    Error(result, "SHEET_RECORD_MISSING", placement.PartName, null, "Plaatplaatsing heeft geen bronrecord.");
                    continue;
                }
                if (sheet.Material == null || sheet.Material.ThicknessMm <= 0)
                {
                    Error(result, "SHEET_THICKNESS_MISSING", sheet.Name, null, "Plaatdikte ontbreekt; er wordt geen fallback gebruikt.");
                    continue;
                }

                var hostVisuals = visuals.Where(value => IsVisualForSheet(value, sheet.Name) && BelongsToPlacement(value, placement, sheet)).ToList();
                if (hostVisuals.Count == 0)
                {
                    Error(result, "SHEET_VISUAL_MISSING", sheet.Name, null, "Plaat ontbreekt in het SolidWorks-geometriecontract.");
                    continue;
                }

                result.ThicknessCount++;
                var thicknessAxis = DrawingContracts.ForOrientation(placement.Orientation).ThicknessAxis;
                foreach (var visual in hostVisuals)
                {
                    var observed = AxisSize(visual, thicknessAxis);
                    if (Math.Abs(observed - sheet.Material.ThicknessMm) > ToleranceMm)
                        Error(result, "SHEET_THICKNESS_MISMATCH", sheet.Name, null,
                            "SolidWorks-dikte " + F(observed) + " mm wijkt af van bron " + F(sheet.Material.ThicknessMm) + " mm.");
                }

                foreach (var hole in sheet.Holes)
                {
                    result.HoleCount++;
                    var matches = hostVisuals.SelectMany(value => value.Holes).Where(value => HoleMatches(value, placement, sheet, hole)).ToList();
                    if (matches.Count == 0 && IsRoundEndCut(hole))
                    {
                        var roundEnd = hostVisuals.SelectMany(value => value.Pockets).Any(value => RoundEndMatches(value, placement, sheet, hole));
                        if (roundEnd) continue;
                    }
                    if (matches.Count == 0)
                        Error(result, "HOLE_MISSING_OR_MISPLACED", sheet.Name, hole.Name, "Gat ontbreekt, ligt niet op de bronpositie of zit op het verkeerde vlak/de verkeerde zijde.");
                }

                foreach (var pocket in sheet.Pockets)
                {
                    result.PocketCount++;
                    var matches = hostVisuals.SelectMany(value => value.Pockets).Where(value => PocketMatches(value, placement, sheet, pocket)).ToList();
                    if (matches.Count == 0 && IsShippingBoxRabbet(model, pocket)) continue;
                    if (matches.Count == 0)
                    {
                        Error(result, "POCKET_MISSING_OR_MISPLACED", sheet.Name, pocket.Name, "Pocket/sleuf ontbreekt, heeft een afwijkende maat/diepte of zit op het verkeerde vlak/de verkeerde zijde.");
                        continue;
                    }
                    if (!pocket.RequiresAssemblyOccupant) continue;
                    result.FitCount++;
                    result.PocketFits.Add(CheckFit(pocket, matches[0], hostVisuals, visuals, sheet.Name));
                }
            }

            foreach (var failedFit in result.PocketFits.Where(value => !value.Passed))
                Error(result, "REQUIRED_POCKET_EMPTY", failedFit.HostPartName, failedFit.PocketName, failedFit.Error);
            return result;
        }

        private static SolidWorksPocketFitAuditItem CheckFit(SheetPocket source, PortalAssemblyPocket pocket, IList<PortalAssemblyPart> hosts, IList<PortalAssemblyPart> all, string hostName)
        {
            var pocketVolume = Math.Max(0.000001, pocket.SizeXmm * pocket.SizeYmm * pocket.SizeZmm);
            var occupants = new List<string>();
            var occupiedVolume = 0.0;
            foreach (var candidate in all.Where(value => value != null && !hosts.Contains(value)))
            {
                var overlap = IntersectionVolume(pocket.Xmm, pocket.Ymm, pocket.Zmm, pocket.SizeXmm, pocket.SizeYmm, pocket.SizeZmm,
                    candidate.Xmm, candidate.Ymm, candidate.Zmm, candidate.SizeXmm, candidate.SizeYmm, candidate.SizeZmm);
                if (overlap <= 0.000001) continue;
                occupiedVolume += overlap;
                occupants.Add(candidate.Name ?? "naamloos onderdeel");
            }
            var ratio = Math.Min(1.0, occupiedVolume / pocketVolume);
            var required = source.MinimumAssemblyOccupancyRatio > 0 ? source.MinimumAssemblyOccupancyRatio : 0.5;
            return new SolidWorksPocketFitAuditItem
            {
                ContractId = source.AssemblyFitContractId,
                HostPartName = hostName,
                PocketName = source.Name,
                RequiredOccupancyRatio = required,
                ObservedOccupancyRatio = Math.Round(ratio, 6),
                OccupantNames = occupants.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                Passed = ratio + 0.000001 >= required,
                Error = ratio + 0.000001 >= required ? null : "Verplichte sleuf/rabat is slechts voor " + F(ratio * 100) + "% gevuld; vereist minimaal " + F(required * 100) + "%."
            };
        }

        private static bool HoleMatches(PortalAssemblyHole visual, AssemblyPlacement placement, SheetPart sheet, SheetHole source)
        {
            if (visual == null || source == null || !string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(visual.Plane, ExpectedPlane(placement), StringComparison.OrdinalIgnoreCase)) return false;
            if (visual.SourceFace != source.Face || visual.SourceDepthMode != source.DepthMode) return false;
            if (!ValidFaceForOrientation(source.Face, placement.Orientation, source.DepthMode)) return false;
            if (Math.Abs(visual.DiameterMm - source.DiameterMm) > ToleranceMm || visual.IsThroughCutout != (source.DepthMode == OperationDepthMode.Through)) return false;
            if (source.DepthMode != OperationDepthMode.Through && Math.Abs(visual.DepthMm - source.DepthMm) > ToleranceMm) return false;
            if (visual.Countersunk != source.Countersunk) return false;
            if (source.Countersunk && (Math.Abs(visual.CountersinkDiameterMm - source.CountersinkDiameterMm) > ToleranceMm || Math.Abs(visual.CountersinkDepthMm - source.CountersinkDepthMm) > ToleranceMm)) return false;
            double first; double second;
            ExpectedTangentialPoint(placement, sheet, source.Xmm, source.Ymm, out first, out second);
            return TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second) && CorrectSide(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, placement, source.Face, source.DepthMode);
        }

        private static bool PocketMatches(PortalAssemblyPocket visual, AssemblyPlacement placement, SheetPart sheet, SheetPocket source)
        {
            if (visual == null || source == null || !string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(visual.Plane, ExpectedPlane(placement), StringComparison.OrdinalIgnoreCase)) return false;
            if (visual.SourceFace != source.Face || visual.SourceDepthMode != source.DepthMode) return false;
            if (!ValidFaceForOrientation(source.Face, placement.Orientation, source.DepthMode)) return false;
            if (visual.IsThroughCutout != (source.DepthMode == OperationDepthMode.Through)) return false;
            if (source.DepthMode != OperationDepthMode.Through && Math.Abs(AxisSize(visual, DrawingContracts.ForOrientation(placement.Orientation).ThicknessAxis) - source.DepthMm) > ToleranceMm) return false;
            double first; double second;
            ExpectedTangentialPoint(placement, sheet, source.Xmm + source.LengthMm / 2.0, source.Ymm + source.WidthMm / 2.0, out first, out second);
            return TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second)
                && TangentialSizeMatches(visual, source.LengthMm, source.WidthMm)
                && CorrectSide(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, placement, source.Face, source.DepthMode);
        }

        private static bool RoundEndMatches(PortalAssemblyPocket visual, AssemblyPlacement placement, SheetPart sheet, SheetHole source)
        {
            if (visual == null || !string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase) || !string.Equals(visual.Shape, "cylinder", StringComparison.OrdinalIgnoreCase)) return false;
            double first; double second;
            ExpectedTangentialPoint(placement, sheet, source.Xmm, source.Ymm, out first, out second);
            return TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second);
        }

        private static bool ValidFaceForOrientation(OperationFace face, AssemblyOrientation orientation, OperationDepthMode depthMode)
        {
            if (depthMode == OperationDepthMode.Through && face == OperationFace.CenterPlane) return true;
            if (face == OperationFace.CenterPlane) return false;
            if (orientation == AssemblyOrientation.SheetHorizontal) return face == OperationFace.PositiveY || face == OperationFace.NegativeY;
            if (orientation == AssemblyOrientation.SheetVerticalX) return face == OperationFace.PositiveZ || face == OperationFace.NegativeZ;
            if (orientation == AssemblyOrientation.SheetVerticalZ) return face == OperationFace.PositiveX || face == OperationFace.NegativeX;
            return false;
        }

        private static bool CorrectSide(string plane, double x, double y, double z, AssemblyPlacement placement, OperationFace face, OperationDepthMode depthMode)
        {
            if (depthMode == OperationDepthMode.Through || face == OperationFace.CenterPlane) return true;
            var delta = string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase) ? x - placement.Xmm
                : (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase) ? y - placement.Ymm : z - placement.Zmm);
            var positive = face == OperationFace.PositiveX || face == OperationFace.PositiveY || face == OperationFace.PositiveZ;
            return positive ? delta > 0 : delta < 0;
        }

        private static bool IsVisualForSheet(PortalAssemblyPart part, string name)
        {
            return part != null && string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase) || (part.Name ?? string.Empty).StartsWith(name + " ", StringComparison.OrdinalIgnoreCase));
        }

        private static bool BelongsToPlacement(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet)
        {
            var t = sheet.Material.ThicknessMm;
            var sx = placement.Orientation == AssemblyOrientation.SheetVerticalZ ? t : sheet.LengthMm;
            var sy = placement.Orientation == AssemblyOrientation.SheetHorizontal ? t : sheet.WidthMm;
            var sz = placement.Orientation == AssemblyOrientation.SheetVerticalX ? t : (placement.Orientation == AssemblyOrientation.SheetVerticalZ ? sheet.LengthMm : sheet.WidthMm);
            return Math.Abs(part.Xmm - placement.Xmm) <= sx / 2.0 + part.SizeXmm / 2.0 + ToleranceMm
                && Math.Abs(part.Ymm - placement.Ymm) <= sy / 2.0 + part.SizeYmm / 2.0 + ToleranceMm
                && Math.Abs(part.Zmm - placement.Zmm) <= sz / 2.0 + part.SizeZmm / 2.0 + ToleranceMm;
        }

        private static void ExpectedTangentialPoint(AssemblyPlacement placement, SheetPart sheet, double sourceX, double sourceY, out double first, out double second)
        {
            var localX = sourceX - sheet.LengthMm / 2.0;
            var localY = sourceY - sheet.WidthMm / 2.0;
            if (placement.Orientation == AssemblyOrientation.SheetHorizontal) { first = placement.Xmm + localX; second = placement.Zmm + localY; }
            else if (placement.Orientation == AssemblyOrientation.SheetVerticalX) { first = placement.Xmm + localX; second = placement.Ymm + localY; }
            else { first = placement.Ymm + localY; second = placement.Zmm + localX; }
        }

        private static bool TangentialPointMatches(string plane, double x, double y, double z, double first, double second)
        {
            if (string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase)) return Math.Abs(y - first) <= ToleranceMm && Math.Abs(z - second) <= ToleranceMm;
            if (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase)) return Math.Abs(x - first) <= ToleranceMm && Math.Abs(z - second) <= ToleranceMm;
            return Math.Abs(x - first) <= ToleranceMm && Math.Abs(y - second) <= ToleranceMm;
        }

        private static bool TangentialSizeMatches(PortalAssemblyPocket pocket, double length, double width)
        {
            if (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase)) return Math.Abs(pocket.SizeZmm - length) <= ToleranceMm && Math.Abs(pocket.SizeYmm - width) <= ToleranceMm;
            if (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase)) return Math.Abs(pocket.SizeXmm - length) <= ToleranceMm && Math.Abs(pocket.SizeZmm - width) <= ToleranceMm;
            return Math.Abs(pocket.SizeXmm - length) <= ToleranceMm && Math.Abs(pocket.SizeYmm - width) <= ToleranceMm;
        }

        private static string ExpectedPlane(AssemblyPlacement placement)
        {
            if (placement.Orientation == AssemblyOrientation.SheetHorizontal) return "y";
            if (placement.Orientation == AssemblyOrientation.SheetVerticalX) return "z";
            if (placement.Orientation == AssemblyOrientation.SheetVerticalZ) return "x";
            return string.Empty;
        }

        private static double AxisSize(PortalAssemblyPart part, ModelAxis axis) { return axis == ModelAxis.X ? part.SizeXmm : (axis == ModelAxis.Y ? part.SizeYmm : part.SizeZmm); }
        private static double AxisSize(PortalAssemblyPocket part, ModelAxis axis) { return axis == ModelAxis.X ? part.SizeXmm : (axis == ModelAxis.Y ? part.SizeYmm : part.SizeZmm); }
        private static bool IsRoundEndCut(SheetHole hole) { return hole != null && hole.SupportKind == SheetHoleSupportKind.MachiningCutout && (hole.Name ?? string.Empty).StartsWith("Uitgefreesde handgreep ronding", StringComparison.OrdinalIgnoreCase); }
        private static bool IsShippingBoxRabbet(WorkbenchModel model, SheetPocket pocket) { return string.Equals(model.ProductId, "shipping_box", StringComparison.OrdinalIgnoreCase) && (pocket.Name ?? string.Empty).StartsWith("Sponning ", StringComparison.OrdinalIgnoreCase); }

        private static double IntersectionVolume(double ax, double ay, double az, double asx, double asy, double asz, double bx, double by, double bz, double bsx, double bsy, double bsz)
        {
            var x = Math.Max(0, Math.Min(ax + asx / 2.0, bx + bsx / 2.0) - Math.Max(ax - asx / 2.0, bx - bsx / 2.0));
            var y = Math.Max(0, Math.Min(ay + asy / 2.0, by + bsy / 2.0) - Math.Max(ay - asy / 2.0, by - bsy / 2.0));
            var z = Math.Max(0, Math.Min(az + asz / 2.0, bz + bsz / 2.0) - Math.Max(az - asz / 2.0, bz - bsz / 2.0));
            return x * y * z;
        }

        private static void Error(SolidWorksSourceGeometryAuditSummary result, string code, string part, string operation, string message)
        {
            result.Findings.Add(new SolidWorksGeometryAuditFinding { Severity = "Error", Code = code, PartName = part, OperationName = operation, Message = message });
        }

        private static string F(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal static class SolidWorksTransformMath
    {
        internal static double[] RotationMatrix(double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var a = RotateVector(1, 0, 0, rotationXDeg, rotationYDeg, rotationZDeg);
            var b = RotateVector(0, 1, 0, rotationXDeg, rotationYDeg, rotationZDeg);
            var c = RotateVector(0, 0, 1, rotationXDeg, rotationYDeg, rotationZDeg);
            return new[] { a[0], a[1], a[2], b[0], b[1], b[2], c[0], c[1], c[2] };
        }

        internal static double[] Transform(double xMm, double yMm, double zMm, double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var rotation = RotationMatrix(rotationXDeg, rotationYDeg, rotationZDeg);
            return new[]
            {
                rotation[0], rotation[1], rotation[2],
                rotation[3], rotation[4], rotation[5],
                rotation[6], rotation[7], rotation[8],
                xMm / 1000.0, yMm / 1000.0, zMm / 1000.0,
                1.0, 0.0, 0.0, 0.0
            };
        }

        internal static double[] ExpectedBoundsMm(double xMm, double yMm, double zMm, double sizeXmm, double sizeYmm, double sizeZmm, double[] rotation)
        {
            var hx = sizeXmm / 2.0;
            var hy = sizeYmm / 2.0;
            var hz = sizeZmm / 2.0;
            var ex = Math.Abs(rotation[0]) * hx + Math.Abs(rotation[3]) * hy + Math.Abs(rotation[6]) * hz;
            var ey = Math.Abs(rotation[1]) * hx + Math.Abs(rotation[4]) * hy + Math.Abs(rotation[7]) * hz;
            var ez = Math.Abs(rotation[2]) * hx + Math.Abs(rotation[5]) * hy + Math.Abs(rotation[8]) * hz;
            return new[] { xMm - ex, yMm - ey, zMm - ez, xMm + ex, yMm + ey, zMm + ez };
        }

        private static double[] RotateVector(double x, double y, double z, double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var rx = rotationXDeg * Math.PI / 180.0;
            var ry = rotationYDeg * Math.PI / 180.0;
            var rz = rotationZDeg * Math.PI / 180.0;
            var y1 = y * Math.Cos(rx) - z * Math.Sin(rx);
            var z1 = y * Math.Sin(rx) + z * Math.Cos(rx);
            var x2 = x * Math.Cos(ry) + z1 * Math.Sin(ry);
            var z2 = -x * Math.Sin(ry) + z1 * Math.Cos(ry);
            return new[]
            {
                x2 * Math.Cos(rz) - y1 * Math.Sin(rz),
                x2 * Math.Sin(rz) + y1 * Math.Cos(rz),
                z2
            };
        }
    }
}
