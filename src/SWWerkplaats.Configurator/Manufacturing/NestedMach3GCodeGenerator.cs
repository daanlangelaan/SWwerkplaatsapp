using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class NestedMach3GCodeGenerator
    {
        private const double DefaultTabWidthMm = 8.0;
        private const double DefaultTabHeightMm = 1.5;

        public string Generate(NestedStockSheet stock, ToolDefinition tool, MachineProfile machine)
        {
            return Generate(stock, tool, machine, CamJobOptions.FromPrimaryTool(tool));
        }

        public string Generate(NestedStockSheet stock, ToolDefinition tool, MachineProfile machine, CamJobOptions jobOptions)
        {
            return Generate(stock, tool, machine, jobOptions, 1, 1, null);
        }

        public string Generate(NestedStockSheet stock, ToolDefinition tool, MachineProfile machine, CamJobOptions jobOptions, int plateNumber, int plateCount, string nextProgramFile)
        {
            var sb = new StringBuilder();
            if (jobOptions == null)
            {
                jobOptions = CamJobOptions.FromPrimaryTool(tool);
            }

            tool = tool ?? jobOptions.PrimaryTool;
            var contourTool = tool;
            var holeTool = FindHoleTool(jobOptions, contourTool);
            sb.AppendLine("(Project: " + stock.Name + ")");
            sb.AppendLine("(Plaat: " + Math.Max(1, plateNumber).ToString(CultureInfo.InvariantCulture) + " van " + Math.Max(1, plateCount).ToString(CultureInfo.InvariantCulture) + ")");
            sb.AppendLine("(Machine: " + machine.Name + ")");
            sb.AppendLine("(Voorraadplaat: " + stock.Material.Name + " " + F(stock.StockLengthMm) + " x " + F(stock.StockWidthMm) + " mm)");
            sb.AppendLine("(Contourtool: " + contourTool.Name + ", diameter " + F(contourTool.DiameterMm) + " mm)");
            sb.AppendLine("(Gatentool: " + holeTool.Name + ", diameter " + F(holeTool.DiameterMm) + " mm)");
            sb.AppendLine("(WERKSTUKNULPUNT: G54 X0/Y0 = links-onder van deze voorraadplaat)");
            sb.AppendLine("(Z0 = bovenzijde materiaal)");
            sb.AppendLine("(Let op: machine-home/machine-0 is alleen wissel-/parkeerpositie, niet het plaatnulpunt)");
            sb.AppendLine("(Initialisatie volgens veilige Mach3/Fusion stijl)");
            sb.AppendLine("G90 G94 G91.1 G40 G49 G17");
            sb.AppendLine("G21");
            sb.AppendLine("(Z-as naar machine-home voor veilige start)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");

            if (jobOptions.EnablePencilMarking)
            {
                new PencilMarkingGCodeGenerator().Append(sb, stock, machine, jobOptions.BuildPencilMarkingOptions());
            }

            var hasHoles = HasHoles(stock);
            if (hasHoles)
            {
                BeginTool(sb, ToolNumber(jobOptions, holeTool), holeTool.Name + " voor montagegaten", holeTool);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 1: alle gaten op geneste plaat ---)");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    var p = Transform(placement, hole.Xmm, hole.Ymm);
                    AddHole(sb, placement, hole, p.X, p.Y, holeTool, machine);
                }
            }

            if (!hasHoles || !SameTool(holeTool, contourTool))
            {
                BeginTool(sb, ToolNumber(jobOptions, contourTool), contourTool.Name + " voor groeven, kopkamers en contouren", contourTool);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 2: alle positioneergroeven / pockets op geneste plaat ---)");
            foreach (var placement in stock.Placements)
            {
                foreach (var pocket in placement.Part.Pockets)
                {
                    AddRectangularPocket(sb, placement, pocket, contourTool, machine);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: alle kopkamers op geneste plaat ---)");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    AddCountersink(sb, placement, hole, contourTool, machine);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 4: buitencontouren geneste onderdelen ---)");
            foreach (var placement in stock.Placements)
            {
                AddContour(sb, placement, contourTool, machine);
            }

            EndProgram(sb, plateNumber, plateCount, nextProgramFile);
            return sb.ToString();
        }

        private static void BeginTool(StringBuilder sb, int toolNumber, string description, ToolDefinition tool)
        {
            sb.AppendLine();
            sb.AppendLine("(Laad tool T" + toolNumber + ": " + description + ")");
            sb.AppendLine("(TOOLCHANGE: machine gaat eerst naar home/wisselpositie)");
            sb.AppendLine("M9");
            sb.AppendLine("M5");
            sb.AppendLine("(1/2 Z-as naar machine-home)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("(2/2 X/Y naar machine-home voor toolwissel)");
            sb.AppendLine("G28 G91 X0. Y0.");
            sb.AppendLine("G90");
            sb.AppendLine("(STOP: wissel nu naar T" + toolNumber + " - " + description + ")");
            sb.AppendLine("(Druk pas op Cycle Start als frees, spanmoer en Z0 gecontroleerd zijn.)");
            sb.AppendLine("M0");
            sb.AppendLine("T" + toolNumber + " M6");
            sb.AppendLine("G17 G90 G94");
            sb.AppendLine("G54");
            sb.AppendLine("(Controleer: G54 X0/Y0 moet links-onder op de plaat liggen; Z0 op bovenzijde materiaal)");
            sb.AppendLine("(Machine-home mag ergens anders liggen dan G54 plaatnulpunt)");
            sb.AppendLine("(Controleer tool en spanmoer voordat je start)");
            sb.AppendLine("M3 S" + F(tool.SpindleRpm));
        }

        private static void EndProgram(StringBuilder sb, int plateNumber, int plateCount, string nextProgramFile)
        {
            sb.AppendLine("M9");
            sb.AppendLine("M5");
            sb.AppendLine("(Einde programma: eerst Z naar machine-home)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("(Daarna X/Y naar machine-home)");
            sb.AppendLine("G28 G91 X0. Y0.");
            sb.AppendLine("G90");
            if (!string.IsNullOrWhiteSpace(nextProgramFile))
            {
                sb.AppendLine("(PLAAT " + plateNumber.ToString(CultureInfo.InvariantCulture) + " VAN " + plateCount.ToString(CultureInfo.InvariantCulture) + " KLAAR)");
                sb.AppendLine("(Machine staat op home. Plaats de volgende voorraadplaat.)");
                sb.AppendLine("(Start daarna bestand: " + nextProgramFile + ")");
                sb.AppendLine("(Controleer opspanning en zet/controleer Z0 op bovenzijde materiaal.)");
            }
            else
            {
                sb.AppendLine("(LAATSTE PLAAT KLAAR - machine staat op home)");
            }
            sb.AppendLine("M30");
        }

        private static ToolDefinition FindHoleTool(CamJobOptions jobOptions, ToolDefinition contourTool)
        {
            var best = contourTool;
            foreach (var candidate in jobOptions.Tools)
            {
                if (candidate.Kind != ToolKind.EndMill) continue;
                if (candidate.DiameterMm < best.DiameterMm)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static int ToolNumber(CamJobOptions jobOptions, ToolDefinition tool)
        {
            for (var i = 0; i < jobOptions.Tools.Count; i++)
            {
                if (SameTool(jobOptions.Tools[i], tool)) return i + 1;
            }

            return 1;
        }

        private static bool SameTool(ToolDefinition left, ToolDefinition right)
        {
            return left != null && right != null && Math.Abs(left.DiameterMm - right.DiameterMm) < 0.001;
        }

        private static bool HasHoles(NestedStockSheet stock)
        {
            foreach (var placement in stock.Placements)
            {
                if (placement.Part.Holes.Count > 0) return true;
            }

            return false;
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
            var cutDepth = HoleDepth(hole, placement.Part.Material.ThicknessMm);
            sb.AppendLine("(" + placement.Label + " - " + hole.Name + " diameter " + F(hole.DiameterMm) + ", diepte " + F(cutDepth) + ", centrum X" + F(x) + " Y" + F(y) + ")");
            if (hole.DiameterMm <= tool.DiameterMm + 0.05)
            {
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, cutDepth);
                return;
            }

            CircularPocket(sb, x, y, hole.DiameterMm, cutDepth, tool, machine);
        }

        private static double HoleDepth(SheetHole hole, double materialThicknessMm)
        {
            if (hole != null && hole.DepthMm > 0)
            {
                return Math.Min(hole.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));
            }

            return materialThicknessMm;
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

        private static void AddRectangularPocket(StringBuilder sb, NestedSheetPlacement placement, SheetPocket pocket, ToolDefinition tool, MachineProfile machine)
        {
            if (pocket == null || pocket.LengthMm <= 0 || pocket.WidthMm <= 0 || pocket.DepthMm <= 0)
            {
                return;
            }

            var inset = Math.Max(tool.RadiusMm, 0.1);
            var lx0 = pocket.Xmm + inset;
            var ly0 = pocket.Ymm + inset;
            var lx1 = pocket.Xmm + pocket.LengthMm - inset;
            var ly1 = pocket.Ymm + pocket.WidthMm - inset;
            if (lx1 <= lx0 || ly1 <= ly0)
            {
                lx0 = pocket.Xmm + pocket.LengthMm / 2.0;
                ly0 = pocket.Ymm + pocket.WidthMm / 2.0;
                lx1 = lx0;
                ly1 = ly0;
            }

            var p0 = Transform(placement, lx0, ly0);
            var p1 = Transform(placement, lx1, ly0);
            var p2 = Transform(placement, lx1, ly1);
            var p3 = Transform(placement, lx0, ly1);
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + pocket.Name + " pocket " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(pocket.DepthMm) + ")");
            sb.AppendLine("G0 X" + F(p0.X) + " Y" + F(p0.Y));
            var depth = 0.0;
            while (depth > -pocket.DepthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -pocket.DepthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPocketClearingPass(sb, placement, lx0, ly0, lx1, ly1, tool);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPocketClearingPass(StringBuilder sb, NestedSheetPlacement placement, double lx0, double ly0, double lx1, double ly1, ToolDefinition tool)
        {
            if (Math.Abs(lx1 - lx0) < 0.001 && Math.Abs(ly1 - ly0) < 0.001)
            {
                AddLocalMove(sb, placement, lx0, ly0, tool);
                return;
            }

            var step = Math.Max(1.0, tool.DiameterMm * 0.65);
            var y = ly0;
            var forward = true;
            while (true)
            {
                var targetX = forward ? lx1 : lx0;
                AddLocalMove(sb, placement, targetX, y, tool);
                if (Math.Abs(y - ly1) < 0.001) break;

                var nextY = Math.Min(y + step, ly1);
                if (Math.Abs(nextY - y) < 0.001) break;
                AddLocalMove(sb, placement, targetX, nextY, tool);
                y = nextY;
                forward = !forward;
            }

            AddLocalMove(sb, placement, lx1, ly0, tool);
            AddLocalMove(sb, placement, lx1, ly1, tool);
            AddLocalMove(sb, placement, lx0, ly1, tool);
            AddLocalMove(sb, placement, lx0, ly0, tool);
        }

        private static void AddLocalMove(StringBuilder sb, NestedSheetPlacement placement, double x, double y, ToolDefinition tool)
        {
            var p = Transform(placement, x, y);
            sb.AppendLine("G1 X" + F(p.X) + " Y" + F(p.Y) + " F" + F(tool.FeedRateMmMin));
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

                if (placement.Part.UseTabs && Math.Abs(depth + placement.Part.Material.ThicknessMm) < 0.001)
                {
                    var tabZ = -Math.Max(0, placement.Part.Material.ThicknessMm - DefaultTabHeightMm);
                    AddTabbedPolylinePass(sb, placement, points, tool, DefaultTabWidthMm, tabZ, depth);
                }
                else
                {
                    AddPolylinePass(sb, placement, points, tool);
                }
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPolylinePass(StringBuilder sb, NestedSheetPlacement placement, List<Point2> points, ToolDefinition tool)
        {
            for (var i = 1; i < points.Count; i++)
            {
                var p = Transform(placement, points[i].X, points[i].Y);
                sb.AppendLine("G1 X" + F(p.X) + " Y" + F(p.Y) + " F" + F(tool.FeedRateMmMin));
            }
        }

        private static void AddTabbedPolylinePass(StringBuilder sb, NestedSheetPlacement placement, List<Point2> points, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            for (var i = 1; i < points.Count; i++)
            {
                AddTabbedSegment(sb, placement, points[i - 1], points[i], tool, tabWidth, tabZ, cutZ);
            }
        }

        private static void AddTabbedSegment(StringBuilder sb, NestedSheetPlacement placement, Point2 start, Point2 end, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= tabWidth * 3.0)
            {
                var shortEndPoint = Transform(placement, end.X, end.Y);
                sb.AppendLine("G1 X" + F(shortEndPoint.X) + " Y" + F(shortEndPoint.Y) + " F" + F(tool.FeedRateMmMin));
                return;
            }

            var half = tabWidth / 2.0;
            var t0 = Math.Max(0, (length / 2.0 - half) / length);
            var t1 = Math.Min(1, (length / 2.0 + half) / length);
            var before = new Point2(start.X + dx * t0, start.Y + dy * t0);
            var after = new Point2(start.X + dx * t1, start.Y + dy * t1);
            var beforePoint = Transform(placement, before.X, before.Y);
            var afterPoint = Transform(placement, after.X, after.Y);
            var endPoint = Transform(placement, end.X, end.Y);

            sb.AppendLine("G1 X" + F(beforePoint.X) + " Y" + F(beforePoint.Y) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 Z" + F(tabZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(afterPoint.X) + " Y" + F(afterPoint.Y) + " F" + F(tool.FeedRateMmMin));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(endPoint.X) + " Y" + F(endPoint.Y) + " F" + F(tool.FeedRateMmMin));
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
