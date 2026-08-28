using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileStickerPlacementService
    {
        private const double HorizontalThreshold = 0.35;
        private const double VerticalThreshold = 0.85;

        public void Assign(WorkbenchModel model, int orderQuantity)
        {
            if (model == null) return;
            var profiles = model.AssemblyPlacements
                .Where(item => item.Kind == AssemblyComponentKind.Profile)
                .ToArray();
            if (profiles.Length == 0) return;

            var assemblyCenterX = (profiles.Min(item => item.Xmm) + profiles.Max(item => item.Xmm)) / 2.0;
            var assemblyCenterZ = (profiles.Min(item => item.Zmm) + profiles.Max(item => item.Zmm)) / 2.0;
            foreach (var placement in profiles)
                placement.Sticker = SelectSticker(model, placement, assemblyCenterX, assemblyCenterZ);

            AssignTraceCopies(model, profiles, Math.Max(1, orderQuantity));
        }

        private static ProfileStickerPlacement SelectSticker(WorkbenchModel model, AssemblyPlacement placement, double assemblyCenterX, double assemblyCenterZ)
        {
            var size = PlacementSize(model, placement);
            var longitudinalAxis = LongitudinalAxis(size);
            var worldLongitudinal = Rotate(Unit(longitudinalAxis, 1), placement);
            var verticality = Math.Abs(worldLongitudinal[1]);
            var rule = verticality >= VerticalThreshold
                ? ProfileStickerPlacementRule.AssemblyViewSide
                : (verticality <= HorizontalThreshold ? ProfileStickerPlacementRule.UpperFace : ProfileStickerPlacementRule.InclinedVisibleFace);
            var desired = DesiredNormal(rule, placement, assemblyCenterX, assemblyCenterZ);
            var camera = Normalize(new[] { 1.0, 0.35, 1.0 });
            var anchorSign = rule == ProfileStickerPlacementRule.UpperFace
                ? -1
                : (worldLongitudinal[1] >= 0 ? 1 : -1);
            var anchorEnd = anchorSign < 0 ? ProfileEnd.A : ProfileEnd.B;
            var inset = StickerInset(size[longitudinalAxis], rule);
            Candidate selected = null;

            foreach (var faceAxis in Enumerable.Range(0, 3).Where(axis => axis != longitudinalAxis))
            foreach (var faceSign in new[] { 1, -1 })
            foreach (var candidateInset in StickerInsets(size[longitudinalAxis], inset, rule))
            {
                var localNormal = Unit(faceAxis, faceSign);
                var worldNormal = Normalize(Rotate(localNormal, placement));
                var localPosition = new double[3];
                localPosition[longitudinalAxis] = anchorSign * Math.Max(0, size[longitudinalAxis] / 2.0 - candidateInset);
                localPosition[faceAxis] = faceSign * (size[faceAxis] / 2.0 + 0.7);
                var obstructionFree = IsStickerAreaFree(model, placement, size, localPosition, localNormal, longitudinalAxis, faceAxis);
                var desiredWeight = rule == ProfileStickerPlacementRule.UpperFace ? 1000.0 : 100.0;
                var blockedPenalty = rule == ProfileStickerPlacementRule.UpperFace ? -100.0 : -1000.0;
                var score = Dot(worldNormal, desired) * desiredWeight + Dot(worldNormal, camera) * 5.0
                    + (obstructionFree ? 20.0 : blockedPenalty) - Math.Max(0, candidateInset - inset) * 0.02;
                if (selected == null || score > selected.Score)
                {
                    selected = new Candidate
                    {
                        FaceAxis = faceAxis,
                        FaceSign = faceSign,
                        LocalNormal = localNormal,
                        WorldNormal = worldNormal,
                        LocalPosition = localPosition,
                        Inset = candidateInset,
                        ObstructionFree = obstructionFree,
                        Score = score
                    };
                }
            }

            var remainingTangentAxis = Enumerable.Range(0, 3).Single(axis => axis != longitudinalAxis && axis != selected.FaceAxis);
            var sticker = new ProfileStickerPlacement
            {
                FaceId = "D0",
                LocalFace = (selected.FaceSign > 0 ? "+" : "-") + AxisName(selected.FaceAxis),
                Rule = rule,
                AnchorEnd = anchorEnd,
                OffsetFromAnchorEndMm = selected.Inset,
                LongitudinalAxis = longitudinalAxis,
                FaceAxis = selected.FaceAxis,
                FaceSign = selected.FaceSign,
                LocalXmm = selected.LocalPosition[0],
                LocalYmm = selected.LocalPosition[1],
                LocalZmm = selected.LocalPosition[2],
                LocalNormalX = selected.LocalNormal[0],
                LocalNormalY = selected.LocalNormal[1],
                LocalNormalZ = selected.LocalNormal[2],
                WorldNormalX = selected.WorldNormal[0],
                WorldNormalY = selected.WorldNormal[1],
                WorldNormalZ = selected.WorldNormal[2],
                LongitudinalSizeMm = Math.Min(55, Math.Max(28, size[longitudinalAxis] - 12)),
                TransverseSizeMm = Math.Min(18, Math.Max(10, size[remainingTangentAxis] - 8)),
                ObstructionFree = selected.ObstructionFree,
                VisibilityScore = Math.Round(selected.Score, 3),
                OrientationInstruction = OrientationInstruction(rule, anchorEnd)
            };
            return sticker;
        }

        private static void AssignTraceCopies(WorkbenchModel model, AssemblyPlacement[] placements, int units)
        {
            foreach (var placement in placements)
                placement.Sticker.TraceIds.Clear();

            var piecesInOneUnit = model.Profiles.Count == 0
                ? 0
                : model.Profiles.Sum(profile => profile.Quantity / units);
            if (piecesInOneUnit == placements.Length)
            {
                var placementIndex = 0;
                foreach (var profile in model.Profiles)
                {
                    var piecesPerUnit = profile.Quantity / units;
                    for (var piece = 0; piece < piecesPerUnit; piece++)
                    {
                        var sticker = placements[placementIndex++].Sticker;
                        for (var unit = 0; unit < units; unit++)
                        {
                            var traceIndex = unit * piecesPerUnit + piece;
                            if (traceIndex < profile.PieceTraceIds.Count)
                                sticker.TraceIds.Add(profile.PieceTraceIds[traceIndex]);
                        }
                    }
                }
                return;
            }

            foreach (var placement in placements)
                if (!string.IsNullOrWhiteSpace(placement.TraceId))
                    placement.Sticker.TraceIds.Add(placement.TraceId);
        }

        private static double[] DesiredNormal(ProfileStickerPlacementRule rule, AssemblyPlacement placement, double centerX, double centerZ)
        {
            if (rule == ProfileStickerPlacementRule.UpperFace) return new[] { 0.0, 1.0, 0.0 };
            var assemblyView = Normalize(new[] { 1.0, 0.0, 1.0 });
            if (rule == ProfileStickerPlacementRule.AssemblyViewSide) return assemblyView;
            return Normalize(new[] { assemblyView[0] * 0.45, 0.75, assemblyView[2] * 0.45 });
        }

        private static bool IsStickerAreaFree(WorkbenchModel model, AssemblyPlacement owner, double[] ownerSize,
            double[] localCenter, double[] localNormal, int longitudinalAxis, int faceAxis)
        {
            var tangentAxis = Enumerable.Range(0, 3).Single(axis => axis != longitudinalAxis && axis != faceAxis);
            var longitudinalHalf = Math.Min(22, Math.Max(8, ownerSize[longitudinalAxis] / 4.0));
            var transverseHalf = Math.Min(7, Math.Max(3, ownerSize[tangentAxis] / 4.0));
            var samples = new[]
            {
                new[] { 0.0, 0.0 },
                new[] { longitudinalHalf, transverseHalf },
                new[] { longitudinalHalf, -transverseHalf },
                new[] { -longitudinalHalf, transverseHalf },
                new[] { -longitudinalHalf, -transverseHalf }
            };
            foreach (var sample in samples)
            {
                var local = (double[])localCenter.Clone();
                local[longitudinalAxis] += sample[0];
                local[tangentAxis] += sample[1];
                local[0] += localNormal[0] * 1.5;
                local[1] += localNormal[1] * 1.5;
                local[2] += localNormal[2] * 1.5;
                var rotated = Rotate(local, owner);
                var world = new[] { owner.Xmm + rotated[0], owner.Ymm + rotated[1], owner.Zmm + rotated[2] };
                foreach (var other in model.AssemblyPlacements)
                {
                    if (ReferenceEquals(other, owner)) continue;
                    if (PointInside(model, other, world, 0.5)) return false;
                }
            }
            return true;
        }

        private static bool PointInside(WorkbenchModel model, AssemblyPlacement placement, double[] world, double margin)
        {
            var translated = new[] { world[0] - placement.Xmm, world[1] - placement.Ymm, world[2] - placement.Zmm };
            var local = InverseRotate(translated, placement);
            var size = PlacementSize(model, placement);
            return Math.Abs(local[0]) <= size[0] / 2.0 + margin
                && Math.Abs(local[1]) <= size[1] / 2.0 + margin
                && Math.Abs(local[2]) <= size[2] / 2.0 + margin;
        }

        private static double[] PlacementSize(WorkbenchModel model, AssemblyPlacement placement)
        {
            var thickness = 18.0;
            if (placement.Kind == AssemblyComponentKind.Sheet)
            {
                var sheet = model.Sheets.FirstOrDefault(item => string.Equals(item.Name, placement.PartName, StringComparison.OrdinalIgnoreCase));
                if (sheet != null && sheet.Material != null) thickness = Math.Max(2, sheet.Material.ThicknessMm);
            }
            var sx = placement.LengthMm;
            var sy = placement.Kind != AssemblyComponentKind.Sheet && placement.HeightMm > 0 ? placement.HeightMm : thickness;
            var sz = placement.WidthMm;
            if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
            {
                sx = placement.LengthMm;
                sy = placement.WidthMm;
                sz = thickness;
            }
            else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
            {
                sx = thickness;
                sy = placement.WidthMm;
                sz = placement.LengthMm;
            }
            return new[] { Math.Max(2, sx), Math.Max(2, sy), Math.Max(2, sz) };
        }

        private static int LongitudinalAxis(double[] size)
        {
            var axis = 0;
            if (size[1] > size[axis]) axis = 1;
            if (size[2] > size[axis]) axis = 2;
            return axis;
        }

        private static double StickerInset(double length, ProfileStickerPlacementRule rule)
        {
            var preferred = rule == ProfileStickerPlacementRule.UpperFace ? length * 0.08 : length * 0.12;
            var maximum = rule == ProfileStickerPlacementRule.UpperFace ? 90.0 : 150.0;
            var bounded = Math.Min(Math.Max(30, preferred), Math.Min(maximum, Math.Max(30, length / 3.0)));
            return RoundToWholeCentimeterMm(bounded);
        }

        private static double RoundToWholeCentimeterMm(double value)
        {
            return Math.Round(value / 10.0, MidpointRounding.AwayFromZero) * 10.0;
        }

        private static IEnumerable<double> StickerInsets(double length, double preferred, ProfileStickerPlacementRule rule)
        {
            yield return preferred;
            if (rule != ProfileStickerPlacementRule.UpperFace) yield break;
            var maximum = Math.Max(preferred, length / 2.0 - 35.0);
            for (var inset = preferred + 40.0; inset <= maximum + 0.001; inset += 40.0)
                yield return inset;
        }

        private static string OrientationInstruction(ProfileStickerPlacementRule rule, ProfileEnd anchorEnd)
        {
            if (rule == ProfileStickerPlacementRule.UpperFace)
                return "Sticker boven houden; D0 is bovenzijde; pijl wijst naar Kop A.";
            var kopA = anchorEnd == ProfileEnd.A ? "boven" : "onder";
            if (rule == ProfileStickerPlacementRule.AssemblyViewSide)
                return "Sticker naar de montage-/zichtzijde houden; D0 blijft zichtbaar; Kop A " + kopA + ".";
            return "Sticker op het best zichtbare bovenvlak houden; D0 blijft zichtbaar; Kop A " + kopA + ".";
        }

        private static double[] Rotate(double[] value, AssemblyPlacement placement)
        {
            var x = value[0]; var y = value[1]; var z = value[2];
            RotateX(ref y, ref z, Degrees(placement.RotationXDeg));
            RotateY(ref x, ref z, Degrees(placement.RotationYDeg));
            RotateZ(ref x, ref y, Degrees(placement.RotationZDeg));
            return new[] { x, y, z };
        }

        private static double[] InverseRotate(double[] value, AssemblyPlacement placement)
        {
            var x = value[0]; var y = value[1]; var z = value[2];
            RotateZ(ref x, ref y, -Degrees(placement.RotationZDeg));
            RotateY(ref x, ref z, -Degrees(placement.RotationYDeg));
            RotateX(ref y, ref z, -Degrees(placement.RotationXDeg));
            return new[] { x, y, z };
        }

        private static void RotateX(ref double y, ref double z, double angle)
        {
            var originalY = y; var originalZ = z;
            y = originalY * Math.Cos(angle) - originalZ * Math.Sin(angle);
            z = originalY * Math.Sin(angle) + originalZ * Math.Cos(angle);
        }

        private static void RotateY(ref double x, ref double z, double angle)
        {
            var originalX = x; var originalZ = z;
            x = originalX * Math.Cos(angle) + originalZ * Math.Sin(angle);
            z = -originalX * Math.Sin(angle) + originalZ * Math.Cos(angle);
        }

        private static void RotateZ(ref double x, ref double y, double angle)
        {
            var originalX = x; var originalY = y;
            x = originalX * Math.Cos(angle) - originalY * Math.Sin(angle);
            y = originalX * Math.Sin(angle) + originalY * Math.Cos(angle);
        }

        private static double Degrees(double value) { return value * Math.PI / 180.0; }
        private static string AxisName(int axis) { return axis == 0 ? "X" : (axis == 1 ? "Y" : "Z"); }
        private static double[] Unit(int axis, int sign) { var value = new double[3]; value[axis] = sign; return value; }
        private static double Dot(double[] left, double[] right) { return left[0] * right[0] + left[1] * right[1] + left[2] * right[2]; }
        private static double Length(double[] value) { return Math.Sqrt(Dot(value, value)); }
        private static double[] Normalize(double[] value)
        {
            var length = Length(value);
            return length < 0.000001 ? new[] { 0.0, 0.0, 0.0 } : new[] { value[0] / length, value[1] / length, value[2] / length };
        }

        private sealed class Candidate
        {
            public int FaceAxis { get; set; }
            public int FaceSign { get; set; }
            public double[] LocalNormal { get; set; }
            public double[] WorldNormal { get; set; }
            public double[] LocalPosition { get; set; }
            public double Inset { get; set; }
            public bool ObstructionFree { get; set; }
            public double Score { get; set; }
        }
    }
}
