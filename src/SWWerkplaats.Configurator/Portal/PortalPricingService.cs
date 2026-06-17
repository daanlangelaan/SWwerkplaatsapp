using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalPrice
    {
        public decimal Material { get; set; }
        public decimal Hardware { get; set; }
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
            return Calculate(model, null);
        }

        public PortalPrice Calculate(WorkbenchModel model, NestingPlan nestingPlan)
        {
            var price = new PortalPrice();
            AddSheetLines(price, model, nestingPlan);
            AddProfileLines(price, model);
            AddHardwareLines(price, model);
            AddMachineAndLabourLines(price, model, nestingPlan);

            foreach (var line in price.Lines)
            {
                price.ExVat += line.SalesTotal;
                if (line.Category == "Machine") price.Machine += line.SalesTotal;
                else if (line.Category == "Arbeid") price.Labour += line.SalesTotal;
                else if (line.Category == "Materiaal") price.Material += line.PurchaseTotal;
                else if (line.Category == "Beslag") price.Hardware += line.PurchaseTotal;
                price.Margin += line.SalesTotal - line.PurchaseTotal;
            }

            price.Material = RoundMoney(price.Material);
            price.Hardware = RoundMoney(price.Hardware);
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

        private static void AddSheetLines(PortalPrice price, WorkbenchModel model, NestingPlan nestingPlan)
        {
            if (nestingPlan != null && nestingPlan.StockSheets.Count > 0)
            {
                AddNestedStockSheetLines(price, nestingPlan);
                return;
            }

            AddNetSheetPartLines(price, model);
        }

        private static void AddNestedStockSheetLines(PortalPrice price, NestingPlan nestingPlan)
        {
            var totals = new Dictionary<string, MaterialAmount>();
            foreach (var stock in nestingPlan.StockSheets)
            {
                if (stock.Material == null) continue;
                var key = stock.Material.Id + "|" + stock.Material.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture)
                    + "|" + stock.StockLengthMm.ToString("0.###", CultureInfo.InvariantCulture)
                    + "|" + stock.StockWidthMm.ToString("0.###", CultureInfo.InvariantCulture);
                MaterialAmount amount;
                if (!totals.TryGetValue(key, out amount))
                {
                    var estimate = PriceEstimate("Materiaal", stock.Material.Id, EstimatedSheetM2Price(stock.Material), 35);
                    amount = new MaterialAmount
                    {
                        Name = stock.Material.Name + " voorraadplaat " + stock.StockLengthMm.ToString("0") + "x" + stock.StockWidthMm.ToString("0") + "mm",
                        Unit = SheetPriceIsPerPlate(estimate.Unit) ? "plaat" : "m2",
                        UnitPrice = estimate.UnitPrice,
                        MarkupPercent = estimate.MarkupPercent,
                        Note = estimate.Note + " - gerekend op gebruikte volledige nestingplaten"
                    };
                    totals.Add(key, amount);
                }

                amount.Quantity += SheetPriceIsPerPlate(amount.Unit) ? 1m : (decimal)(stock.StockLengthMm * stock.StockWidthMm / 1000000.0);
            }

            foreach (var amount in totals.Values)
            {
                AddLine(price, "Materiaal", amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note);
            }
        }

        private static void AddNetSheetPartLines(PortalPrice price, WorkbenchModel model)
        {
            var totals = new Dictionary<string, MaterialAmount>();
            foreach (var sheet in model.Sheets)
            {
                if (sheet.Material == null) continue;
                var key = sheet.Material.Id + "|" + sheet.Material.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture);
                MaterialAmount amount;
                if (!totals.TryGetValue(key, out amount))
                {
                    var estimate = PriceEstimate("Materiaal", sheet.Material.Id, EstimatedSheetM2Price(sheet.Material), 35);
                    amount = new MaterialAmount { Name = sheet.Material.Name, Unit = "m2", UnitPrice = estimate.UnitPrice, MarkupPercent = estimate.MarkupPercent, Note = estimate.Note };
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
                    var estimate = PriceEstimate("Profiel", profile.Material.Id, EstimatedProfileMeterPrice(profile.Material), 30);
                    amount = new MaterialAmount { Name = profile.Material.Name, Unit = "m", UnitPrice = estimate.UnitPrice, MarkupPercent = estimate.MarkupPercent, Note = estimate.Note };
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
                var estimate = PriceEstimate("Beslag", HardwarePriceKey(item), EstimatedHardwareUnitPrice(item), 45);
                AddLine(price, "Beslag", item.Name, Math.Max(0, item.Quantity), item.Unit ?? "st", estimate.UnitPrice, estimate.MarkupPercent, estimate.Note);
            }
        }

        private static void AddMachineAndLabourLines(PortalPrice price, WorkbenchModel model, NestingPlan nestingPlan)
        {
            var holeCount = 0;
            foreach (var sheet in model.Sheets) holeCount += sheet.Holes.Count * Math.Max(1, sheet.Quantity);
            var stockSheetCount = 0;
            if (nestingPlan != null) stockSheetCount = nestingPlan.StockSheets.Count;
            if (stockSheetCount <= 0)
            {
                foreach (var sheet in model.Sheets) stockSheetCount += Math.Max(1, sheet.Quantity);
            }

            var profileCount = 0;
            foreach (var profile in model.Profiles) profileCount += Math.Max(1, profile.Quantity);

            var cncMinutes = Math.Max(25, stockSheetCount * 7 + holeCount * 0.08m);
            var labourMinutes = Math.Max(25, stockSheetCount * 6 + profileCount * 4.5m);
            var machine = PriceEstimate("Machine", "cnc_hour", 38m, 40);
            var labour = PriceEstimate("Arbeid", "labour_hour", 42m, 35);
            AddLine(price, "Machine", "CNC frezen / boren", cncMinutes / 60m, "uur", machine.UnitPrice, machine.MarkupPercent, machine.Note);
            AddLine(price, "Arbeid", "Voorbereiding, controle en handling", labourMinutes / 60m, "uur", labour.UnitPrice, labour.MarkupPercent, labour.Note);
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

        private static string HardwarePriceKey(HardwareItem item)
        {
            var name = (item == null ? "" : item.Name ?? "").ToLowerInvariant();
            if (name.IndexOf("rail") >= 0 || name.IndexOf("ladegeleider") >= 0) return "drawer_rail";
            if (name.IndexOf("scharnier") >= 0) return "hinge";
            if (name.IndexOf("schroef") >= 0 || name.IndexOf("bout") >= 0 || name.IndexOf("ring") >= 0) return "fastener";
            return name;
        }

        private static PricingEstimate PriceEstimate(string category, string key, decimal fallbackUnitPrice, decimal fallbackMarkupPercent)
        {
            PricingEstimate estimate;
            if (!string.IsNullOrEmpty(key) && PricingEstimates().TryGetValue(PriceKey(category, key), out estimate))
            {
                return estimate;
            }

            return new PricingEstimate
            {
                UnitPrice = fallbackUnitPrice,
                MarkupPercent = fallbackMarkupPercent,
                Note = "Fallback schatting; geen regel gevonden in config/pricing-estimates.csv"
            };
        }

        private static bool SheetPriceIsPerPlate(string unit)
        {
            unit = (unit ?? "").Trim().ToLowerInvariant();
            return unit == "plaat" || unit == "platen" || unit == "st" || unit == "stuk";
        }

        private static Dictionary<string, PricingEstimate> pricingEstimates;

        private static Dictionary<string, PricingEstimate> PricingEstimates()
        {
            if (pricingEstimates != null) return pricingEstimates;
            pricingEstimates = new Dictionary<string, PricingEstimate>(StringComparer.OrdinalIgnoreCase);

            var path = PricingConfigPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return pricingEstimates;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                return pricingEstimates;
            }

            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var columns = SplitCsvLine(lines[i]);
                if (columns.Length < 6) continue;

                decimal unitPrice;
                decimal markup;
                if (!TryParseMoney(columns[4], out unitPrice)) continue;
                if (!TryParseMoney(columns[5], out markup)) markup = 0;

                var category = columns[0];
                var key = columns[1];
                if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key)) continue;

                pricingEstimates[PriceKey(category, key)] = new PricingEstimate
                {
                    Unit = columns.Length > 3 ? columns[3] : "",
                    UnitPrice = unitPrice,
                    MarkupPercent = markup,
                    Note = columns.Length > 6 && !string.IsNullOrWhiteSpace(columns[6]) ? columns[6] : "Uit config/pricing-estimates.csv"
                };
            }

            return pricingEstimates;
        }

        private static string PricingConfigPath()
        {
            var fromBase = FindPricingConfigUpwards(AppDomain.CurrentDomain.BaseDirectory);
            if (fromBase != null) return fromBase;

            var fromCurrent = FindPricingConfigUpwards(Environment.CurrentDirectory);
            if (fromCurrent != null) return fromCurrent;

            return null;
        }

        private static string FindPricingConfigUpwards(string startFolder)
        {
            if (string.IsNullOrEmpty(startFolder)) return null;

            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 6 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config", "pricing-estimates.csv");
                if (File.Exists(candidate)) return candidate;

                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }

            return null;
        }

        private static string PriceKey(string category, string key)
        {
            return (category ?? "").Trim() + "|" + (key ?? "").Trim();
        }

        private static bool TryParseMoney(string value, out decimal result)
        {
            value = (value ?? "").Trim().Replace(',', '.');
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        private static string[] SplitCsvLine(string line)
        {
            var columns = new List<string>();
            var current = new StringBuilder();
            var quoted = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (ch == ';' && !quoted)
                {
                    columns.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(ch);
                }
            }

            columns.Add(current.ToString());
            return columns.ToArray();
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

        private sealed class PricingEstimate
        {
            public string Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal MarkupPercent { get; set; }
            public string Note { get; set; }
        }
    }

}
