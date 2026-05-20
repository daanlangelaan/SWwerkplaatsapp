using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PriceOverviewXlsxExporter
    {
        public void Export(string path, PortalPrice price)
        {
            if (File.Exists(path)) File.Delete(path);
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddText(archive, "[Content_Types].xml", ContentTypes());
                AddText(archive, "_rels/.rels", RootRelationships());
                AddText(archive, "xl/workbook.xml", Workbook());
                AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
                AddText(archive, "xl/styles.xml", Styles());
                AddText(archive, "xl/worksheets/sheet1.xml", Worksheet(price));
            }
        }

        private static string Worksheet(PortalPrice price)
        {
            var rows = new StringBuilder();
            rows.Append(Row(1, new[] { "Categorie", "Omschrijving", "Aantal", "Eenheid", "Inkoop/eenheid", "Inkoop totaal", "Opslag %", "Verkoop/eenheid", "Verkoop totaal", "Notitie" }, 1));
            var row = 2;
            foreach (var line in price.Lines)
            {
                rows.Append(Row(row++, new[]
                {
                    line.Category,
                    line.Description,
                    F(line.Quantity),
                    line.Unit,
                    M(line.PurchaseUnitPrice),
                    M(line.PurchaseTotal),
                    F(line.MarkupPercent),
                    M(line.SalesUnitPrice),
                    M(line.SalesTotal),
                    line.Note
                }, 0));
            }

            rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Subtotaal excl. btw", M(price.ExVat), "" }, 2));
            rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Btw 21%", M(price.Vat), "" }, 2));
            rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Totaal incl. btw", M(price.IncVat), "" }, 3));

            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
                "<cols>" + Col(1, 1, 16) + Col(2, 2, 42) + Col(3, 4, 12) + Col(5, 9, 16) + Col(10, 10, 56) + "</cols>" +
                "<sheetData>" + rows + "</sheetData>" +
                "<autoFilter ref=\"A1:J" + (row - 4).ToString(CultureInfo.InvariantCulture) + "\"/>" +
                "</worksheet>";
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
                sb.Append("><is><t>").Append(Xml(values[i])).Append("</t></is></c>");
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
                "<sheets><sheet name=\"PrijsOverzicht\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
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
                "<fonts count=\"3\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts>" +
                "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEAF4EA\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"4\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs>" +
                "</styleSheet>";
        }

        private static string M(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string F(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
