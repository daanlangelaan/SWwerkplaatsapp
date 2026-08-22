using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using SWWerkplaats.Configurator.Application;
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
        public string Key { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal PurchaseUnitPrice { get; set; }
        public decimal PurchaseTotal { get; set; }
        public decimal MarkupPercent { get; set; }
        public decimal SalesUnitPrice { get; set; }
        public decimal SalesTotal { get; set; }
        public string Supplier { get; set; }
        public string SupplierId { get; set; }
        public string SupplierArticleCode { get; set; }
        public string OrderUrl { get; set; }
        public string PriceDate { get; set; }
        public string PriceStatus { get; set; }
        public string OfferId { get; set; }
        public string ImageId { get; set; }
        public string ImageSourceUrl { get; set; }
        public string LocalImagePath { get; set; }
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
            sb.AppendLine("Categorie;Omschrijving;Aantal;Eenheid;Inkoop_per_eenheid;Inkoop_totaal;Opslag_pct;Verkoop_per_eenheid;Verkoop_totaal;Notitie;Inkoopsleutel;Aanbieding_ID;Leverancier_ID;Leverancier;Leveranciers_artikelcode;Bestel_URL;Prijsdatum;Prijsstatus;Afbeelding_ID;Afbeelding_bron_URL;Lokale_afbeelding");
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
                sb.Append(E(line.Note)).Append(';');
                sb.Append(E(line.Key)).Append(';');
                sb.Append(E(line.OfferId)).Append(';');
                sb.Append(E(line.SupplierId)).Append(';');
                sb.Append(E(line.Supplier)).Append(';');
                sb.Append(E(line.SupplierArticleCode)).Append(';');
                sb.Append(E(line.OrderUrl)).Append(';');
                sb.Append(E(line.PriceDate)).Append(';');
                sb.Append(E(line.PriceStatus)).Append(';');
                sb.Append(E(line.ImageId)).Append(';');
                sb.Append(E(line.ImageSourceUrl)).Append(';');
                sb.AppendLine(E(line.LocalImagePath));
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
            if (!string.IsNullOrWhiteSpace(request.ProjectName)) sb.AppendLine("Project: " + request.ProjectName);
            sb.AppendLine("Email: " + request.CustomerEmail);
            sb.AppendLine("Product: " + ProductName(request));
            sb.AppendLine();
            sb.AppendLine("Prijsregels:");
            foreach (var line in price.Lines)
            {
                sb.AppendLine("- " + CustomerFacingDescription(request, line.Description) + ": " + F(line.Quantity) + " " + line.Unit + " x EUR " + M(line.SalesUnitPrice) + " = EUR " + M(line.SalesTotal));
            }

            sb.AppendLine();
            sb.AppendLine("Subtotaal excl. btw: EUR " + M(price.ExVat));
            sb.AppendLine("Btw 21%: EUR " + M(price.Vat));
            sb.AppendLine("Totaal incl. btw: EUR " + M(price.IncVat));
            sb.AppendLine();
            sb.AppendLine("Let op: prijsstatus, prijsdatum en leverancier komen uit de centrale masterdata. Regels met status Schatting, Voorlopig of Offerte nodig moeten vóór bestellen worden bevestigd.");
            return sb.ToString();
        }

        private static string CustomerFacingDescription(PortalQuoteRequest request, string description)
        {
            if (request == null || (request.Product != "werktafel_lex" && request.Product != "werktafel_lex_revolution"))
                return description;
            var value = (description ?? "").ToLowerInvariant();
            if (value.Contains("hte2") || value.Contains("hefkolom")) return "Elektrische hoogteverstelling";
            if (value.Contains("hsr15") || value.Contains("rail") || value.Contains("lineair")) return "Lineaire geleiding en montage";
            if (value.Contains("kogelpot")) return "Kogelpotten voor het werkvlak";
            if (value.Contains("hpl")) return "HPL-werkvlak";
            if (value.Contains("adapterplaat") || value.Contains("en aw")) return "Aluminium montageplaten";
            if (value.Contains("profiel") || value.Contains("80x") || value.Contains("40x")) return "Geanodiseerde aluminium profielconstructie";
            return description;
        }

        private static void AddSheetLines(PortalPrice price, WorkbenchModel model, NestingPlan nestingPlan)
        {
            if (nestingPlan != null && nestingPlan.StockSheets.Count > 0)
            {
                AddNestedStockSheetLines(price, model.ProductId, nestingPlan);
                return;
            }

            AddNetSheetPartLines(price, model);
        }

        private static void AddNestedStockSheetLines(PortalPrice price, string productId, NestingPlan nestingPlan)
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
                    var estimate = PriceEstimate(productId, "Materiaal", stock.Material.Id, EstimatedSheetM2Price(stock.Material), 35);
                    amount = new MaterialAmount
                    {
                        Key = stock.Material.Id,
                        Estimate = estimate,
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
                AddLine(price, "Materiaal", amount.Key, amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note, amount.Estimate);
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
                    var estimate = PriceEstimate(model.ProductId, "Materiaal", sheet.Material.Id, EstimatedSheetM2Price(sheet.Material), 35);
                    amount = new MaterialAmount { Key = sheet.Material.Id, Estimate = estimate, Name = sheet.Material.Name, Unit = "m2", UnitPrice = estimate.UnitPrice, MarkupPercent = estimate.MarkupPercent, Note = estimate.Note };
                    totals.Add(key, amount);
                }

                amount.Quantity += (decimal)(sheet.LengthMm * sheet.WidthMm * Math.Max(1, sheet.Quantity) / 1000000.0);
            }

            foreach (var amount in totals.Values)
            {
                AddLine(price, "Materiaal", amount.Key, amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note, amount.Estimate);
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
                    var estimate = PriceEstimate(model.ProductId, "Profiel", profile.Material.Id, EstimatedProfileMeterPrice(profile.Material), 30);
                    amount = new MaterialAmount { Key = profile.Material.Id, Estimate = estimate, Name = profile.Material.Name, Unit = "m", UnitPrice = estimate.UnitPrice, MarkupPercent = estimate.MarkupPercent, Note = estimate.Note };
                    totals.Add(key, amount);
                }

                amount.Quantity += (decimal)(profile.LengthMm * Math.Max(1, profile.Quantity) / 1000.0);
            }

            foreach (var amount in totals.Values)
            {
                AddLine(price, "Materiaal", amount.Key, amount.Name, amount.Quantity, amount.Unit, amount.UnitPrice, amount.MarkupPercent, amount.Note, amount.Estimate);
            }
        }

        private static void AddHardwareLines(PortalPrice price, WorkbenchModel model)
        {
            foreach (var item in model.Hardware)
            {
                var key = HardwarePriceKey(item);
                var estimate = PriceEstimate(model.ProductId, "Beslag", key, EstimatedHardwareUnitPrice(item), 45);
                AddLine(price, "Beslag", key, item.Name, Math.Max(0, item.Quantity), item.Unit ?? "st", estimate.UnitPrice, estimate.MarkupPercent, estimate.Note, estimate, item.ArticleNumber);
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
            var inHouseSawCount = 0;
            foreach (var operation in model.ProfileOperations)
            {
                if (operation.Kind == ProfileOperationKind.SawCut && string.Equals(operation.ExecutionParty, "WERKPLAATS", StringComparison.OrdinalIgnoreCase))
                {
                    inHouseSawCount += Math.Max(1, operation.Quantity);
                }
            }

            var cncMinutes = Math.Max(25, stockSheetCount * 7 + holeCount * 0.08m);
            var labourMinutes = Math.Max(25, stockSheetCount * 6 + profileCount * 2m);
            var machine = PriceEstimate(model.ProductId, "Machine", "cost_cnc_hour", 38m, 40);
            var labour = PriceEstimate(model.ProductId, "Arbeid", "cost_labour_hour", 42m, 35);
            AddLine(price, "Machine", "cost_cnc_hour", "CNC frezen / boren", cncMinutes / 60m, "uur", machine.UnitPrice, machine.MarkupPercent, machine.Note, machine);
            AddLine(price, "Arbeid", "cost_labour_hour", "Voorbereiding, controle en handling", labourMinutes / 60m, "uur", labour.UnitPrice, labour.MarkupPercent, labour.Note, labour);
            if (inHouseSawCount > 0)
            {
                AddLine(price, "Arbeid", "cost_labour_hour", "Profielen afkorten in werkplaats", inHouseSawCount * 3m / 60m, "uur", labour.UnitPrice, labour.MarkupPercent, labour.Note + " - alleen actief bij keuze zelf zagen", labour);
            }
        }

        private static void AddLine(PortalPrice price, string category, string key, string description, decimal quantity, string unit, decimal purchaseUnitPrice, decimal markupPercent, string note, PricingEstimate estimate, string sourceArticleNumber = null)
        {
            quantity = RoundQuantity(quantity);
            var purchaseTotal = RoundMoney(quantity * purchaseUnitPrice);
            var salesUnit = RoundMoney(purchaseUnitPrice * (1 + markupPercent / 100m));
            var salesTotal = RoundMoney(quantity * salesUnit);
            price.Lines.Add(new PortalPriceLine
            {
                Category = category,
                Key = key,
                Description = description,
                Quantity = quantity,
                Unit = unit,
                PurchaseUnitPrice = purchaseUnitPrice,
                PurchaseTotal = purchaseTotal,
                MarkupPercent = markupPercent,
                SalesUnitPrice = salesUnit,
                SalesTotal = salesTotal,
                Supplier = estimate == null ? "" : estimate.Supplier,
                SupplierId = estimate == null ? "" : estimate.SupplierId,
                SupplierArticleCode = estimate != null && !string.IsNullOrWhiteSpace(estimate.SupplierArticleCode) ? estimate.SupplierArticleCode : (sourceArticleNumber ?? ""),
                OrderUrl = estimate == null ? "" : estimate.OrderUrl,
                PriceDate = estimate == null ? "" : estimate.PriceDate,
                PriceStatus = estimate == null ? "Fallback" : estimate.PriceStatus,
                OfferId = estimate == null ? "" : estimate.OfferId,
                ImageId = estimate == null ? "" : estimate.ImageId,
                ImageSourceUrl = estimate == null ? "" : estimate.ImageSourceUrl,
                LocalImagePath = estimate == null ? "" : estimate.LocalImagePath,
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
            if (string.Equals(item.ArticleNumber, "905.560.71", StringComparison.OrdinalIgnoreCase)) return 4m;
            if (name.IndexOf("schroef") >= 0 || name.IndexOf("bout") >= 0 || name.IndexOf("ring") >= 0) return 0.18m;
            if (name.IndexOf("rail") >= 0 || name.IndexOf("ladegeleider") >= 0) return 7.5m;
            if (name.IndexOf("scharnier") >= 0) return 3.5m;
            if (item.Unit == "set") return 12m;
            return 0.75m;
        }

        private static string HardwarePriceKey(HardwareItem item)
        {
            var name = (item == null ? "" : item.Name ?? "").ToLowerInvariant();
            if (item != null && string.Equals(item.ArticleNumber, "905.560.71", StringComparison.OrdinalIgnoreCase)) return "ikea_sektion_90556071";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101445", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_footplate_10_80x80_m16";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101219", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_leveling_foot_d80_m16x150";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101245", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_corner_bracket_set_10_40x80";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("100199", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_end_cap_8_160x40_black";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("100360", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_corner_bracket_set_8_40x40";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101068", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_caster_fixed_d100";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101074", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_caster_swivel_d100";
            if (item != null && (item.ArticleNumber ?? "").IndexOf("101076", StringComparison.OrdinalIgnoreCase) >= 0) return "techxxl_caster_swivel_brake_d100";
            if (name.IndexOf("schroef") >= 0 || name.IndexOf("bout") >= 0 || name.IndexOf("ring") >= 0) return "cost_fastener_generic";
            if (name.IndexOf("rail") >= 0 || name.IndexOf("ladegeleider") >= 0) return "measured_500_r2";
            if (name.IndexOf("scharnier") >= 0) return "komplement_hinge_r2";
            return name;
        }

        private static PricingEstimate PriceEstimate(string productId, string category, string key, decimal fallbackUnitPrice, decimal fallbackMarkupPercent)
        {
            List<PricingEstimate> candidates;
            if (!string.IsNullOrEmpty(key) && PricingEstimates().TryGetValue(PriceKey(category, key), out candidates) && candidates.Count > 0)
            {
                PricingEstimate selected = null;
                var selectedRank = int.MaxValue;
                foreach (var candidate in candidates)
                {
                    var rank = SupplierPreferenceRank(SupplierPreferences(), productId, candidate.Category, candidate.Subcategory, candidate.SupplierId);
                    if (selected == null || rank < selectedRank || (rank == selectedRank && string.Compare(candidate.OfferId, selected.OfferId, StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        selected = candidate;
                        selectedRank = rank;
                    }
                }
                selected.SupplierRank = selectedRank;
                return selected;
            }

            return new PricingEstimate
            {
                UnitPrice = fallbackUnitPrice,
                MarkupPercent = fallbackMarkupPercent,
                PriceStatus = "Fallback",
                Note = "Fallback schatting; geen passende regel gevonden in de gevalideerde runtime-masterdata"
            };
        }

        private static bool SheetPriceIsPerPlate(string unit)
        {
            unit = (unit ?? "").Trim().ToLowerInvariant();
            return unit == "plaat" || unit == "platen" || unit == "st" || unit == "stuk";
        }

        private static Dictionary<string, List<PricingEstimate>> pricingEstimates;
        private static List<SupplierPreference> supplierPreferences;
        private static string pricingEstimatesSourcePath;
        private static DateTime pricingEstimatesSourceWriteTimeUtc;

        private static Dictionary<string, List<PricingEstimate>> PricingEstimates()
        {
            var runtimePath = RuntimeMasterDataPath();
            var preferredWriteTime = !string.IsNullOrEmpty(runtimePath) && File.Exists(runtimePath)
                ? File.GetLastWriteTimeUtc(runtimePath)
                : DateTime.MinValue;
            if (pricingEstimates != null
                && string.Equals(pricingEstimatesSourcePath, runtimePath, StringComparison.OrdinalIgnoreCase)
                && pricingEstimatesSourceWriteTimeUtc == preferredWriteTime)
            {
                return pricingEstimates;
            }

            pricingEstimates = new Dictionary<string, List<PricingEstimate>>(StringComparer.OrdinalIgnoreCase);
            supplierPreferences = new List<SupplierPreference>();
            if (!string.IsNullOrEmpty(runtimePath) && LoadPricingRuntime(runtimePath, pricingEstimates) && pricingEstimates.Count > 0)
            {
                RememberPricingSource(runtimePath);
            }
            return pricingEstimates;
        }

        private static void RememberPricingSource(string path)
        {
            pricingEstimatesSourcePath = path;
            pricingEstimatesSourceWriteTimeUtc = !string.IsNullOrEmpty(path) && File.Exists(path)
                ? File.GetLastWriteTimeUtc(path)
                : DateTime.MinValue;
        }

        private static string PricingConfigPath()
        {
            var fromBase = FindPricingConfigUpwards(AppDomain.CurrentDomain.BaseDirectory);
            if (fromBase != null) return fromBase;

            var fromCurrent = FindPricingConfigUpwards(Environment.CurrentDirectory);
            if (fromCurrent != null) return fromCurrent;

            return null;
        }

        private static string ProductMasterWorkbookPath()
        {
            var fromBase = FindConfigFileUpwards(AppDomain.CurrentDomain.BaseDirectory, "product-master-data.xlsx");
            if (fromBase != null) return fromBase;
            return FindConfigFileUpwards(Environment.CurrentDirectory, "product-master-data.xlsx");
        }

        private static string RuntimeMasterDataPath()
        {
            var fromBase = FindRuntimeMasterDataUpwards(AppDomain.CurrentDomain.BaseDirectory);
            return fromBase ?? FindRuntimeMasterDataUpwards(Environment.CurrentDirectory);
        }

        private static string FindRuntimeMasterDataUpwards(string startFolder)
        {
            if (string.IsNullOrEmpty(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config", "runtime", "masterdata-runtime.json");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }

        private static bool LoadPricingRuntime(string path, Dictionary<string, List<PricingEstimate>> target)
        {
            try
            {
                var catalog = MasterDataRuntimeCatalog.LoadRequired();
                if (!string.Equals(Path.GetFullPath(catalog.SourcePath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)) return false;

                var supplierNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var supplier in catalog.Records("suppliers"))
                {
                    var id = MasterDataRuntimeCatalog.Value(supplier, "Leverancier-ID");
                    var name = MasterDataRuntimeCatalog.Value(supplier, "Naam");
                    if (!string.IsNullOrWhiteSpace(id)) supplierNames[id] = string.IsNullOrWhiteSpace(name) ? id : name;
                }

                supplierPreferences = new List<SupplierPreference>();
                foreach (var row in catalog.Records("supplierPreferences"))
                {
                    int rank;
                    if (!int.TryParse(MasterDataRuntimeCatalog.Value(row, "Rang"), NumberStyles.Integer, CultureInfo.InvariantCulture, out rank)) continue;
                    supplierPreferences.Add(new SupplierPreference
                    {
                        PreferenceId = MasterDataRuntimeCatalog.Value(row, "Voorkeur-ID"),
                        Category = MasterDataRuntimeCatalog.Value(row, "Categorie"),
                        Subcategory = MasterDataRuntimeCatalog.Value(row, "Subcategorie"),
                        SupplierId = MasterDataRuntimeCatalog.Value(row, "Leverancier-ID"),
                        Rank = rank,
                        ScopeType = MasterDataRuntimeCatalog.Value(row, "Scope-type"),
                        ScopeId = MasterDataRuntimeCatalog.Value(row, "Scope-ID"),
                        Status = MasterDataRuntimeCatalog.Value(row, "Status")
                    });
                }

                foreach (var row in catalog.Records("offers"))
                {
                    var category = MasterDataRuntimeCatalog.Value(row, "Categorie");
                    var key = MasterDataRuntimeCatalog.Value(row, "Interne-ID");
                    decimal unitPrice;
                    decimal markup;
                    if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key)) continue;
                    if (!TryParseMoney(MasterDataRuntimeCatalog.Value(row, "Inkoopprijs excl. btw"), out unitPrice)) continue;
                    if (!TryParseMoney(MasterDataRuntimeCatalog.Value(row, "Opslag %"), out markup)) markup = 0;
                    var supplierId = MasterDataRuntimeCatalog.Value(row, "Leverancier-ID");
                    string supplierName;
                    AddPricingEstimate(target, PriceKey(category, key), new PricingEstimate
                    {
                        OfferId = MasterDataRuntimeCatalog.Value(row, "Aanbieding-ID"),
                        Category = category,
                        Subcategory = MasterDataRuntimeCatalog.Value(row, "Subcategorie"),
                        Unit = MasterDataRuntimeCatalog.Value(row, "Eenheid"),
                        UnitPrice = unitPrice,
                        MarkupPercent = markup,
                        SupplierId = supplierId,
                        Supplier = supplierNames.TryGetValue(supplierId ?? string.Empty, out supplierName) ? supplierName : supplierId,
                        SupplierArticleCode = MasterDataRuntimeCatalog.Value(row, "Leveranciers-artikelcode"),
                        OrderUrl = MasterDataRuntimeCatalog.Value(row, "Bestel-URL"),
                        PriceDate = NormalizeExcelDate(MasterDataRuntimeCatalog.Value(row, "Prijsdatum")),
                        PriceStatus = MasterDataRuntimeCatalog.Value(row, "Prijsstatus"),
                        ImageId = MasterDataRuntimeCatalog.Value(row, "Afbeelding-ID"),
                        ImageSourceUrl = MasterDataRuntimeCatalog.Value(row, "Afbeelding-bron-URL"),
                        LocalImagePath = MasterDataRuntimeCatalog.Value(row, "Lokale afbeelding"),
                        Note = MasterDataRuntimeCatalog.Value(row, "Notitie")
                    });
                }
                return target.Count > 0;
            }
            catch
            {
                target.Clear();
                supplierPreferences = new List<SupplierPreference>();
                return false;
            }
        }

        private static string FindPricingConfigUpwards(string startFolder)
        {
            return FindConfigFileUpwards(startFolder, "pricing-estimates.csv");
        }

        private static string FindConfigFileUpwards(string startFolder, string fileName)
        {
            if (string.IsNullOrEmpty(startFolder)) return null;

            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 6 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config", fileName);
                if (File.Exists(candidate)) return candidate;

                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }

            return null;
        }

        private static bool LoadPricingWorkbook(string path, Dictionary<string, List<PricingEstimate>> target)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    var workbook = LoadXml(archive, "xl/workbook.xml");
                    var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
                    if (workbook == null || relationships == null) return false;
                    var sharedStrings = ReadSharedStrings(archive);
                    var rows = ReadNamedWorksheetRows(archive, workbook, relationships, sharedStrings, "Prijs & inkoop");
                    if (rows == null) return false;
                    var supplierData = ReadSupplierData(archive, workbook, relationships, sharedStrings);
                    supplierPreferences = supplierData.Preferences;
                    Dictionary<string, int> headers = null;
                    foreach (var row in rows)
                    {
                        if (headers == null)
                        {
                            headers = HeaderMap(row);
                            if (!headers.ContainsKey("Categorie") || !headers.ContainsKey("Interne-ID") || !headers.ContainsKey("Aanbieding-ID")) headers = null;
                            continue;
                        }

                        var category = Cell(row, headers, "Categorie");
                        var key = Cell(row, headers, "Interne-ID");
                        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key)) continue;
                        decimal unitPrice;
                        decimal markup;
                        if (!TryParseMoney(Cell(row, headers, "Inkoopprijs excl. btw"), out unitPrice)) continue;
                        if (!TryParseMoney(Cell(row, headers, "Opslag %"), out markup)) markup = 0;

                        var candidate = new PricingEstimate
                        {
                            OfferId = Cell(row, headers, "Aanbieding-ID"),
                            Category = category,
                            Subcategory = Cell(row, headers, "Subcategorie"),
                            Unit = Cell(row, headers, "Eenheid"),
                            UnitPrice = unitPrice,
                            MarkupPercent = markup,
                            SupplierId = Cell(row, headers, "Leverancier-ID"),
                            SupplierArticleCode = Cell(row, headers, "Leveranciers-artikelcode"),
                            OrderUrl = Cell(row, headers, "Bestel-URL"),
                            PriceDate = NormalizeExcelDate(Cell(row, headers, "Prijsdatum")),
                            PriceStatus = Cell(row, headers, "Prijsstatus"),
                            ImageId = Cell(row, headers, "Afbeelding-ID"),
                            ImageSourceUrl = Cell(row, headers, "Afbeelding-bron-URL"),
                            LocalImagePath = Cell(row, headers, "Lokale afbeelding"),
                            Note = Cell(row, headers, "Notitie")
                        };
                        string supplierName;
                        candidate.Supplier = supplierData.Names.TryGetValue(candidate.SupplierId ?? "", out supplierName) ? supplierName : candidate.SupplierId;
                        var priceKey = PriceKey(category, key);
                        AddPricingEstimate(target, priceKey, candidate);
                    }
                }

                return target.Count > 0;
            }
            catch
            {
                target.Clear();
                supplierPreferences = new List<SupplierPreference>();
                return false;
            }
        }

        private static List<Dictionary<int, string>> ReadNamedWorksheetRows(ZipArchive archive, XmlDocument workbook, XmlDocument relationships, List<string> sharedStrings, string sheetName)
        {
            const string spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string officeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var manager = new XmlNamespaceManager(workbook.NameTable);
            manager.AddNamespace("m", spreadsheetNs);
            var sheet = workbook.SelectSingleNode("//m:sheet[@name='" + sheetName.Replace("'", "&apos;") + "']", manager) as XmlElement;
            if (sheet == null) return null;
            var relationshipId = sheet.GetAttribute("id", officeRelNs);
            if (string.IsNullOrEmpty(relationshipId)) return null;

            var relManager = new XmlNamespaceManager(relationships.NameTable);
            relManager.AddNamespace("r", "http://schemas.openxmlformats.org/package/2006/relationships");
            var relationship = relationships.SelectSingleNode("//r:Relationship[@Id='" + relationshipId + "']", relManager) as XmlElement;
            if (relationship == null) return null;
            var worksheetPath = relationship.GetAttribute("Target").Replace('\\', '/').TrimStart('/');
            if (!worksheetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) worksheetPath = "xl/" + worksheetPath;
            var worksheet = LoadXml(archive, worksheetPath);
            return worksheet == null ? null : ReadWorksheetRows(worksheet, sharedStrings);
        }

        private static SupplierData ReadSupplierData(ZipArchive archive, XmlDocument workbook, XmlDocument relationships, List<string> sharedStrings)
        {
            var result = new SupplierData();
            var rows = ReadNamedWorksheetRows(archive, workbook, relationships, sharedStrings, "Leveranciers");
            if (rows == null) return result;
            Dictionary<string, int> headers = null;
            foreach (var row in rows)
            {
                var candidateHeaders = HeaderMap(row);
                if (candidateHeaders.ContainsKey("Leverancier-ID") && candidateHeaders.ContainsKey("Naam"))
                {
                    headers = candidateHeaders;
                    continue;
                }
                if (candidateHeaders.ContainsKey("Voorkeur-ID") && candidateHeaders.ContainsKey("Leverancier-ID") && candidateHeaders.ContainsKey("Rang"))
                {
                    headers = candidateHeaders;
                    continue;
                }
                if (headers == null) continue;
                if (headers.ContainsKey("Naam"))
                {
                    var supplierId = Cell(row, headers, "Leverancier-ID");
                    var name = Cell(row, headers, "Naam");
                    if (!string.IsNullOrWhiteSpace(supplierId) && !string.IsNullOrWhiteSpace(name)) result.Names[supplierId] = name;
                    continue;
                }
                if (!headers.ContainsKey("Voorkeur-ID")) continue;
                int rank;
                if (!int.TryParse(Cell(row, headers, "Rang"), NumberStyles.Integer, CultureInfo.InvariantCulture, out rank)) continue;
                var preference = new SupplierPreference
                {
                    PreferenceId = Cell(row, headers, "Voorkeur-ID"),
                    Category = Cell(row, headers, "Categorie"),
                    Subcategory = Cell(row, headers, "Subcategorie"),
                    SupplierId = Cell(row, headers, "Leverancier-ID"),
                    Rank = rank,
                    ScopeType = Cell(row, headers, "Scope-type"),
                    ScopeId = Cell(row, headers, "Scope-ID"),
                    Status = Cell(row, headers, "Status")
                };
                if (!string.IsNullOrWhiteSpace(preference.PreferenceId)) result.Preferences.Add(preference);
            }
            return result;
        }

        private static List<SupplierPreference> SupplierPreferences()
        {
            PricingEstimates();
            return supplierPreferences ?? new List<SupplierPreference>();
        }

        private static int SupplierPreferenceRank(List<SupplierPreference> preferences, string productId, string category, string subcategory, string supplierId)
        {
            if (string.IsNullOrWhiteSpace(supplierId)) return 0;
            var best = int.MaxValue;
            foreach (var preference in preferences ?? new List<SupplierPreference>())
            {
                if (!string.Equals(preference.Status, "Actief", StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(preference.SupplierId, supplierId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(preference.Category, category, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(preference.Subcategory) && !string.Equals(preference.Subcategory, subcategory, StringComparison.OrdinalIgnoreCase)) continue;
                var allProducts = string.Equals(preference.ScopeType, "Alle producten", StringComparison.OrdinalIgnoreCase);
                var thisProduct = string.Equals(preference.ScopeType, "Product", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(preference.ScopeId, productId, StringComparison.OrdinalIgnoreCase);
                if (!allProducts && !thisProduct) continue;
                best = Math.Min(best, preference.Rank);
            }
            return best;
        }

        private static void AddPricingEstimate(Dictionary<string, List<PricingEstimate>> target, string key, PricingEstimate estimate)
        {
            List<PricingEstimate> values;
            if (!target.TryGetValue(key, out values))
            {
                values = new List<PricingEstimate>();
                target[key] = values;
            }
            values.Add(estimate);
        }

        private static XmlDocument LoadXml(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null) return null;
            var document = new XmlDocument();
            using (var stream = entry.Open()) document.Load(stream);
            return document;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var result = new List<string>();
            var document = LoadXml(archive, "xl/sharedStrings.xml");
            if (document == null) return result;
            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            foreach (XmlNode item in document.SelectNodes("//m:si", manager))
            {
                var value = new StringBuilder();
                foreach (XmlNode text in item.SelectNodes(".//m:t", manager)) value.Append(text.InnerText);
                result.Add(value.ToString());
            }
            return result;
        }

        private static List<Dictionary<int, string>> ReadWorksheetRows(XmlDocument worksheet, List<string> sharedStrings)
        {
            var result = new List<Dictionary<int, string>>();
            var manager = new XmlNamespaceManager(worksheet.NameTable);
            manager.AddNamespace("m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            foreach (XmlNode rowNode in worksheet.SelectNodes("//m:sheetData/m:row", manager))
            {
                var row = new Dictionary<int, string>();
                foreach (XmlElement cell in rowNode.SelectNodes("m:c", manager))
                {
                    var column = ColumnIndex(cell.GetAttribute("r"));
                    var type = cell.GetAttribute("t");
                    string value;
                    if (type == "inlineStr")
                    {
                        var text = new StringBuilder();
                        foreach (XmlNode node in cell.SelectNodes("m:is//m:t", manager)) text.Append(node.InnerText);
                        value = text.ToString();
                    }
                    else
                    {
                        var valueNode = cell.SelectSingleNode("m:v", manager);
                        value = valueNode == null ? "" : valueNode.InnerText;
                        int sharedIndex;
                        if (type == "s" && int.TryParse(value, out sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count) value = sharedStrings[sharedIndex];
                    }
                    row[column] = value;
                }
                result.Add(row);
            }
            return result;
        }

        private static int ColumnIndex(string cellReference)
        {
            var result = 0;
            foreach (var ch in cellReference ?? "")
            {
                if (ch < 'A' || ch > 'Z') break;
                result = result * 26 + (ch - 'A' + 1);
            }
            return result;
        }

        private static Dictionary<string, int> HeaderMap(Dictionary<int, string> row)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row)
            {
                if (!string.IsNullOrWhiteSpace(cell.Value)) result[cell.Value.Trim()] = cell.Key;
            }
            return result;
        }

        private static string Cell(Dictionary<int, string> row, Dictionary<string, int> headers, string name)
        {
            int column;
            string value;
            return headers.TryGetValue(name, out column) && row.TryGetValue(column, out value) ? value : "";
        }

        private static string NormalizeExcelDate(string value)
        {
            double serial;
            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out serial) && serial > 20000 && serial < 100000)
            {
                return DateTime.FromOADate(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            return value ?? "";
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
            if (request != null && string.Equals(request.Product, "machinebasis", StringComparison.OrdinalIgnoreCase)) return "Parametrische machinebasis";
            if (request != null && string.Equals(request.Product, "robotcel", StringComparison.OrdinalIgnoreCase)) return "Robot cel";
            if (request != null && string.Equals(request.Product, "materiaalwagen", StringComparison.OrdinalIgnoreCase)) return "Modulaire materiaal- en gereedschapswagen";
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            if (request != null && string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)) return "Workstation";
            if (request != null && string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)) return "Workstation ontwikkelvariant";
            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase)) return "Werkbank met kastonderbouw";
            if (request != null && string.Equals(request.Product, "shipping_box", StringComparison.OrdinalIgnoreCase)) return "Shipping box / clipkist";
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
            public string Key { get; set; }
            public PricingEstimate Estimate { get; set; }
            public string Name { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal MarkupPercent { get; set; }
            public string Note { get; set; }
        }

        private sealed class PricingEstimate
        {
            public string OfferId { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public string Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal MarkupPercent { get; set; }
            public string Supplier { get; set; }
            public string SupplierId { get; set; }
            public string SupplierArticleCode { get; set; }
            public string OrderUrl { get; set; }
            public string PriceDate { get; set; }
            public string PriceStatus { get; set; }
            public string ImageId { get; set; }
            public string ImageSourceUrl { get; set; }
            public string LocalImagePath { get; set; }
            public string Note { get; set; }
            public int SupplierRank { get; set; }
        }

        private sealed class SupplierPreference
        {
            public string PreferenceId { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public string SupplierId { get; set; }
            public int Rank { get; set; }
            public string ScopeType { get; set; }
            public string ScopeId { get; set; }
            public string Status { get; set; }
        }

        private sealed class SupplierData
        {
            public Dictionary<string, string> Names { get; private set; }
            public List<SupplierPreference> Preferences { get; private set; }

            public SupplierData()
            {
                Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Preferences = new List<SupplierPreference>();
            }
        }
    }

}
