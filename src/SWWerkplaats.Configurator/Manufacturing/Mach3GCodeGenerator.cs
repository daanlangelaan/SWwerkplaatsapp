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
            if (part == null) throw new ArgumentNullException("part");
            if (tool == null) throw new ArgumentNullException("tool");
            if (machine == null) throw new ArgumentNullException("machine");
            if (part.LengthMm > machine.MaxXmm || part.WidthMm > machine.MaxYmm)
            {
                throw new InvalidOperationException("Plaatdeel past niet binnen het machinebereik.");
            }
            ValidateToolCanMachinePart(part, tool);

            var sb = new StringBuilder();
            Header(sb, part, tool, machine);

            if (HasCountersinks(part))
            {
                BeginTool(sb, 1, tool.Name + " voor kopkamers, gaten en buitencontour", tool);
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 1: alle kopkamers helix-frezen ---)");
                foreach (var hole in part.Holes)
                {
                    AddCountersink(sb, hole, tool, machine);
                }
            }
            else
            {
                BeginTool(sb, 1, tool.Name + " voor gaten en buitencontour", tool);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 2: alle door-en-door montagegaten ---)");
            foreach (var hole in part.Holes)
            {
                AddHole(sb, hole, tool, machine, materialThicknessMm);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: buitencontour ---)");
            AddOutsideRectangle(sb, part, tool, machine, materialThicknessMm, tabWidthMm, tabHeightMm);

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
            sb.AppendLine("(" + hole.Name + " diameter " + F(hole.DiameterMm) + ", centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ")");

            if (hole.DiameterMm <= tool.DiameterMm * 1.15)
            {
                sb.AppendLine("(Start op centrum, gat bijna gelijk aan tooldiameter)");
                sb.AppendLine("G0 X" + F(hole.Xmm) + " Y" + F(hole.Ymm));
                DrillPeck(sb, tool, machine, materialThicknessMm);
                return;
            }

            var radius = (hole.DiameterMm - tool.DiameterMm) / 2.0;
            var startX = hole.Xmm + radius;
            sb.AppendLine("(Centrum X" + F(hole.Xmm) + " Y" + F(hole.Ymm) + ", freesbaan start X" + F(startX) + " Y" + F(hole.Ymm) + ", baanradius " + F(radius) + ")");
            sb.AppendLine("G0 X" + F(startX) + " Y" + F(hole.Ymm));

            var depth = 0.0;
            while (depth > -materialThicknessMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -materialThicknessMm);
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

            if (hole.CountersinkDiameterMm <= tool.DiameterMm * 1.15)
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

        private static void ValidateToolCanMachinePart(SheetPart part, ToolDefinition tool)
        {
            foreach (var hole in part.Holes)
            {
                if (hole.DiameterMm < tool.DiameterMm * 0.95)
                {
                    throw new InvalidOperationException(
                        "Tool " + F(tool.DiameterMm) + "mm is te groot voor " + hole.Name +
                        " diameter " + F(hole.DiameterMm) + "mm in " + part.Name + ".");
                }

                if (hole.Countersunk && hole.CountersinkDiameterMm > 0 && hole.CountersinkDiameterMm < tool.DiameterMm * 0.95)
                {
                    throw new InvalidOperationException(
                        "Tool " + F(tool.DiameterMm) + "mm is te groot voor kopkamer " + hole.Name +
                        " diameter " + F(hole.CountersinkDiameterMm) + "mm in " + part.Name + ".");
                }
            }
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
            var r = tool.RadiusMm;
            var x0 = -r;
            var y0 = -r;
            var x1 = part.LengthMm + r;
            var y1 = part.WidthMm + r;
            var tabZ = -Math.Max(0, materialThicknessMm - tabHeightMm);

            sb.AppendLine();
            sb.AppendLine("(Buitencontour met automatische tool-offset)");
            sb.AppendLine("G0 X" + F(x0) + " Y" + F(y0));

            var depth = 0.0;
            while (depth > -materialThicknessMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -materialThicknessMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));

                if (part.HasCornerNotches)
                {
                    AddNotchedPass(sb, part, tool);
                }
                else if (part.HasToeKickNotch)
                {
                    AddToeKickPass(sb, part, tool);
                }
                else if (part.UseTabs && Math.Abs(depth + materialThicknessMm) < 0.001)
                {
                    AddTabbedRectanglePass(sb, x0, y0, x1, y1, tool, tabWidthMm, tabZ, depth);
                }
                else
                {
                    AddRectanglePass(sb, x0, y0, x1, y1, tool);
                }
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
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
    }
}
