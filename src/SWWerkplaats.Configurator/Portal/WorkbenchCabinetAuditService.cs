using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Manufacturing;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class WorkbenchCabinetAuditResult
    {
        public bool Passed { get { return Errors.Count == 0; } }
        public List<string> Errors { get; private set; }
        public List<string> Warnings { get; private set; }
        public List<string> Checks { get; private set; }

        public WorkbenchCabinetAuditResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Checks = new List<string>();
        }
    }

    /// <summary>
    /// Deterministische vrijgavecontrole voor het werkbankkast-product. Deze controle
    /// valideert de gegenereerde geometrie; hij vervangt geen proefpassing van nieuw
    /// of nog niet fysiek geverifieerd beslag.
    /// </summary>
    public sealed class WorkbenchCabinetAuditService
    {
        private const double ToleranceMm = 0.05;

        public WorkbenchCabinetAuditResult Audit(WorkbenchModel model, PortalQuoteRequest request, NestingPlan nestingPlan)
        {
            var result = new WorkbenchCabinetAuditResult();
            if (model == null) { result.Errors.Add("Model ontbreekt."); return result; }
            if (request == null) { result.Errors.Add("Configuratie ontbreekt."); return result; }

            CheckUniquePartsAndPlacements(model, result);
            CheckSheetOperations(model, result);
            CheckAssemblyEnvelope(model, request, result);
            if (string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                CheckWorkbenchCabinetRules(model, request, result);
            }
            else if (IsLexProduct(request.Product))
            {
                CheckLexBomRules(model, request, result);
            }
            else
            {
                result.Checks.Add("Productspecifieke kastonderbouwcontrole overgeslagen voor " + (request.Product ?? "onbekend product") + ".");
            }
            CheckNesting(nestingPlan, result);
            CheckCrossOutputContract(model, request, nestingPlan, result);
            return result;
        }

        private static void CheckCrossOutputContract(WorkbenchModel model, PortalQuoteRequest request, NestingPlan nestingPlan, WorkbenchCabinetAuditResult result)
        {
            CheckAssemblyMachiningContract(model, request, result);
            CheckNestedGCodeContract(model, nestingPlan, result);
        }

        private static void CheckAssemblyMachiningContract(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetAuditResult result)
        {
            var assembly = new PortalAssembly3DService().Build(model, request);
            var checkedHoles = 0;
            var checkedPockets = 0;
            var checkedContours = 0;
            foreach (var placement in model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Sheet))
            {
                var sheet = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, placement.PartName, StringComparison.OrdinalIgnoreCase));
                if (sheet == null) continue;
                var visualParts = assembly
                    .Where(p => IsVisualForSheet(p, sheet.Name) && VisualPartBelongsToPlacement(p, placement, sheet))
                    .ToList();
                if (visualParts.Count == 0)
                {
                    result.Errors.Add("Web/SW-contract mist het plaatdeel " + sheet.Name + ".");
                    continue;
                }

                checkedContours++;
                foreach (var visualPart in visualParts)
                {
                    if (Math.Abs(visualPart.CornerRadiusMm - sheet.CornerRadiusMm) > ToleranceMm)
                        result.Errors.Add("Web/SW-contract wijkt af bij buitencontour: " + sheet.Name + " heeft R" + F(sheet.CornerRadiusMm) + "mm in het bronmodel en R" + F(visualPart.CornerRadiusMm) + "mm in de 3D-geometrie.");
                }

                foreach (var hole in sheet.Holes)
                {
                    checkedHoles++;
                    if (IsDrawerPullRoundEnd(hole))
                    {
                        if (!visualParts.SelectMany(p => p.Pockets).Any(p => VisualRoundEndMatches(p, placement, sheet, hole)))
                            result.Errors.Add("Web/SW-contract mist handgreepronding: " + sheet.Name + " / " + hole.Name + ".");
                        continue;
                    }

                    if (!visualParts.SelectMany(p => p.Holes).Any(h => VisualHoleMatches(h, placement, sheet, hole)))
                        result.Errors.Add("Web/SW-contract wijkt af bij gat: " + sheet.Name + " / " + hole.Name + ".");
                }

                foreach (var pocket in sheet.Pockets)
                {
                    checkedPockets++;
                    if (!visualParts.SelectMany(p => p.Pockets).Any(p => VisualPocketMatches(p, placement, sheet, pocket)))
                        result.Errors.Add("Web/SW-contract wijkt af bij pocket: " + sheet.Name + " / " + pocket.Name + ".");
                }

                foreach (var visualHole in visualParts.SelectMany(p => p.Holes))
                {
                    if (!sheet.Holes.Any(h => !IsDrawerPullRoundEnd(h) && VisualHoleMatches(visualHole, placement, sheet, h)))
                        result.Errors.Add("Web/SW-contract bevat een onverwacht gat: " + sheet.Name + " / " + (visualHole.Name ?? "naamloos") + ".");
                }

                foreach (var visualPocket in visualParts.SelectMany(p => p.Pockets).Where(p => !IsVisualReveal(p)))
                {
                    var matchesPocket = sheet.Pockets.Any(p => VisualPocketMatches(visualPocket, placement, sheet, p));
                    var matchesRoundEnd = sheet.Holes.Any(h => IsDrawerPullRoundEnd(h) && VisualRoundEndMatches(visualPocket, placement, sheet, h));
                    if (!matchesPocket && !matchesRoundEnd)
                        result.Errors.Add("Web/SW-contract bevat een onverwachte pocket: " + sheet.Name + " / " + (visualPocket.Name ?? "naamloos") + ".");
                }
            }

            result.Checks.Add(checkedContours.ToString(CultureInfo.InvariantCulture) + " buitencontouren, " + checkedHoles.ToString(CultureInfo.InvariantCulture) + " gaten en " + checkedPockets.ToString(CultureInfo.InvariantCulture) + " pockets één-op-één tussen bronmodel en web/SW-geometrie vergeleken.");
        }

        private static bool IsVisualForSheet(PortalAssemblyPart part, string sheetName)
        {
            return part != null && string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(part.Name, sheetName, StringComparison.OrdinalIgnoreCase)
                    || (part.Name != null && part.Name.StartsWith(sheetName + " ", StringComparison.OrdinalIgnoreCase)));
        }

        private static bool VisualPartBelongsToPlacement(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet)
        {
            if (part == null || placement == null || sheet == null) return false;
            var thickness = sheet.Material == null ? 18.0 : Math.Max(0.1, sheet.Material.ThicknessMm);
            double sizeX;
            double sizeY;
            double sizeZ;
            if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
            {
                sizeX = sheet.LengthMm;
                sizeY = sheet.WidthMm;
                sizeZ = thickness;
            }
            else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
            {
                sizeX = thickness;
                sizeY = sheet.WidthMm;
                sizeZ = sheet.LengthMm;
            }
            else
            {
                sizeX = sheet.LengthMm;
                sizeY = thickness;
                sizeZ = sheet.WidthMm;
            }

            return Math.Abs(part.Xmm - placement.Xmm) <= sizeX / 2.0 + part.SizeXmm / 2.0 + ToleranceMm
                && Math.Abs(part.Ymm - placement.Ymm) <= sizeY / 2.0 + part.SizeYmm / 2.0 + ToleranceMm
                && Math.Abs(part.Zmm - placement.Zmm) <= sizeZ / 2.0 + part.SizeZmm / 2.0 + ToleranceMm;
        }

        private static bool VisualHoleMatches(PortalAssemblyHole visual, AssemblyPlacement placement, SheetPart sheet, SheetHole source)
        {
            if (visual == null || placement == null || sheet == null || source == null) return false;
            if (!string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(visual.Plane, ExpectedPlane(placement), StringComparison.OrdinalIgnoreCase)) return false;
            if (Math.Abs(visual.DiameterMm - source.DiameterMm) > ToleranceMm) return false;
            if (visual.IsThroughCutout != (source.DepthMode == OperationDepthMode.Through)) return false;
            if (source.DepthMode != OperationDepthMode.Through && Math.Abs(visual.DepthMm - source.DepthMm) > ToleranceMm) return false;
            if (visual.Countersunk != source.Countersunk) return false;
            if (source.Countersunk && (Math.Abs(visual.CountersinkDiameterMm - source.CountersinkDiameterMm) > ToleranceMm || Math.Abs(visual.CountersinkDepthMm - source.CountersinkDepthMm) > ToleranceMm)) return false;

            double first;
            double second;
            ExpectedTangentialPoint(placement, sheet, source.Xmm, source.Ymm, out first, out second);
            return TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second);
        }

        private static bool VisualPocketMatches(PortalAssemblyPocket visual, AssemblyPlacement placement, SheetPart sheet, SheetPocket source)
        {
            if (visual == null || placement == null || sheet == null || source == null) return false;
            if (!string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(visual.Plane, ExpectedPlane(placement), StringComparison.OrdinalIgnoreCase)) return false;
            if (visual.IsThroughCutout != (source.DepthMode == OperationDepthMode.Through)) return false;

            var centerX = source.Xmm + source.LengthMm / 2.0;
            var centerY = source.Ymm + source.WidthMm / 2.0;
            double first;
            double second;
            ExpectedTangentialPoint(placement, sheet, centerX, centerY, out first, out second);
            if (!TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second)) return false;
            return TangentialSizeMatches(visual, source.LengthMm, source.WidthMm);
        }

        private static bool VisualRoundEndMatches(PortalAssemblyPocket visual, AssemblyPlacement placement, SheetPart sheet, SheetHole source)
        {
            if (visual == null || !string.Equals(visual.Name, source.Name, StringComparison.OrdinalIgnoreCase) || !string.Equals(visual.Shape, "cylinder", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(visual.Plane, ExpectedPlane(placement), StringComparison.OrdinalIgnoreCase) || !visual.IsThroughCutout) return false;
            double first;
            double second;
            ExpectedTangentialPoint(placement, sheet, source.Xmm, source.Ymm, out first, out second);
            if (!TangentialPointMatches(visual.Plane, visual.Xmm, visual.Ymm, visual.Zmm, first, second)) return false;
            return Math.Abs(PocketDiameter(visual) - source.DiameterMm) <= ToleranceMm;
        }

        private static string ExpectedPlane(AssemblyPlacement placement)
        {
            if (placement.Orientation == AssemblyOrientation.SheetHorizontal) return "y";
            if (placement.Orientation == AssemblyOrientation.SheetVerticalX) return "z";
            if (placement.Orientation == AssemblyOrientation.SheetVerticalZ) return "x";
            return string.Empty;
        }

        private static void ExpectedTangentialPoint(AssemblyPlacement placement, SheetPart sheet, double sourceX, double sourceY, out double first, out double second)
        {
            var localX = sourceX - sheet.LengthMm / 2.0;
            var localY = sourceY - sheet.WidthMm / 2.0;
            if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
            {
                first = placement.Xmm + localX;
                second = placement.Zmm + localY;
            }
            else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
            {
                first = placement.Xmm + localX;
                second = placement.Ymm + localY;
            }
            else
            {
                first = placement.Ymm + localY;
                second = placement.Zmm + localX;
            }
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

        private static bool IsDrawerPullRoundEnd(SheetHole hole)
        {
            return hole != null && hole.SupportKind == SheetHoleSupportKind.MachiningCutout
                && !string.IsNullOrWhiteSpace(hole.Name)
                && hole.Name.StartsWith("Uitgefreesde handgreep ronding", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVisualReveal(PortalAssemblyPocket pocket)
        {
            return pocket != null && pocket.Name != null && pocket.Name.EndsWith(" zichtbaar", StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckNestedGCodeContract(WorkbenchModel model, NestingPlan nestingPlan, WorkbenchCabinetAuditResult result)
        {
            if (nestingPlan == null) return;
            var expectedCounts = model.Sheets.ToDictionary(s => s.Name, s => Math.Max(1, s.Quantity), StringComparer.OrdinalIgnoreCase);
            var actualCounts = nestingPlan.StockSheets.SelectMany(s => s.Placements).GroupBy(p => p.Part.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            foreach (var expectedCount in expectedCounts)
            {
                int count;
                actualCounts.TryGetValue(expectedCount.Key, out count);
                if (count != expectedCount.Value) result.Errors.Add("Freescontract heeft " + count + " geneste instanties van " + expectedCount.Key + "; verwacht " + expectedCount.Value + ".");
            }

            var factory = new PortalConfigurationFactory();
            var contourTool = factory.DefaultTool();
            var holeTool = LibraryCatalog.DefaultEndMill(3, 2.0);
            var camJob = CamJobOptions.FromPrimaryTool(holeTool);
            camJob.AddTool(contourTool);
            var camMaster = CamMasterSettings.LoadRequired();
            camMaster.ApplyTo(camJob);
            var machine = factory.DefaultMachine();
            var generator = new NestedMach3GCodeGenerator();
            var operationCount = 0;
            for (var stockIndex = 0; stockIndex < nestingPlan.StockSheets.Count; stockIndex++)
            {
                var stock = nestingPlan.StockSheets[stockIndex];
                var gcode = generator.Generate(stock, contourTool, machine, camJob, stockIndex + 1, nestingPlan.StockSheets.Count, null);
                var actual = GCodeOperationLines(gcode, stock);
                var expected = ExpectedGCodeOperationLines(stock, camMaster.ThroughCutOvertravelMm);
                CompareMultiset(expected, actual, stock.Name, result);
                operationCount += expected.Count;
            }
            result.Checks.Add(operationCount.ToString(CultureInfo.InvariantCulture) + " nominale freesbewerkingen exact in de gegenereerde geneste G-code teruggevonden.");
        }

        private static List<string> GCodeOperationLines(string gcode, NestedStockSheet stock)
        {
            var result = new List<string>();
            var labels = stock.Placements.Select(p => "(" + p.Label).ToList();
            foreach (var raw in (gcode ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (!labels.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal))) continue;
                if ((line.Contains(" diameter ") && line.Contains(", diepte ") && line.Contains(", centrum X"))
                    || line.Contains(" kopkamer diameter ")
                    || line.Contains(" pocket ")
                    || line.Contains(" door-uitsparing ")
                    || line.EndsWith(" buitencontour voorfrezen)", StringComparison.Ordinal)
                    || line.EndsWith(" LAATSTE contourlaag)", StringComparison.Ordinal))
                    result.Add(line);
            }
            return result;
        }

        private static List<string> ExpectedGCodeOperationLines(NestedStockSheet stock, double throughCutOvertravelMm)
        {
            var result = new List<string>();
            foreach (var placement in stock.Placements)
            {
                foreach (var hole in placement.Part.Holes)
                {
                    double x;
                    double y;
                    TransformNested(placement, hole.Xmm, hole.Ymm, out x, out y);
                    var depth = hole.DepthMode == OperationDepthMode.Through
                        ? placement.Part.Material.ThicknessMm + Math.Max(0, throughCutOvertravelMm)
                        : Math.Min(hole.DepthMm, Math.Max(0.1, placement.Part.Material.ThicknessMm - 0.1));
                    result.Add("(" + placement.Label + " - " + hole.Name + " diameter " + F(hole.DiameterMm) + ", diepte " + F(depth) + ", centrum X" + F(x) + " Y" + F(y) + ")");
                    if (hole.Countersunk && hole.CountersinkDiameterMm > hole.DiameterMm && hole.CountersinkDepthMm > 0)
                        result.Add("(" + placement.Label + " - " + hole.Name + " kopkamer diameter " + F(hole.CountersinkDiameterMm) + " diepte " + F(hole.CountersinkDepthMm) + ", centrum X" + F(x) + " Y" + F(y) + ")");
                }
                foreach (var pocket in placement.Part.Pockets)
                {
                    var operationName = pocket.DepthMode == OperationDepthMode.Through ? "door-uitsparing" : "pocket";
                    var depth = pocket.DepthMode == OperationDepthMode.Through
                        ? placement.Part.Material.ThicknessMm + Math.Max(0, throughCutOvertravelMm)
                        : Math.Min(pocket.DepthMm, Math.Max(0.1, placement.Part.Material.ThicknessMm - 0.1));
                    result.Add("(" + placement.Label + " - " + pocket.Name + " " + operationName + " " + F(pocket.LengthMm) + "x" + F(pocket.WidthMm) + " diepte " + F(depth) + ")");
                }
                result.Add("(" + placement.Label + " buitencontour voorfrezen)");
                result.Add("(" + placement.Label + " LAATSTE contourlaag)");
            }
            return result;
        }

        private static void TransformNested(NestedSheetPlacement placement, double sourceX, double sourceY, out double x, out double y)
        {
            if (placement.Part.MirrorInNestingX) sourceX = placement.Part.LengthMm - sourceX;
            if (!placement.Rotated)
            {
                x = placement.Xmm + sourceX;
                y = placement.Ymm + sourceY;
                return;
            }
            x = placement.Xmm + sourceY;
            y = placement.Ymm + placement.Part.LengthMm - sourceX;
        }

        private static void CompareMultiset(List<string> expected, List<string> actual, string stockName, WorkbenchCabinetAuditResult result)
        {
            var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var line in actual)
            {
                int count;
                remaining.TryGetValue(line, out count);
                remaining[line] = count + 1;
            }
            foreach (var line in expected)
            {
                int count;
                if (!remaining.TryGetValue(line, out count) || count == 0)
                    result.Errors.Add("Freescontract mist op " + stockName + ": " + line);
                else
                    remaining[line] = count - 1;
            }
            foreach (var extra in remaining.Where(p => p.Value > 0))
                result.Errors.Add("Freescontract bevat onverwachte bewerking op " + stockName + ": " + extra.Key);
        }

        private static void CheckUniquePartsAndPlacements(WorkbenchModel model, WorkbenchCabinetAuditResult result)
        {
            var duplicateNames = model.Sheets.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            foreach (var name in duplicateNames) result.Errors.Add("Dubbele plaatnaam: " + name + ".");
            foreach (var sheet in model.Sheets)
            {
                var placements = model.AssemblyPlacements.Count(p => p.Kind == AssemblyComponentKind.Sheet && string.Equals(p.PartName, sheet.Name, StringComparison.OrdinalIgnoreCase));
                var expectedPlacements = Math.Max(1, sheet.Quantity);
                if (placements != expectedPlacements) result.Errors.Add("Plaat " + sheet.Name + " heeft " + placements + " assemblyplaatsingen; verwacht " + expectedPlacements + " volgens de stukshoeveelheid.");
            }
            result.Checks.Add(model.Sheets.Count.ToString(CultureInfo.InvariantCulture) + " unieke plaatdelen en " + model.AssemblyPlacements.Count.ToString(CultureInfo.InvariantCulture) + " plaatsingen gecontroleerd.");
        }

        private static void CheckSheetOperations(WorkbenchModel model, WorkbenchCabinetAuditResult result)
        {
            var holes = 0;
            var pockets = 0;
            foreach (var sheet in model.Sheets)
            {
                var thickness = sheet.Material == null ? 18.0 : sheet.Material.ThicknessMm;
                foreach (var hole in sheet.Holes)
                {
                    holes++;
                    var radius = hole.DiameterMm / 2.0;
                    if (hole.DiameterMm <= 0 || hole.Xmm - radius < -ToleranceMm || hole.Xmm + radius > sheet.LengthMm + ToleranceMm || hole.Ymm - radius < -ToleranceMm || hole.Ymm + radius > sheet.WidthMm + ToleranceMm)
                        result.Errors.Add("Gat buiten plaatgrens: " + sheet.Name + " / " + hole.Name + ".");
                    if (hole.DepthMode == OperationDepthMode.BlindFromFace && (hole.DepthMm <= 0 || hole.DepthMm >= thickness))
                        result.Errors.Add("Ongeldige blinddiepte: " + sheet.Name + " / " + hole.Name + " = " + F(hole.DepthMm) + "mm bij " + F(thickness) + "mm plaat.");
                    if (hole.DepthMode == OperationDepthMode.Through && Math.Abs(hole.DepthMm) > ToleranceMm)
                        result.Warnings.Add("Doorlopend gat heeft tevens een dieptewaarde: " + sheet.Name + " / " + hole.Name + ".");
                }
                foreach (var pocket in sheet.Pockets)
                {
                    pockets++;
                    if (pocket.LengthMm <= 0 || pocket.WidthMm <= 0 || pocket.Xmm < -ToleranceMm || pocket.Ymm < -ToleranceMm || pocket.Xmm + pocket.LengthMm > sheet.LengthMm + ToleranceMm || pocket.Ymm + pocket.WidthMm > sheet.WidthMm + ToleranceMm)
                        result.Errors.Add("Pocket buiten plaatgrens: " + sheet.Name + " / " + pocket.Name + ".");
                    if (pocket.DepthMode != OperationDepthMode.Through && (pocket.DepthMm <= 0 || pocket.DepthMm >= thickness))
                        result.Errors.Add("Ongeldige pocketdiepte: " + sheet.Name + " / " + pocket.Name + ".");
                }
            }
            result.Checks.Add(holes.ToString(CultureInfo.InvariantCulture) + " gaten en " + pockets.ToString(CultureInfo.InvariantCulture) + " pockets op plaatgrenzen en diepte gecontroleerd.");
        }

        private static void CheckAssemblyEnvelope(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetAuditResult result)
        {
            var parts = new PortalAssembly3DService().Build(model, request);
            var allowedTop = request.HeightMm;
            if (IsLexProduct(request.Product))
                allowedTop += new PortalConfigurationFactory().BuildLexWorkbench(request).BallTransferWorkingHeightMm;
            foreach (var part in parts)
            {
                if (part.SizeXmm <= 0 || part.SizeYmm <= 0 || part.SizeZmm <= 0) result.Errors.Add("Ongeldige 3D-maat bij " + part.Name + ".");
                var xMin = part.Xmm - part.SizeXmm / 2.0;
                var xMax = part.Xmm + part.SizeXmm / 2.0;
                if (xMin < -request.WidthMm / 2.0 - 25.0 || xMax > request.WidthMm / 2.0 + 25.0)
                    result.Errors.Add("Onderdeel buiten breedte-envelope: " + part.Name + ".");
                if (IsLexProduct(request.Product))
                {
                    var zMin = part.Zmm - part.SizeZmm / 2.0;
                    var zMax = part.Zmm + part.SizeZmm / 2.0;
                    if (zMin < -request.DepthMm / 2.0 - ToleranceMm || zMax > request.DepthMm / 2.0 + ToleranceMm)
                        result.Errors.Add("Onderdeel buiten diepte-envelope: " + part.Name + ".");
                }
                if (part.Ymm - part.SizeYmm / 2.0 < -ToleranceMm || part.Ymm + part.SizeYmm / 2.0 > allowedTop + ToleranceMm)
                    result.Errors.Add("Onderdeel buiten hoogte-envelope: " + part.Name + ".");
            }
            var plinths = parts.Where(p => string.Equals(p.Name, "Losse voorzetplint", StringComparison.OrdinalIgnoreCase) || Starts(p.Name, "Zijplint ")).ToList();
            var mountingBlocks = parts.Where(p => Starts(p.Name, "SEKTION montagevoet ")).ToList();
            foreach (var plinth in plinths)
            {
                foreach (var mountingBlock in mountingBlocks)
                {
                    if (AssemblyBodiesOverlap(plinth, mountingBlock))
                        result.Errors.Add(plinth.Name + " kruist " + mountingBlock.Name + ".");
                }
            }
            var adapterBodies = parts.Where(p => Starts(p.Name, "Plintclip-adapter ") || Starts(p.Name, "SEKTION C-clip inschuiftong ")).ToList();
            foreach (var plinth in plinths)
            {
                foreach (var adapterBody in adapterBodies)
                {
                    if (AssemblyBodiesOverlap(plinth, adapterBody))
                        result.Errors.Add(plinth.Name + " kruist " + adapterBody.Name + ".");
                }
            }
            result.Checks.Add(parts.Count.ToString(CultureInfo.InvariantCulture) + " 3D-bodies op positieve maat en product-envelope gecontroleerd.");
        }

        private static bool IsLexProduct(string product)
        {
            return string.Equals(product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckLexBomRules(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetAuditResult result)
        {
            var config = new PortalConfigurationFactory().BuildLexWorkbench(request);
            var footAdapter = config.LevelingFootCornerAdapter;
            var levelingFoot = config.LevelingFoot;
            var stabilizer = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "HPL stabilisatieplaat tussen kolommen", StringComparison.OrdinalIgnoreCase));
            if (stabilizer == null)
                result.Errors.Add("LEX BOM-sync: HPL-stabilisatieplaat ontbreekt.");
            else if (stabilizer.Material == null || Math.Abs(stabilizer.Material.ThicknessMm - 6.0) > ToleranceMm)
                result.Errors.Add("LEX BOM-sync: HPL-stabilisatieplaat moet 6 mm zijn.");

            var top = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "Kogelpotblad HPL", StringComparison.OrdinalIgnoreCase));
            if (top == null || top.Holes.Count != 53)
                result.Errors.Add("LEX BOM-sync: kogelpotblad moet exact 53 gemodelleerde kogelpotposities hebben.");

            var adapter = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "HSR15R wagen adapterplaat naar vast frame", StringComparison.OrdinalIgnoreCase));
            if (adapter == null || adapter.Quantity != 4)
                result.Errors.Add("LEX BOM-sync: vier HSR15R-adapterplaten vereist.");

            var holders = model.Profiles.FirstOrDefault(p => string.Equals(p.Name, "Bewegende werkbladhouder horizontaal", StringComparison.OrdinalIgnoreCase));
            var holderPlacements = model.AssemblyPlacements.Count(p => p.PartName != null && p.PartName.StartsWith("Werkbladhouder ", StringComparison.OrdinalIgnoreCase));
            if (holders == null || holders.Quantity != 3 || holderPlacements != 3)
                result.Errors.Add("LEX BOM-sync: drie horizontale werkbladhouders moeten zowel in profiel-BOM als 3D-model staan.");
            var centerHolder = model.AssemblyPlacements.FirstOrDefault(p => string.Equals(p.PartName, "Werkbladhouder middenligger", StringComparison.OrdinalIgnoreCase));
            if (centerHolder == null || Math.Abs(centerHolder.Zmm - config.WorktopCenterSupportOffsetMm) > ToleranceMm)
                result.Errors.Add("LEX geometrie-sync: middenligger moet op Z=" + F(config.WorktopCenterSupportOffsetMm) + " mm tussen twee kogelpotrijen liggen.");

            var footProfile = model.Profiles.FirstOrDefault(p => string.Equals(p.Name, "Voetprofiel", StringComparison.OrdinalIgnoreCase));
            if (footProfile == null || footProfile.Material == null || Math.Abs(footProfile.Material.WidthMm - 80.0) > ToleranceMm || Math.Abs(footProfile.Material.HeightMm - 80.0) > ToleranceMm)
                result.Errors.Add("LEX BOM-sync: actuele voetprofielen zijn niet 80x80.");
            else if (string.IsNullOrWhiteSpace(footProfile.BomStatus) || footProfile.BomStatus.IndexOf("OPEN", StringComparison.OrdinalIgnoreCase) < 0)
                result.Errors.Add("LEX BOM-sync: de open keuze 80x80 versus 80x40 voor beenruimte moet zichtbaar in de BOM blijven.");
            else if (Math.Abs(footProfile.LengthMm - (config.DepthMm - 2.0 * footAdapter.ReachMm)) > ToleranceMm)
                result.Errors.Add("LEX voetprofiel is niet ingekort voor twee ZI-1744 hoekadapters; verwacht " + F(config.DepthMm - 2.0 * footAdapter.ReachMm) + " mm.");

            RequireLexHardware(model, "GeMinG HTE2 complete 2-koloms hefset O1, slag 400 mm", 1, result);
            RequireLexHardware(model, "Maunsystem stelvoet D80 M16x130 zwart", 4, result);
            RequireLexHardware(model, "Maunsystem Stellfusssockel 8 D80 hoekadapter M16", 4, result);
            RequireLexHardware(model, "Bevestigingsset Nut 8 voor ZI-1744 hoekadapter", 16, result);
            RequireLexHardware(model, "HSR15-compatible lineaire geleidingsset, 2x 1500 mm + 4 wagens", 1, result);
            RequireLexHardware(model, "HSR15R M4 verzonken wagenschroef", 16, result);
            RequireLexHardware(model, "Adapterplaat M8 bout met inschuifmoer", 8, result);
            RequireLexHardware(model, "Railmontage met inschuifmoeren", 50, result);
            RequireLexHardware(model, "Standaard profielverbinder serie 8 inclusief bout", 28, result);
            RequireLexHardware(model, "HTE2 eindplaat M8 bout met inschuifmoer", 8, result);
            RequireLexHardware(model, "Kogelpot / ball transfer unit", 53, result);
            RequireLexHardware(model, "Kogelpot / ball transfer unit - reserve", 4, result);
            RequireLexHardware(model, "Plunjerborging lineaire verschuiving", 3, result);
            RequireLexHardware(model, "Mechanische eindstop HSR15", 4, result);
            RequireLexHardware(model, "Bevestigingsset HPL-kogelpotblad aan bewegend frame", 1, result);
            RequireLexHardware(model, "Bevestigingsset HPL-stabilisatieplaat 6 mm", 1, result);
            RequireLexHardware(model, "Kabelmanagement en trekontlasting", 1, result);
            RequireLexHardware(model, "Typeplaat en veiligheidslabels", 1, result);
            RequireLexHardware(model, "Afdekkap 8 80x80 zwart", 4, result);
            RequireLexHardware(model, "Afdekkap 8 80x40 zwart", 4, result);

            var footAdaptersInModel = model.AssemblyPlacements.Count(p => Starts(p.PartName, "Stelpoot hoekadapter ZI-1744 "));
            var footDishesInModel = model.AssemblyPlacements.Count(p => string.Equals(p.PartName, "Stelvoet D80 schotel ZI-1415-S", StringComparison.OrdinalIgnoreCase));
            if (footAdaptersInModel != 4 || footDishesInModel != 4)
                result.Errors.Add("LEX BOM-sync: vier ZI-1744 hoekadapters en vier ZI-1415-S stelvoeten moeten fysiek in het 3D-model staan.");
            if (model.AssemblyPlacements.Any(p => Starts(p.PartName, "Afdekkap 8 80x80 zwart - voetprofiel")))
                result.Errors.Add("LEX voetprofielkoppen hebben nog een afdekkap terwijl de ZI-1744 adapter die kop volledig afdekt.");

            var assembly = new PortalAssembly3DService().Build(model, request);
            var adapterBodies = assembly.Where(p => Starts(p.Name, "Stelpoot hoekadapter ZI-1744 ")).ToList();
            if (adapterBodies.Count != 20)
                result.Errors.Add("LEX ZI-1744 3D-opbouw moet per hoek uit montageplaat, draagarm, afgeronde opname en twee ribben bestaan.");
            else
            {
                var adapterMinZ = adapterBodies.Min(p => p.Zmm - p.SizeZmm / 2.0);
                var adapterMaxZ = adapterBodies.Max(p => p.Zmm + p.SizeZmm / 2.0);
                if (Math.Abs(adapterMinZ + config.DepthMm / 2.0) > ToleranceMm || Math.Abs(adapterMaxZ - config.DepthMm / 2.0) > ToleranceMm)
                    result.Errors.Add("LEX ZI-1744 adapters vormen niet exact de totale diepte van " + F(config.DepthMm) + " mm.");
            }
            var footDishes = assembly.Where(p => string.Equals(p.Name, "Stelvoet D80 schotel ZI-1415-S", StringComparison.OrdinalIgnoreCase)).ToList();
            if (footDishes.Any(p => p.Zmm - levelingFoot.ActualFootDiameterMm / 2.0 < -config.DepthMm / 2.0 - ToleranceMm || p.Zmm + levelingFoot.ActualFootDiameterMm / 2.0 > config.DepthMm / 2.0 + ToleranceMm))
                result.Errors.Add("LEX D80-stelvoeten steken buiten het 1000-mm blad.");

            foreach (var item in model.Hardware)
            {
                if (string.IsNullOrWhiteSpace(item.ModelStatus)) result.Errors.Add("LEX BOM-sync: modelstatus ontbreekt bij " + item.Name + ".");
                if (string.IsNullOrWhiteSpace(item.BomStatus)) result.Errors.Add("LEX BOM-sync: BOM-status ontbreekt bij " + item.Name + ".");
                else if (item.BomStatus.IndexOf("OPEN", StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Warnings.Add("LEX open BOM-punt: " + item.Name + " - " + item.BomStatus + ".");
            }

            result.Checks.Add("LEX BOM-sync gecontroleerd: modeldelen, ZI-1744 hoekadapters, D80/M16x130-stelvoeten, 1000-mm diepte-envelope, 6 mm tussenplaat, bevestigingen, reserves en open punten.");
        }

        private static void RequireLexHardware(WorkbenchModel model, string name, int expectedQuantity, WorkbenchCabinetAuditResult result)
        {
            var item = model.Hardware.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                result.Errors.Add("LEX BOM-sync: hardware-regel ontbreekt: " + name + ".");
            else if (item.Quantity != expectedQuantity)
                result.Errors.Add("LEX BOM-sync: " + name + " heeft aantal " + item.Quantity.ToString(CultureInfo.InvariantCulture) + "; verwacht " + expectedQuantity.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static bool AssemblyBodiesOverlap(PortalAssemblyPart first, PortalAssemblyPart second)
        {
            return AxisOverlap(first.Xmm, first.SizeXmm, second.Xmm, second.SizeXmm) > ToleranceMm
                && AxisOverlap(first.Ymm, first.SizeYmm, second.Ymm, second.SizeYmm) > ToleranceMm
                && AxisOverlap(first.Zmm, first.SizeZmm, second.Zmm, second.SizeZmm) > ToleranceMm;
        }

        private static double AxisOverlap(double firstCenter, double firstSize, double secondCenter, double secondSize)
        {
            return Math.Min(firstCenter + firstSize / 2.0, secondCenter + secondSize / 2.0)
                - Math.Max(firstCenter - firstSize / 2.0, secondCenter - secondSize / 2.0);
        }

        private static bool PocketFootprintsOverlap(PortalAssemblyPocket first, PortalAssemblyPocket second)
        {
            if (first == null || second == null || !string.Equals(first.Plane, second.Plane, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(first.Plane, "x", StringComparison.OrdinalIgnoreCase))
                return AxisOverlap(first.Ymm, first.SizeYmm, second.Ymm, second.SizeYmm) > ToleranceMm
                    && AxisOverlap(first.Zmm, first.SizeZmm, second.Zmm, second.SizeZmm) > ToleranceMm;
            if (string.Equals(first.Plane, "y", StringComparison.OrdinalIgnoreCase))
                return AxisOverlap(first.Xmm, first.SizeXmm, second.Xmm, second.SizeXmm) > ToleranceMm
                    && AxisOverlap(first.Zmm, first.SizeZmm, second.Zmm, second.SizeZmm) > ToleranceMm;
            return AxisOverlap(first.Xmm, first.SizeXmm, second.Xmm, second.SizeXmm) > ToleranceMm
                && AxisOverlap(first.Ymm, first.SizeYmm, second.Ymm, second.SizeYmm) > ToleranceMm;
        }

        private static double PocketDiameter(PortalAssemblyPocket pocket)
        {
            if (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase)) return Math.Max(pocket.SizeYmm, pocket.SizeZmm);
            if (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase)) return Math.Max(pocket.SizeXmm, pocket.SizeZmm);
            return Math.Max(pocket.SizeXmm, pocket.SizeYmm);
        }

        private static double PocketDepth(PortalAssemblyPocket pocket)
        {
            if (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase)) return pocket.SizeXmm;
            if (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase)) return pocket.SizeYmm;
            return pocket.SizeZmm;
        }

        private static void CheckWorkbenchCabinetRules(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetAuditResult result)
        {
            var config = new PortalConfigurationFactory().BuildWorkbenchCabinet(request);
            CheckProductFastenerStandard(model, request, config, result);
            CheckWorkbenchFastenerBomSync(model, request, config, result);
            var units = Math.Max(1, request.UnitCount);
            var doorGap = 3.0;
            var doors = model.Sheets.Where(s => Starts(s.Name, "Draaideur ")).OrderBy(s => FindPlacement(model, s.Name).Xmm).ToList();
            if (doors.Count > 0)
            {
                const double expectedOuterRevealMm = 1.5;
                var firstDoorPlacement = FindPlacement(model, doors[0].Name);
                var lastDoorPlacement = FindPlacement(model, doors[doors.Count - 1].Name);
                var leftReveal = firstDoorPlacement.Xmm - doors[0].LengthMm / 2.0 + request.WidthMm / 2.0;
                var rightReveal = request.WidthMm / 2.0 - (lastDoorPlacement.Xmm + doors[doors.Count - 1].LengthMm / 2.0);
                if (Math.Abs(leftReveal - expectedOuterRevealMm) > ToleranceMm)
                    result.Errors.Add("Zichtlijn tussen linker deur en kopse kastzijde is " + F(leftReveal) + "mm; verwacht 1,5mm bij 16,5mm KOMPLEMENT-overlap.");
                if (Math.Abs(rightReveal - expectedOuterRevealMm) > ToleranceMm)
                    result.Errors.Add("Zichtlijn tussen rechter deur en kopse kastzijde is " + F(rightReveal) + "mm; verwacht 1,5mm bij 16,5mm KOMPLEMENT-overlap.");

                var carcassFrontZ = -config.DepthMm / 2.0;
                foreach (var door in doors)
                {
                    var placement = FindPlacement(model, door.Name);
                    var thickness = door.Material == null ? 18.0 : door.Material.ThicknessMm;
                    var doorBackZ = placement.Zmm + thickness / 2.0;
                    var clearance = carcassFrontZ - doorBackZ;
                    if (Math.Abs(clearance - config.DoorToCarcassClearanceMm) > ToleranceMm)
                        result.Errors.Add("Afstand tussen " + door.Name + " en de kopse kastkant is " + F(clearance) + "mm; verwacht " + F(config.DoorToCarcassClearanceMm) + "mm.");
                }
            }
            for (var i = 1; i < doors.Count; i++)
            {
                var left = FindPlacement(model, doors[i - 1].Name);
                var right = FindPlacement(model, doors[i].Name);
                var gap = (right.Xmm - doors[i].LengthMm / 2.0) - (left.Xmm + doors[i - 1].LengthMm / 2.0);
                if (Math.Abs(gap - doorGap) > ToleranceMm) result.Errors.Add("Deurspleet tussen " + doors[i - 1].Name + " en " + doors[i].Name + " is " + F(gap) + "mm; verwacht 3mm.");
            }

            var tStops = model.Sheets.Count(s => Starts(s.Name, "T-stijl deuraanslag"));
            var expectedTStops = units / 2;
            if (tStops != expectedTStops) result.Errors.Add("Aantal T-aanslagstroken is " + tStops + "; verwacht " + expectedTStops + ".");

            var assembly = new PortalAssembly3DService().Build(model, request);
            var footTemplate = config.AdjustableFoot ?? SWWerkplaats.Configurator.Application.ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var adapterTemplate = footTemplate.PlinthClipAdapter ?? SWWerkplaats.Configurator.Application.ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            var adapterBases = assembly.Where(p => Starts(p.Name, "Plintclip-adapter basis ")).ToList();
            var expectedAdapterCount = units + 1
                + (config.IncludeLeftSidePlinth ? 2 : 0)
                + (config.IncludeRightSidePlinth ? 2 : 0);
            if (adapterBases.Count != expectedAdapterCount)
                result.Errors.Add("Aantal plintclip-adapterbasissen is " + adapterBases.Count + "; verwacht " + expectedAdapterCount + ".");
            foreach (var adapterBase in adapterBases)
            {
                if (adapterBase.Pockets.Count(p => string.Equals(p.Name, "Schroefgat adapter", StringComparison.OrdinalIgnoreCase)) != 2)
                    result.Errors.Add(adapterBase.Name + " heeft niet exact twee schroefgaten.");
                var countersinks = adapterBase.Pockets.Where(p => string.Equals(p.Name, "Conische verzinking adapterschroef", StringComparison.OrdinalIgnoreCase)).ToList();
                if (countersinks.Count != 2)
                    result.Errors.Add(adapterBase.Name + " heeft niet exact twee conische schroefverzinkingen.");
                foreach (var countersink in countersinks)
                {
                    if (!Starts(countersink.Shape, "cone")
                        || Math.Abs(PocketDiameter(countersink) - adapterTemplate.MountingCountersinkDiameterMm) > ToleranceMm
                        || Math.Abs(PocketDepth(countersink) - adapterTemplate.MountingCountersinkDepthMm) > ToleranceMm
                        || Math.Abs(countersink.MinorDiameterMm - adapterTemplate.MountingHoleDiameterMm) > ToleranceMm)
                        result.Errors.Add(adapterBase.Name + " heeft een afwijkende conische kopzitting; verwacht Ø" + F(adapterTemplate.MountingCountersinkDiameterMm) + "x" + F(adapterTemplate.MountingCountersinkDepthMm) + "mm naar Ø" + F(adapterTemplate.MountingHoleDiameterMm) + "mm.");
                }
                var tongueSlot = adapterBase.Pockets.FirstOrDefault(p => string.Equals(p.Name, "Inschuifkamer cliptong", StringComparison.OrdinalIgnoreCase));
                if (tongueSlot == null)
                    result.Errors.Add(adapterBase.Name + " mist de inschuifkamer voor de cliptong.");
                else if (countersinks.Any(c => PocketFootprintsOverlap(c, tongueSlot)))
                    result.Errors.Add(adapterBase.Name + " heeft een schroefkopzitting die het schuifvlak van de cliptong kruist.");
            }
            var frontAdapterBases = adapterBases.Where(p => Contains(p.Name, "basis voor grens")).ToList();
            var sideAdapterBases = adapterBases.Where(p => Contains(p.Name, "basis zijde")).ToList();
            foreach (var frontAdapter in frontAdapterBases)
            {
                foreach (var sideAdapter in sideAdapterBases)
                {
                    if (AssemblyBodiesOverlap(frontAdapter, sideAdapter))
                        result.Errors.Add(frontAdapter.Name + " kruist " + sideAdapter.Name + " op de gedeelde hoek.");
                }
            }

            foreach (var tongue in assembly.Where(p => Starts(p.Name, "SEKTION C-clip inschuiftong ")))
            {
                var label = tongue.Name.Substring("SEKTION C-clip inschuiftong ".Length);
                PortalAssemblyPart roundFoot = null;
                double axisDistance;
                if (Starts(label, "voor grens "))
                {
                    roundFoot = assembly.FirstOrDefault(p => string.Equals(p.Name, "SEKTION ronde stelpoot " + label, StringComparison.OrdinalIgnoreCase));
                    axisDistance = roundFoot == null ? -1 : roundFoot.Zmm - (tongue.Zmm - tongue.SizeZmm / 2.0);
                }
                else
                {
                    var isLeft = Contains(label, "links");
                    var isFront = Contains(label, "voor");
                    var boundary = isLeft ? 0 : units;
                    var footLabel = (isFront ? "voor grens " : "achter grens ") + boundary.ToString(CultureInfo.InvariantCulture);
                    roundFoot = assembly.FirstOrDefault(p => string.Equals(p.Name, "SEKTION ronde stelpoot " + footLabel, StringComparison.OrdinalIgnoreCase));
                    axisDistance = roundFoot == null
                        ? -1
                        : (isLeft
                            ? roundFoot.Xmm - (tongue.Xmm - tongue.SizeXmm / 2.0)
                            : (tongue.Xmm + tongue.SizeXmm / 2.0) - roundFoot.Xmm);
                }
                if (roundFoot == null)
                    result.Errors.Add("Geen bijbehorende ronde poot gevonden voor " + tongue.Name + ".");
                else if (Math.Abs(axisDistance - adapterTemplate.FootAxisFromTongueBackMm) > ToleranceMm)
                    result.Errors.Add(tongue.Name + " ligt " + F(axisDistance) + "mm van de pootas; verwacht " + F(adapterTemplate.FootAxisFromTongueBackMm) + "mm.");
            }
            var doubledBoundaries = model.Sheets.Count(s => Starts(s.Name, "Volledig tussenschot dubbel"));
            var expectedDoubledPanels = Math.Max(0, (units - 1) / 2) * 2;
            if (doubledBoundaries != expectedDoubledPanels) result.Errors.Add("Aantal platen in dubbele scheidingen is " + doubledBoundaries + "; verwacht " + expectedDoubledPanels + ".");
            foreach (var stop in model.AssemblyPlacements.Where(p => Starts(p.PartName, "T-stijl deuraanslag")))
            {
                var divider = model.AssemblyPlacements.FirstOrDefault(p => Starts(p.PartName, "Volledig tussenschot T") && Math.Abs(p.Xmm - stop.Xmm) < ToleranceMm);
                if (divider == null) { result.Errors.Add("Geen middenstaander achter " + stop.PartName + "."); continue; }
                var dividerFront = divider.Zmm - divider.LengthMm / 2.0;
                var stopBack = stop.Zmm + 9.0;
                var engagement = stopBack - dividerFront;
                if (Math.Abs(engagement - 3.0) > ToleranceMm)
                    result.Errors.Add("Middenstaander grijpt " + F(engagement) + "mm in " + stop.PartName + "; verwacht 3mm centreersleuf.");
            }

            foreach (var shelf in model.Sheets.Where(s => Starts(s.Name, "Legplank unit")))
            {
                var unitNumber = ParseUnitNumber(shelf.Name);
                if (unitNumber <= 0 || (unitNumber % 2 != 0 && unitNumber == units)) continue;
                var requiresStopNotch = unitNumber % 2 == 1 || unitNumber % 2 == 0;
                var pairBoundary = unitNumber % 2 == 1 ? unitNumber : unitNumber - 1;
                if (!requiresStopNotch || pairBoundary >= units) continue;

                var dividerThickness = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
                var expectedNotchLength = Math.Max(0.0, (config.DoorStopWidthMm - dividerThickness) / 2.0 + 1.0);
                var expectedNotchWidth = Math.Max(0.0, dividerThickness + 1.0 - config.ShelfFrontInsetMm);
                var notch = shelf.Pockets.FirstOrDefault(p => Starts(p.Name, "Uitsparing T-stijl"));
                if (expectedNotchLength <= ToleranceMm || expectedNotchWidth <= ToleranceMm)
                {
                    // De legplank begint achter de volledige diepte van de T-stijl;
                    // een uitsparing is dan niet nodig en zou alleen materiaal wegnemen.
                    continue;
                }
                if (notch == null
                    || notch.DepthMode != OperationDepthMode.Through
                    || Math.Abs(notch.LengthMm - expectedNotchLength) > ToleranceMm
                    || Math.Abs(notch.WidthMm - expectedNotchWidth) > ToleranceMm)
                    result.Errors.Add(
                        "Ontbrekende of afwijkende T-stijluitsparing in " + shelf.Name
                        + "; verwacht " + F(expectedNotchLength) + "x" + F(expectedNotchWidth) + "mm bij een legplankoffset van "
                        + F(config.ShelfFrontInsetMm) + "mm.");
            }

            var bottom = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "Doorlopende bodemplaat", StringComparison.OrdinalIgnoreCase));
            if (bottom == null) result.Errors.Add("Doorlopende bodemplaat ontbreekt.");
            else
            {
                var footHoles = bottom.Holes.Where(h => h.SupportKind == SheetHoleSupportKind.AdjustableFoot).ToList();
                var expectedFootHoles = 2 * (units + 1) * 2;
                if (footHoles.Count != expectedFootHoles) result.Errors.Add("Aantal CNC-pengaten voor poten is " + footHoles.Count + "; verwacht " + expectedFootHoles + ".");
                if (footHoles.Any(h => h.DepthMode != OperationDepthMode.Through || Math.Abs(h.DiameterMm - 10.0) > ToleranceMm))
                    result.Errors.Add("Niet alle poot-pengaten zijn doorlopend Ø10mm.");
                if (bottom.Holes.Any(h => h.SupportKind == SheetHoleSupportKind.AdjustableFoot && h.DiameterMm < 9.9))
                    result.Errors.Add("Onverwacht klein CNC-pootgat aangetroffen.");

                var expectedFrontCenter = SWWerkplaats.Configurator.Application.ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
                var sidePlinthFootInset = SWWerkplaats.Configurator.Application.ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
                var frontFootHoles = footHoles.Where(h => Contains(h.Name, "voor grens")).ToList();
                if (frontFootHoles.Count > 0)
                {
                    var actualFrontCenter = frontFootHoles.Average(h => h.Ymm);
                    if (Math.Abs(actualFrontCenter - expectedFrontCenter) > ToleranceMm)
                        result.Errors.Add("Hart voorpoten ligt op " + F(actualFrontCenter) + "mm; verwacht " + F(expectedFrontCenter) + "mm vanuit plintgeometrie.");
                }

                CheckOuterFootCenter(footHoles, "grens 0", config.IncludeLeftSidePlinth ? sidePlinthFootInset : config.AdjustableFootInsetMm, "links", result);
                CheckOuterFootCenter(footHoles, "grens " + units.ToString(CultureInfo.InvariantCulture), config.WidthMm - (config.IncludeRightSidePlinth ? sidePlinthFootInset : config.AdjustableFootInsetMm), "rechts", result);
            }

            var frontPlinth = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "Losse voorzetplint", StringComparison.OrdinalIgnoreCase));
            var leftSidePlinth = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "Zijplint links", StringComparison.OrdinalIgnoreCase));
            var rightSidePlinth = model.Sheets.FirstOrDefault(s => string.Equals(s.Name, "Zijplint rechts", StringComparison.OrdinalIgnoreCase));
            if (frontPlinth == null)
            {
                result.Errors.Add("Losse voorzetplint ontbreekt.");
            }
            else
            {
                CheckPlinthAdapterMarks(frontPlinth, 2 * (units + 1), OperationFace.PositiveZ, adapterTemplate, "voorplint", result);
                var frontPlacement = FindPlacement(model, frontPlinth.Name);
                var plinthThickness = frontPlinth.Material == null ? 18.0 : frontPlinth.Material.ThicknessMm;
                var frontFaceZ = frontPlacement.Zmm - plinthThickness / 2.0;
                CheckSidePlinth(model, request.WorkbenchCabinetIncludeLeftSidePlinth, leftSidePlinth, true, frontPlinth, frontPlacement, frontFaceZ, request, result);
                CheckSidePlinth(model, request.WorkbenchCabinetIncludeRightSidePlinth, rightSidePlinth, false, frontPlinth, frontPlacement, frontFaceZ, request, result);
            }
            if (leftSidePlinth != null) CheckPlinthAdapterMarks(leftSidePlinth, 4, OperationFace.PositiveX, adapterTemplate, "linker zijplint", result);
            if (rightSidePlinth != null) CheckPlinthAdapterMarks(rightSidePlinth, 4, OperationFace.NegativeX, adapterTemplate, "rechter zijplint", result);
            CheckPlinthMarkAlignment(assembly, adapterTemplate, result);

            var shelfHoles = model.Sheets.SelectMany(s => s.Holes.Select(h => new { Sheet = s, Hole = h })).Where(x => x.Hole.SupportKind == SheetHoleSupportKind.ShelfSupport).ToList();
            if (request.IncludeAdjustableShelfHoles)
            {
                foreach (var row in shelfHoles.GroupBy(x => x.Sheet.Name + "|" + F(x.Hole.Xmm) + "|" + x.Hole.Face))
                {
                    if (row.Count() != request.AdjustableShelfPositionCount)
                        result.Errors.Add("Legplankrij " + row.Key + " bevat " + row.Count() + " gaten; verwacht " + request.AdjustableShelfPositionCount + ".");
                    var ys = row.Select(x => x.Hole.Ymm).OrderBy(y => y).ToList();
                    for (var i = 1; i < ys.Count; i++)
                    {
                        var delta = ys[i] - ys[i - 1];
                        if (Math.Abs(delta / 32.0 - Math.Round(delta / 32.0)) > 0.001)
                            result.Errors.Add("Legplankrij wijkt af van systeem-32: " + row.Key + ".");
                    }
                }
            }

            var railHoles = model.Sheets.SelectMany(s => s.Holes.Select(h => new { Sheet = s, Hole = h })).Where(x => x.Hole.SupportKind == SheetHoleSupportKind.DrawerRail).ToList();
            if (request.IncludeTopDrawer)
            {
                foreach (var rail in railHoles)
                {
                    var drawerSideHole = Starts(rail.Hole.Name, "Laderailgat ladezijde");
                    var expectedPilotDiameter = drawerSideHole
                        ? config.DrawerRail.DrawerHoleDiameterMm
                        : config.DrawerRail.CabinetHoleDiameterMm;
                    if (expectedPilotDiameter <= 0)
                        result.Errors.Add("Gewenste ontvangende rail-pilotgatdiameter ontbreekt in de railcomponent: "
                            + rail.Sheet.Name + " / " + rail.Hole.Name + ".");
                    else if (Math.Abs(rail.Hole.DiameterMm - expectedPilotDiameter) > ToleranceMm)
                        result.Errors.Add("Rail-pilotgat moet diameter " + F(expectedPilotDiameter) + "mm zijn voor de "
                            + F(config.DrawerRail.CabinetFastenerDiameterMm) + "mm schroef: "
                            + rail.Sheet.Name + " / " + rail.Hole.Name + ".");
                    var exterior = Starts(rail.Sheet.Name, "Werkbank zijwand");
                    if (exterior && (rail.Hole.DepthMode != OperationDepthMode.BlindFromFace || Math.Abs(rail.Hole.DepthMm - 12.0) > ToleranceMm))
                        result.Errors.Add("Buitenste railgat moet 12mm blind zijn: " + rail.Sheet.Name + " / " + rail.Hole.Name + ".");
                    if (!exterior && rail.Hole.DepthMode != OperationDepthMode.Through)
                        result.Errors.Add("Intern railgat moet doorlopend zijn: " + rail.Sheet.Name + " / " + rail.Hole.Name + ".");
                }

                var railTemplate = config.DrawerRail;
                if (railTemplate.CabinetFastenerLengthMm <= 0)
                {
                    result.Errors.Add("Schroeflengte voor bevestiging van de ladegeleider aan de kast ontbreekt; botsing tussen tegenoverliggende rails kan niet worden gecontroleerd.");
                }
                else
                {
                    var penetration = Math.Max(0.0, railTemplate.CabinetFastenerLengthMm - Math.Max(0.0, railTemplate.CabinetFastenerPassingStackMm));
                    var minimumOpposingClearance = Math.Max(2.0, config.SheetFastener == null ? 0.0 : config.SheetFastener.MinimumTipClearanceMm);
                    var primaryPositions = RailPositionsForAudit(
                        railTemplate.CabinetHolePositionsMm,
                        railTemplate.CabinetHoleCount,
                        railTemplate.CabinetFirstHoleOffsetMm,
                        railTemplate.CabinetHoleSpacingMm);
                    var oppositePatternMissing = string.IsNullOrWhiteSpace(railTemplate.CabinetOppositeHolePositionsMm);
                    var oppositePositions = RailPositionsForAudit(
                        oppositePatternMissing ? railTemplate.CabinetHolePositionsMm : railTemplate.CabinetOppositeHolePositionsMm,
                        railTemplate.CabinetHoleCount,
                        railTemplate.CabinetFirstHoleOffsetMm,
                        railTemplate.CabinetHoleSpacingMm);
                    const double minimumOpposingAxisSpacingMm = 6.0;
                    var verifiedZeroClearanceFitUsed = false;
                    foreach (var group in railHoles
                        .Where(x => x.Hole.DepthMode == OperationDepthMode.Through
                            && Starts(x.Hole.Name, "Bovenlade railgat")
                            && Starts(x.Sheet.Name, "Volledig tussenschot T "))
                        .GroupBy(x => x.Sheet))
                    {
                        var thickness = group.Key.Material == null ? 0.0 : group.Key.Material.ThicknessMm;
                        var clearance = FastenerSelectionService.OpposingScrewTipClearance(thickness, penetration, penetration);
                        var verifiedZeroClearanceFit = clearance >= -ToleranceMm
                            && string.Equals(
                                railTemplate.CabinetOpposingFitVerificationSignature,
                                RailOpposingFitVerificationSignature(railTemplate, thickness),
                                StringComparison.OrdinalIgnoreCase);
                        var collidingPairs = 0;
                        if (clearance + ToleranceMm < minimumOpposingClearance && !verifiedZeroClearanceFit)
                        {
                            foreach (var primary in primaryPositions)
                            {
                                foreach (var opposite in oppositePositions)
                                {
                                    if (Math.Abs(primary - opposite) + ToleranceMm < minimumOpposingAxisSpacingMm) collidingPairs++;
                                }
                            }
                        }
                        if (primaryPositions.Count < 3 || oppositePositions.Count < 3)
                        {
                            result.Errors.Add(
                                group.Key.Name + " heeft onvoldoende vrijgegeven railbevestigingsposities. "
                                + "Leg minimaal drie bruikbare posities per railzijde vast.");
                        }
                        else if (collidingPairs > 0)
                        {
                            result.Errors.Add(
                                group.Key.Name + " heeft " + collidingPairs.ToString(CultureInfo.InvariantCulture)
                                + " botsende combinaties tussen de twee railpatronen met " + railTemplate.FastenerName
                                + ". De tipruimte op dezelfde of te nabije as is " + F(clearance) + "mm; minimaal "
                                + F(minimumOpposingClearance) + "mm tipruimte of " + F(minimumOpposingAxisSpacingMm)
                                + "mm hartafstand tussen versprongen assen vereist."
                                + (oppositePatternMissing ? " Het tegenoverliggende patroon ontbreekt; daarom is conservatief hetzelfde patroon aangenomen." : ""));
                        }
                        else if (verifiedZeroClearanceFit)
                        {
                            verifiedZeroClearanceFitUsed = true;
                        }
                    }
                    if (verifiedZeroClearanceFitUsed)
                        result.Checks.Add("Fysieke railproef vrijgegeven voor Ø4,2x9,5 plaatkop door 0,5mm rail aan beide zijden van exact 18mm plaat; nominale tipruimte 0mm.");
                }

                var worktop = model.Sheets.FirstOrDefault(s => Starts(s.Name, "Werkblad werkbankkast"));
                var worktopThickness = worktop == null || worktop.Material == null ? 18.0 : worktop.Material.ThicknessMm;
                foreach (var drawerFront in model.Sheets.Where(s => Starts(s.Name, "Bovenlade front")))
                {
                    var placement = FindPlacement(model, drawerFront.Name);
                    var frontTop = placement.Ymm + drawerFront.WidthMm / 2.0;
                    if (Math.Abs(frontTop - (request.HeightMm - 3.0)) > ToleranceMm)
                        result.Errors.Add("Bovenkant " + drawerFront.Name + " ligt op Y=" + F(frontTop) + "mm; verwacht 3mm onder het bovenvlak.");

                    var roundEnds = drawerFront.Holes.Count(h => h.SupportKind == SheetHoleSupportKind.MachiningCutout && Starts(h.Name, "Uitgefreesde handgreep ronding") && h.DepthMode != OperationDepthMode.Through);
                    var slotMiddle = drawerFront.Pockets.Count(p => Starts(p.Name, "Uitgefreesde handgreep midden") && p.DepthMode != OperationDepthMode.Through);
                    var finishContour = drawerFront.Pockets.Count(p => p.Name.IndexOf("handgreep afwerkcontour", StringComparison.OrdinalIgnoreCase) >= 0 && p.DepthMode == OperationDepthMode.Through);
                    if (request.IncludeDrawerPullCutouts && (roundEnds != 2 || slotMiddle != 1 || finishContour != 1))
                        result.Errors.Add("Handgreepsleuf van " + drawerFront.Name + " mist de 2mm-voorpocket of de getabde afwerkcontour.");

                    var sideRabbets = drawerFront.Pockets.Where(p => Starts(p.Name, "Ladefront linker zij-rabat") || Starts(p.Name, "Ladefront rechter zij-rabat")).ToList();
                    if (sideRabbets.Count != 2)
                    {
                        result.Errors.Add(drawerFront.Name + " heeft niet exact twee zij-rabatten voor de ladebak.");
                    }
                    else
                    {
                        foreach (var rabbet in sideRabbets)
                        {
                            var closedTopEdge = drawerFront.WidthMm - (rabbet.Ymm + rabbet.WidthMm);
                            var expectedTopEdge = Math.Max(0, worktopThickness - 3.0);
                            if (Math.Abs(closedTopEdge - expectedTopEdge) > ToleranceMm)
                                result.Errors.Add(rabbet.Name + " in " + drawerFront.Name + " laat " + F(closedTopEdge) + "mm bovenrand staan; verwacht " + F(expectedTopEdge) + "mm volgens de proefcorrectie van 3mm.");
                        }
                    }

                    CheckDrawerFrontFastenerPattern(drawerFront, result);
                }

                foreach (var drawerBoxPart in model.Sheets.Where(s => Starts(s.Name, "Bovenlade zijde") || Starts(s.Name, "Bovenlade achter")))
                {
                    var placement = FindPlacement(model, drawerBoxPart.Name);
                    var boxTop = placement.Ymm + drawerBoxPart.WidthMm / 2.0;
                    var expectedMaximum = request.HeightMm - worktopThickness - 3.0;
                    if (boxTop > expectedMaximum + ToleranceMm)
                        result.Errors.Add(drawerBoxPart.Name + " kruist de werkbladzone; bovenkant Y=" + F(boxTop) + "mm, maximaal " + F(expectedMaximum) + "mm.");
                }
            }

            var hingeCups = model.Sheets.SelectMany(s => s.Holes).Where(h => h.SupportKind == SheetHoleSupportKind.HingeCup).ToList();
            if (hingeCups.Any(h => Math.Abs(h.DiameterMm - 35.0) > ToleranceMm || Math.Abs(h.DepthMm - 13.0) > ToleranceMm))
                result.Errors.Add("Afwijkend KOMPLEMENT-potgat aangetroffen; verwacht Ø35x13mm.");
            result.Checks.Add("Deurspleten, T-stijlen, dubbele scheidingen, pootgaten inclusief plintrelatie, voor- en zijplinten, plintclip-adapters met blinde pilotgaten en conische verzinkingen, legplankrijen, railgaten, ladefront-offsets, handgreepsleuven en scharnierpotten gecontroleerd.");

            if (adapterTemplate.FullDesignVerified)
                result.Checks.Add("SEKTION plintclipadapter V2 is inclusief montagevleugel en Ø8,3x4,2mm kopzittingen fysiek goedgekeurd.");
            else
                result.Warnings.Add("De passing van de ingemeten 28x34,5x3,3mm inschuiftong met 0,25mm printspeling per zijde is fysiek goedgekeurd. Print één V2-adapter om alleen de nieuwe montagevleugel en de Ø8,3x4,2mm kopzittingen te controleren voordat de volledige serie wordt geprint.");
        }

        private static List<double> RailPositionsForAudit(string explicitPositions, int count, double firstOffset, double spacing)
        {
            var positions = new List<double>();
            if (!string.IsNullOrWhiteSpace(explicitPositions))
            {
                var parts = explicitPositions.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    double value;
                    if (double.TryParse(part.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        AddUniqueAuditRailPosition(positions, value);
                }
            }
            if (positions.Count == 0)
            {
                for (var i = 0; i < count; i++) AddUniqueAuditRailPosition(positions, firstOffset + i * spacing);
            }
            positions.Sort();
            return positions;
        }

        private static void AddUniqueAuditRailPosition(List<double> positions, double candidate)
        {
            if (!positions.Any(position => Math.Abs(position - candidate) <= ToleranceMm)) positions.Add(candidate);
        }

        private static void CheckWorkbenchFastenerBomSync(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetConfig config, WorkbenchCabinetAuditResult result)
        {
            var units = Math.Max(1, request.UnitCount);
            var foot = config.AdjustableFoot;
            var adapter = foot == null ? null : foot.PlinthClipAdapter;
            if (adapter != null)
            {
                RequireHardwareArticle(
                    model,
                    "PLINTH_ADAPTER_SCREW_4X" + adapter.FrontScrewLengthMm.ToString("0", CultureInfo.InvariantCulture),
                    2 * (units + 1),
                    "korte plintclip-adapterschroeven",
                    result);
                var sideAdapterCount = (config.IncludeLeftSidePlinth ? 2 : 0) + (config.IncludeRightSidePlinth ? 2 : 0);
                if (sideAdapterCount > 0)
                {
                    RequireHardwareArticle(
                        model,
                        "PLINTH_ADAPTER_SCREW_4X" + adapter.SideScrewLengthMm.ToString("0", CultureInfo.InvariantCulture),
                        2 * sideAdapterCount,
                        "verlengde plintclip-adapterschroeven",
                        result);
                }
            }
            if (foot != null)
            {
                RequireHardwareArticle(
                    model,
                    "SEKTION_FOOT_SCREW_4X" + foot.CentralFastenerLengthMm.ToString("0", CultureInfo.InvariantCulture),
                    2 * (units + 1),
                    "SEKTION-voetschroeven",
                    result);
            }
            if (request.IncludeTopDrawer && config.DrawerRail != null)
            {
                var railLength = config.DrawerRail.CabinetFastenerLengthMm.ToString("0.#", CultureInfo.InvariantCulture);
                RequireHardwareArticle(
                    model,
                    RailCabinetFastenerArticle(config.DrawerRail),
                    units * 2 * Math.Max(0, config.DrawerRail.CabinetHoleCount),
                    "kastschroeven voor ladegeleiders",
                    result);
                if (model.Hardware.Any(h => string.Equals(h.ArticleNumber, "RAIL_SCREW_4X10_SHORT", StringComparison.OrdinalIgnoreCase)))
                    result.Errors.Add("BOM bevat de vervallen 4x10-railbevestiger terwijl de railcontrole met "
                        + F(config.DrawerRail.CabinetFastenerDiameterMm) + "x" + railLength + " rekent.");
            }
            var panelScrew = config.SheetFastener;
            if (panelScrew != null)
            {
                RequireHardwareArticle(
                    model,
                    panelScrew.Id + "_L" + panelScrew.LengthMm.ToString("0", CultureInfo.InvariantCulture),
                    model.Sheets.SelectMany(s => s.Holes).Count(h => h.SupportKind == SheetHoleSupportKind.PanelScrew),
                    "plaat-op-plaat montageschroeven",
                    result);
            }
            result.Checks.Add("BOM-bevestigers gecontroleerd op dezelfde artikel-ID, lengte en hoeveelheid als geometrie en veiligheidsregels.");
        }

        private static void RequireHardwareArticle(WorkbenchModel model, string articleNumber, int expectedQuantity, string label, WorkbenchCabinetAuditResult result)
        {
            var item = model.Hardware.FirstOrDefault(h => string.Equals(h.ArticleNumber, articleNumber, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                result.Errors.Add("BOM-sync: " + label + " ontbreken; verwacht artikel " + articleNumber + ".");
                return;
            }
            if (item.Quantity != expectedQuantity)
                result.Errors.Add("BOM-sync: " + label + " hebben aantal " + item.Quantity.ToString(CultureInfo.InvariantCulture) + "; verwacht " + expectedQuantity.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static void CheckProductFastenerStandard(WorkbenchModel model, PortalQuoteRequest request, WorkbenchCabinetConfig config, WorkbenchCabinetAuditResult result)
        {
            var standard = ProductFastenerStandards.Resolve(string.IsNullOrWhiteSpace(request.Product) ? "werkbankkast" : request.Product);
            var screw = config.SheetFastener;
            if (screw == null || screw.UsageKind != FastenerUsageKind.WoodScrew || !string.Equals(screw.Id, standard.WoodToWoodFastenerId, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Werkbankkast gebruikt niet de productgebonden hout-op-hout bevestigerstandaard.");
                return;
            }
            if (Math.Abs(screw.ClearanceHoleDiameterMm - 4.0) > ToleranceMm)
                result.Errors.Add("Hout-op-hout boorgat is Ø" + F(screw.ClearanceHoleDiameterMm) + "mm; verwacht geteste productstandaard Ø4mm.");
            if (screw.ReceivingPilotHoleDiameterMm <= 0)
                result.Errors.Add("Gewenste ontvangende pilotgatdiameter ontbreekt bij houtschroeffamilie " + screw.Id + ".");
            var panelHoles = model.Sheets.SelectMany(s => s.Holes).Where(h => h.SupportKind == SheetHoleSupportKind.PanelScrew).ToList();
            if (panelHoles.Any(h => Math.Abs(h.DiameterMm - screw.ClearanceHoleDiameterMm) > ToleranceMm))
                result.Errors.Add("Niet alle hout-op-hout CNC-gaten volgen de productstandaard Ø" + F(screw.ClearanceHoleDiameterMm) + "mm.");

            var carcassThickness = config.CarcassMaterial == null ? 18.0 : config.CarcassMaterial.ThicknessMm;
            var selectedEdgeLength = FastenerSelectionService.SelectWoodToWoodEdgeLength(screw, carcassThickness, Math.Max(config.DepthMm, config.WorktopHeightMm));
            if (Math.Abs(screw.LengthMm - selectedEdgeLength) > ToleranceMm || screw.LengthMm - carcassThickness < 20.0 - ToleranceMm)
                result.Errors.Add("Hout-op-hout schroeflengte voldoet niet aan minimaal 20mm grip in de kopse kant; gekozen 4x" + F(screw.LengthMm) + ".");

            var adapter = config.AdjustableFoot == null ? null : config.AdjustableFoot.PlinthClipAdapter;
            if (adapter != null)
            {
                if (Math.Abs(adapter.PlinthCenterMarkDiameterMm - screw.ReceivingPilotHoleDiameterMm) > ToleranceMm)
                    result.Errors.Add("Pilotgaten van de plintclipadapter volgen niet de centrale houtschroefwaarde Ø"
                        + F(screw.ReceivingPilotHoleDiameterMm) + "mm.");
                var expectedFront = FastenerSelectionService.SelectComponentToWoodFaceLength(screw, ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config), carcassThickness);
                var expectedSide = FastenerSelectionService.SelectComponentToWoodFaceLength(screw, ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config), carcassThickness);
                if (Math.Abs(adapter.FrontScrewLengthMm - expectedFront) > ToleranceMm || Math.Abs(adapter.SideScrewLengthMm - expectedSide) > ToleranceMm)
                    result.Errors.Add("Plintclipschroeflengte volgt niet automatisch de adapteruitstand en plaatdikte.");
            }
            var foot = config.AdjustableFoot;
            if (foot != null)
            {
                var expectedFoot = FastenerSelectionService.SelectComponentToWoodFaceLength(screw, 2.0, carcassThickness);
                if (Math.Abs(foot.CentralFastenerLengthMm - expectedFoot) > ToleranceMm)
                    result.Errors.Add("SEKTION-voetschroef volgt niet de 2mm kunststofweg en bodemplaatdikte.");
            }
            result.Checks.Add("Productgebonden bevestigerstandaard gecontroleerd: hout-op-hout Ø4, ontvangend pilotgat Ø"
                + F(screw.ReceivingPilotHoleDiameterMm) + ", handelslengte 4x" + F(screw.LengthMm)
                + ", minimaal 20mm kopse grip en plintschroeven zonder doorsteek.");
        }

        private static void CheckDrawerFrontFastenerPattern(SheetPart drawerFront, WorkbenchCabinetAuditResult result)
        {
            var left = drawerFront.Holes
                .Where(h => h.SupportKind == SheetHoleSupportKind.PanelScrew && Starts(h.Name, "Ladefront naar linker zijkant"))
                .OrderBy(h => h.Ymm)
                .ToList();
            var right = drawerFront.Holes
                .Where(h => h.SupportKind == SheetHoleSupportKind.PanelScrew && Starts(h.Name, "Ladefront naar rechter zijkant"))
                .OrderBy(h => h.Ymm)
                .ToList();
            var bottom = drawerFront.Holes
                .Where(h => h.SupportKind == SheetHoleSupportKind.PanelScrew && Starts(h.Name, "Montagegat ladefront naar ladebodem"))
                .OrderBy(h => h.Xmm)
                .ToList();

            if (left.Count != 2 || right.Count != 2 || bottom.Count != 3)
            {
                result.Errors.Add(drawerFront.Name + " heeft geen gereduceerd 2+2+3-schroefpatroon.");
                return;
            }

            if (left.Where((hole, index) => Math.Abs(hole.Ymm - right[index].Ymm) > ToleranceMm).Any()
                || left.Where((hole, index) => Math.Abs((hole.Xmm + right[index].Xmm) - drawerFront.LengthMm) > ToleranceMm).Any())
                result.Errors.Add("De zijschroeven van " + drawerFront.Name + " zijn niet spiegelsymmetrisch verdeeld.");

            var firstGap = bottom[1].Xmm - bottom[0].Xmm;
            var secondGap = bottom[2].Xmm - bottom[1].Xmm;
            if (Math.Abs(firstGap - secondGap) > ToleranceMm
                || Math.Abs((bottom[0].Xmm + bottom[2].Xmm) - drawerFront.LengthMm) > ToleranceMm)
                result.Errors.Add("De drie bodemschroeven van " + drawerFront.Name + " zijn niet gelijkmatig en symmetrisch verdeeld.");
        }

        private static void CheckOuterFootCenter(
            List<SheetHole> footHoles,
            string boundaryLabel,
            double expectedCenter,
            string side,
            WorkbenchCabinetAuditResult result)
        {
            var boundaryHoles = footHoles.Where(h => Contains(h.Name, boundaryLabel)).ToList();
            if (boundaryHoles.Count == 0) return;
            var actualCenter = boundaryHoles.Average(h => h.Xmm);
            if (Math.Abs(actualCenter - expectedCenter) > ToleranceMm)
                result.Errors.Add("Hart buitenste poten " + side + " ligt op " + F(actualCenter) + "mm; verwacht " + F(expectedCenter) + "mm.");
        }

        private static void CheckPlinthAdapterMarks(
            SheetPart plinth,
            int expectedCount,
            OperationFace expectedFace,
            PlinthClipAdapterTemplate adapter,
            string label,
            WorkbenchCabinetAuditResult result)
        {
            var marks = plinth.Holes.Where(h => h.SupportKind == SheetHoleSupportKind.PlinthClip).ToList();
            if (marks.Count != expectedCount)
                result.Errors.Add("Aantal blinde centreergaten in " + label + " is " + marks.Count.ToString(CultureInfo.InvariantCulture) + "; verwacht " + expectedCount.ToString(CultureInfo.InvariantCulture) + ".");
            if (marks.Any(h => h.DepthMode != OperationDepthMode.BlindFromFace
                || h.Face != expectedFace
                || Math.Abs(h.DiameterMm - adapter.PlinthCenterMarkDiameterMm) > ToleranceMm
                || Math.Abs(h.DepthMm - adapter.PlinthCenterMarkDepthMm) > ToleranceMm))
                result.Errors.Add("Afwijkend centreergat in " + label + "; verwacht blind Ø" + F(adapter.PlinthCenterMarkDiameterMm) + "x" + F(adapter.PlinthCenterMarkDepthMm) + "mm vanaf de binnenzijde.");
        }

        private static void CheckPlinthMarkAlignment(List<PortalAssemblyPart> assembly, PlinthClipAdapterTemplate adapter, WorkbenchCabinetAuditResult result)
        {
            var adapterScrewPockets = assembly
                .Where(p => Starts(p.Name, "Plintclip-adapter basis "))
                .SelectMany(p => p.Pockets.Where(x => string.Equals(x.Name, "Schroefgat adapter", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var plinthParts = assembly.Where(p => string.Equals(p.Name, "Losse voorzetplint", StringComparison.OrdinalIgnoreCase) || Starts(p.Name, "Zijplint ")).ToList();
            foreach (var plinth in plinthParts)
            {
                foreach (var hole in plinth.Holes.Where(h => Math.Abs(h.DepthMm - adapter.PlinthCenterMarkDepthMm) < ToleranceMm && Math.Abs(h.DiameterMm - adapter.PlinthCenterMarkDiameterMm) < ToleranceMm))
                {
                    var matches = adapterScrewPockets.Count(p =>
                        string.Equals(p.Plane, hole.Plane, StringComparison.OrdinalIgnoreCase)
                        && Math.Abs(p.Ymm - hole.Ymm) < ToleranceMm
                        && (string.Equals(hole.Plane, "z", StringComparison.OrdinalIgnoreCase)
                            ? Math.Abs(p.Xmm - hole.Xmm) < ToleranceMm
                            : Math.Abs(p.Zmm - hole.Zmm) < ToleranceMm && Math.Sign(p.Xmm) == Math.Sign(hole.Xmm)));
                    if (matches != 1)
                        result.Errors.Add(plinth.Name + " heeft een centreergat zonder exact uitgelijnd adapterschroefgat op (" + F(hole.Xmm) + ", " + F(hole.Ymm) + ", " + F(hole.Zmm) + ").");
                }
            }
        }

        private static void CheckSidePlinth(
            WorkbenchModel model,
            bool requested,
            SheetPart sidePlinth,
            bool isLeft,
            SheetPart frontPlinth,
            AssemblyPlacement frontPlacement,
            double frontFaceZ,
            PortalQuoteRequest request,
            WorkbenchCabinetAuditResult result)
        {
            var label = isLeft ? "linker" : "rechter";
            if (!requested)
            {
                if (sidePlinth != null) result.Errors.Add("Onverwachte " + label + " zijplint aangetroffen.");
                return;
            }
            if (sidePlinth == null)
            {
                result.Errors.Add("Gekozen " + label + " zijplint ontbreekt.");
                return;
            }

            var sidePlacement = FindPlacement(model, sidePlinth.Name);
            var thickness = sidePlinth.Material == null ? 18.0 : sidePlinth.Material.ThicknessMm;
            var sideFrontZ = sidePlacement.Zmm - sidePlinth.LengthMm / 2.0;
            var sideBackZ = sidePlacement.Zmm + sidePlinth.LengthMm / 2.0;
            var sideOuterX = sidePlacement.Xmm + (isLeft ? -thickness / 2.0 : thickness / 2.0);
            var expectedOuterX = (isLeft ? -1.0 : 1.0) * request.WidthMm / 2.0;
            var frontEndX = frontPlacement.Xmm + (isLeft ? -frontPlinth.LengthMm / 2.0 : frontPlinth.LengthMm / 2.0);
            var frontBackZ = frontFaceZ + (frontPlinth.Material == null ? 18.0 : frontPlinth.Material.ThicknessMm);

            if (Math.Abs(sideFrontZ - frontBackZ) > ToleranceMm)
                result.Errors.Add("Voorzijde van de " + label + " zijplint begint niet achter de voorplint.");
            if (Math.Abs(sideBackZ - (request.DepthMm / 2.0 - 3.0)) > ToleranceMm)
                result.Errors.Add("Achterzijde van de " + label + " zijplint heeft niet de bedoelde 3mm randspeling.");
            if (Math.Abs(sideOuterX - expectedOuterX) > ToleranceMm)
                result.Errors.Add("Buitenzijde van de " + label + " zijplint ligt niet vlak met de kastzijde.");
            if (Math.Abs(frontEndX - sideOuterX) > ToleranceMm)
                result.Errors.Add("Voorplint dekt de kopse kant van de " + label + " zijplint niet tot de buitenmaat af.");
        }

        private static void CheckNesting(NestingPlan plan, WorkbenchCabinetAuditResult result)
        {
            if (plan == null) { result.Errors.Add("Nestingplan ontbreekt."); return; }
            foreach (var stock in plan.StockSheets)
            {
                foreach (var placement in stock.Placements)
                {
                    if (placement.Xmm < -ToleranceMm || placement.Ymm < -ToleranceMm || placement.Xmm + placement.LengthMm > stock.StockLengthMm + ToleranceMm || placement.Ymm + placement.WidthMm > stock.StockWidthMm + ToleranceMm)
                        result.Errors.Add("Nestingdeel buiten plaat: " + placement.Part.Name + " op " + stock.Name + ".");
                }
                for (var i = 0; i < stock.Placements.Count; i++)
                for (var j = i + 1; j < stock.Placements.Count; j++)
                {
                    var a = stock.Placements[i];
                    var b = stock.Placements[j];
                    if (a.Xmm < b.Xmm + b.LengthMm - ToleranceMm && a.Xmm + a.LengthMm > b.Xmm + ToleranceMm && a.Ymm < b.Ymm + b.WidthMm - ToleranceMm && a.Ymm + a.WidthMm > b.Ymm + ToleranceMm)
                        result.Errors.Add("Nestingoverlap op " + stock.Name + ": " + a.Part.Name + " en " + b.Part.Name + ".");
                }
            }
            result.Checks.Add(plan.StockSheets.Count.ToString(CultureInfo.InvariantCulture) + " nestplaten op grensoverschrijding en overlap gecontroleerd.");
        }

        private static AssemblyPlacement FindPlacement(WorkbenchModel model, string name)
        {
            return model.AssemblyPlacements.First(p => p.Kind == AssemblyComponentKind.Sheet && string.Equals(p.PartName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string RailCabinetFastenerArticle(RailTemplate rail)
        {
            var diameter = rail.CabinetFastenerDiameterMm.ToString("0.#", CultureInfo.InvariantCulture);
            var length = rail.CabinetFastenerLengthMm.ToString("0.#", CultureInfo.InvariantCulture);
            var headStyle = new string((rail.CabinetFastenerHeadStyle ?? "").Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
            return "RAIL_CABINET_SCREW_" + diameter + "X" + length
                + (headStyle.Length == 0 ? "" : "_" + headStyle);
        }

        private static string RailOpposingFitVerificationSignature(RailTemplate rail, double panelThicknessMm)
        {
            return F(rail.CabinetFastenerDiameterMm) + "x" + F(rail.CabinetFastenerLengthMm)
                + "|" + F(rail.CabinetFastenerPassingStackMm)
                + "|" + (rail.CabinetFastenerHeadStyle ?? "").Trim().ToUpperInvariant()
                + "|" + F(panelThicknessMm);
        }

        private static bool Starts(string value, string prefix) { return value != null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase); }
        private static bool Contains(string value, string fragment) { return value != null && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0; }
        private static int ParseUnitNumber(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var marker = name.IndexOf("unit ", StringComparison.OrdinalIgnoreCase);
            if (marker < 0) return 0;
            marker += 5;
            var end = marker;
            while (end < name.Length && char.IsDigit(name[end])) end++;
            int value;
            return int.TryParse(name.Substring(marker, end - marker), out value) ? value : 0;
        }
        private static string F(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
    }
}
