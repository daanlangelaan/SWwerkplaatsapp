using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalPrice
    {
        public decimal Material { get; set; }
        public decimal Machine { get; set; }
        public decimal Labour { get; set; }
        public decimal Margin { get; set; }
        public decimal ExVat { get; set; }
        public decimal Vat { get; set; }
        public decimal IncVat { get; set; }
        public List<PortalPriceLine> Lines { get; private set; }

        public PortalPrice()
        {
            Lines = new List<PortalPriceLine>();
        }
    }

    public sealed class PortalPriceLine
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal PurchaseUnitPrice { get; set; }
        public decimal PurchaseTotal { get; set; }
        public decimal MarkupPercent { get; set; }
        public decimal SalesUnitPrice { get; set; }
        public decimal SalesTotal { get; set; }
        public string Note { get; set; }
    }

    public sealed class PortalPricingService
    {
        public PortalPrice Calculate(WorkbenchModel model)
        {
            var price = new PortalPrice();
            AddSheetLines(price, model);
            AddProfileLines(price, model);
            AddHardwareLines(price, model);
            AddMachineAndLabourLines(price, model);

            foreach (var line in price.Lines)
            {
                price.ExVat += line.SalesTotal;
                if (line.Category == "Machine") price.Machine += line.SalesTotal;
                else if (line.Category == "Arbeid") price.Labour += line.SalesTotal;
                else price.Material += line.SalesTotal;
                price.Margin += line.SalesTotal - line.PurchaseTotal;
            }

            price.Material = RoundMoney(price.Material);
            price.Machine = RoundMoney(price.Machine);
            price.Labour = RoundMoney(price.Labour);
            price.Margin = RoundMoney(price.Margin);
            price.ExVat = RoundMoney(price.ExVat);
            price.Vat = RoundMoney(price.ExVat * 0.21m);
            price.IncVat = RoundMoney(price.ExVat + price.Vat);
            return price;
        }

        public string ExportCsv(PortalPrice price)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Categorie;Omschrijving;Aantal;Eenheid;Inkoop_per_eenheid;Inkoop_totaal;Opslag_pct;Verkoop_per_eenheid;Verkoop_totaal;Notitie");
            foreach (var line in price.Lines)
            {
                sb.Append(E(line.Category)).Append(';');
                sb.Append(E(line.Description)).Append(';');
                sb.Append(F(line.Quantity)).Append(';');
                sb.Append(E(line.Unit)).Append(';');
                sb.Append(M(line.PurchaseUnitPrice)).Append(';');
                sb.Append(M(line.PurchaseTotal)).Append(';');
                sb.Append(F(line.MarkupPercent)).Append(';');
                sb.Append(M(line.SalesUnitPrice)).Append(';');
                sb.Append(M(line.SalesTotal)).Append(';');
                sb.AppendLine(E(line.Note));
            }

            sb.AppendLine();
            sb.AppendLine(";;;;;;;Subtotaal excl. btw;" + M(price.ExVat) + ";");
            sb.AppendLine(";;;;;;;Btw 21%;" + M(price.Vat) + ";");
            sb.AppendLine(";;;;;;;Totaal incl. btw;" + M(price.IncVat) + ";");
            return sb.ToString();
        }

        public string ExportOfferText(PortalQuoteRequest request, PortalPrice price, string orderId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Offerte " + orderId);
            sb.AppendLine();
            sb.AppendLine("Klant: " + request.CustomerName);
            sb.AppendLine("Email: " + request.CustomerEmail);
            sb.AppendLine("Product: " + ProductName(request));
            sb.AppendLine();
            sb.AppendLine("Prijsregels:");
            foreach (var line in price.Lines)
            {
                sb.AppendLine("- " + line.Description + ": " + F(line.Quantity) + " " + line.Unit + " x EUR " + M(line.SalesUnitPrice) + " = EUR " + M(line.SalesTotal));
            }

            sb.AppendLine();
            sb.AppendLine("Subtotaal excl. btw: EUR " + M(price.ExVat));
            sb.AppendLine("Btw 21%: EUR " + M(price.Vat));
            sb.AppendLine("Totaal incl. btw: EUR " + M(price.IncVat));
            sb.AppendLine();
            sb.AppendLine("Let op: inkoopprijzen zijn geschatte invulwaarden voor de MVP. Vervang deze later door echte materiaal-, beslag- en uurtarieven.");
            return sb.ToString();
        }

        private static void AddSheetLines(PortalPrice price, WorkbenchModel model)
        {
            var totals = new Dictionary<string, MaterialAmount>();
            foreach (var sheet in model.Sheets)
            {
                if (sheet.Material == null) continue;
                var key = sheet.Material.Id + "|" + sheet.Material.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture);
                MaterialAmount amount;
                if (!totals.TryGetValue(key, out amount))
                {
                    amount = new MaterialAmount { Name = sheet.Material.Name, Unit = "m2", UnitPrice = EstimatedSheetM2Price(sheet.Material), MarkupPercent = 35, Note = "Geschatte inkoop plaatmateriaal per m2" };
                    totals.Add(key, amount);
                }

                amount.Quantity += (decimal)(sheet.LengthMm * sheet.WidthMm * Math.Max(1, sheet.Quantity) / 1000000.0);
            }

            foreach (var amount in totals.Values)
            {
                AddLine(price, "Materiaal", amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note);
            }
        }

        private static void AddProfileLines(PortalPrice price, WorkbenchModel model)
        {
            var totals = new Dictionary<string, MaterialAmount>();
            foreach (var profile in model.Profiles)
            {
                if (profile.Material == null) continue;
                var key = profile.Material.Id;
                MaterialAmount amount;
                if (!totals.TryGetValue(key, out amount))
                {
                    amount = new MaterialAmount { Name = profile.Material.Name, Unit = "m", UnitPrice = EstimatedProfileMeterPrice(profile.Material), MarkupPercent = 30, Note = "Geschatte inkoop profiel per meter" };
                    totals.Add(key, amount);
                }

                amount.Quantity += (decimal)(profile.LengthMm * Math.Max(1, profile.Quantity) / 1000.0);
            }

            foreach (var amount in totals.Values)
            {
                AddLine(price, "Materiaal", amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note);
            }
        }

        private static void AddHardwareLines(PortalPrice price, WorkbenchModel model)
        {
            foreach (var item in model.Hardware)
            {
                AddLine(price, "Beslag", item.Name, Math.Max(0, item.Quantity), item.Unit ?? "st", EstimatedHardwareUnitPrice(item), 45, "Geschatte inkoop beslag/verbruiksmateriaal");
            }
        }

        private static void AddMachineAndLabourLines(PortalPrice price, WorkbenchModel model)
        {
            var holeCount = 0;
            foreach (var sheet in model.Sheets) holeCount += sheet.Holes.Count * Math.Max(1, sheet.Quantity);
            var sheetCount = 0;
            foreach (var sheet in model.Sheets) sheetCount += Math.Max(1, sheet.Quantity);
            var profileCount = 0;
            foreach (var profile in model.Profiles) profileCount += Math.Max(1, profile.Quantity);

            var cncMinutes = Math.Max(25, sheetCount * 7 + holeCount * 0.55m);
            var labourMinutes = Math.Max(35, (sheetCount + profileCount) * 4.5m);
            AddLine(price, "Machine", "CNC frezen / boren", cncMinutes / 60m, "uur", 38m, 40, "Geschatte machinekost incl. frees/slijtage");
            AddLine(price, "Arbeid", "Voorbereiding, controle en handling", labourMinutes / 60m, "uur", 42m, 35, "Geschat intern uurtarief");
        }

        private static void AddLine(PortalPrice price, string category, string description, decimal quantity, string unit, decimal purchaseUnitPrice, decimal markupPercent, string note)
        {
            quantity = RoundQuantity(quantity);
            var purchaseTotal = RoundMoney(quantity * purchaseUnitPrice);
            var salesUnit = RoundMoney(purchaseUnitPrice * (1 + markupPercent / 100m));
            var salesTotal = RoundMoney(quantity * salesUnit);
            price.Lines.Add(new PortalPriceLine
            {
                Category = category,
                Description = description,
                Quantity = quantity,
                Unit = unit,
                PurchaseUnitPrice = purchaseUnitPrice,
                PurchaseTotal = purchaseTotal,
                MarkupPercent = markupPercent,
                SalesUnitPrice = salesUnit,
                SalesTotal = salesTotal,
                Note = note
            });
        }

        private static decimal EstimatedSheetM2Price(Material material)
        {
            if (material.Id != null && material.Id.IndexOf("betonplex", StringComparison.OrdinalIgnoreCase) >= 0) return material.ThicknessMm >= 18 ? 42m : 34m;
            if (material.Id != null && material.Id.IndexOf("multiplex", StringComparison.OrdinalIgnoreCase) >= 0) return 38m;
            if (material.Id != null && material.Id.IndexOf("osb", StringComparison.OrdinalIgnoreCase) >= 0) return 18m;
            return 32m;
        }

        private static decimal EstimatedProfileMeterPrice(Material material)
        {
            if (material.Id != null && material.Id.IndexOf("steel", StringComparison.OrdinalIgnoreCase) >= 0) return 9m;
            if (material.WidthMm >= 45 || material.HeightMm >= 45) return 15m;
            if (material.WidthMm >= 40 || material.HeightMm >= 40) return 12m;
            return 9m;
        }

        private static decimal EstimatedHardwareUnitPrice(HardwareItem item)
        {
            var name = (item.Name ?? "").ToLowerInvariant();
            if (name.IndexOf("rail") >= 0 || name.IndexOf("ladegeleider") >= 0) return 7.5m;
            if (name.IndexOf("scharnier") >= 0) return 3.5m;
            if (name.IndexOf("schroef") >= 0 || name.IndexOf("bout") >= 0 || name.IndexOf("ring") >= 0) return 0.18m;
            if (item.Unit == "set") return 12m;
            return 0.75m;
        }

        private static string ProductName(PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            return "Cabinet";
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal RoundQuantity(decimal value)
        {
            return Math.Round(value, 3, MidpointRounding.AwayFromZero);
        }

        private static string M(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string F(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string E(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private sealed class MaterialAmount
        {
            public string Name { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal MarkupPercent { get; set; }
            public string Note { get; set; }
        }
    }

}
