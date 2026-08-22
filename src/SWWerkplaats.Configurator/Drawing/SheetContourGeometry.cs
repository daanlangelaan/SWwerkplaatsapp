using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Drawing
{
    public static class SheetContourGeometry
    {
        public static List<SheetContourPoint> ToolCenterContour(SheetPart part, double toolRadiusMm)
        {
            if (part == null || part.CustomContour == null || part.CustomContour.Count < 3)
                return new List<SheetContourPoint>();

            var source = new List<SheetContourPoint>();
            foreach (var point in part.CustomContour)
            {
                if (point == null) continue;
                if (source.Count > 0 && Distance(source[source.Count - 1], point) < 0.001) continue;
                source.Add(new SheetContourPoint(point.Xmm, point.Ymm));
            }
            if (source.Count > 1 && Distance(source[0], source[source.Count - 1]) < 0.001)
                source.RemoveAt(source.Count - 1);
            if (source.Count < 3) return new List<SheetContourPoint>();

            if (part.CustomContourCornerRadiusMm > 0.001)
                source = RoundCorners(source, part.CustomContourCornerRadiusMm);

            var radius = Math.Max(0, toolRadiusMm);
            var result = radius < 0.001 ? source : OffsetPolygon(source, radius);
            if (result.Count > 0) result.Add(new SheetContourPoint(result[0].Xmm, result[0].Ymm));
            return result;
        }

        private static List<SheetContourPoint> RoundCorners(List<SheetContourPoint> points, double radius)
        {
            var rounded = new List<SheetContourPoint>();
            const int segments = 6;
            for (var i = 0; i < points.Count; i++)
            {
                var previous = points[(i + points.Count - 1) % points.Count];
                var current = points[i];
                var next = points[(i + 1) % points.Count];
                var previousLength = Distance(previous, current);
                var nextLength = Distance(current, next);
                if (previousLength < 0.001 || nextLength < 0.001) continue;
                var trim = Math.Min(Math.Max(0, radius), Math.Min(previousLength, nextLength) * 0.42);
                if (trim < 0.001)
                {
                    rounded.Add(new SheetContourPoint(current.Xmm, current.Ymm));
                    continue;
                }
                var start = new SheetContourPoint(
                    current.Xmm + (previous.Xmm - current.Xmm) * trim / previousLength,
                    current.Ymm + (previous.Ymm - current.Ymm) * trim / previousLength);
                var end = new SheetContourPoint(
                    current.Xmm + (next.Xmm - current.Xmm) * trim / nextLength,
                    current.Ymm + (next.Ymm - current.Ymm) * trim / nextLength);
                rounded.Add(start);
                for (var segment = 1; segment <= segments; segment++)
                {
                    var t = segment / (double)segments;
                    var u = 1.0 - t;
                    rounded.Add(new SheetContourPoint(
                        u * u * start.Xmm + 2.0 * u * t * current.Xmm + t * t * end.Xmm,
                        u * u * start.Ymm + 2.0 * u * t * current.Ymm + t * t * end.Ymm));
                }
            }
            return rounded;
        }

        private static List<SheetContourPoint> OffsetPolygon(List<SheetContourPoint> points, double distance)
        {
            var result = new List<SheetContourPoint>();
            var ccw = SignedArea(points) > 0;
            for (var i = 0; i < points.Count; i++)
            {
                var previous = points[(i + points.Count - 1) % points.Count];
                var current = points[i];
                var next = points[(i + 1) % points.Count];
                var lineA = ShiftedLine(previous, current, distance, ccw);
                var lineB = ShiftedLine(current, next, distance, ccw);
                SheetContourPoint intersection;
                if (!Intersect(lineA.Item1, lineA.Item2, lineB.Item1, lineB.Item2, out intersection))
                {
                    intersection = new SheetContourPoint(
                        (lineA.Item2.Xmm + lineB.Item1.Xmm) / 2.0,
                        (lineA.Item2.Ymm + lineB.Item1.Ymm) / 2.0);
                }
                result.Add(intersection);
            }
            return result;
        }

        private static Tuple<SheetContourPoint, SheetContourPoint> ShiftedLine(SheetContourPoint a, SheetContourPoint b, double distance, bool ccw)
        {
            var dx = b.Xmm - a.Xmm;
            var dy = b.Ymm - a.Ymm;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) return Tuple.Create(new SheetContourPoint(a.Xmm, a.Ymm), new SheetContourPoint(b.Xmm, b.Ymm));
            var sign = ccw ? 1.0 : -1.0;
            var nx = sign * dy / length;
            var ny = sign * -dx / length;
            return Tuple.Create(
                new SheetContourPoint(a.Xmm + nx * distance, a.Ymm + ny * distance),
                new SheetContourPoint(b.Xmm + nx * distance, b.Ymm + ny * distance));
        }

        private static bool Intersect(SheetContourPoint a, SheetContourPoint b, SheetContourPoint c, SheetContourPoint d, out SheetContourPoint point)
        {
            var x1 = a.Xmm; var y1 = a.Ymm; var x2 = b.Xmm; var y2 = b.Ymm;
            var x3 = c.Xmm; var y3 = c.Ymm; var x4 = d.Xmm; var y4 = d.Ymm;
            var denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denominator) < 0.000001)
            {
                point = null;
                return false;
            }
            var crossA = x1 * y2 - y1 * x2;
            var crossB = x3 * y4 - y3 * x4;
            point = new SheetContourPoint(
                (crossA * (x3 - x4) - (x1 - x2) * crossB) / denominator,
                (crossA * (y3 - y4) - (y1 - y2) * crossB) / denominator);
            return true;
        }

        private static double SignedArea(List<SheetContourPoint> points)
        {
            var area = 0.0;
            for (var i = 0; i < points.Count; i++)
            {
                var next = points[(i + 1) % points.Count];
                area += points[i].Xmm * next.Ymm - next.Xmm * points[i].Ymm;
            }
            return area / 2.0;
        }

        private static double Distance(SheetContourPoint a, SheetContourPoint b)
        {
            var dx = a.Xmm - b.Xmm;
            var dy = a.Ymm - b.Ymm;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
