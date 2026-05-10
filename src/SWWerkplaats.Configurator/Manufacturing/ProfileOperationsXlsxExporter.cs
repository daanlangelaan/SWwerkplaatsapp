using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ProfileOperationsXlsxExporter
    {
        public void Export(string path, IEnumerable<ProfileOperation> operations)
        {
            if (File.Exists(path)) File.Delete(path);

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddText(archive, "[Content_Types].xml", ContentTypes());
                AddText(archive, "_rels/.rels", RootRelationships());
                AddText(archive, "xl/workbook.xml", Workbook());
                AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
                AddText(archive, "xl/styles.xml", Styles());
                AddText(archive, "xl/worksheets/sheet1.xml", Worksheet(operations));
            }
        }

        private static string Worksheet(IEnumerable<ProfileOperation> operations)
        {
            var rows = new StringBuilder();
            var headers = new[]
            {
                "ProfielId", "Onderdeel", "Aantal", "Materiaal", "Profielmaat mm", "Lengte mm", "Volgorde",
                "Bewerking", "Nulpunt", "Zijde", "Positie mm", "Diameter mm", "Doorlopend", "MachineHint", "Opmerking"
            };

            rows.Append(Row(1, headers, 1));

            string lastProfileId = null;
            var rowIndex = 2;
            foreach (var operation in operations)
            {
                rows.Append(Row(rowIndex++, Cells(operation, operation.ProfileId == lastProfileId), 0));
                lastProfileId = operation.ProfileId;
            }

            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
                "<cols>" +
                Col(1, 1, 24) + Col(2, 2, 25) + Col(3, 3, 9) + Col(4, 4, 19) + Col(5, 5, 16) +
                Col(6, 6, 12) + Col(7, 7, 10) + Col(8, 8, 16) + Col(9, 9, 12) + Col(10, 10, 18) +
                Col(11, 11, 18) + Col(12, 12, 14) + Col(13, 13, 12) + Col(14, 14, 16) + Col(15, 15, 58) +
                "</cols>" +
                "<sheetData>" + rows + "</sheetData>" +
                "<autoFilter ref=\"A1:O" + (rowIndex - 1).ToString(CultureInfo.InvariantCulture) + "\"/>" +
                "</worksheet>";
        }

        private static string[] Cells(ProfileOperation operation, bool suppressRepeatedProfileData)
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
                OperationText(operation.Kind),
                operation.WorkOrigin,
                operation.Side,
                PositionText(operation),
                operation.DiameterMm > 0 ? F(operation.DiameterMm) : "",
                operation.DiameterMm > 0 ? (operation.ThroughHole ? "ja" : "nee") : "",
                operation.MachineHint,
                operation.Note
            };
        }

        private static string Row(int rowIndex, string[] values, int style)
        {
            var sb = new StringBuilder();
            sb.Append("<row r=\"").Append(rowIndex.ToString(CultureInfo.InvariantCulture)).Append("\">");
            for (var i = 0; i < values.Length; i++)
            {
                var cellRef = ColumnName(i + 1) + rowIndex.ToString(CultureInfo.InvariantCulture);
                sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"");
                if (style > 0) sb.Append(" s=\"").Append(style.ToString(CultureInfo.InvariantCulture)).Append("\"");
                sb.Append("><is><t");
                if (!string.IsNullOrEmpty(values[i]) && (values[i].StartsWith(" ") || values[i].EndsWith(" ")))
                {
                    sb.Append(" xml:space=\"preserve\"");
                }

                sb.Append(">").Append(Xml(values[i])).Append("</t></is></c>");
            }

            sb.Append("</row>");
            return sb.ToString();
        }

        private static string ColumnName(int number)
        {
            var name = "";
            while (number > 0)
            {
                var modulo = (number - 1) % 26;
                name = (char)('A' + modulo) + name;
                number = (number - modulo) / 26;
            }

            return name;
        }

        private static string Col(int min, int max, double width)
        {
            return "<col min=\"" + min + "\" max=\"" + max + "\" width=\"" + width.ToString("0.##", CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>";
        }

        private static void AddText(ZipArchive archive, string path, string text)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(text);
            }
        }

        private static string ContentTypes()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>";
        }

        private static string RootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private static string Workbook()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Profielbewerkingen\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>";
        }

        private static string WorkbookRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>";
        }

        private static string Styles()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts>" +
                "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs>" +
                "</styleSheet>";
        }

        private static string PositionText(ProfileOperation operation)
        {
            if (operation.Kind == ProfileOperationKind.SawCut)
            {
                return "L=" + F(operation.ProfileLengthMm) + " / hoek " + F(operation.SawAngleDeg) + " graden";
            }

            return F(operation.PositionFromEndAMm) + " vanaf " + operation.WorkOrigin;
        }

        private static string OperationText(ProfileOperationKind kind)
        {
            if (kind == ProfileOperationKind.SawCut) return "Afkorten";
            if (kind == ProfileOperationKind.Drill) return "Boren";
            return "Tappen";
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
