using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class CsvExporter
    {
        public string ExportCutList(IEnumerable<ProfilePart> profiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Materiaal;Onderdeel;Aantal;Lengte_mm;Zaaghoek_graden;Opmerking");
            foreach (var profile in profiles)
            {
                sb.Append(E(profile.Material.Name)).Append(';');
                sb.Append(E(profile.Name)).Append(';');
                sb.Append(profile.Quantity).Append(';');
                sb.Append(F(profile.LengthMm)).Append(';');
                sb.Append("90").Append(';');
                sb.AppendLine(E(profile.OrientationNote));
            }

            return sb.ToString();
        }

        public string ExportDrillList(IEnumerable<ProfilePart> profiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Onderdeel;Aantal;Materiaal;Lengte_mm;Zijde;Positie_vanaf_kop_A_mm;Diameter_mm;Doorlopend;Opmerking");
            foreach (var profile in profiles)
            {
                foreach (var drill in profile.Drills)
                {
                    sb.Append(E(profile.Name)).Append(';');
                    sb.Append(profile.Quantity).Append(';');
                    sb.Append(E(profile.Material.Name)).Append(';');
                    sb.Append(F(profile.LengthMm)).Append(';');
                    sb.Append(E(drill.Side)).Append(';');
                    sb.Append(F(drill.PositionFromEndAMm)).Append(';');
                    sb.Append(F(drill.DiameterMm)).Append(';');
                    sb.Append(drill.ThroughHole ? "ja" : "nee").Append(';');
                    sb.AppendLine(E(drill.Note));
                }
            }

            return sb.ToString();
        }

        public string ExportProfileOperations(IEnumerable<ProfileOperation> operations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ProfielId;Onderdeel;Aantal;Materiaal;Profielmaat_mm;Lengte_mm;Volgorde;Bewerking;Nulpunt;Zijde;Positie_mm;Diameter_mm;Doorlopend;MachineHint;Opmerking");

            string lastProfileId = null;
            foreach (var operation in operations)
            {
                AppendProfileOperation(
                    sb,
                    operation,
                    operation.ProfileId == lastProfileId);
                lastProfileId = operation.ProfileId;
            }

            return sb.ToString();
        }

        public string ExportProfileOperationsExcelXml(IEnumerable<ProfileOperation> operations)
        {
            var rows = new StringBuilder();
            rows.AppendLine(Row(new[]
            {
                "ProfielId", "Onderdeel", "Aantal", "Materiaal", "Profielmaat mm", "Lengte mm", "Volgorde",
                "Bewerking", "Nulpunt", "Zijde", "Positie mm", "Diameter mm", "Doorlopend", "MachineHint", "Opmerking"
            }, "Header"));

            string lastProfileId = null;
            foreach (var operation in operations)
            {
                rows.AppendLine(Row(ProfileOperationCells(operation, operation.ProfileId == lastProfileId), null));
                lastProfileId = operation.ProfileId;
            }

            return "<?xml version=\"1.0\"?>\r\n" +
                "<?mso-application progid=\"Excel.Sheet\"?>\r\n" +
                "<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" " +
                "xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                "xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">\r\n" +
                "<Styles>\r\n" +
                "<Style ss:ID=\"Header\"><Font ss:Bold=\"1\" ss:Color=\"#FFFFFF\"/><Interior ss:Color=\"#1F4E78\" ss:Pattern=\"Solid\"/><Alignment ss:Vertical=\"Center\"/></Style>\r\n" +
                "</Styles>\r\n" +
                "<Worksheet ss:Name=\"Profielbewerkingen\">\r\n" +
                "<Table>\r\n" +
                Columns(new[] { 130, 150, 55, 120, 95, 75, 60, 90, 70, 105, 90, 80, 75, 90, 360 }) +
                rows +
                "</Table>\r\n" +
                "<WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\">\r\n" +
                "<FreezePanes/><FrozenNoSplit/><SplitHorizontal>1</SplitHorizontal><TopRowBottomPane>1</TopRowBottomPane><ActivePane>2</ActivePane>\r\n" +
                "</WorksheetOptions>\r\n" +
                "</Worksheet>\r\n" +
                "</Workbook>\r\n";
        }

        public string ExportSheetHoleList(IEnumerable<SheetPart> sheets)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Plaat;Aantal;Materiaal;Lengte_mm;Breedte_mm;Gatnaam;X_vanaf_links_mm;Y_vanaf_onder_mm;Diameter_mm;Gatdiepte_mm;Doorlopend;Bewerkingsvlak;Dieptemodus;Kopkamer;Kopkamerdiameter_mm;Kopkamerdiepte_mm;Bevestiging;Opmerking");
            foreach (var sheet in sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    sb.Append(E(sheet.Name)).Append(';');
                    sb.Append(sheet.Quantity).Append(';');
                    sb.Append(E(sheet.Material.Name)).Append(';');
                    sb.Append(F(sheet.LengthMm)).Append(';');
                    sb.Append(F(sheet.WidthMm)).Append(';');
                    sb.Append(E(hole.Name)).Append(';');
                    sb.Append(F(hole.Xmm)).Append(';');
                    sb.Append(F(hole.Ymm)).Append(';');
                    sb.Append(F(hole.DiameterMm)).Append(';');
                    sb.Append(hole.DepthMm > 0 ? F(hole.DepthMm) : "").Append(';');
                    sb.Append(hole.DepthMm > 0 ? "nee" : "ja").Append(';');
                    sb.Append(E(hole.Face.ToString())).Append(';');
                    sb.Append(E(hole.DepthMode.ToString())).Append(';');
                    sb.Append(hole.Countersunk ? "ja" : "nee").Append(';');
                    sb.Append(hole.Countersunk ? F(hole.CountersinkDiameterMm) : "").Append(';');
                    sb.Append(hole.Countersunk ? F(hole.CountersinkDepthMm) : "").Append(';');
                    sb.Append(E(HoleSupportText(hole))).Append(';');
                    sb.AppendLine(E("Montagegat voor bladbevestiging"));
                }
            }

            return sb.ToString();
        }

        public string ExportCamOperations(IEnumerable<SheetPart> sheets, ToolDefinition tool)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Plaat;Volgorde;Bewerking;Tool;X_mm;Y_mm;Diameter_mm;Lengte_mm;Breedte_mm;Diepte_mm;Opmerking");

            foreach (var sheet in sheets)
            {
                var order = 1;
                foreach (var pocket in sheet.Pockets)
                {
                    AppendCamOperation(
                        sb,
                        sheet,
                        order++,
                        "Rechthoekige pocket/groef",
                        tool,
                        pocket.Xmm,
                        pocket.Ymm,
                        0,
                        pocket.LengthMm,
                        pocket.WidthMm,
                        pocket.DepthMm,
                        pocket.Name + " - " + pocket.Note);
                }

                foreach (var hole in sheet.Holes)
                {
                    if (!hole.Countersunk || hole.CountersinkDiameterMm <= hole.DiameterMm || hole.CountersinkDepthMm <= 0)
                    {
                        continue;
                    }

                    AppendCamOperation(
                        sb,
                        sheet,
                        order++,
                        "Kopkamer helix-frezen",
                        tool,
                        hole.Xmm,
                        hole.Ymm,
                        hole.CountersinkDiameterMm,
                        0,
                        0,
                        hole.CountersinkDepthMm,
                        hole.Name);
                }

                foreach (var hole in sheet.Holes)
                {
                    var holeDepth = HoleDepth(hole, sheet.Material.ThicknessMm);
                    AppendCamOperation(
                        sb,
                        sheet,
                        order++,
                        hole.DepthMm > 0 ? "Blindgat/circulair frezen" : "Doorboren/circulair frezen",
                        tool,
                        hole.Xmm,
                        hole.Ymm,
                        hole.DiameterMm,
                        0,
                        0,
                        holeDepth,
                        hole.Name);
                }

                AppendCamOperation(
                    sb,
                    sheet,
                    order,
                    "Buitencontour",
                    tool,
                    0,
                    0,
                    tool.DiameterMm,
                    0,
                    0,
                    sheet.Material.ThicknessMm,
                    ContourNote(sheet));
            }

            return sb.ToString();
        }

        private static double HoleDepth(SheetHole hole, double materialThicknessMm)
        {
            if (hole != null && hole.DepthMm > 0)
            {
                return System.Math.Min(hole.DepthMm, System.Math.Max(0.1, materialThicknessMm - 0.1));
            }

            return materialThicknessMm;
        }

        public string ExportToolLibrary(ToolDefinition tool)
        {
            return ExportToolLibrary(new[] { tool });
        }

        public string ExportToolLibrary(IEnumerable<ToolDefinition> tools)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Toolnummer;ToolId;Naam;Type;Diameter_mm;Radius_mm;Feed_mm_min;Plunge_mm_min;Spindle_rpm;Passdiepte_mm;Opmerking");
            var toolNumber = 1;
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    AppendToolRow(sb, toolNumber, tool, toolNumber == 1 ? "Primaire frees voor huidige G-code" : "Geselecteerd voor deze CAM-job");
                    toolNumber++;
                }
            }

            return sb.ToString();
        }

        public string ExportToolLibrary(CamJobOptions jobOptions)
        {
            if (jobOptions == null)
            {
                return ExportToolLibrary((IEnumerable<ToolDefinition>)null);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Toolnummer;ToolId;Naam;Type;Diameter_mm;Radius_mm;Feed_mm_min;Plunge_mm_min;Spindle_rpm;Passdiepte_mm;Opmerking");
            var toolNumber = 1;
            foreach (var tool in jobOptions.Tools)
            {
                AppendToolRow(sb, toolNumber, tool, toolNumber == 1 ? "Primaire frees voor huidige G-code" : "Geselecteerd voor deze CAM-job");
                toolNumber++;
            }

            if (jobOptions.EnablePencilMarking)
            {
                var pencil = jobOptions.BuildPencilMarkingOptions();
                sb.Append(jobOptions.PencilToolNumber).Append(';');
                sb.Append("spring_pencil").Append(';');
                sb.Append(E(pencil.ToolName)).Append(';');
                sb.Append("Marker").Append(';');
                sb.Append(';');
                sb.Append(';');
                sb.Append(F(pencil.FeedRateMmMin)).Append(';');
                sb.Append(F(pencil.PlungeRateMmMin)).Append(';');
                sb.Append(';');
                sb.Append(F(Math.Abs(pencil.WriteDepthMm))).Append(';');
                sb.AppendLine(E("Geveerd potlood in spindelhouder; Z0 zetten op potloodpunt bij toolchange"));
            }

            return sb.ToString();
        }

        private static void AppendToolRow(StringBuilder sb, int toolNumber, ToolDefinition tool, string note)
        {
            if (tool == null)
            {
                return;
            }

            sb.Append(toolNumber).Append(';');
            sb.Append(E(tool.Id)).Append(';');
            sb.Append(E(tool.Name)).Append(';');
            sb.Append(E(tool.Kind.ToString())).Append(';');
            sb.Append(F(tool.DiameterMm)).Append(';');
            sb.Append(F(tool.RadiusMm)).Append(';');
            sb.Append(F(tool.FeedRateMmMin)).Append(';');
            sb.Append(F(tool.PlungeRateMmMin)).Append(';');
            sb.Append(F(tool.SpindleRpm)).Append(';');
            sb.Append(F(tool.PassDepthMm)).Append(';');
            sb.AppendLine(E(note));
        }

        public string ExportProfileStationPlan(WorkbenchModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Profiel-bewerkingsstation plan");
            sb.AppendLine();
            sb.AppendLine("Status: lijst-output gereed, G-code generator volgt later.");
            sb.AppendLine("Nulpunt: Kop A van profiel, lengte-as positief richting Kop B.");
            sb.AppendLine("Machine-as voorstel:");
            sb.AppendLine("- X = lengtepositie vanaf Kop A");
            sb.AppendLine("- Y/Z = afhankelijk van gekozen profielzijde en boorunit");
            sb.AppendLine("- Rotatie/indexering = zijde uit Profielbewerkingen.csv");
            sb.AppendLine();
            sb.AppendLine("Te gebruiken bronbestand:");
            sb.AppendLine("- Profielbewerkingen.csv");
            sb.AppendLine();
            sb.AppendLine("MachineHints:");
            sb.AppendLine("- SAW_CUT = afkorten op lengte");
            sb.AppendLine("- DRILL = boren op positie");
            sb.AppendLine("- TAP = tappen op positie");
            sb.AppendLine();
            sb.AppendLine("Aantal profielbewerkingsregels: " + model.ProfileOperations.Count);
            return sb.ToString();
        }

        public string ExportBom(WorkbenchModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Type;Naam;Artikelnummer;Aantal;Eenheid;Materiaal;Maat;Opmerking");

            foreach (var profile in model.Profiles)
            {
                sb.Append("Profiel;");
                sb.Append(E(profile.Name)).Append(';');
                sb.Append(';');
                sb.Append(profile.Quantity).Append(';');
                sb.Append("st;");
                sb.Append(E(profile.Material.Name)).Append(';');
                sb.Append(E(F(profile.LengthMm) + " mm")).Append(';');
                sb.AppendLine(E(profile.OrientationNote));
            }

            foreach (var sheet in model.Sheets)
            {
                sb.Append("Plaat;");
                sb.Append(E(sheet.Name)).Append(';');
                sb.Append(';');
                sb.Append(sheet.Quantity).Append(';');
                sb.Append("st;");
                sb.Append(E(sheet.Material.Name)).Append(';');
                sb.Append(E(F(sheet.LengthMm) + " x " + F(sheet.WidthMm) + " x " + F(sheet.Material.ThicknessMm) + " mm")).Append(';');
                sb.AppendLine(E(ContourNote(sheet)));
            }

            foreach (var item in model.Hardware)
            {
                sb.Append("Bevestiging;");
                sb.Append(E(item.Name)).Append(';');
                sb.Append(E(item.ArticleNumber)).Append(';');
                sb.Append(item.Quantity).Append(';');
                sb.Append(E(item.Unit)).Append(';');
                sb.Append(';');
                sb.Append(';');
                sb.AppendLine(E(item.Note));
            }

            return sb.ToString();
        }

        private static string ContourNote(SheetPart sheet)
        {
            if (sheet.Pockets.Count > 0)
            {
                var suffix = sheet.Pockets.Count == 1 ? "1 groef/pocket" : sheet.Pockets.Count.ToString(CultureInfo.InvariantCulture) + " groeven/pockets";
                if (sheet.HasCornerNotches) return "Hoekuitsparingen; " + suffix;
                if (sheet.HasToeKickNotch) return "Plintuitsparing; " + suffix;
                return suffix;
            }
            if (sheet.HasCornerNotches) return "Hoekuitsparingen";
            if (sheet.HasToeKickNotch) return "Plintuitsparing";
            return "";
        }

        private static string E(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void AppendCamOperation(
            StringBuilder sb,
            SheetPart sheet,
            int order,
            string operation,
            ToolDefinition tool,
            double x,
            double y,
            double diameter,
            double length,
            double width,
            double depth,
            string note)
        {
            sb.Append(E(sheet.Name)).Append(';');
            sb.Append(order).Append(';');
            sb.Append(E(operation)).Append(';');
            sb.Append(E(tool.Name)).Append(';');
            sb.Append(F(x)).Append(';');
            sb.Append(F(y)).Append(';');
            sb.Append(F(diameter)).Append(';');
            sb.Append(F(length)).Append(';');
            sb.Append(F(width)).Append(';');
            sb.Append(F(depth)).Append(';');
            sb.AppendLine(E(note));
        }

        private static void AppendProfileOperation(StringBuilder sb, ProfileOperation operation, bool suppressRepeatedProfileData)
        {
            var cells = ProfileOperationCells(operation, suppressRepeatedProfileData);
            for (var i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(E(cells[i]));
            }
            sb.AppendLine();
        }

        private static string[] ProfileOperationCells(ProfileOperation operation, bool suppressRepeatedProfileData)
        {
            return new[]
            {
                suppressRepeatedProfileData ? "" : operation.ProfileId,
                suppressRepeatedProfileData ? "" : operation.PartName,
                suppressRepeatedProfileData ? "" : operation.Quantity.ToString(CultureInfo.InvariantCulture),
                suppressRepeatedProfileData ? "" : operation.Material.Name,
                suppressRepeatedProfileData ? "" : F(operation.Material.WidthMm) + " x " + F(operation.Material.HeightMm),
                suppressRepeatedProfileData ? "" : F(operation.ProfileLengthMm),
                operation.Sequence.ToString(CultureInfo.InvariantCulture),
                ProfileOperationText(operation.Kind),
                operation.WorkOrigin,
                operation.Side,
                PositionText(operation),
                operation.DiameterMm > 0 ? F(operation.DiameterMm) : "",
                operation.DiameterMm > 0 ? (operation.ThroughHole ? "ja" : "nee") : "",
                operation.MachineHint,
                operation.Note
            };
        }

        private static string PositionText(ProfileOperation operation)
        {
            if (operation.Kind == ProfileOperationKind.SawCut)
            {
                return "L=" + F(operation.ProfileLengthMm) + " / hoek " + F(operation.SawAngleDeg) + " graden";
            }

            return F(operation.PositionFromEndAMm) + " vanaf " + operation.WorkOrigin;
        }

        private static string Columns(int[] widths)
        {
            var sb = new StringBuilder();
            foreach (var width in widths)
            {
                sb.Append("<Column ss:AutoFitWidth=\"0\" ss:Width=\"").Append(width).AppendLine("\"/>");
            }

            return sb.ToString();
        }

        private static string Row(string[] cells, string styleId)
        {
            var sb = new StringBuilder();
            sb.Append("<Row>");
            foreach (var cell in cells)
            {
                sb.Append("<Cell");
                if (!string.IsNullOrEmpty(styleId))
                {
                    sb.Append(" ss:StyleID=\"").Append(styleId).Append("\"");
                }

                sb.Append("><Data ss:Type=\"String\">").Append(XmlEscape(cell)).Append("</Data></Cell>");
            }

            sb.Append("</Row>");
            return sb.ToString();
        }

        private static string XmlEscape(string value)
        {
            if (value == null) return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string HoleSupportText(SheetHole hole)
        {
            if (hole.SupportKind == SheetHoleSupportKind.TappedProfileEnd) return "M8 draad in kopse staander";
            if (hole.SupportKind == SheetHoleSupportKind.PanelScrew) return "Plaat-op-plaat schroef 4x45, boorgat 4,5mm";
            if (hole.SupportKind == SheetHoleSupportKind.HingeScrew) return "Scharnier-op-hout schroef 4x12, boorgat 4,5mm";
            return "M8 T-moer / profielmoer";
        }

        private static string ProfileOperationText(ProfileOperationKind kind)
        {
            if (kind == ProfileOperationKind.SawCut) return "Afkorten";
            if (kind == ProfileOperationKind.Drill) return "Boren";
            return "Tappen";
        }
    }
}
