using System;
using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Drawing
{
    public sealed class SheetPoint
    {
        public double Xmm { get; set; }
        public double Ymm { get; set; }
    }

    public static class SheetPatterns
    {
        public static List<double> EdgeDistributedPositions(double length, double edgeInset, double maxSpacing, int minimumCount)
        {
            var positions = new List<double>();
            if (length <= 0) return positions;

            var start = Math.Min(Math.Max(8, edgeInset), length / 2.0);
            var end = Math.Max(start, length - start);
            var usable = Math.Max(0, end - start);
            var count = Math.Max(minimumCount, (int)Math.Ceiling(usable / Math.Max(1, maxSpacing)) + 1);
            if (count <= 1)
            {
                positions.Add(Math.Round(length / 2.0, 3));
                return positions;
            }

            for (var i = 0; i < count; i++)
            {
                var t = (double)i / (count - 1);
                positions.Add(Math.Round(start + usable * t, 3));
            }

            return positions;
        }

        public static List<SheetPoint> LinePoints(double x1, double y1, double x2, double y2, double maxSpacing)
        {
            var points = new List<SheetPoint>();
            var length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            var segments = Math.Max(1, (int)Math.Ceiling(length / Math.Max(1, maxSpacing)));
            for (var i = 0; i <= segments; i++)
            {
                var t = (double)i / segments;
                points.Add(new SheetPoint
                {
                    Xmm = x1 + (x2 - x1) * t,
                    Ymm = y1 + (y2 - y1) * t
                });
            }

            return points;
        }
    }
}
