using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class NestedMach3GCodeGenerator
    {
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

            machine.SafeZmm = jobOptions.SafeTravelZMm;

            tool = tool ?? jobOptions.PrimaryTool;
            var contourTool = tool;
            var holeTool = FindHoleTool(jobOptions, contourTool);
            var vBitTool = FindTool(jobOptions, ToolKind.VBit);
            var useVBitCountersinks = jobOptions.EnableWoodScrewCountersinks && vBitTool != null;
            var useEdgeChamfer = jobOptions.EnableOutsideEdgeChamfer && vBitTool != null && jobOptions.EdgeChamferWidthMm > 0;
            var throughCutOvertravelMm = Math.Max(0, jobOptions.ThroughCutOvertravelMm);
            sb.AppendLine("(Project: " + stock.Name + ")");
            sb.AppendLine("(Plaat: " + Math.Max(1, plateNumber).ToString(CultureInfo.InvariantCulture) + " van " + Math.Max(1, plateCount).ToString(CultureInfo.InvariantCulture) + ")");
            sb.AppendLine("(Machine: " + machine.Name + ")");
            sb.AppendLine("(Voorraadplaat: " + stock.Material.Name + " " + F(stock.StockLengthMm) + " x " + F(stock.StockWidthMm) + " mm)");
            sb.AppendLine("(Contourtool: " + contourTool.Name + ", diameter " + F(contourTool.DiameterMm) + " mm)");
            sb.AppendLine("(Gatentool: " + holeTool.Name + ", diameter " + F(holeTool.DiameterMm) + " mm)");
            sb.AppendLine("(WERKSTUKNULPUNT: G54 X0/Y0 = links-onder van deze voorraadplaat)");
            sb.AppendLine("(Z0 = bovenzijde materiaal)");
            sb.AppendLine("(Door-en-doorbewerkingen gaan " + F(throughCutOvertravelMm) + " mm onder de werkelijke plaatonderzijde)");
            sb.AppendLine("(Enige veilige retract- en transporthoogte: Z+" + F(machine.SafeZmm) + " mm)");
            GCodeMonitoringMarkerWriter.AppendProgramMetadata(
                sb,
                jobOptions.EnableMonitoringMarkers,
                stock.Name,
                Math.Max(1, plateNumber),
                Math.Max(1, plateCount),
                machine.SafeZmm);
            sb.AppendLine("(Buitencontouren: voorfrezen met " + F(jobOptions.ContourOnionSkinMm) + " mm restmateriaal; eindlaag na bewaakte M0-pauze)");
            sb.AppendLine("(Tabs: breedte " + F(jobOptions.TabWidthMm) + " mm, resterende hoogte " + F(jobOptions.TabHeightMm) + " mm vanaf plaatonderzijde)");
            sb.AppendLine("(Let op: machine-home/machine-0 is alleen wissel-/parkeerpositie, niet het plaatnulpunt)");
            sb.AppendLine("(Initialisatie volgens veilige Mach3/Fusion stijl)");
            sb.AppendLine("G90 G94 G91.1 G40 G49 G17");
            sb.AppendLine("G21");
            sb.AppendLine("(Z-as naar machine-home voor veilige start)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");

            if (jobOptions.EnablePencilMarking)
            {
                new PencilMarkingGCodeGenerator().Append(sb, stock, machine, jobOptions.BuildPencilMarkingOptions(), jobOptions.EnableMonitoringMarkers);
            }

            var hasSmallHoles = HasSmallHoles(stock, contourTool);
            var hasLargeHoles = HasLargeHoles(stock, contourTool);
            var hasPockets = HasPockets(stock);
            var hasWoodScrewCountersinks = HasWoodScrewCountersinks(stock);
            var hasContourToolCountersinks = HasContourToolCountersinks(stock, useVBitCountersinks);
            if (hasSmallHoles)
            {
                BeginTool(sb, ToolNumber(jobOptions, holeTool), holeTool.Name + " voor kleine gaten", holeTool, jobOptions.EnableMonitoringMarkers);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 1: kleine gaten met 3mm-frees ---)");
            if (hasSmallHoles) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 1, "Kleine gaten met 3mm-frees");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (!RequiresSmallHoleTool(hole, contourTool)) continue;
                    var p = Transform(placement, hole.Xmm, hole.Ymm);
                    AddHole(sb, placement, hole, p.X, p.Y, holeTool, machine, throughCutOvertravelMm);
                }
            }

            if (!hasSmallHoles || !SameTool(holeTool, contourTool))
            {
                BeginTool(sb, ToolNumber(jobOptions, contourTool), contourTool.Name + " voor grote gaten, groeven, kopkamers en contouren", contourTool, jobOptions.EnableMonitoringMarkers);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 2: gaten vanaf 6mm met contourfrees ---)");
            if (hasLargeHoles) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 2, "Gaten vanaf 6mm met contourfrees");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (RequiresSmallHoleTool(hole, contourTool)) continue;
                    var p = Transform(placement, hole.Xmm, hole.Ymm);
                    AddHole(sb, placement, hole, p.X, p.Y, contourTool, machine, throughCutOvertravelMm);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 3: alle positioneergroeven / pockets op geneste plaat ---)");
            if (hasPockets) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 3, "Positioneergroeven en pockets");
            foreach (var placement in stock.Placements)
            {
                foreach (var pocket in placement.Part.Pockets)
                {
                    AddRectangularPocket(sb, placement, pocket, contourTool, machine, throughCutOvertravelMm);
                }
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 4: alle kopkamers op geneste plaat ---)");
            if (hasContourToolCountersinks) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 4, "Kopkamers");
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (!useVBitCountersinks || !IsWoodScrewCountersink(hole))
                        AddCountersink(sb, placement, hole, contourTool, machine);
                }
            }

            if (useVBitCountersinks || useEdgeChamfer)
            {
                BeginTool(sb, ToolNumber(jobOptions, vBitTool), vBitTool.Name + " voor verzinken en/of randafwerking", vBitTool, jobOptions.EnableMonitoringMarkers);
                if (useVBitCountersinks)
                {
                    sb.AppendLine();
                    sb.AppendLine("(--- BEWERKING 5: hout-op-hout schroefgaten met V-frees verzinken ---)");
                    if (hasWoodScrewCountersinks) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 5, "Hout-op-hout schroefgaten verzinken");
                    foreach (var placement in stock.Placements)
                    {
                        foreach (var hole in placement.Part.Holes)
                        {
                            if (IsWoodScrewCountersink(hole)) AddVBitCountersink(sb, placement, hole, vBitTool, machine);
                        }
                    }
                }

                if (useEdgeChamfer)
                {
                    sb.AppendLine();
                    sb.AppendLine("(--- BEWERKING 6: volledige buitencontour 1mm afschuinen met V-frees ---)");
                    if (stock.Placements.Count > 0) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 6, "Buitencontour afschuinen");
                    foreach (var placement in stock.Placements)
                    {
                        AddEdgeChamfer(sb, placement, vBitTool, machine, jobOptions.EdgeChamferWidthMm);
                    }
                }

                BeginTool(sb, ToolNumber(jobOptions, contourTool), contourTool.Name + " voor buitencontouren", contourTool, jobOptions.EnableMonitoringMarkers);
            }

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 7: buitencontouren voorfrezen; plaat blijft overal gesloten ---)");
            if (stock.Placements.Count > 0) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 7, "Buitencontour voorfrezen");
            foreach (var placement in stock.Placements)
            {
                AddContourRough(sb, placement, contourTool, machine, jobOptions);
            }

            AddFinalContourPause(sb, contourTool, machine, jobOptions);
            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 8: LAATSTE CONTOURLAAG; kleine/risicovolle delen eerst ---)");
            if (stock.Placements.Count > 0) GCodeMonitoringMarkerWriter.AppendStep(sb, jobOptions.EnableMonitoringMarkers, 8, "Laatste contourlaag");
            var finalPlacements = new List<NestedSheetPlacement>(stock.Placements);
            finalPlacements.Sort(delegate(NestedSheetPlacement a, NestedSheetPlacement b)
            {
                var areaA = a.Part.LengthMm * a.Part.WidthMm;
                var areaB = b.Part.LengthMm * b.Part.WidthMm;
                return areaA.CompareTo(areaB);
            });
            for (var finalIndex = 0; finalIndex < finalPlacements.Count; finalIndex++)
            {
                AddContourFinal(sb, finalPlacements[finalIndex], contourTool, machine, jobOptions, finalIndex == 0);
            }

            EndProgram(sb, plateNumber, plateCount, nextProgramFile);
            var result = sb.ToString();
            GCodeSafetyValidator.Validate(result, machine.SafeZmm, stock.Material.ThicknessMm + throughCutOvertravelMm);
            return result;
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
            sb.AppendLine("(Extra veiligheid na Cycle Start: Z opnieuw naar machine-home voordat XY naar de plaat beweegt)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
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

        private static ToolDefinition FindTool(CamJobOptions jobOptions, ToolKind kind)
        {
            foreach (var candidate in jobOptions.Tools)
            {
                if (candidate.Kind == kind) return candidate;
            }
            return null;
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

        private static void AddVBitCountersink(StringBuilder sb, NestedSheetPlacement placement, SheetHole hole, ToolDefinition tool, MachineProfile machine)
        {
            var depthMm = Math.Min(hole.CountersinkDepthMm, tool.MaximumCutDepthMm > 0 ? tool.MaximumCutDepthMm : hole.CountersinkDepthMm);
            var p = Transform(placement, hole.Xmm, hole.Ymm);
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(depthMm) + ", centrum X" + F(p.X) + " Y" + F(p.Y) + ")");
            sb.AppendLine("(V-frees peck: 2 snijders, rechtsom, S" + F(tool.SpindleRpm) + ", plunge F" + F(tool.PlungeRateMmMin) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(p.X) + " Y" + F(p.Y));
            DrillPeck(sb, tool, machine, depthMm);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddEdgeChamfer(StringBuilder sb, NestedSheetPlacement placement, ToolDefinition tool, MachineProfile machine, double chamferWidthMm)
        {
            var halfAngleRad = Math.Max(1.0, tool.IncludedAngleDeg) * Math.PI / 360.0;
            var depthMm = chamferWidthMm / Math.Tan(halfAngleRad);
            if (tool.MaximumCutDepthMm > 0) depthMm = Math.Min(depthMm, tool.MaximumCutDepthMm);
            var points = ContourPoints(placement.Part, 0);
            var start = Transform(placement, points[0].X, points[0].Y);
            sb.AppendLine();
            sb.AppendLine("(V-frees randafwerking " + placement.Label + ": afschuining " + F(chamferWidthMm) + "x" + F(depthMm) + "mm, volledige gesloten buitencontour)");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            sb.AppendLine("G1 Z" + F(-depthMm) + " F" + F(tool.PlungeRateMmMin));
            AddPolylinePass(sb, placement, points, tool, tool.FeedRateMmMin);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool HasSmallHoles(NestedStockSheet stock, ToolDefinition contourTool)
        {
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (RequiresSmallHoleTool(hole, contourTool)) return true;
                }
            }

            return false;
        }

        private static bool HasLargeHoles(NestedStockSheet stock, ToolDefinition contourTool)
        {
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (!RequiresSmallHoleTool(hole, contourTool)) return true;
                }
            }

            return false;
        }

        private static bool HasPockets(NestedStockSheet stock)
        {
            foreach (var placement in stock.Placements)
            {
                if (placement.Part.Pockets.Count > 0) return true;
            }

            return false;
        }

        private static bool HasWoodScrewCountersinks(NestedStockSheet stock)
        {
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (IsWoodScrewCountersink(hole)) return true;
                }
            }

            return false;
        }

        private static bool HasContourToolCountersinks(NestedStockSheet stock, bool useVBitCountersinks)
        {
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    if (!hole.Countersunk || hole.CountersinkDiameterMm <= hole.DiameterMm || hole.CountersinkDepthMm <= 0) continue;
                    if (!useVBitCountersinks || !IsWoodScrewCountersink(hole)) return true;
                }
            }

            return false;
        }

        private static bool RequiresSmallHoleTool(SheetHole hole, ToolDefinition contourTool)
        {
            return hole != null && contourTool != null && hole.DiameterMm < contourTool.DiameterMm - 0.05;
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

        private static void AddHole(StringBuilder sb, NestedSheetPlacement placement, SheetHole hole, double x, double y, ToolDefinition tool, MachineProfile machine, double throughCutOvertravelMm)
        {
            sb.AppendLine();
            var cutDepth = HoleDepth(hole, placement.Part.Material.ThicknessMm, throughCutOvertravelMm);
            sb.AppendLine("(" + placement.Label + " - " + hole.Name + " diameter " + F(hole.DiameterMm) + ", diepte " + F(cutDepth) + ", centrum X" + F(x) + " Y" + F(y) + ")");
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

        private static double HoleDepth(SheetHole hole, double materialThicknessMm, double throughCutOvertravelMm)
        {
            if (hole != null && hole.DepthMode == OperationDepthMode.Through)
                return materialThicknessMm + Math.Max(0, throughCutOvertravelMm);

            if (hole != null && hole.DepthMm > 0)
                return Math.Min(hole.DepthMm, Math.Max(0.1, materialThicknessMm - 0.1));

            return Math.Max(0.1, materialThicknessMm - 0.1);
        }

        private static void CircularPocket(StringBuilder sb, double x, double y, double diameter, double depthMm, ToolDefinition tool, MachineProfile machine)
        {
            var maximumRadius = (diameter - tool.DiameterMm) / 2.0;
            if (maximumRadius <= 0)
            {
                sb.AppendLine("(Circulaire bewerking gelijk/kleiner dan frees: peck boren, niet in een keer door)");
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                sb.AppendLine("G0 X" + F(x) + " Y" + F(y));
                DrillPeck(sb, tool, machine, depthMm);
                return;
            }

            var radialStep = Math.Max(0.5, tool.DiameterMm * 0.4);
            sb.AppendLine("(Circulaire pocket volledig uitfrezen: diameter " + F(diameter) + ", baanradius max " + F(maximumRadius) + ", radiale stap " + F(radialStep) + ")");
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

        private static void AddRectangularPocket(StringBuilder sb, NestedSheetPlacement placement, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double throughCutOvertravelMm)
        {
            if (pocket == null || pocket.LengthMm <= 0 || pocket.WidthMm <= 0)
            {
                return;
            }
            if (IsCapsulePocket(pocket))
            {
                AddCapsulePocket(sb, placement, pocket, tool, machine, throughCutOvertravelMm);
                return;
            }
            if (IsDrawerPullFinishContour(pocket))
            {
                AddDrawerPullFinishContour(sb, placement, pocket, tool, machine, throughCutOvertravelMm);
                return;
            }
            var depthMm = pocket.DepthMode == OperationDepthMode.Through
                ? placement.Part.Material.ThicknessMm + Math.Max(0, throughCutOvertravelMm)
                : Math.Min(pocket.DepthMm, Math.Max(0.1, placement.Part.Material.ThicknessMm - 0.1));
            if (depthMm <= 0) return;

            var inset = Math.Max(tool.RadiusMm, 0.1);
            var lx0 = pocket.Xmm + inset;
            var ly0 = pocket.Ymm + inset;
            var lx1 = pocket.Xmm + pocket.LengthMm - inset;
            var ly1 = pocket.Ymm + pocket.WidthMm - inset;
            ApplyShortEndOvercut(placement.Part, pocket, tool, ref lx0, ref ly0, ref lx1, ref ly1);
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
            var operationName = pocket.DepthMode == OperationDepthMode.Through ? "door-uitsparing" : "pocket";
            sb.AppendLine("(" + placement.Label + " - " + pocket.Name + " " + operationName + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(depthMm) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(p0.X) + " Y" + F(p0.Y));
            var depth = 0.0;
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -depthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPocketClearingPass(sb, placement, lx0, ly0, lx1, ly1, tool);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool IsCapsulePocket(SheetPocket pocket)
        {
            return pocket != null && string.Equals(pocket.Shape, "capsule", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddCapsulePocket(StringBuilder sb, NestedSheetPlacement placement, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double throughCutOvertravelMm)
        {
            var depthMm = pocket.DepthMode == OperationDepthMode.Through
                ? placement.Part.Material.ThicknessMm + Math.Max(0, throughCutOvertravelMm)
                : Math.Min(pocket.DepthMm, Math.Max(0.1, placement.Part.Material.ThicknessMm - 0.1));
            var points = CapsuleContourPoints(pocket.Xmm, pocket.Ymm, pocket.LengthMm, pocket.WidthMm, tool.RadiusMm);
            if (depthMm <= 0 || points.Count < 2) return;
            var start = Transform(placement, points[0].X, points[0].Y);

            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + pocket.Name + " capsule " + (pocket.DepthMode == OperationDepthMode.Through ? "door-uitsparing" : "pocket") + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(depthMm) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            var depth = 0.0;
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -depthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                AddPolylinePass(sb, placement, points, tool, tool.FeedRateMmMin);
            }
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static bool IsDrawerPullFinishContour(SheetPocket pocket)
        {
            return pocket != null
                && !string.IsNullOrWhiteSpace(pocket.Name)
                && pocket.Name.IndexOf("handgreep afwerkcontour", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddDrawerPullFinishContour(StringBuilder sb, NestedSheetPlacement placement, SheetPocket pocket, ToolDefinition tool, MachineProfile machine, double throughCutOvertravelMm)
        {
            var thickness = placement.Part.Material.ThicknessMm;
            var clearedZ = -Math.Max(0.1, thickness - 2.0);
            var cutZ = -(thickness + Math.Max(0, throughCutOvertravelMm));
            var tabZ = -Math.Max(0.1, thickness - 2.0);
            var points = CapsuleContourPoints(pocket.Xmm, pocket.Ymm, pocket.LengthMm, pocket.WidthMm, tool.RadiusMm);
            if (points.Count < 2) return;
            var start = Transform(placement, points[0].X, points[0].Y);
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " - " + pocket.Name + " door-uitsparing " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(-cutZ) + ")");
            sb.AppendLine("(Handgreep afwerkcontour: 2mm voorpocket, tabs 8x2mm, tussenafstand max 70mm)");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            sb.AppendLine("G1 Z" + F(clearedZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            AddDistributedTabbedPolylinePass(sb, placement, points, tool, 8.0, 70.0, tabZ, cutZ);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static List<Point2> CapsuleContourPoints(double x, double y, double length, double height, double toolRadius)
        {
            if (height > length)
            {
                var transposed = CapsuleContourPoints(y, x, height, length, toolRadius);
                var vertical = new List<Point2>();
                foreach (var point in transposed) vertical.Add(new Point2(point.Y, point.X));
                return vertical;
            }
            var points = new List<Point2>();
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

        private static void AddCapsuleArc(List<Point2> points, double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            const int segments = 16;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + (endAngle - startAngle) * i / segments;
                points.Add(new Point2(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
            }
        }

        private static void AddDistributedTabbedPolylinePass(StringBuilder sb, NestedSheetPlacement placement, List<Point2> points, ToolDefinition tool, double tabWidth, double maxSpacing, double tabZ, double cutZ)
        {
            var total = 0.0;
            for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
            if (total <= 0) return;
            var tabCount = Math.Max(1, (int)Math.Ceiling(total / Math.Max(tabWidth * 2.0, maxSpacing)));
            var boundaries = new List<double>();
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
                var cuts = new List<double> { 0.0, segmentLength };
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
                    var point = Transform(placement, start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
                    sb.AppendLine("G1 X" + F(point.X) + " Y" + F(point.Y) + " F" + F(tool.FeedRateMmMin));
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

        private static void AddPocketClearingPass(StringBuilder sb, NestedSheetPlacement placement, double lx0, double ly0, double lx1, double ly1, ToolDefinition tool)
        {
            if (Math.Abs(lx1 - lx0) < 0.001 && Math.Abs(ly1 - ly0) < 0.001)
            {
                AddLocalMove(sb, placement, lx0, ly0, tool);
                return;
            }

            var step = Math.Max(1.0, tool.DiameterMm * 0.45);
            var runVertical = Math.Abs(ly1 - ly0) > Math.Abs(lx1 - lx0);
            if (runVertical)
            {
                var x = lx0;
                var forwardY = true;
                while (true)
                {
                    var targetY = forwardY ? ly1 : ly0;
                    AddLocalMove(sb, placement, x, targetY, tool);
                    if (Math.Abs(x - lx1) < 0.001) break;

                    var nextX = Math.Min(x + step, lx1);
                    if (Math.Abs(nextX - x) < 0.001) break;
                    AddLocalMove(sb, placement, nextX, targetY, tool);
                    x = nextX;
                    forwardY = !forwardY;
                }

                AddLocalMove(sb, placement, lx1, ly0, tool);
                AddLocalMove(sb, placement, lx1, ly1, tool);
                AddLocalMove(sb, placement, lx0, ly1, tool);
                AddLocalMove(sb, placement, lx0, ly0, tool);
                return;
            }

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

        private static void ApplyShortEndOvercut(SheetPart part, SheetPocket pocket, ToolDefinition tool, ref double x0, ref double y0, ref double x1, ref double y1)
        {
            if (part == null || pocket == null || tool == null) return;

            var overcut = Math.Max(0, tool.RadiusMm);
            if (overcut <= 0) return;

            var horizontalSlot = pocket.LengthMm >= pocket.WidthMm;
            if (horizontalSlot)
            {
                if (pocket.Xmm <= 0.001) x0 = pocket.Xmm - overcut;
                if (pocket.Xmm + pocket.LengthMm >= part.LengthMm - 0.001) x1 = pocket.Xmm + pocket.LengthMm + overcut;
                return;
            }

            if (pocket.Ymm <= 0.001) y0 = pocket.Ymm - overcut;
            if (pocket.Ymm + pocket.WidthMm >= part.WidthMm - 0.001) y1 = pocket.Ymm + pocket.WidthMm + overcut;
        }

        private static void DrillPeck(StringBuilder sb, ToolDefinition tool, MachineProfile machine, double depthMm)
        {
            var depth = 0.0;
            var step = Math.Max(0.1, tool.PassDepthMm);
            sb.AppendLine("(Peck stap max " + F(step) + " mm tot Z" + F(-depthMm) + ", iedere retract naar veilige Z+" + F(machine.SafeZmm) + ")");
            while (depth > -depthMm)
            {
                depth = Math.Max(depth - step, -depthMm);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));
                sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            }
        }

        private static void AddContourRough(StringBuilder sb, NestedSheetPlacement placement, ToolDefinition tool, MachineProfile machine, CamJobOptions jobOptions)
        {
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " buitencontour voorfrezen)");
            var points = ContourPoints(placement.Part, tool.RadiusMm);
            var start = Transform(placement, points[0].X, points[0].Y);
            var targetDepth = ContourDepthStrategy.RoughDepthMm(placement.Part.Material.ThicknessMm, jobOptions.ContourOnionSkinMm);
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));

            var depth = 0.0;
            while (depth > -targetDepth)
            {
                depth = Math.Max(depth - tool.PassDepthMm, -targetDepth);
                sb.AppendLine("G1 Z" + F(depth) + " F" + F(tool.PlungeRateMmMin));

                AddPolylinePass(sb, placement, points, tool, tool.FeedRateMmMin);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddFinalContourPause(StringBuilder sb, ToolDefinition tool, MachineProfile machine, CamJobOptions jobOptions)
        {
            sb.AppendLine();
            sb.AppendLine("(============================================================)");
            GCodeMonitoringMarkerWriter.AppendEvent(sb, jobOptions.EnableMonitoringMarkers, "FINAL_CONTOUR_APPROACHING");
            sb.AppendLine("(LAATSTE CONTOURLAAG - BLIJF VANAF NU BIJ DE MACHINE)");
            sb.AppendLine("(Controleer vacuum, frees, spanmoer en werkstuk-Z0.)");
            sb.AppendLine("(Na Cycle Start worden alle contouren van de onion-skin naar door-en-door gefreesd.)");
            sb.AppendLine("(Eindvoeding " + F(jobOptions.FinalContourFeedRateMmMin) + " mm/min; toerental " + F(tool.SpindleRpm) + " rpm.)");
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
            sb.AppendLine("(============================================================)");
        }

        private static void AddContourFinal(StringBuilder sb, NestedSheetPlacement placement, ToolDefinition tool, MachineProfile machine, CamJobOptions jobOptions, bool emitFinalContourEvent)
        {
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " LAATSTE contourlaag)");
            var points = ContourPoints(placement.Part, tool.RadiusMm);
            var start = Transform(placement, points[0].X, points[0].Y);
            var roughZ = -ContourDepthStrategy.RoughDepthMm(placement.Part.Material.ThicknessMm, jobOptions.ContourOnionSkinMm);
            var cutZ = -ContourDepthStrategy.FinalDepthMm(placement.Part.Material.ThicknessMm, jobOptions.ThroughCutOvertravelMm);
            var feed = Math.Max(100, jobOptions.FinalContourFeedRateMmMin);
            sb.AppendLine("(Plaatdikte " + F(placement.Part.Material.ThicknessMm) + " mm: voorlaag Z" + F(roughZ) + ", eindlaag Z" + F(cutZ) + ")");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
            sb.AppendLine("G0 X" + F(start.X) + " Y" + F(start.Y));
            sb.AppendLine("G1 Z" + F(roughZ) + " F" + F(tool.PlungeRateMmMin));
            if (emitFinalContourEvent) GCodeMonitoringMarkerWriter.AppendEvent(sb, jobOptions.EnableMonitoringMarkers, "FINAL_CONTOUR");

            var passPoints = new List<Point2>(points);
            if (points.Count > 1)
            {
                var first = points[0];
                var second = points[1];
                var dx = second.X - first.X;
                var dy = second.Y - first.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (length > 0.001)
                {
                    var rampLength = Math.Min(length, Math.Max(1, jobOptions.FinalContourRampLengthMm));
                    var t = rampLength / length;
                    var rampEnd = new Point2(first.X + dx * t, first.Y + dy * t);
                    var transformedRampEnd = Transform(placement, rampEnd.X, rampEnd.Y);
                    sb.AppendLine("(Ramp alleen in eindlaag over " + F(rampLength) + " mm in bestaande groef)");
                    sb.AppendLine("G1 X" + F(transformedRampEnd.X) + " Y" + F(transformedRampEnd.Y) + " Z" + F(cutZ) + " F" + F(feed));
                    passPoints.RemoveAt(0);
                    passPoints.Insert(0, rampEnd);
                }
                else
                {
                    sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
                }
            }

            if (placement.Part.UseTabs && placement.Part.CustomContour != null && placement.Part.CustomContour.Count >= 3)
            {
                var tabZ = -Math.Max(0, placement.Part.Material.ThicknessMm - Math.Max(0, jobOptions.TabHeightMm));
                AddDistributedTabbedPolylinePass(sb, placement, passPoints, tool, Math.Max(0, jobOptions.TabWidthMm), Math.Max(jobOptions.TabWidthMm * 2.0, PolylineLength(passPoints) / 4.0), tabZ, cutZ);
            }
            else if (placement.Part.UseTabs)
            {
                var tabZ = -Math.Max(0, placement.Part.Material.ThicknessMm - Math.Max(0, jobOptions.TabHeightMm));
                AddTabbedPolylinePass(sb, placement, passPoints, tool, Math.Max(0, jobOptions.TabWidthMm), tabZ, cutZ, feed);
            }
            else
            {
                AddPolylinePass(sb, placement, passPoints, tool, feed);
            }
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AddPolylinePass(StringBuilder sb, NestedSheetPlacement placement, List<Point2> points, ToolDefinition tool, double feedRate)
        {
            for (var i = 1; i < points.Count; i++)
            {
                var p = Transform(placement, points[i].X, points[i].Y);
                sb.AppendLine("G1 X" + F(p.X) + " Y" + F(p.Y) + " F" + F(feedRate));
            }
        }

        private static double PolylineLength(List<Point2> points)
        {
            var total = 0.0;
            for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
            return total;
        }

        private static void AddTabbedPolylinePass(StringBuilder sb, NestedSheetPlacement placement, List<Point2> points, ToolDefinition tool, double tabWidth, double tabZ, double cutZ, double feedRate)
        {
            for (var i = 1; i < points.Count; i++)
            {
                AddTabbedSegment(sb, placement, points[i - 1], points[i], tool, tabWidth, tabZ, cutZ, feedRate);
            }
        }

        private static void AddTabbedSegment(StringBuilder sb, NestedSheetPlacement placement, Point2 start, Point2 end, ToolDefinition tool, double tabWidth, double tabZ, double cutZ, double feedRate)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= tabWidth * 3.0)
            {
                var shortEndPoint = Transform(placement, end.X, end.Y);
                sb.AppendLine("G1 X" + F(shortEndPoint.X) + " Y" + F(shortEndPoint.Y) + " F" + F(feedRate));
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

            sb.AppendLine("G1 X" + F(beforePoint.X) + " Y" + F(beforePoint.Y) + " F" + F(feedRate));
            sb.AppendLine("G1 Z" + F(tabZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(afterPoint.X) + " Y" + F(afterPoint.Y) + " F" + F(feedRate));
            sb.AppendLine("G1 Z" + F(cutZ) + " F" + F(tool.PlungeRateMmMin));
            sb.AppendLine("G1 X" + F(endPoint.X) + " Y" + F(endPoint.Y) + " F" + F(feedRate));
        }

        private static List<Point2> ContourPoints(SheetPart part, double radius)
        {
            var points = new List<Point2>();
            var custom = SheetContourGeometry.ToolCenterContour(part, radius);
            if (custom.Count > 0)
            {
                foreach (var point in custom) points.Add(new Point2(point.Xmm, point.Ymm));
                return points;
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
                return points;
            }

            if (!part.HasCornerNotches)
            {
                if (part.CornerRadiusMm > 0.001)
                {
                    AddRoundedRectangleContour(points, part.LengthMm, part.WidthMm, part.CornerRadiusMm, radius);
                    return points;
                }
                points.Add(new Point2(x0, y0));
                points.Add(new Point2(x1, y0));
                points.Add(new Point2(x1, y1));
                points.Add(new Point2(x0, y1));
                points.Add(new Point2(x0, y0));
                return points;
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
            return points;
        }

        private static void AddRoundedRectangleContour(List<Point2> points, double length, double width, double cornerRadius, double toolRadius)
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

        private static void AddContourArc(List<Point2> points, double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            const int segments = 8;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + (endAngle - startAngle) * i / segments;
                points.Add(new Point2(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
            }
        }

        private static Point2 Transform(NestedSheetPlacement placement, double x, double y)
        {
            if (placement.Part != null && placement.Part.MirrorInNestingX)
            {
                x = placement.Part.LengthMm - x;
            }

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
