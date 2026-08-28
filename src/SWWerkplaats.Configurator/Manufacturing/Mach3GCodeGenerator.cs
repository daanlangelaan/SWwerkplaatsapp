using System;
using System.Globalization;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

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
            return GenerateSheetPart(part, holeTool, contourTool, null, false, false, 1.0, machine, materialThicknessMm, tabWidthMm, tabHeightMm);
        }

        public string GenerateSheetPart(SheetPart part, ToolDefinition holeTool, ToolDefinition contourTool, ToolDefinition vBitTool, bool enableWoodScrewCountersinks, bool enableOutsideEdgeChamfer, double edgeChamferWidthMm, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm)
        {
            return GenerateSheetPart(part, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, edgeChamferWidthMm, machine, materialThicknessMm, tabWidthMm, tabHeightMm, 1.0);
        }

        public string GenerateSheetPart(SheetPart part, ToolDefinition holeTool, ToolDefinition contourTool, ToolDefinition vBitTool, bool enableWoodScrewCountersinks, bool enableOutsideEdgeChamfer, double edgeChamferWidthMm, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm, double throughCutOvertravelMm)
        {
            return GenerateSheetPart(part, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, edgeChamferWidthMm, machine, materialThicknessMm, tabWidthMm, tabHeightMm, throughCutOvertravelMm, 15.0, 1.0, 1600.0, 30.0);
        }

        public string GenerateSheetPart(SheetPart part, ToolDefinition holeTool, ToolDefinition contourTool, ToolDefinition vBitTool, bool enableWoodScrewCountersinks, bool enableOutsideEdgeChamfer, double edgeChamferWidthMm, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm, double throughCutOvertravelMm, double safeTravelZMm, double contourOnionSkinMm, double finalContourFeedRateMmMin, double finalContourRampLengthMm, bool enableMonitoringMarkers = true)
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
            machine.SafeZmm = safeTravelZMm;

            var sb = new StringBuilder();
            throughCutOvertravelMm = Math.Max(0, throughCutOvertravelMm);
            Header(sb, part, contourTool, machine, materialThicknessMm, throughCutOvertravelMm, tabWidthMm, tabHeightMm, enableMonitoringMarkers);
            var hasSmallHoles = part.Holes.Exists(h => RequiresSmallHoleTool(h, contourTool));
            var hasLargeHoles = part.Holes.Exists(h => !RequiresSmallHoleTool(h, contourTool));
            var useVBitCountersinks = enableWoodScrewCountersinks && vBitTool != null;
            var useEdgeChamfer = enableOutsideEdgeChamfer && vBitTool != null && edgeChamferWidthMm > 0;
            var hasWoodScrewCountersinks = part.Holes.Any(IsWoodScrewCountersink);
            var hasContourToolCountersinks = part.Holes.Any(h => h.Countersunk
                && h.CountersinkDiameterMm > h.DiameterMm
                && h.CountersinkDepthMm > 0
                && (!useVBitCountersinks || !IsWoodScrewCountersink(h)));
            var contourToolNumber = hasSmallHoles ? 2 : 1;
            var vBitToolNumber = contourToolNumber + 1;

            if (hasSmallHoles)
            {
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 1: kleine gaten met 3mm-frees ---)");
                BeginTool(sb, 1, holeTool.Name + " voor kleine gaten", holeTool, enableMonitoringMarkers);
                GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 1, "Kleine gaten met 3mm-frees");
                foreach (var hole in part.Holes.Where(h => RequiresSmallHoleTool(h, contourTool)))
                {
                    AddHole(sb, part, hole, holeTool, machine, materialThicknessMm, throughCutOvertravelMm);
                }
            }

            if (!hasSmallHoles || !SameTool(holeTool, contourTool))
            {
                BeginTool(sb, contourToolNumber, contourTool.Name + " voor grote gaten, groeven, kopkamers en buitencontour", contourTool, enableMonitoringMarkers);
            }

            if (hasLargeHoles)
            {
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 2: gaten vanaf 6mm met contourfrees ---)");
                GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 2, "Gaten vanaf 6mm met contourfrees");
                foreach (var hole in part.Holes.Where(h => !RequiresSmallHoleTool(h, contourTool)))
                {
                    AddHole(sb, part, hole, contourTool, machine, materialThicknessMm, throughCutOvertravelMm);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: alle positioneergroeven / pockets ---)");
            if (part.Pockets.Count > 0) GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 3, "Positioneergroeven en pockets");
            foreach (var pocket in part.Pockets)
            {
                AddRectangularPocket(sb, part, pocket, contourTool, machine, materialThicknessMm, throughCutOvertravelMm);
            }

            if (HasCountersinks(part))
            {
                sb.AppendLine();
                sb.AppendLine("(--- BEWERKING 4: alle kopkamers helix-frezen ---)");
                if (hasContourToolCountersinks) GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 4, "Kopkamers");
                foreach (var hole in part.Holes)
                {
                    if (!useVBitCountersinks || !IsWoodScrewCountersink(hole))
                        AddCountersink(sb, part, hole, contourTool, machine);
                }
            }

            if (useVBitCountersinks || useEdgeChamfer)
            {
                BeginTool(sb, vBitToolNumber, vBitTool.Name + " voor verzinken en/of randafwerking", vBitTool, enableMonitoringMarkers);
                if (useVBitCountersinks)
                {
                    sb.AppendLine();
                    sb.AppendLine("(--- BEWERKING 5: hout-op-hout schroefgaten met V-frees verzinken ---)");
                    if (hasWoodScrewCountersinks) GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 5, "Hout-op-hout schroefgaten verzinken");
                    foreach (var hole in part.Holes.Where(IsWoodScrewCountersink))
                        AddVBitCountersink(sb, part, hole, vBitTool, machine);
                }
                if (useEdgeChamfer)
                {
                    sb.AppendLine();
                    sb.AppendLine("(--- BEWERKING 6: volledige buitencontour afschuinen met V-frees ---)");
                    GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 6, "Buitencontour afschuinen");
                    AddEdgeChamfer(sb, part, vBitTool, machine, edgeChamferWidthMm);
                }
                BeginTool(sb, contourToolNumber, contourTool.Name + " voor buitencontour", contourTool, enableMonitoringMarkers);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 7: buitencontour voorfrezen; plaat blijft gesloten ---)");
            GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 7, "Buitencontour voorfrezen");
            AddOutsideRectangleRough(sb, part, contourTool, machine, materialThicknessMm, contourOnionSkinMm);
            AddFinalContourPause(sb, contourTool, machine, finalContourFeedRateMmMin, enableMonitoringMarkers);
            sb.AppendLine("(--- BEWERKING 8: LAATSTE CONTOURLAAG ---)");
            GCodeMonitoringMarkerWriter.AppendStep(sb, enableMonitoringMarkers, 8, "Laatste contourlaag");
            AddOutsideRectangleFinal(sb, part, contourTool, machine, materialThicknessMm, tabWidthMm, tabHeightMm, throughCutOvertravelMm, contourOnionSkinMm, finalContourFeedRateMmMin, finalContourRampLengthMm, enableMonitoringMarkers);

            EndProgram(sb);
            var result = sb.ToString();
            GCodeSafetyValidator.Validate(result, machine.SafeZmm, materialThicknessMm + throughCutOvertravelMm);
            return result;
        }

        private static void Header(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double throughCutOvertravelMm, double tabWidthMm, double tabHeightMm, bool enableMonitoringMarkers)
        {
            sb.AppendLine("(Project: " + part.Name + ")");
            sb.AppendLine("(Machine: " + machine.Name + ")");
            sb.AppendLine("(Tool: " + tool.Name + ", diameter " + F(tool.DiameterMm) + " mm)");
            sb.AppendLine("(Origin: links onder, Z0 op bovenzijde materiaal)");
            sb.AppendLine("(Door-en-door: plaat " + F(materialThicknessMm) + " mm + " + F(throughCutOvertravelMm) + " mm doorsteek = eind-Z " + F(-(materialThicknessMm + throughCutOvertravelMm)) + " mm)");
            sb.AppendLine("(Tabs: breedte " + F(tabWidthMm) + " mm, resterende hoogte " + F(tabHeightMm) + " mm vanaf plaatonderzijde)");
            GCodeMonitoringMarkerWriter.AppendProgramMetadata(sb, enableMonitoringMarkers, part.Name, null, null, machine.SafeZmm);
            sb.AppendLine("(Initialisatie volgens veilige Mach3/Fusion stijl)");
            sb.AppendLine("G90 G94 G91.1 G40 G49 G17");
            sb.AppendLine("G21");
            sb.AppendLine("(Z-as naar machine-home voor veilige start)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
        }

        private static void BeginTool(StringBuilder sb, int toolNumber, string description, ToolDefinition tool, bool enableMonitoringMarkers)
        {
            sb.AppendLine();
            GCodeMonitoringMarkerWriter.AppendToolChangeEvent(sb, enableMonitoringMarkers, toolNumber, description);
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
            sb.AppendLine("(Extra veiligheid na Cycle Start: Z opnieuw naar machine-home voordat XY naar het onderdeel beweegt)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("G17 G90 G94");
            sb.AppendLine("G54");
            sb.AppendLine("(Controleer tool, spanmoer en Z0 op bovenzijde materiaal voordat je start)");
            sb.AppendLine("M3 S" + F(tool.SpindleRpm));
        }

        private static void EndProgram(StringBuilder sb)
        {
            sb.AppendLine("M9");
            sb.AppendLine("M5");
            sb.AppendLine("(Einde programma: eerst Z naar machine-home)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("(Daarna X/Y naar machine-home)");
            sb.AppendLine("G28 G91 X0. Y0.");
            sb.AppendLine("G90");
            sb.AppendLine("M30");
        }

        private static void AddHole(StringBuilder sb, SheetPart part, SheetHole hole, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double throughCutOvertravelMm)
        {
            sb.AppendLine();
            var cutDepth = HoleDepth(hole, materialThicknessMm, throughCutOvertravelMm);
            var x = MirrorX(part, hole.Xmm);
            var y = hole.Ymm;
            sb.AppendLine("(" + hole.Name + " diameter " + F(hole.DiameterMm) + ", diepte " + F(cutDepth) + ", centrum X" + F(x) + " Y" + F(y) + ")");

            if (hole.DiameterMm <= tool.DiameterMm + 0.05)
            {
                sb.AppendLine("(Gatdiameter gelijk/kleiner dan frees: peck boren, niet in een keer door)");
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, cutDepth);
                return;
            }

            CircularPocket(sb, x, y, hole.DiameterMm, cutDepth, tool, machine);
        }

        private static void AddCountersink(StringBuilder sb, SheetPart part, SheetHole hole, ToolDefinition tool, MachineProfile machine)
        {
            if (!hole.Countersunk || hole.CountersinkDiameterMm <= hole.DiameterMm || hole.CountersinkDepthMm <= 0)
            {
                return;
            }

            sb.AppendLine();
            var x = MirrorX(part, hole.Xmm);
            var y = hole.Ymm;
            sb.AppendLine("(" + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(hole.CountersinkDepthMm) + ", centrum X" + F(x) + " Y" + F(y) + ")");

            if (hole.CountersinkDiameterMm <= tool.DiameterMm + 0.05)
            {
                sb.AppendLine("(Kopkamerdiameter gelijk/kleiner dan frees: peck boren, niet in een keer door)");
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, hole.CountersinkDepthMm);
                return;
            }

            CircularPocket(sb, x, y, hole.CountersinkDiameterMm, hole.CountersinkDepthMm, tool, machine);
        }

        private static void CircularPocket(StringBuilder sb, double x, double y, double diameterMm, double depthMm, ToolDefinition tool, MachineProfile machine)
        {
            var maximumRadius = (diameterMm - tool.DiameterMm) / 2.0;
            if (maximumRadius <= 0)
            {
                sb.AppendLine("(Circulaire bewerking gelijk/kleiner dan frees: peck boren, niet in een keer door)");
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, depthMm);
                return;
            }

            // Een enkele cirkel laat een volle schijf in het midden staan. De radiale stap houdt
            // overlap tussen de banen zodat een scharnierpot/tegenboring echt wordt leeggefreesd.
            var radialStep = Math.Max(0.5, tool.DiameterMm * 0.4);
            sb.AppendLine("(Circulaire pocket volledig uitfrezen: diameter " + F(diameterMm) + ", baanradius max " + F(maximumRadius) + ", radiale stap " + F(radialStep) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
            var depth = 0.0;
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -depthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                var radius = 0.0;
                while (radius < maximumRadius - 0.001)
                {
                    radius = Math.Min(maximumRadius, radius + radialStep);
                    var startX = x + radius;
                    sb.AppendLine("G1 X" + F(startX) + " Y" + F(y) + " F" + F(tool.FeedRateMmMin));
                    sb.AppendLine("G2 X" + F(startX) + " Y" + F(y) + " I" + F(-radius) + " J0 F" + F(tool.FeedRateMmMin));
                }
                sb.AppendLine("G1 X" + F(x) + " Y" + F(y) + " F" + F(tool.FeedRateMmMin));
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static double HoleDepth(SheetHole hole, double materialThicknessMm, double throughCutOvertravelMm)
        {
            if (hole != null && hole.DepthMode == OperationDepthMode.Through)
                return materialThicknessMm + Math.Max(0, throughCutOvertravelMm);

            if (hole != null && hole.DepthMm > 0)
                return Math.Min(hole.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));

            return Math.Max(0.1, materialThicknessMm - 0.1);
        }

        private static void AddRectangularPocket(StringBuilder sb, SheetPart part, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double throughCutOvertravelMm)
        {
            if (pocket == null || pocket.LengthMm <= 0 || pocket.WidthMm <= 0)
            {
                return;
            }
            if (IsCapsulePocket(pocket))
            {
                AddCapsulePocket(sb, part, pocket, tool, machine, materialThicknessMm, throughCutOvertravelMm);
                return;
            }
            if (IsDrawerPullFinishContour(pocket))
            {
                AddDrawerPullFinishContour(sb, part, pocket, tool, machine, materialThicknessMm, throughCutOvertravelMm);
                return;
            }
            var cutDepth = pocket.DepthMode == OperationDepthMode.Through
                ? materialThicknessMm + Math.Max(0, throughCutOvertravelMm)
                : Math.Min(pocket.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));
            if (cutDepth <= 0) return;

            var pocketX = part != null && part.MirrorInNestingX ? part.LengthMm - pocket.Xmm - pocket.LengthMm : pocket.Xmm;
            var inset = Math.Max(tool.RadiusMm, 0.1);
            var x0 = pocketX + inset;
            var y0 = pocket.Ymm + inset;
            var x1 = pocketX + pocket.LengthMm - inset;
            var y1 = pocket.Ymm + pocket.WidthMm - inset;
            ApplyShortEndOvercut(part, pocket, pocketX, tool, ref x0, ref y0, ref x1, ref y1);
            if (x1 <= x0 || y1 <= y0)
            {
                x0 = pocketX + pocket.LengthMm / 2.0;
                y0 = pocket.Ymm + pocket.WidthMm / 2.0;
                x1 = x0;
                y1 = y0;
            }

            sb.AppendLine();
            var operationName = pocket.DepthMode == OperationDepthMode.Through ? "door-uitsparing" : "pocket";
            sb.AppendLine("(" + pocket.Name + " " + operationName + " X" + F(pocket.Xmm) + " Y" + F(pocket.Ymm) + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(cutDepth) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(x0) + " Y" + F(y0));
            var depth = 0.0;
            while (depth > -cutDepth)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -cutDepth);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPocketClearingPass(sb, x0, y0, x1, y1, tool);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool IsCapsulePocket(SheetPocket pocket)
        {
            return pocket != null && string.Equals(pocket.Shape, "capsule", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddCapsulePocket(StringBuilder sb, SheetPart part, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double throughCutOvertravelMm)
        {
            var cutDepth = pocket.DepthMode == OperationDepthMode.Through
                ? materialThicknessMm + Math.Max(0, throughCutOvertravelMm)
                : Math.Min(pocket.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));
            var pocketX = part != null && part.MirrorInNestingX ? part.LengthMm - pocket.Xmm - pocket.LengthMm : pocket.Xmm;
            var points = CapsuleContourPoints(pocketX, pocket.Ymm, pocket.LengthMm, pocket.WidthMm, tool.RadiusMm);
            if (cutDepth <= 0 || points.Count < 2) return;

            sb.AppendLine();
            sb.AppendLine("(" + pocket.Name + " capsule " + (pocket.DepthMode == OperationDepthMode.Through ? "door-uitsparing" : "pocket") + " X" + F(pocket.Xmm) + " Y" + F(pocket.Ymm) + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(cutDepth) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(points[0].X) + " Y" + F(points[0].Y));
            var depth = 0.0;
            while (depth > -cutDepth)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -cutDepth);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPolylinePass(sb, points, tool);
            }
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool IsDrawerPullFinishContour(SheetPocket pocket)
        {
            return pocket != null
                && !string.IsNullOrWhiteSpace(pocket.Name)
                && pocket.Name.IndexOf("handgreep afwerkcontour", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddDrawerPullFinishContour(StringBuilder sb, SheetPart part, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double throughCutOvertravelMm)
        {
            var pocketX = part != null && part.MirrorInNestingX ? part.LengthMm - pocket.Xmm - pocket.LengthMm : pocket.Xmm;
            var clearedZ = -Math.Max(0.1, materialThicknessMm - 2.0);
            var cutZ = -(materialThicknessMm + Math.Max(0, throughCutOvertravelMm));
            var tabZ = -Math.Max(0.1, materialThicknessMm - 2.0);
            var points = CapsuleContourPoints(pocketX, pocket.Ymm, pocket.LengthMm, pocket.WidthMm, tool.RadiusMm);
            if (points.Count < 2) return;
            sb.AppendLine();
            sb.AppendLine("(Handgreep afwerkcontour: 2mm voorpocket, tabs 8x2mm, tussenafstand max 70mm)");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(points[0].X) + " Y" + F(points[0].Y));
            sb.AppendLine("G1 Z" + F(clearedZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            AddDistributedTabbedPolylinePass(sb, points, tool, 8.0, 70.0, tabZ, cutZ);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static System.Collections.Generic.List<Point2> CapsuleContourPoints(double x, double y, double length, double height, double toolRadius)
        {
            if (height > length)
            {
                var transposed = CapsuleContourPoints(y, x, height, length, toolRadius);
                var vertical = new System.Collections.Generic.List<Point2>();
                foreach (var point in transposed) vertical.Add(new Point2(point.Y, point.X));
                return vertical;
            }
            var points = new System.Collections.Generic.List<Point2>();
            var radius = height / 2.0;
            var pathRadius = radius - toolRadius;
            if (pathRadius <= 0 || length < height) return points;
            var leftX = x + radius;
            var rightX = x + length - radius;
            var centerY = y + radius;
            points.Add(new Point2(leftX, centerY + pathRadius));
            points.Add(new Point2(rightX, centerY + pathRadius));
            AddCapsuleArc(points, rightX, centerY, pathRadius, Math.PI / 2.0, -Math.PI / 2.0);
            points.Add(new Point2(leftX, centerY - pathRadius));
            AddCapsuleArc(points, leftX, centerY, pathRadius, -Math.PI / 2.0, -Math.PI * 1.5);
            return points;
        }

        private static void AddCapsuleArc(System.Collections.Generic.List<Point2> points, double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            const int segments = 16;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + (endAngle - startAngle) * i / segments;
                points.Add(new Point2(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
            }
        }

        private static void AddDistributedTabbedPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool, double tabWidth, double maxSpacing, double tabZ, double cutZ)
        {
            var total = 0.0;
            for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
            if (total <= 0) return;
            var tabCount = Math.Max(1, (int)Math.Ceiling(total / Math.Max(tabWidth * 2.0, maxSpacing)));
            var boundaries = new System.Collections.Generic.List<double>();
            for (var i = 0; i < tabCount; i++)
            {
                var center = (i + 0.5) * total / tabCount;
                boundaries.Add(Math.Max(0, center - tabWidth / 2.0));
                boundaries.Add(Math.Min(total, center + tabWidth / 2.0));
            }
            boundaries.Sort();
            var travelled = 0.0;
            var currentZ = cutZ;
            for (var segment = 1; segment < points.Count; segment++)
            {
                var start = points[segment - 1];
                var end = points[segment];
                var segmentLength = Distance(start, end);
                if (segmentLength <= 0) continue;
                var cuts = new System.Collections.Generic.List<double> { 0.0, segmentLength };
                foreach (var boundary in boundaries)
                    if (boundary > travelled + 0.0001 && boundary < travelled + segmentLength - 0.0001) cuts.Add(boundary - travelled);
                cuts.Sort();
                for (var i = 1; i < cuts.Count; i++)
                {
                    var from = cuts[i - 1];
                    var to = cuts[i];
                    var midpoint = travelled + (from + to) / 2.0;
                    var inTab = false;
                    for (var b = 0; b + 1 < boundaries.Count; b += 2)
                        if (midpoint >= boundaries[b] && midpoint <= boundaries[b + 1]) { inTab = true; break; }
                    var wantedZ = inTab ? tabZ : cutZ;
                    if (Math.Abs(wantedZ - currentZ) > 0.001)
                    {
                        sb.AppendLine("G1 Z" + F(wantedZ) + " F" + F(tool.PlungeRateMmMin));
                        currentZ = wantedZ;
                    }
                    var t = to / segmentLength;
                    sb.AppendLine("G1 X" + F(start.X + (end.X - start.X) * t) + " Y" + F(start.Y + (end.Y - start.Y) * t) + " F" + F(tool.FeedRateMmMin));
                }
                travelled += segmentLength;
            }
            if (Math.Abs(currentZ - cutZ) > 0.001) sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
        }

        private static double Distance(Point2 a, Point2 b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void ApplyShortEndOvercut(SheetPart part, SheetPocket pocket, double pocketX, ToolDefinition tool, ref double x0, ref double y0, ref double x1, ref double y1)
        {
            if (part == null || pocket == null || tool == null) return;

            var overcut = Math.Max(0, tool.RadiusMm);
            if (overcut <= 0) return;

            var horizontalSlot = pocket.LengthMm >= pocket.WidthMm;
            if (horizontalSlot)
            {
                if (pocketX <= 0.001) x0 = pocketX - overcut;
                if (pocketX + pocket.LengthMm >= part.LengthMm - 0.001) x1 = pocketX + pocket.LengthMm + overcut;
                return;
            }

            if (pocket.Ymm <= 0.001) y0 = pocket.Ymm - overcut;
            if (pocket.Ymm + pocket.WidthMm >= part.WidthMm - 0.001) y1 = pocket.Ymm + pocket.WidthMm + overcut;
        }

        private static void AddPocketClearingPass(StringBuilder sb, double x0, double y0, double x1, double y1, ToolDefinition tool)
        {
            if (Math.Abs(x1 - x0) < 0.001 && Math.Abs(y1 - y0) < 0.001)
            {
                sb.AppendLine("G1 X" + F(x0) + " Y" + F(y0) + " F" + F(tool.FeedRateMmMin));
                return;
            }

            var step = Math.Max(1.0, tool.DiameterMm * 0.45);
            var runVertical = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (runVertical)
            {
                var x = x0;
                var forwardY = true;
                while (true)
                {
                    var targetY = forwardY ? y1 : y0;
                    sb.AppendLine("G1 X" + F(x) + " Y" + F(targetY) + " F" + F(tool.FeedRateMmMin));
                    if (Math.Abs(x - x1) < 0.001) break;

                    var nextX = Math.Min(x + step, x1);
                    if (Math.Abs(nextX - x) < 0.001) break;
                    sb.AppendLine("G1 X" + F(nextX) + " Y" + F(targetY));
                    x = nextX;
                    forwardY = !forwardY;
                }

                AddRectanglePass(sb, x0, y0, x1, y1, tool);
                return;
            }

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

        private static double MirrorX(SheetPart part, double x)
        {
            return part != null && part.MirrorInNestingX ? part.LengthMm - x : x;
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

        private static bool HasHoles(SheetPart part)
        {
            return part != null && part.Holes.Count > 0;
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
            return left != null && right != null && left.Kind == right.Kind && Math.Abs(left.DiameterMm - right.DiameterMm) < 0.001;
        }

        private static bool IsWoodScrewCountersink(SheetHole hole)
        {
            return hole != null
                && hole.SupportKind == SheetHoleSupportKind.PanelScrew
                && hole.Countersunk
                && hole.CountersinkDiameterMm > hole.DiameterMm
                && hole.CountersinkDepthMm > 0;
        }

        private static void AddVBitCountersink(StringBuilder sb, SheetPart part, SheetHole hole, ToolDefinition tool, MachineProfile machine)
        {
            var depthMm = Math.Min(hole.CountersinkDepthMm, tool.MaximumCutDepthMm > 0 ? tool.MaximumCutDepthMm : hole.CountersinkDepthMm);
            var x = MirrorX(part, hole.Xmm);
            var y = hole.Ymm;
            sb.AppendLine();
            sb.AppendLine("(" + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(depthMm) + ", centrum X" + F(x) + " Y" + F(y) + ")");
            sb.AppendLine("(V-frees peck: 2 snijders, rechtsom, S" + F(tool.SpindleRpm) + ", plunge F" + F(tool.PlungeRateMmMin) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
            DrillPeck(sb, tool, machine, depthMm);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddEdgeChamfer(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine, double chamferWidthMm)
        {
            var halfAngleRad = Math.Max(1.0, tool.IncludedAngleDeg) * Math.PI / 360.0;
            var depthMm = chamferWidthMm / Math.Tan(halfAngleRad);
            if (tool.MaximumCutDepthMm > 0) depthMm = Math.Min(depthMm, tool.MaximumCutDepthMm);
            var points = ContourPoints(part, 0);
            var start = points[0];
            sb.AppendLine();
            sb.AppendLine("(V-frees randafwerking: afschuining " + F(chamferWidthMm) + "x" + F(depthMm) + "mm, volledige gesloten buitencontour)");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            sb.AppendLine("G1 Z" + F(-depthMm) + " F" + F(tool.PlungeRateMmMin));
            AddPolylinePass(sb, points, tool);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool RequiresSmallHoleTool(SheetHole hole, ToolDefinition contourTool)
        {
            return hole != null && contourTool != null && hole.DiameterMm < contourTool.DiameterMm - 0.05;
        }

        private static void DrillPeck(StringBuilder sb, ToolDefinition tool, MachineProfile machine, double materialThicknessMm)
        {
            var depth = 0.0;
            var step = Math.Max(0.1, tool.PassDepthMm);
            sb.AppendLine("(Peck stap max " + F(step) + " mm tot Z" + F(-materialThicknessMm) + ", iedere retract naar veilige Z+" + F(machine.SafeZmm) + ")");
            while (depth > -materialThicknessMm)
            {
                depth = Math.Max(depth - step, -materialThicknessMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            }
        }

        private static void AddOutsideRectangleRough(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double contourOnionSkinMm)
        {
            var points = ContourPoints(part, tool.RadiusMm);
            var start = points[0];
            var targetDepth = ContourDepthStrategy.RoughDepthMm(materialThicknessMm, contourOnionSkinMm);

            sb.AppendLine();
            sb.AppendLine("(Buitencontour voorfrezen tot Z" + F(-targetDepth) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));

            var depth = 0.0;
            while (depth > -targetDepth)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -targetDepth);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));

                AddPolylinePass(sb, points, tool);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddFinalContourPause(StringBuilder sb, ToolDefinition tool, MachineProfile machine, double finalFeed, bool enableMonitoringMarkers)
        {
            GCodeMonitoringMarkerWriter.AppendEvent(sb, enableMonitoringMarkers, "FINAL_CONTOUR_APPROACHING");
            sb.AppendLine("(LAATSTE CONTOURLAAG - BLIJF BIJ DE MACHINE)");
            sb.AppendLine("(Controleer vacuum, frees, spanmoer en werkstuk-Z0; hervat met Cycle Start.)");
            sb.AppendLine("(Eindvoeding " + F(finalFeed) + " mm/min; toerental " + F(tool.SpindleRpm) + " rpm.)");
            sb.AppendLine("M5");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("G28 G91 X0. Y0.");
            sb.AppendLine("G90");
            sb.AppendLine("M0");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("G17 G90 G94");
            sb.AppendLine("G54");
            sb.AppendLine("M3 S" + F(tool.SpindleRpm));
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddOutsideRectangleFinal(StringBuilder sb, SheetPart part, ToolDefinition tool, MachineProfile machine, double materialThicknessMm, double tabWidthMm, double tabHeightMm, double throughCutOvertravelMm, double contourOnionSkinMm, double finalFeed, double rampLengthMm, bool enableMonitoringMarkers)
        {
            var points = ContourPoints(part, tool.RadiusMm);
            var start = points[0];
            var roughZ = -ContourDepthStrategy.RoughDepthMm(materialThicknessMm, contourOnionSkinMm);
            var cutZ = -ContourDepthStrategy.FinalDepthMm(materialThicknessMm, throughCutOvertravelMm);
            sb.AppendLine("(Plaatdikte " + F(materialThicknessMm) + " mm: voorlaag Z" + F(roughZ) + ", eindlaag Z" + F(cutZ) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            sb.AppendLine("G1 Z" + F(roughZ) + " F" + F(tool.PlungeRateMmMin));
            GCodeMonitoringMarkerWriter.AppendEvent(sb, enableMonitoringMarkers, "FINAL_CONTOUR");
            var passPoints = new System.Collections.Generic.List<Point2>(points);
            if (points.Count > 1)
            {
                var dx = points[1].X - points[0].X;
                var dy = points[1].Y - points[0].Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                var rampLength = Math.Min(length, Math.Max(1, rampLengthMm));
                var t = length > 0.001 ? rampLength / length : 0;
                var rampEnd = new Point2(points[0].X + dx * t, points[0].Y + dy * t);
                sb.AppendLine("G1 X" + F(rampEnd.X) + " Y" + F(rampEnd.Y) + " Z" + F(cutZ) + " F" + F(finalFeed));
                passPoints.RemoveAt(0);
                passPoints.Insert(0, rampEnd);
            }
            if (part.UseTabs && part.CustomContour != null && part.CustomContour.Count >= 3)
                AddDistributedTabbedPolylinePass(sb, passPoints, tool, tabWidthMm, Math.Max(tabWidthMm * 2.0, PolylineLength(passPoints) / 4.0), -Math.Max(0, materialThicknessMm - tabHeightMm), cutZ);
            else if (part.UseTabs)
                AddTabbedPolylinePass(sb, passPoints, tool, tabWidthMm, -Math.Max(0, materialThicknessMm - tabHeightMm), cutZ, finalFeed);
            else
                AddPolylinePass(sb, passPoints, tool, finalFeed);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool)
        {
            AddPolylinePass(sb, points, tool, tool.FeedRateMmMin);
        }

        private static double PolylineLength(System.Collections.Generic.List<Point2> points)
        {
            var total = 0.0;
            for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
            return total;
        }

        private static void AddPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool, double feedRate)
        {
            for (var i = 1; i < points.Count; i++)
            {
                sb.AppendLine("G1 X" + F(points[i].X) + " Y" + F(points[i].Y) + " F" + F(feedRate));
            }
        }

        private static void AddTabbedPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            AddTabbedPolylinePass(sb, points, tool, tabWidth, tabZ, cutZ, tool.FeedRateMmMin);
        }

        private static void AddTabbedPolylinePass(StringBuilder sb, System.Collections.Generic.List<Point2> points, ToolDefinition tool, double tabWidth, double tabZ, double cutZ, double feedRate)
        {
            for (var i = 1; i < points.Count; i++)
            {
                AddTabbedSegment(sb, points[i - 1], points[i], tool, tabWidth, tabZ, cutZ, feedRate);
            }
        }

        private static void AddTabbedSegment(StringBuilder sb, Point2 start, Point2 end, ToolDefinition tool, double tabWidth, double tabZ, double cutZ)
        {
            AddTabbedSegment(sb, start, end, tool, tabWidth, tabZ, cutZ, tool.FeedRateMmMin);
        }

        private static void AddTabbedSegment(StringBuilder sb, Point2 start, Point2 end, ToolDefinition tool, double tabWidth, double tabZ, double cutZ, double feedRate)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= tabWidth * 3.0)
            {
                sb.AppendLine("G1 X" + F(end.X) + " Y" + F(end.Y) + " F" + F(feedRate));
                return;
            }

            var half = tabWidth / 2.0;
            var t0 = Math.Max(0, (length / 2.0 - half) / length);
            var t1 = Math.Min(1, (length / 2.0 + half) / length);
            var before = new Point2(start.X + dx * t0, start.Y + dy * t0);
            var after = new Point2(start.X + dx * t1, start.Y + dy * t1);

            sb.AppendLine("G1 X" + F(before.X) + " Y" + F(before.Y) + " F" + F(feedRate));
            sb.AppendLine("G1 Z" + F(tabZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(after.X) + " Y" + F(after.Y) + " F" + F(feedRate));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(end.X) + " Y" + F(end.Y) + " F" + F(feedRate));
        }

        private static System.Collections.Generic.List<Point2> ContourPoints(SheetPart part, double radius)
        {
            var points = new System.Collections.Generic.List<Point2>();
            var custom = SheetContourGeometry.ToolCenterContour(part, radius);
            if (custom.Count > 0)
            {
                foreach (var point in custom) points.Add(new Point2(point.Xmm, point.Ymm));
                return MaybeMirrorContour(part, points);
            }
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
                return MaybeMirrorContour(part, points);
            }

            if (!part.HasCornerNotches)
            {
                if (part.CornerRadiusMm > 0.001)
                {
                    AddRoundedRectangleContour(points, part.LengthMm, part.WidthMm, part.CornerRadiusMm, radius);
                    return MaybeMirrorContour(part, points);
                }
                points.Add(new Point2(x0, y0));
                points.Add(new Point2(x1, y0));
                points.Add(new Point2(x1, y1));
                points.Add(new Point2(x0, y1));
                points.Add(new Point2(x0, y0));
                return MaybeMirrorContour(part, points);
            }

            var nl = part.CornerNotchLengthMm > 0 ? part.CornerNotchLengthMm : part.CornerNotchSizeMm;
            var nw = part.CornerNotchWidthMm > 0 ? part.CornerNotchWidthMm : part.CornerNotchSizeMm;
            var nx0 = nl + radius;
            var ny0 = nw + radius;
            var nx1 = part.LengthMm - nl - radius;
            var ny1 = part.WidthMm - nw - radius;
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
            return MaybeMirrorContour(part, points);
        }

        private static void AddRoundedRectangleContour(System.Collections.Generic.List<Point2> points, double length, double width, double cornerRadius, double toolRadius)
        {
            var r = Math.Max(0, Math.Min(cornerRadius, Math.Min(length, width) / 2.0));
            var pathRadius = r + toolRadius;
            points.Add(new Point2(r, -toolRadius));
            points.Add(new Point2(length - r, -toolRadius));
            AddContourArc(points, length - r, r, pathRadius, -Math.PI / 2.0, 0);
            points.Add(new Point2(length + toolRadius, width - r));
            AddContourArc(points, length - r, width - r, pathRadius, 0, Math.PI / 2.0);
            points.Add(new Point2(r, width + toolRadius));
            AddContourArc(points, r, width - r, pathRadius, Math.PI / 2.0, Math.PI);
            points.Add(new Point2(-toolRadius, r));
            AddContourArc(points, r, r, pathRadius, Math.PI, Math.PI * 1.5);
        }

        private static void AddContourArc(System.Collections.Generic.List<Point2> points, double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            const int segments = 8;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + (endAngle - startAngle) * i / segments;
                points.Add(new Point2(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
            }
        }

        private static System.Collections.Generic.List<Point2> MaybeMirrorContour(SheetPart part, System.Collections.Generic.List<Point2> points)
        {
            if (part == null || !part.MirrorInNestingX) return points;

            var mirrored = new System.Collections.Generic.List<Point2>();
            foreach (var point in points)
            {
                mirrored.Add(new Point2(part.LengthMm - point.X, point.Y));
            }

            return mirrored;
        }

        private static void AddNotchedPass(StringBuilder sb, SheetPart part, ToolDefinition tool)
        {
            var r = tool.RadiusMm;
            var nl = part.CornerNotchLengthMm > 0 ? part.CornerNotchLengthMm : part.CornerNotchSizeMm;
            var nw = part.CornerNotchWidthMm > 0 ? part.CornerNotchWidthMm : part.CornerNotchSizeMm;
            var x0 = -r;
            var y0 = -r;
            var x1 = part.LengthMm + r;
            var y1 = part.WidthMm + r;
            var nx0 = nl + r;
            var ny0 = nw + r;
            var nx1 = part.LengthMm - nl - r;
            var ny1 = part.WidthMm - nw - r;

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
