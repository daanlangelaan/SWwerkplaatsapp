using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PriceOverviewXlsxExporter
    {
        public void Export(string path, WorkbenchModel model, PortalPrice price)
        {
            if (File.Exists(path)) File.Delete(path);
            var sheets = new[]
            {
                new SheetDocument("Samenvatting", SummaryWorksheet(price)),
                new SheetDocument("BOM & inkoop", BomWorksheet(model, price)),
                new SheetDocument("Prijsregels", PriceWorksheet(price, false)),
                new SheetDocument("Open prijsdata", PriceWorksheet(price, true))
            };

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddText(archive, "[Content_Types].xml", ContentTypes(sheets.Length));
                AddText(archive, "_rels/.rels", RootRelationships());
                AddText(archive, "xl/workbook.xml", Workbook(sheets));
                AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheets.Length));
                AddText(archive, "xl/styles.xml", Styles());
                for (var i = 0; i < sheets.Length; i++)
                    AddText(archive, "xl/worksheets/sheet" + (i + 1).ToString(CultureInfo.InvariantCulture) + ".xml", sheets[i].Xml);
            }
        }

        private static string SummaryWorksheet(PortalPrice price)
        {
            var openCount = price.Lines.Count(IsOpenPriceLine);
            var rows = new StringBuilder();
            rows.Append(Row(1, new[] { "Projectcalculatie", "Waarde" }, 1));
            rows.Append(Row(2, new[] { "Plaatmateriaal inkoop", M(price.Material) }, 0));
            rows.Append(Row(3, new[] { "Beslag inkoop", M(price.Hardware) }, 0));
            rows.Append(Row(4, new[] { "Machine verkoop", M(price.Machine) }, 0));
            rows.Append(Row(5, new[] { "Arbeid verkoop", M(price.Labour) }, 0));
            rows.Append(Row(6, new[] { "Opslag / marge", M(price.Margin) }, 0));
            rows.Append(Row(7, new[] { "Subtotaal excl. btw", M(price.ExVat) }, 2));
            rows.Append(Row(8, new[] { "Btw 21%", M(price.Vat) }, 2));
            rows.Append(Row(9, new[] { "Totaal incl. btw", M(price.IncVat) }, 3));
            rows.Append(Row(11, new[] { "Aantal prijsregels", price.Lines.Count.ToString(CultureInfo.InvariantCulture) }, 0));
            rows.Append(Row(12, new[] { "Open / te bevestigen prijsregels", openCount.ToString(CultureInfo.InvariantCulture) }, openCount == 0 ? 3 : 2));
            rows.Append(Row(14, new[] { "Gebruik", "BOM & inkoop bevat de modelstuklijst met gekoppelde leveranciersdata. Prijsregels is de volledige calculatie. Open prijsdata bevat uitsluitend regels die vóór bestellen nog moeten worden bevestigd." }, 0));
            return WorksheetXml(rows.ToString(), Col(1, 1, 36) + Col(2, 2, 100), "A1:B14", false);
        }

        private static string BomWorksheet(WorkbenchModel model, PortalPrice price)
        {
            var rows = new StringBuilder();
            rows.Append(Row(1, new[] { "Type", "Naam", "Artikelnummer", "Aantal", "Eenheid", "Materiaal", "Maat", "BOM-status", "Inkoopsleutel", "Leverancier", "Leveranciers-artikelcode", "Inkoopprijs", "Prijseenheid", "Prijsstatus", "Bestel-URL", "Notitie" }, 1));
            var row = 2;
            foreach (var profile in model.Profiles)
            {
                var purchase = FindPurchaseLine(price, profile.Material == null ? "" : profile.Material.Id, profile.Material == null ? "" : profile.Material.Name);
                rows.Append(BomRow(row++, "Profiel", profile.Name, "", profile.Quantity, "st", profile.Material == null ? "" : profile.Material.Name,
                    profile.LengthMm.ToString("0.###", CultureInfo.InvariantCulture) + " mm", profile.BomStatus, profile.OrientationNote, purchase));
            }
            foreach (var sheet in model.Sheets)
            {
                var purchase = FindPurchaseLine(price, sheet.Material == null ? "" : sheet.Material.Id, sheet.Material == null ? "" : sheet.Material.Name);
                rows.Append(BomRow(row++, "Plaat", sheet.Name, "", sheet.Quantity, "st", sheet.Material == null ? "" : sheet.Material.Name,
                    sheet.LengthMm.ToString("0.###", CultureInfo.InvariantCulture) + " x " + sheet.WidthMm.ToString("0.###", CultureInfo.InvariantCulture) + " x " + (sheet.Material == null ? "" : sheet.Material.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture)) + " mm",
                    sheet.BomStatus, "", purchase));
            }
            foreach (var item in model.Hardware)
            {
                var purchase = FindPurchaseLine(price, "", item.Name, item.ArticleNumber);
                rows.Append(BomRow(row++, "Bevestiging", item.Name, item.ArticleNumber, item.Quantity, item.Unit, "", "", item.BomStatus, item.Note, purchase));
            }
            return WorksheetXml(rows.ToString(),
                Col(1, 1, 14) + Col(2, 2, 44) + Col(3, 3, 30) + Col(4, 5, 12) + Col(6, 8, 30) + Col(9, 11, 28) + Col(12, 14, 16) + Col(15, 15, 52) + Col(16, 16, 70),
                "A1:P" + Math.Max(1, row - 1).ToString(CultureInfo.InvariantCulture), true);
        }

        private static string BomRow(int row, string type, string name, string article, int quantity, string unit, string material, string size, string bomStatus, string note, PortalPriceLine purchase)
        {
            return Row(row, new[]
            {
                type, name, article, quantity.ToString(CultureInfo.InvariantCulture), unit, material, size, bomStatus,
                purchase == null ? "" : purchase.Key,
                purchase == null ? "" : purchase.Supplier,
                purchase == null || string.IsNullOrWhiteSpace(purchase.SupplierArticleCode) ? article : purchase.SupplierArticleCode,
                purchase == null ? "" : M(purchase.PurchaseUnitPrice),
                purchase == null ? "" : purchase.Unit,
                purchase == null ? "Niet gekoppeld" : purchase.PriceStatus,
                purchase == null ? "" : purchase.OrderUrl,
                note
            }, 0);
        }

        private static string PriceWorksheet(PortalPrice price, bool openOnly)
        {
            var rows = new StringBuilder();
            rows.Append(Row(1, new[] { "Categorie", "Omschrijving", "Aantal", "Eenheid", "Inkoop/eenheid", "Inkoop totaal", "Opslag %", "Verkoop/eenheid", "Verkoop totaal", "Notitie", "Inkoopsleutel", "Aanbieding-ID", "Leverancier-ID", "Leverancier", "Leveranciers-artikelcode", "Bestel-URL", "Prijsdatum", "Prijsstatus" }, 1));
            var row = 2;
            foreach (var line in price.Lines.Where(value => !openOnly || IsOpenPriceLine(value)))
            {
                rows.Append(Row(row++, new[]
                {
                    line.Category, line.Description, F(line.Quantity), line.Unit, M(line.PurchaseUnitPrice), M(line.PurchaseTotal), F(line.MarkupPercent),
                    M(line.SalesUnitPrice), M(line.SalesTotal), line.Note, line.Key, line.OfferId, line.SupplierId, line.Supplier,
                    line.SupplierArticleCode, line.OrderUrl, line.PriceDate, line.PriceStatus
                }, 0));
            }
            if (!openOnly)
            {
                rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Subtotaal excl. btw", M(price.ExVat), "", "", "", "", "", "", "", "", "" }, 2));
                rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Btw 21%", M(price.Vat), "", "", "", "", "", "", "", "", "" }, 2));
                rows.Append(Row(row++, new[] { "", "", "", "", "", "", "", "Totaal incl. btw", M(price.IncVat), "", "", "", "", "", "", "", "", "" }, 3));
            }
            return WorksheetXml(rows.ToString(),
                Col(1, 1, 16) + Col(2, 2, 44) + Col(3, 4, 12) + Col(5, 9, 16) + Col(10, 10, 72) + Col(11, 15, 28) + Col(16, 16, 52) + Col(17, 18, 22),
                "A1:R" + Math.Max(1, row - 1).ToString(CultureInfo.InvariantCulture), true);
        }

        private static PortalPriceLine FindPurchaseLine(PortalPrice price, string key, string description, string article = "")
        {
            if (price == null) return null;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var byKey = price.Lines.FirstOrDefault(line => string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase));
                if (byKey != null) return byKey;
            }
            if (!string.IsNullOrWhiteSpace(article))
            {
                var byArticle = price.Lines.FirstOrDefault(line =>
                    string.Equals(line.SupplierArticleCode, article, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(line.SupplierArticleCode) && article.IndexOf(line.SupplierArticleCode, StringComparison.OrdinalIgnoreCase) >= 0));
                if (byArticle != null) return byArticle;
            }
            return price.Lines.FirstOrDefault(line => string.Equals(line.Description, description, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsOpenPriceLine(PortalPriceLine line)
        {
            var status = (line == null ? "" : line.PriceStatus ?? "").ToLowerInvariant();
            return string.IsNullOrWhiteSpace(status)
                || status.Contains("fallback") || status.Contains("schatting") || status.Contains("raming") || status.Contains("voorlopig")
                || status.Contains("controleren") || status.Contains("offerte") || status.Contains("bevestigen")
                || status.Contains("opvragen") || status.Contains("kandidaat") || status.Contains("ontbrekende");
        }

        private static string WorksheetXml(string rows, string columns, string filterRange, bool freezeHeader)
        {
            var views = freezeHeader
                ? "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>"
                : "<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>";
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" + views +
                "<cols>" + columns + "</cols><sheetData>" + rows + "</sheetData>" +
                (freezeHeader ? "<autoFilter ref=\"" + filterRange + "\"/>" : "") + "</worksheet>";
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
                sb.Append("><is><t xml:space=\"preserve\">").Append(Xml(values[i])).Append("</t></is></c>");
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
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text);
        }

        private static string ContentTypes(int sheetCount)
        {
            var sheets = new StringBuilder();
            for (var i = 1; i <= sheetCount; i++)
                sheets.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                sheets + "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        }

        private static string RootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        }

        private static string Workbook(IList<SheetDocument> sheets)
        {
            var nodes = new StringBuilder();
            for (var i = 0; i < sheets.Count; i++)
                nodes.Append("<sheet name=\"").Append(Xml(sheets[i].Name)).Append("\" sheetId=\"").Append(i + 1).Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" + nodes + "</sheets></workbook>";
        }

        private static string WorkbookRelationships(int sheetCount)
        {
            var rels = new StringBuilder();
            for (var i = 1; i <= sheetCount; i++)
                rels.Append("<Relationship Id=\"rId").Append(i).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet").Append(i).Append(".xml\"/>");
            rels.Append("<Relationship Id=\"rId").Append(sheetCount + 1).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" + rels + "</Relationships>";
        }

        private static string Styles()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"3\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts><fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEAF4EA\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"4\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs></styleSheet>";
        }

        private static string M(decimal value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static string F(decimal value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private sealed class SheetDocument
        {
            public SheetDocument(string name, string xml) { Name = name; Xml = xml; }
            public string Name { get; private set; }
            public string Xml { get; private set; }
        }
    }
}
