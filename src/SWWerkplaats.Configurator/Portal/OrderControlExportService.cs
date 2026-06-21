using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class OrderControlExportService
    {
        public bool HasRailData(WorkbenchModel model)
        {
            if (model == null) return false;
            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    if (IsRailHole(hole)) return true;
                }
            }

            return false;
        }

        public string ExportRailHoleControl(WorkbenchModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Plaat;Materiaal;Lengte_mm;Breedte_mm;Gatnaam;X_mm;Y_mm;Diameter_mm;Diepte_mm;Doorlopend;Controle");
            if (model == null) return sb.ToString();

            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    if (!IsRailHole(hole)) continue;
                    sb.Append(E(sheet.Name)).Append(';');
                    sb.Append(E(sheet.Material == null ? "" : sheet.Material.Name)).Append(';');
                    sb.Append(F(sheet.LengthMm)).Append(';');
                    sb.Append(F(sheet.WidthMm)).Append(';');
                    sb.Append(E(hole.Name)).Append(';');
                    sb.Append(F(hole.Xmm)).Append(';');
                    sb.Append(F(hole.Ymm)).Append(';');
                    sb.Append(F(hole.DiameterMm)).Append(';');
                    sb.Append(hole.DepthMm > 0 ? F(hole.DepthMm) : "").Append(';');
                    sb.Append(hole.DepthMm > 0 ? "nee" : "ja").Append(';');
                    sb.AppendLine(E(RailHoleCheck(sheet, hole)));
                }
            }

            return sb.ToString();
        }

        public string ExportUsedRailTemplates(WorkbenchModel model)
        {
            var usedIds = UsedRailTemplateIds(model);
            var sb = new StringBuilder();
            sb.AppendLine("TemplateId;Naam;Lengte_mm;Dikte_mm;Kast_gaten;Kast_Xposities_mm;Kast_Yoffset_mm;Kast_gatdiameter_mm;Lade_gaten;Lade_Xposities_mm;Lade_Yoffset_mm;Lade_gatdiameter_mm;Bevestiging");
            foreach (var rail in HardwareLibraryRepository.DrawerRails())
            {
                if (!usedIds.Contains(rail.Id)) continue;
                sb.Append(E(rail.Id)).Append(';');
                sb.Append(E(rail.Name)).Append(';');
                sb.Append(F(rail.LengthMm)).Append(';');
                sb.Append(F(rail.ThicknessMm)).Append(';');
                sb.Append(rail.CabinetHoleCount).Append(';');
                sb.Append(E(JoinPositions(RailPositions(rail.CabinetHolePositionsMm, rail.CabinetHoleCount, rail.CabinetFirstHoleOffsetMm, rail.CabinetHoleSpacingMm)))).Append(';');
                sb.Append(F(rail.CabinetVerticalOffsetMm)).Append(';');
                sb.Append(F(rail.CabinetHoleDiameterMm)).Append(';');
                sb.Append(rail.DrawerHoleCount).Append(';');
                sb.Append(E(JoinPositions(RailPositions(rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm)))).Append(';');
                sb.Append(F(rail.DrawerVerticalOffsetMm)).Append(';');
                sb.Append(F(rail.DrawerHoleDiameterMm)).Append(';');
                sb.AppendLine(E(rail.FastenerName));
            }

            return sb.ToString();
        }

        public string ExportUsedRailTemplatesSvg(WorkbenchModel model)
        {
            var usedIds = UsedRailTemplateIds(model);
            var rails = new List<RailTemplate>();
            foreach (var rail in HardwareLibraryRepository.DrawerRails())
            {
                if (usedIds.Contains(rail.Id)) rails.Add(rail);
            }

            var width = 980.0;
            var rowHeight = 150.0;
            var height = Math.Max(220.0, 70.0 + rails.Count * rowHeight);
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" viewBox=\"0 0 " + F(width) + " " + F(height) + "\">");
            sb.AppendLine("<style>text{font-family:Arial,sans-serif;fill:#111827}.title{font-size:24px;font-weight:700}.sub{font-size:13px;fill:#667085}.rail{fill:#eef2f7;stroke:#344054;stroke-width:1.4}.cab{fill:#dbeafe;stroke:#1d4ed8;stroke-width:1.2}.drawer{fill:#fee2e2;stroke:#b42318;stroke-width:1.2}.dim{font-size:12px;fill:#344054}.label{font-size:14px;font-weight:700}</style>");
            sb.AppendLine("<rect x=\"0\" y=\"0\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" fill=\"#fff\"/>");
            sb.AppendLine("<text class=\"title\" x=\"32\" y=\"38\">Railtemplate controle</text>");
            sb.AppendLine("<text class=\"sub\" x=\"32\" y=\"58\">Blauw = kastzijde gaten, rood = ladezijde gaten. X-posities komen uit de rail-library.</text>");
            var y = 92.0;
            foreach (var rail in rails)
            {
                DrawRailTemplate(sb, rail, 56, y, 850, 76);
                y += rowHeight;
            }

            if (rails.Count == 0)
            {
                sb.AppendLine("<text class=\"sub\" x=\"32\" y=\"100\">Geen gebruikte railtemplates gevonden.</text>");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public string ExportAssemblyControl(WorkbenchModel model, PortalQuoteRequest request)
        {
            var findings = Validate(model, request);
            var sb = new StringBuilder();
            sb.AppendLine("Assemblagecontrole");
            sb.AppendLine("=================");
            sb.AppendLine("Project: " + (model == null ? "" : model.ProjectName));
            sb.AppendLine("Datum: " + DateTime.Now.ToString("s"));
            sb.AppendLine();

            if (findings.Count == 0)
            {
                sb.AppendLine("OK - geen directe assemblagewaarschuwingen gevonden.");
                return sb.ToString();
            }

            foreach (var finding in findings)
            {
                sb.AppendLine("- [" + finding.Severity + "] " + finding.Code + ": " + finding.Message);
            }

            return sb.ToString();
        }

        public string ExportAssemblyControlCsv(WorkbenchModel model, PortalQuoteRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Severity;Code;Message");
            foreach (var finding in Validate(model, request))
            {
                sb.Append(E(finding.Severity)).Append(';');
                sb.Append(E(finding.Code)).Append(';');
                sb.AppendLine(E(finding.Message));
            }

            return sb.ToString();
        }

        public string ExportDrawingContractControl(WorkbenchModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Onderdeel;Orientatie;Basisvlak;Lengte_as;Breedte_as;Dikte_as;SheetX_sign;SheetY_sign;Default_bewerkingsvlak;Notitie");
            if (model == null) return sb.ToString();

            foreach (var placement in model.AssemblyPlacements)
            {
                if (placement.Kind != AssemblyComponentKind.Sheet) continue;
                var contract = DrawingContracts.ForOrientation(placement.Orientation);
                sb.Append(E(placement.PartName)).Append(';');
                sb.Append(E(placement.Orientation.ToString())).Append(';');
                sb.Append(E(contract.BasePlane.ToString())).Append(';');
                sb.Append(E(contract.LengthAxis.ToString())).Append(';');
                sb.Append(E(contract.WidthAxis.ToString())).Append(';');
                sb.Append(E(contract.ThicknessAxis.ToString())).Append(';');
                sb.Append(contract.SheetXSign).Append(';');
                sb.Append(contract.SheetYSign).Append(';');
                sb.Append(E(contract.DefaultOperationFace.ToString())).Append(';');
                sb.AppendLine(E(contract.Notes));
            }

            return sb.ToString();
        }

        public string ExportDrawingContractValidation(WorkbenchModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Severity;Code;Onderdeel;Message");
            foreach (var finding in new DrawingContractValidationService().Validate(model))
            {
                sb.Append(E(finding.Severity)).Append(';');
                sb.Append(E(finding.Code)).Append(';');
                sb.Append(E(finding.PartName)).Append(';');
                sb.AppendLine(E(finding.Message));
            }

            return sb.ToString();
        }

        private static List<AssemblyFinding> Validate(WorkbenchModel model, PortalQuoteRequest request)
        {
            var findings = new List<AssemblyFinding>();
            if (model == null) return findings;

            ValidateSheetOperations(model, findings);
            ValidateRailHoles(model, findings);
            ValidateDrawerShelfOverlap(model, findings);
            ValidateBackPanelWorktop(model, request, findings);
            return findings;
        }

        private static void ValidateSheetOperations(WorkbenchModel model, List<AssemblyFinding> findings)
        {
            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    if (hole.Xmm < 0 || hole.Xmm > sheet.LengthMm || hole.Ymm < 0 || hole.Ymm > sheet.WidthMm)
                    {
                        findings.Add(new AssemblyFinding("Fout", "GAT_BUITEN_PLAAT", sheet.Name + " - " + hole.Name + " valt buiten de plaatmaat."));
                    }

                    if (hole.DepthMm > 0 && sheet.Material != null && hole.DepthMm > sheet.Material.ThicknessMm)
                    {
                        findings.Add(new AssemblyFinding("Fout", "GAT_TE_DIEP", sheet.Name + " - " + hole.Name + " diepte " + F(hole.DepthMm) + " mm is dieper dan plaat " + F(sheet.Material.ThicknessMm) + " mm."));
                    }
                }

                foreach (var pocket in sheet.Pockets)
                {
                    if (pocket.Xmm < 0 || pocket.Ymm < 0 || pocket.Xmm + pocket.LengthMm > sheet.LengthMm + 0.001 || pocket.Ymm + pocket.WidthMm > sheet.WidthMm + 0.001)
                    {
                        findings.Add(new AssemblyFinding("Fout", "GROEF_BUITEN_PLAAT", sheet.Name + " - " + pocket.Name + " valt buiten de plaatmaat."));
                    }

                    if (sheet.Material != null && pocket.DepthMm > sheet.Material.ThicknessMm)
                    {
                        findings.Add(new AssemblyFinding("Fout", "GROEF_TE_DIEP", sheet.Name + " - " + pocket.Name + " is dieper dan de plaat."));
                    }
                }
            }
        }

        private static void ValidateRailHoles(WorkbenchModel model, List<AssemblyFinding> findings)
        {
            foreach (var sheet in model.Sheets)
            {
                var railCount = 0;
                var drawerRailCount = 0;
                foreach (var hole in sheet.Holes)
                {
                    if (StartsWith(hole.Name, "Railgat U")) railCount++;
                    if (StartsWith(hole.Name, "Laderailgat")) drawerRailCount++;
                }

                if ((StartsWith(sheet.Name, "Zijwand") || StartsWith(sheet.Name, "Tussenschot")) && railCount > 0 && railCount < 2)
                {
                    findings.Add(new AssemblyFinding("Waarschuwing", "RAIL_WEINIG_KASTGATEN", sheet.Name + " heeft maar " + railCount + " kast-railgat(en). Controleer railtemplate en kastdiepte."));
                }

                if (StartsWith(sheet.Name, "Ladezijde") && drawerRailCount > 0 && drawerRailCount < 2)
                {
                    findings.Add(new AssemblyFinding("Waarschuwing", "RAIL_WEINIG_LADEGATEN", sheet.Name + " heeft maar " + drawerRailCount + " lade-railgat(en). Controleer railtemplate en ladediepte."));
                }
            }
        }

        private static void ValidateDrawerShelfOverlap(WorkbenchModel model, List<AssemblyFinding> findings)
        {
            var drawerTops = new List<double>();
            foreach (var placement in model.AssemblyPlacements)
            {
                if (StartsWith(placement.PartName, "Ladefront") || StartsWith(placement.PartName, "Bovenlade front"))
                {
                    drawerTops.Add(placement.Ymm + placement.WidthMm / 2.0);
                }
            }

            if (drawerTops.Count == 0) return;

            foreach (var placement in model.AssemblyPlacements)
            {
                if (!StartsWith(placement.PartName, "Legplank")) continue;
                foreach (var drawerTop in drawerTops)
                {
                    if (placement.Ymm < drawerTop + 25.0)
                    {
                        findings.Add(new AssemblyFinding("Waarschuwing", "LEGPLANK_DICHT_BIJ_LADE", placement.PartName + " zit dicht boven/in het ladegebied. Controleer of de lade vrij loopt."));
                        break;
                    }
                }
            }
        }

        private static void ValidateBackPanelWorktop(WorkbenchModel model, PortalQuoteRequest request, List<AssemblyFinding> findings)
        {
            if (request == null || !request.IncludeBackPanel) return;
            var back = FindSheet(model, "Achterwand");
            var top = FindSheet(model, "Werkblad");
            if (back == null || top == null) return;
            var expected = request.DepthMm + (back.Material == null ? 0 : back.Material.ThicknessMm);
            if (Math.Abs(top.WidthMm - expected) > 0.5)
            {
                findings.Add(new AssemblyFinding("Waarschuwing", "ACHTERWAND_BLAD_DIEPTE", "Werkblad diepte " + F(top.WidthMm) + " mm wijkt af van kastdiepte + achterwand " + F(expected) + " mm."));
            }
        }

        private static SheetPart FindSheet(WorkbenchModel model, string name)
        {
            foreach (var sheet in model.Sheets)
            {
                if (string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase)) return sheet;
            }

            return null;
        }

        private static HashSet<string> UsedRailTemplateIds(WorkbenchModel model)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (model == null) return ids;
            foreach (var hardware in model.Hardware)
            {
                foreach (var rail in HardwareLibraryRepository.DrawerRails())
                {
                    if (string.Equals(hardware.ArticleNumber, rail.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(rail.Id);
                    }
                }
            }

            return ids;
        }

        private static bool IsRailHole(SheetHole hole)
        {
            if (hole == null || hole.Name == null) return false;
            return hole.Name.IndexOf("Railgat", StringComparison.OrdinalIgnoreCase) >= 0
                || hole.Name.IndexOf("Laderailgat", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string RailHoleCheck(SheetPart sheet, SheetHole hole)
        {
            if (hole.Xmm <= 5 || hole.Xmm >= sheet.LengthMm - 5) return "Dicht bij plaatrand controleren";
            if (hole.Ymm <= 5 || hole.Ymm >= sheet.WidthMm - 5) return "Dicht bij plaatrand controleren";
            return "OK";
        }

        private static List<double> RailPositions(string explicitPositions, int count, double firstOffset, double spacing)
        {
            var positions = new List<double>();
            if (!string.IsNullOrWhiteSpace(explicitPositions))
            {
                var parts = explicitPositions.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    double value;
                    if (double.TryParse(part.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        positions.Add(Math.Round(value, 3));
                    }
                }

                return positions;
            }

            for (var i = 0; i < count; i++)
            {
                positions.Add(Math.Round(firstOffset + i * spacing, 3));
            }

            return positions;
        }

        private static string JoinPositions(List<double> positions)
        {
            var values = new List<string>();
            foreach (var position in positions)
            {
                values.Add(F(position));
            }

            return string.Join(";", values.ToArray());
        }

        private static void DrawRailTemplate(StringBuilder sb, RailTemplate rail, double x, double y, double maxWidth, double railHeight)
        {
            var scale = maxWidth / Math.Max(1.0, rail.LengthMm);
            var w = rail.LengthMm * scale;
            sb.AppendLine("<text class=\"label\" x=\"" + F(x) + "\" y=\"" + F(y - 14) + "\">" + Xml(rail.Name + " (" + F(rail.LengthMm) + " mm)") + "</text>");
            sb.AppendLine("<rect class=\"rail\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(railHeight) + "\" rx=\"3\"/>");
            DrawRailHoleRow(sb, RailPositions(rail.CabinetHolePositionsMm, rail.CabinetHoleCount, rail.CabinetFirstHoleOffsetMm, rail.CabinetHoleSpacingMm), rail.CabinetHoleDiameterMm, rail.CabinetVerticalOffsetMm, x, y, scale, railHeight, "cab", "Kastzijde");
            DrawRailHoleRow(sb, RailPositions(rail.DrawerHolePositionsMm, rail.DrawerHoleCount, rail.DrawerFirstHoleOffsetMm, rail.DrawerHoleSpacingMm), rail.DrawerHoleDiameterMm, rail.DrawerVerticalOffsetMm, x, y + railHeight + 18, scale, railHeight * 0.35, "drawer", "Ladezijde");
            sb.AppendLine("<text class=\"dim\" x=\"" + F(x) + "\" y=\"" + F(y + railHeight + 104) + "\">Bevestiging: " + Xml(rail.FastenerName) + "</text>");
        }

        private static void DrawRailHoleRow(StringBuilder sb, List<double> positions, double diameter, double verticalOffset, double x, double y, double scale, double rowHeight, string cssClass, string label)
        {
            sb.AppendLine("<text class=\"dim\" x=\"" + F(x) + "\" y=\"" + F(y + rowHeight + 16) + "\">" + Xml(label + ": diameter " + F(diameter) + " mm, Y-offset " + F(verticalOffset) + " mm, X " + JoinPositions(positions)) + "</text>");
            foreach (var position in positions)
            {
                var cx = x + position * scale;
                var cy = y + Math.Max(8, Math.Min(rowHeight - 8, verticalOffset));
                sb.AppendLine("<circle class=\"" + cssClass + "\" cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"5\"><title>" + Xml(label + " X" + F(position) + " diameter " + F(diameter)) + "</title></circle>");
            }
        }

        private static bool StartsWith(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string E(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private sealed class AssemblyFinding
        {
            public string Severity { get; private set; }
            public string Code { get; private set; }
            public string Message { get; private set; }

            public AssemblyFinding(string severity, string code, string message)
            {
                Severity = severity;
                Code = code;
                Message = message;
            }
        }
    }
}
