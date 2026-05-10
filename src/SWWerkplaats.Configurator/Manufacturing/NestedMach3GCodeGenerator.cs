using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class NestedMach3GCodeGenerator
    {
        public string Generate(NestedStockSheet stock, ToolDefinition tool, MachineProfile machine)
        {
            var sb = new StringBuilder();
            sb.AppendLine("(Project: " + stock.Name + ")");
            sb.AppendLine("(Machine: " + machine.Name + ")");
            sb.AppendLine("(Voorraadplaat: " + stock.Material.Name + " " + F(stock.StockLengthMm) + " x " + F(stock.StockWidthMm) + " mm)");
            sb.AppendLine("(Tool: " + tool.Name + ", diameter " + F(tool.DiameterMm) + " mm)");
            sb.AppendLine("(Origin: links onder, Z0 op bovenzijde materiaal)");
            sb.AppendLine("G21");
            sb.AppendLine("G90");
            sb.AppendLine("G17");
            sb.AppendLine("G40");
            sb.AppendLine("G49");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine();
            sb.AppendLine("(Laad tool T1: " + tool.Name + " voor kopkamers, gaten en contouren)");
            sb.AppendLine("M5");
            sb.AppendLine("T1 M6");
            sb.AppendLine("M3 S" + F(tool.SpindleRpm));

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 1: alle kopkamers op geneste plaat ---)");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    AddCountersink(sb, placement, hole, tool, machine);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 2: alle door-en-door gaten op geneste plaat ---)");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    var p = Transform(placement, hole.Xmm, hole.Ymm);
                    AddHole(sb, placement, hole, p.X, p.Y, tool, machine);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: buitencontouren geneste onderdelen ---)");
            foreach (var placement in stock.Placements)
            {
                AddContour(sb, placement, tool, machine);
            }

            sb.AppendLine("M5");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X0 Y0");
            sb.AppendLine("M30");
            return sb.ToString();
        }

        private static void AddCountersink(StringBuilder sb, NestedSheetPlacement placement, SheetHole hole, ToolDefinition tool, MachineProfile machine)
        {
            if (!hole.Countersunk || hole.CountersinkDiameterMm <= hole.DiameterMm || hole.CountersinkDepthMm <= 0)
            {
                return;
            }

            var p = Transform(placement, hole.Xmm, hole.Ymm);
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(hole.CountersinkDepthMm) + ", centrum X" + F(p.X) + " Y" + F(p.Y) + ")");
            CircularPocket(sb, p.X, p.Y, hole.CountersinkDiameterMm, hole.CountersinkDepthMm, tool, machine);
        }

        private static void AddHole(StringBuilder sb, NestedSheetPlacement placement, SheetHole hole, double x, double y, ToolDefinition tool, MachineProfile machine)
        {
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + hole.Name + " diameter " + F(hole.DiameterMm) + ", centrum X" + F(x) + " Y" + F(y) + ")");
            if (hole.DiameterMm <= tool.DiameterMm * 1.15)
            {
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, placement.Part.Material.ThicknessMm);
                return;
            }

            CircularPocket(sb, x, y, hole.DiameterMm, placement.Part.Material.ThicknessMm, tool, machine);
        }

        private static void CircularPocket(StringBuilder sb, double x, double y, double diameter, double depthMm, ToolDefinition tool, MachineProfile machine)
        {
            var radius = (diameter - tool.DiameterMm) / 2.0;
            if (radius <= 0)
            {
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                sb.AppendLine("G1 Z" + F(-depthMm) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                return;
            }

            var startX = x + radius;
            sb.AppendLine("(Centrum X" + F(x) + " Y" + F(y) + ", freesbaan start X" + F(startX) + " Y" + F(y) + ", baanradius " + F(radius) + ")");
            sb.AppendLine("G0 X" + F(startX) + " Y" + F(y));
            sb.AppendLine("G1 Z0 F" + F(tool.PlungeRateMmMin));
            var depth = 0.0;
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -depthMm);
                sb.AppendLine("G2 X" + F(startX) + " Y" + F(y) + " Z" + F(depth) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
            }

            sb.AppendLine("G2 X" + F(startX) + " Y" + F(y) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void DrillPeck(StringBuilder sb, ToolDefinition tool, MachineProfile machine, double depthMm)
        {
            var depth = 0.0;
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -depthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            }
        }

        private static void AddContour(StringBuilder sb, NestedSheetPlacement placement, ToolDefinition tool, MachineProfile machine)
        {
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " buitencontour)");
            var points = ContourPoints(placement.Part, tool.RadiusMm);
            var start = Transform(placement, points[0].X, points[0].Y);
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));

            var depth = 0.0;
            while (depth > -placement.Part.Material.ThicknessMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -placement.Part.Material.ThicknessMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                for (var i = 1; i < points.Count; i++)
                {
                    var p = Transform(placement, points[i].X, points[i].Y);
                    sb.AppendLine("G1 X" + F(p.X) + " Y" + F(p.Y) + " F" + F(tool.FeedRateMmMin));
                }
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static List<Point2> ContourPoints(SheetPart part, double radius)
        {
            var points = new List<Point2>();
            var x0 = -radius;
            var y0 = -radius;
            var x1 = part.LengthMm + radius;
            var y1 = part.WidthMm + radius;
            if (part.HasToeKickNotch)
            {
                var notchX = Math.Min(part.ToeKickDepthMm + radius, x1);
                var notchY = Math.Min(part.ToeKickHeightMm + radius, y1);
                points.Add(new Point2(notchX, y0));
                points.Add(new Point2(x1, y0));
                points.Add(new Point2(x1, y1));
                points.Add(new Point2(x0, y1));
                points.Add(new Point2(x0, notchY));
                points.Add(new Point2(notchX, notchY));
                points.Add(new Point2(notchX, y0));
                return points;
            }

            if (!part.HasCornerNotches)
            {
                points.Add(new Point2(x0, y0));
                points.Add(new Point2(x1, y0));
                points.Add(new Point2(x1, y1));
                points.Add(new Point2(x0, y1));
                points.Add(new Point2(x0, y0));
                return points;
            }

            var n = part.CornerNotchSizeMm;
            var nx0 = n + radius;
            var ny0 = n + radius;
            var nx1 = part.LengthMm - n - radius;
            var ny1 = part.WidthMm - n - radius;
            points.Add(new Point2(nx0, y0));
            points.Add(new Point2(nx1, y0));
            points.Add(new Point2(nx1, ny0));
            points.Add(new Point2(x1, ny0));
            points.Add(new Point2(x1, ny1));
            points.Add(new Point2(nx1, ny1));
            points.Add(new Point2(nx1, y1));
            points.Add(new Point2(nx0, y1));
            points.Add(new Point2(nx0, ny1));
            points.Add(new Point2(x0, ny1));
            points.Add(new Point2(x0, ny0));
            points.Add(new Point2(nx0, ny0));
            points.Add(new Point2(nx0, y0));
            return points;
        }

        private static Point2 Transform(NestedSheetPlacement placement, double x, double y)
        {
            if (!placement.Rotated)
            {
                return new Point2(placement.Xmm + x, placement.Ymm + y);
            }

            return new Point2(placement.Xmm + y, placement.Ymm + placement.Part.LengthMm - x);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private struct Point2
        {
            public readonly double X;
            public readonly double Y;

            public Point2(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
