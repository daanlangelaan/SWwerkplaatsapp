using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ProfileTapWorklistXlsxExporter
    {
        public void Export(string path, IEnumerable<ProfileTapWorklistRow> source)
        {
            var rows = (source ?? Enumerable.Empty<ProfileTapWorklistRow>()).Where(row => row.TapRequired).ToArray();
            if (File.Exists(path)) File.Delete(path);
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Add(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                Add(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                Add(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Tap-werkplaatslijst\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Add(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                Add(archive, "xl/styles.xml", Styles());
                Add(archive, "xl/worksheets/sheet1.xml", Worksheet(rows));
            }
        }

        private static string Worksheet(ProfileTapWorklistRow[] data)
        {
            var sb = new StringBuilder();
            var headers = new[] { "Freesvolgorde", "Profielstuk-ID", "Onderdeel", "Profieltype", "Lengte mm", "Machinezijde", "Stickervlak", "Kernboring", "Onder stickervlak", "Tapdraad", "Werkinstructie" };
            sb.Append(Row(1, headers, 1));
            var index = 2;
            foreach (var row in data) sb.Append(Row(index++, new[] {
                row.ProductionOrder.ToString(CultureInfo.InvariantCulture), row.TraceId, row.PartName,
                F(row.WidthMm) + " x " + F(row.HeightMm), F(row.LengthMm),
                row.StickerEnd ? "X=0 · stickerzijde" : "X=L · eindzijde", row.StickerFace,
                row.CoreHole, row.UnderStickerFace ? "ja" : "nee", row.Thread, row.Instruction
            }, 2));
            var last = System.Math.Max(1, index - 1);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><cols><col min=\"1\" max=\"1\" width=\"14\" customWidth=\"1\"/><col min=\"2\" max=\"4\" width=\"24\" customWidth=\"1\"/><col min=\"5\" max=\"10\" width=\"20\" customWidth=\"1\"/><col min=\"11\" max=\"11\" width=\"52\" customWidth=\"1\"/></cols><sheetData>" + sb + "</sheetData><autoFilter ref=\"A1:K" + last + "\"/></worksheet>";
        }

        private static string Row(int row, string[] values, int style)
        {
            var sb = new StringBuilder("<row r=\"").Append(row).Append("\">");
            for (var i = 0; i < values.Length; i++) sb.Append("<c r=\"").Append(Column(i + 1)).Append(row).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>").Append(Xml(values[i])).Append("</t></is></c>");
            return sb.Append("</row>").ToString();
        }
        private static string Column(int number) { var value = ""; while (number > 0) { var m = (number - 1) % 26; value = (char)('A' + m) + value; number = (number - m) / 26; } return value; }
        private static string F(double value) { return value.ToString("0.##", CultureInfo.InvariantCulture); }
        private static string Xml(string value) { return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"); }
        private static void Add(ZipArchive archive, string path, string text) { var entry = archive.CreateEntry(path, CompressionLevel.Optimal); using (var stream = entry.Open()) using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text); }
        private static string Styles() { return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts><fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFE699\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"0\" xfId=\"0\"/></cellXfs></styleSheet>"; }
    }
}
