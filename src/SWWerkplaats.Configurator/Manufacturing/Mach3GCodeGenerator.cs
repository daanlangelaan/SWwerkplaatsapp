using System;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class Mach3GCodeGenerator
    {
        public string GenerateSheetPart(SheetPart part, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm)
        {
            return GenerateSheetPart(part, tool, tool, machine, materialThicknessMm, tabWidthMm, tabHeightMm);
        }

        public string GenerateSheetPart(SheetPart part, ToolDefinition holeTool, ToolDefinition contourTool, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm)
        {
            if (part == null) throw new ArgumentNullException("part");
            if (holeTool == null) throw new ArgumentNullException("holeTool");
            if (contourTool == null) throw new ArgumentNullException("contourTool");
            if (machine == null) throw new ArgumentNullException("machine");
            if (part.LengthMm > machine.MaxXmm || part.WidthMm > machine.MaxYmm)
            {
                throw new InvalidOperationException("Plaatdeel past niet binnen het machinebereik.");
            }
            ValidateToolCanMachinePart(part, holeTool, contourTool);

            var sb = new StringBuilder();
            Header(sb, part, contourTool, machine);

            if (HasCountersinks(part))
            {
                BeginTool(sb, 1, contourTool.Name + " voor groeven, kopkamers en buitencontour", contourTool);
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 1: alle positioneergroeven / pockets ---)");
                foreach (var pocket in part.Pockets)
                {
                    AddRectangularPocket(sb, pocket, contourTool, machine);
                }
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 2: alle kopkamers helix-frezen ---)");
                foreach (var hole in part.Holes)
                {
                    AddCountersink(sb, hole, contourTool, machine);
                }
            }
            else
            {
                BeginTool(sb, 1, contourTool.Name + " voor groeven en buitencontour", contourTool);
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 1: alle positioneergroeven / pockets ---)");
                foreach (var pocket in part.Pockets)
                {
                    AddRectangularPocket(sb, pocket, contourTool, machine);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: alle montagegaten ---)");
            if (!SameTool(holeTool, contourTool))
            {
                BeginTool(sb, 2, holeTool.Name + " voor montagegaten", holeTool);
            }

            foreach (var hole in part.Holes)
            {
                AddHole(sb, hole, holeTool, machine, materialThicknessMm);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 4: buitencontour ---)");
            if (!SameTool(holeTool, contourTool))
            {
                BeginTool(sb, 1, contourTool.Name + " voor buitencontour", contourTool);
            }

            AddOutsideRectangle(sb, part, contourTool, machine, materialThicknessMm, tabWidthMm, tabHeightMm);

            sb.AppendLine("M5");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X0 Y0");
            sb.AppendLine("M30");
            return sb.ToString();
        }

        private static void Header(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine)
        {
            sb.AppendLine("(Project: " + part.Name + ")");
            sb.AppendLine("(Machine: " + machine.Name + ")");
            sb.AppendLine("(Tool: " + tool.Name + ", diameter " + F(tool.DiameterMm) + " mm)");
            sb.AppendLine("(Origin: links onder, Z0 op bovenzijde materiaal)");
            sb.AppendLine("G21");
            sb.AppendLine("G90");
            sb.AppendLine("G17");
            sb.AppendLine("G40");
            sb.AppendLine("G49");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void BeginTool(StringBuilder sb, int toolNumber, string description, ToolDefinition tool)
        {
            sb.AppendLine();
            sb.AppendLine("(Laad tool T" + toolNumber + ": " + description + ")");
            sb.AppendLine("M5");
            sb.AppendLine("T" + toolNumber + " M6");
            sb.AppendLine("M3 S" + F(tool.SpindleRpm));
        }

        private static void AddHole(StringBuilder sb, SheetHole hole, ToolDefinition tool, MachineProfile machine, double materialThicknessMm)
        {
            sb.AppendLine();
            var cutDepth = HoleDepth(hole, materialThicknessMm);
            sb.AppendLine("(" + hole.Name + " diameter " + F(hole.DiameterMm) + ", diepte " + F(cutDepth) + ", centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ")");

            if (hole.DiameterMm <= tool.DiameterMm + 0.05)
            {
                sb.AppendLine("(Start op centrum, gat bijna gelijk aan tooldiameter)");
                sb.AppendLine("G0 X" + F(hole.Xmm) + " Y" + F(hole.Ymm));
                DrillPeck(sb, tool, machine, cutDepth);
                return;
            }

            var radius = (hole.DiameterMm - tool.DiameterMm) / 2.0;
            var startX = hole.Xmm + radius;
            sb.AppendLine("(Centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ", freesbaan start X" + F(startX) + " Y" + F(hole.Ymm) + ", baanradius " + F(radius) + ")");
            sb.AppendLine("G0 X" + F(startX) + " Y" + F(hole.Ymm));

            var depth = 0.0;
            while (depth > -cutDepth)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -cutDepth);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G2 X" + F(startX) + " Y" + F(hole.Ymm) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddCountersink(StringBuilder sb, SheetHole hole, ToolDefinition tool, MachineProfile machine)
        {
            if (!hole.Countersunk || hole.CountersinkDiameterMm <= hole.DiameterMm || hole.CountersinkDepthMm <= 0)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine("(" + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(hole.CountersinkDepthMm) + ", centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ")");

            if (hole.CountersinkDiameterMm <= tool.DiameterMm + 0.05)
            {
                sb.AppendLine("(Start op centrum, kopkamer bijna gelijk aan tooldiameter)");
                sb.AppendLine("G0 X" + F(hole.Xmm) + " Y" + F(hole.Ymm));
                sb.AppendLine("G1 Z" + F(-hole.CountersinkDepthMm) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                return;
            }

            var radius = (hole.CountersinkDiameterMm - tool.DiameterMm) / 2.0;
            var startX = hole.Xmm + radius;
            sb.AppendLine("(Centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ", freesbaan start X" + F(startX) + " Y" + F(hole.Ymm) + ", baanradius " + F(radius) + ")");
            sb.AppendLine("G0 X" + F(startX) + " Y" + F(hole.Ymm));
            sb.AppendLine("G1 Z0 F" + F(tool.PlungeRateMmMin));

            var depth = 0.0;
            while (depth > -hole.CountersinkDepthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -hole.CountersinkDepthMm);
                sb.AppendLine("G2 X" + F(startX) + " Y" + F(hole.Ymm) + " Z" + F(depth) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
            }

            sb.AppendLine("G2 X" + F(startX) + " Y" + F(hole.Ymm) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static double HoleDepth(SheetHole hole, double materialThicknessMm)
        {
            if (hole != null && hole.DepthMm > 0)
            {
                return Math.Min(hole.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));
            }

            return materialThicknessMm;
        }

        private static void AddRectangularPocket(StringBuilder sb, SheetPocket pocket, ToolDefinition tool, MachineProfile machine)
        {
            if (pocket == null || pocket.LengthMm <= 0 || pocket.WidthMm <= 0 || pocket.DepthMm <= 0)
            {
                return;
            }

            var inset = Math.Max(tool.RadiusMm, 0.1);
            var x0 = pocket.Xmm + inset;
            var y0 = pocket.Ymm + inset;
            var x1 = pocket.Xmm + pocket.LengthMm - inset;
            var y1 = pocket.Ymm + pocket.WidthMm - inset;
            if (x1 <= x0 || y1 <= y0)
            {
                x0 = pocket.Xmm + pocket.LengthMm / 2.0;
                y0 = pocket.Ymm + pocket.WidthMm / 2.0;
                x1 = x0;
                y1 = y0;
            }

            sb.AppendLine();
            sb.AppendLine("(" + pocket.Name + " pocket X" + F(pocket.Xmm) + " Y" + F(pocket.Ymm) + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(pocket.DepthMm) + ")");
            sb.AppendLine("G0 X" + F(x0) + " Y" + F(y0));
            var depth = 0.0;
            while (depth > -pocket.DepthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -pocket.DepthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPocketClearingPass(sb, x0, y0, x1, y1, tool);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPocketClearingPass(StringBuilder sb, double x0, double y0, double x1, double y1, ToolDefinition tool)
        {
            if (Math.Abs(x1 - x0) < 0.001 && Math.Abs(y1 - y0) < 0.001)
            {
                sb.AppendLine("G1 X" + F(x0) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
                return;
            }

            var step = Math.Max(1.0, tool.DiameterMm * 0.65);
            var y = y0;
            var forward = true;
            while (true)
            {
                var targetX = forward ? x1 : x0;
                sb.AppendLine("G1 X" + F(targetX) + " Y" + F(y) + " F" + F(tool.FeedRateMmMin));
                if (Math.Abs(y - y1) < 0.001) break;

                var nextY = Math.Min(y + step, y1);
                if (Math.Abs(nextY - y) < 0.001) break;
                sb.AppendLine("G1 X" + F(targetX) + " Y" + F(nextY));
                y = nextY;
                forward = !forward;
            }

            AddRectanglePass(sb, x0, y0, x1, y1, tool);
        }

        private static bool HasCountersinks(SheetPart part)
        {
            foreach (var hole in part.Holes)
            {
                if (hole.Countersunk && hole.CountersinkDiameterMm > hole.DiameterMm && hole.CountersinkDepthMm > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateToolCanMachinePart(SheetPart part, ToolDefinition holeTool, ToolDefinition contourTool)
        {
            foreach (var hole in part.Holes)
            {
                if (hole.DiameterMm < holeTool.DiameterMm * 0.95)
                {
                    throw new InvalidOperationException(
                        "Tool " + F(holeTool.DiameterMm) + "mm is te groot voor " + hole.Name +
                        " diameter " + F(hole.DiameterMm) + "mm in " + part.Name + ".");
                }

                if (hole.Countersunk && hole.CountersinkDiameterMm > 0 && hole.CountersinkDiameterMm < contourTool.DiameterMm * 0.95)
                {
                    throw new InvalidOperationException(
                        "Tool " + F(contourTool.DiameterMm) + "mm is te groot voor kopkamer " + hole.Name +
                        " diameter " + F(hole.CountersinkDiameterMm) + "mm in " + part.Name + ".");
                }
            }
        }

        private static bool SameTool(ToolDefinition left, ToolDefinition right)
        {
            return left != null && right != null && Math.Abs(left.DiameterMm - right.DiameterMm) < 0.001;
        }

        private static void DrillPeck(StringBuilder sb, ToolDefinition tool, MachineProfile machine, double materialThicknessMm)
        {
            var depth = 0.0;
            while (depth > -materialThicknessMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -materialThicknessMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            }
        }

        private static void AddOutsideRectangle(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm)
        {
            var points = ContourPoints(part, tool.RadiusMm);
            var start = points[0];
            var tabZ = -Math.Max(0, materialThicknessMm - tabHeightMm);

            sb.AppendLine();
            sb.AppendLine("(Buitencontour met automatische tool-offset)");
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));

            var depth = 0.0;
            while (depth > -materialThicknessMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -materialThicknessMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));

                if (part.UseTabs && Math.Abs(depth + materialThicknessMm) < 0.001)
                {
                    AddTabbedPolylinePass(sb, points, tool, tabWidthMm, tabZ, depth);
                }
                else
                {
                    AddPolylinePass(sb, points, tool);
                }
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool)
        {
            for (var i = 1; i < points.Count; i++)
            {
                sb.AppendLine("G1 X" + F(points[i].X) + " Y" + F(points[i].Y) + " F" + F(tool.FeedRateMmMin));
            }
        }

        private static void AddTabbedPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            for (var i = 1; i < points.Count; i++)
            {
                AddTabbedSegment(sb, points[i - 1], points[i], tool, tabWidth, tabZ, cutZ);
            }
        }

        private static void AddTabbedSegment(StringBuilder sb, Point2 start, Point2 end, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= tabWidth * 3.0)
            {
                sb.AppendLine("G1 X" + F(end.X) + " Y" + F(end.Y) + " F" + F(tool.FeedRateMmMin));
                return;
            }

            var half = tabWidth / 2.0;
            var t0 = Math.Max(0, (length / 2.0 - half) / length);
            var t1 = Math.Min(1, (length / 2.0 + half) / length);
            var before = new Point2(start.X + dx * t0, start.Y + dy * t0);
            var after = new Point2(start.X + dx * t1, start.Y + dy * t1);

            sb.AppendLine("G1 X" + F(before.X) + " Y" + F(before.Y) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 Z" + F(tabZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(after.X) + " Y" + F(after.Y) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(end.X) + " Y" + F(end.Y) + " F" + F(tool.FeedRateMmMin));
        }

        private static System.Collections.Generic.List<Point2> ContourPoints(SheetPart part, double radius)
        {
            var points = new System.Collections.Generic.List<Point2>();
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

        private static void AddNotchedPass(StringBuilder sb, SheetPart part, ToolDefinition tool)
        {
            var r = tool.RadiusMm;
            var n = part.CornerNotchSizeMm;
            var x0 = -r;
            var y0 = -r;
            var x1 = part.LengthMm + r;
            var y1 = part.WidthMm + r;
            var nx0 = n + r;
            var ny0 = n + r;
            var nx1 = part.LengthMm - n - r;
            var ny1 = part.WidthMm - n - r;

            sb.AppendLine("G1 X" + F(nx1) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 X" + F(nx1) + " Y" + F(ny0));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(ny0));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(ny1));
            sb.AppendLine("G1 X" + F(nx1) + " Y" + F(ny1));
            sb.AppendLine("G1 X" + F(nx1) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(nx0) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(nx0) + " Y" + F(ny1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(ny1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(ny0));
            sb.AppendLine("G1 X" + F(nx0) + " Y" + F(ny0));
            sb.AppendLine("G1 X" + F(nx0) + " Y" + F(y0));
            sb.AppendLine("G1 X" + F(nx1) + " Y" + F(y0));
        }

        private static void AddToeKickPass(StringBuilder sb, SheetPart part, ToolDefinition tool)
        {
            var r = tool.RadiusMm;
            var x0 = -r;
            var y0 = -r;
            var x1 = part.LengthMm + r;
            var y1 = part.WidthMm + r;
            var notchX = Math.Min(part.ToeKickDepthMm + r, x1);
            var notchY = Math.Min(part.ToeKickHeightMm + r, y1);

            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(notchY));
            sb.AppendLine("G1 X" + F(notchX) + " Y" + F(notchY));
            sb.AppendLine("G1 X" + F(notchX) + " Y" + F(y0));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y0));
        }

        private static void AddRectanglePass(StringBuilder sb, double x0, double y0, double x1, double y1, ToolDefinition tool)
        {
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(y1));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(y0));
        }

        private static void AddTabbedRectanglePass(StringBuilder sb, double x0, double y0, double x1, double y1, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            var midX = (x0 + x1) / 2.0;
            var midY = (y0 + y1) / 2.0;
            var halfTab = tabWidth / 2.0;

            sb.AppendLine("G1 X" + F(midX - halfTab) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 Z" + F(tabZ));
            sb.AppendLine("G1 X" + F(midX + halfTab) + " Y" + F(y0));
            sb.AppendLine("G1 Z" + F(cutZ));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y0));

            sb.AppendLine("G1 X" + F(x1) + " Y" + F(midY - halfTab));
            sb.AppendLine("G1 Z" + F(tabZ));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(midY + halfTab));
            sb.AppendLine("G1 Z" + F(cutZ));
            sb.AppendLine("G1 X" + F(x1) + " Y" + F(y1));

            sb.AppendLine("G1 X" + F(midX + halfTab) + " Y" + F(y1));
            sb.AppendLine("G1 Z" + F(tabZ));
            sb.AppendLine("G1 X" + F(midX - halfTab) + " Y" + F(y1));
            sb.AppendLine("G1 Z" + F(cutZ));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(y1));

            sb.AppendLine("G1 X" + F(x0) + " Y" + F(midY + halfTab));
            sb.AppendLine("G1 Z" + F(tabZ));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(midY - halfTab));
            sb.AppendLine("G1 Z" + F(cutZ));
            sb.AppendLine("G1 X" + F(x0) + " Y" + F(y0));
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
