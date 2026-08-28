using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ProfileStickerXlsxExporter
    {
        public void Export(string path, IEnumerable<ProfileProductionSequenceItem> sequence)
        {
            if (File.Exists(path)) File.Delete(path);
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddText(archive, "[Content_Types].xml", ContentTypes());
                AddText(archive, "_rels/.rels", RootRelationships());
                AddText(archive, "xl/workbook.xml", Workbook());
                AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
                AddText(archive, "xl/styles.xml", Styles());
                AddText(archive, "xl/worksheets/sheet1.xml", Worksheet(sequence));
            }
        }

        private static string Worksheet(IEnumerable<ProfileProductionSequenceItem> sequence)
        {
            var items = (sequence ?? Enumerable.Empty<ProfileProductionSequenceItem>()).ToArray();
            var rows = new StringBuilder();
            var headers = new[]
            {
                "Freesvolgorde", "Stickertekst", "Profielstuk-ID", "Onderdeel", "Profiel-ID", "Profieltype",
                "Breedte mm", "Hoogte mm", "Lengte mm", "Kleminstructie", "Stickervlak", "Assemblagezijde",
                "Doorsnedevlak", "Vlakbreedte mm", "Ankerkop", "Afstand vanaf ankerkop mm", "Plaatsingsinstructie",
                "Obstructievrij", "Bewerkingen", "Printerstatus"
            };
            rows.Append(Row(1, headers, 1));
            var rowIndex = 2;
            foreach (var item in items) rows.Append(Row(rowIndex++, Cells(item), 0));
            var lastRow = System.Math.Max(1, rowIndex - 1);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
                "<cols>" + Col(1, 1, 13) + Col(2, 2, 20) + Col(3, 3, 18) + Col(4, 5, 27) + Col(6, 6, 14)
                + Col(7, 9, 12) + Col(10, 10, 68) + Col(11, 16, 18) + Col(17, 17, 82) + Col(18, 18, 15)
                + Col(19, 19, 48) + Col(20, 20, 20) + "</cols>" +
                "<sheetData>" + rows + "</sheetData><autoFilter ref=\"A1:T" + lastRow.ToString(CultureInfo.InvariantCulture) + "\"/>" +
                "</worksheet>";
        }

        private static string[] Cells(ProfileProductionSequenceItem item)
        {
            var material = item.Material;
            var sticker = item.Sticker;
            var stickerFace = item.MachiningFrame == null ? null : item.MachiningFrame.Face("D0");
            return new[]
            {
                item.ProductionOrder.ToString(CultureInfo.InvariantCulture), item.TraceId, item.TraceId, item.PartName, item.ProfileId,
                material == null ? "" : F(material.WidthMm) + " x " + F(material.HeightMm),
                material == null ? "" : F(material.WidthMm), material == null ? "" : F(material.HeightMm), F(item.ProfileLengthMm),
                item.ClampInstruction, sticker == null ? "ONTBREEKT" : sticker.FaceId, sticker == null ? "" : sticker.LocalFace,
                stickerFace == null ? "" : stickerFace.CrossSectionFace, stickerFace == null ? "" : F(stickerFace.FaceSpanMm),
                sticker == null ? "" : (sticker.AnchorEnd == ProfileEnd.A ? "Kop A" : "Kop B"),
                sticker == null ? "" : F(sticker.OffsetFromAnchorEndMm), item.StickerInstruction,
                sticker == null ? "nee" : (sticker.ObstructionFree ? "ja" : "nee - controleren"), OperationSummary(item.Operations),
                "HANDMATIG / PRINTER NOG TE KIEZEN"
            };
        }

        private static string OperationSummary(IEnumerable<ProfileOperation> operations)
        {
            return string.Join(" | ", operations.Select(operation => operation.Kind + " " + (operation.Side ?? "")
                + (operation.Kind == ProfileOperationKind.SawCut ? " L=" + F(operation.ProfileLengthMm) : " @" + F(operation.PositionFromEndAMm) + " mm")));
        }

        private static string Row(int rowIndex, string[] values, int style)
        {
            var sb = new StringBuilder("<row r=\"").Append(rowIndex.ToString(CultureInfo.InvariantCulture)).Append("\">");
            for (var i = 0; i < values.Length; i++)
            {
                sb.Append("<c r=\"").Append(ColumnName(i + 1)).Append(rowIndex.ToString(CultureInfo.InvariantCulture)).Append("\" t=\"inlineStr\"");
                if (style > 0) sb.Append(" s=\"").Append(style.ToString(CultureInfo.InvariantCulture)).Append("\"");
                sb.Append("><is><t>").Append(Xml(values[i])).Append("</t></is></c>");
            }
            return sb.Append("</row>").ToString();
        }

        private static string ColumnName(int number)
        {
            var name = "";
            while (number > 0) { var modulo = (number - 1) % 26; name = (char)('A' + modulo) + name; number = (number - modulo) / 26; }
            return name;
        }

        private static string Col(int min, int max, double width) { return "<col min=\"" + min + "\" max=\"" + max + "\" width=\"" + width.ToString("0.##", CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>"; }
        private static string F(double value) { return value.ToString("0.##", CultureInfo.InvariantCulture); }
        private static string Xml(string value) { return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;"); }
        private static void AddText(ZipArchive archive, string path, string text) { var entry = archive.CreateEntry(path, CompressionLevel.Optimal); using (var stream = entry.Open()) using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text); }

        private static string ContentTypes() { return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>"; }
        private static string RootRelationships() { return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>"; }
        private static string Workbook() { return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sticker-volgorde\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>"; }
        private static string WorkbookRelationships() { return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>"; }
        private static string Styles() { return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs></styleSheet>"; }
    }
}
