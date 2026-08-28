using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductCatalogItem
    {
        public string Product { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string CardImageUrl { get; set; }
        public string CardImageAlt { get; set; }
        public string CardImageStatus { get; set; }
        public double DefaultWidthMm { get; set; }
        public double DefaultDepthMm { get; set; }
        public double DefaultHeightMm { get; set; }
        public int DefaultQuantity { get; set; }
        public int DefaultUnitCount { get; set; }
        public int DefaultShelfCount { get; set; }
        public int DefaultDrawerCount { get; set; }
        public string DefaultShelfStartMode { get; set; }
        public bool SupportsProfiles { get; set; }
        public bool SupportsDrawers { get; set; }
        public bool SupportsDoors { get; set; }
        public bool SupportsBackPanel { get; set; }
        public bool SupportsAdjustableShelfHoles { get; set; }
        public ProductInputConstraint[] InputConstraints { get; set; }
        public string[] AllowedSheetMaterialIds { get; set; }
        public string[] AllowedProfileMaterialIds { get; set; }
        public string DefaultSheetMaterialId { get; set; }
        public string DefaultProfileMaterialId { get; set; }
        public ProductConfigurationInput[] ConfigurationInputs { get; set; }
        public string[] ConfigurationSections { get; set; }
        public bool CanConfigure { get; set; }
        public string[] MissingConfigurationData { get; set; }
        public bool ProductionReleased { get; set; }
        public string[] ConceptExportOutputs { get; set; }
        public string[] OpenReleaseItems { get; set; }
        public string ConceptExportNote { get; set; }
    }

    public sealed class ProductInputConstraint
    {
        public string InputId { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public string Unit { get; set; }
        public string SourceRuleId { get; set; }
    }

    public sealed class ProductConfigurationOption
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }

    public sealed class ProductConfigurationInput
    {
        public string ContractId { get; set; }
        public string Section { get; set; }
        public int Order { get; set; }
        public string InputId { get; set; }
        public string RequestField { get; set; }
        public string Label { get; set; }
        public string InputType { get; set; }
        public string DefaultValue { get; set; }
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
        public double? Step { get; set; }
        public string Unit { get; set; }
        public ProductConfigurationOption[] Options { get; set; }
        public bool Required { get; set; }
        public string Status { get; set; }
        public bool BlocksConfiguration { get; set; }
        public string Source { get; set; }
        public string MissingReason { get; set; }
    }

    public sealed class ProductCatalogApplicationService
    {
        private readonly ProductRegistry products;

        public ProductCatalogApplicationService()
            : this(new ProductRegistry())
        {
        }

        public ProductCatalogApplicationService(ProductRegistry products)
        {
            this.products = products ?? new ProductRegistry();
        }

        public ProductCatalogItem[] ListProducts()
        {
            var masterData = MasterDataRuntimeCatalog.LoadRequired();
            var rows = masterData.Records("products");
            var rules = masterData.Records("productRules");
            var inputContracts = masterData.Records("productInputContracts");
            var materials = masterData.Records("materials");
            return products.CatalogItems().Select(item =>
            {
                var row = rows.FirstOrDefault(record => string.Equals(
                    MasterDataRuntimeCatalog.Value(record, "Product-ID"), item.Product, StringComparison.OrdinalIgnoreCase));
                if (row == null) throw new InvalidOperationException("Product ontbreekt in runtime-masterdata: " + item.Product);
                item.Name = Required(row, "Naam");
                item.Category = Required(row, "Categorie-ID");
                item.Status = Required(row, "Status");
                item.CardImageStatus = Required(row, "Kaartafbeelding-status");
                if (string.Equals(item.CardImageStatus, "Beschikbaar", StringComparison.OrdinalIgnoreCase))
                {
                    item.CardImageUrl = Required(row, "Kaartafbeelding-pad");
                    item.CardImageAlt = Required(row, "Kaartafbeelding-alt");
                }
                item.DefaultWidthMm = RequiredDouble(row, "Breedte default mm");
                item.DefaultDepthMm = RequiredDouble(row, "Diepte default mm");
                item.DefaultHeightMm = RequiredDouble(row, "Hoogte default mm");
                item.DefaultQuantity = RequiredPositiveInt(row, "Aantal stuks default");
                item.DefaultUnitCount = RequiredInt(row, "Units");
                item.DefaultShelfCount = RequiredInt(row, "Legplanken");
                item.DefaultDrawerCount = RequiredInt(row, "Laden");
                item.DefaultShelfStartMode = Required(row, "Startmodus");
                item.SupportsProfiles = IsYes(row, "Profielen");
                item.SupportsDrawers = IsYes(row, "Laden ondersteund");
                item.SupportsDoors = IsYes(row, "Deuren");
                item.SupportsBackPanel = IsYes(row, "Achterwand");
                var resolvedRules = ResolveProductRules(item.Product, rows, rules).ToArray();
                ApplyConfigurationContract(
                    item,
                    row,
                    resolvedRules,
                    ResolveProductInputs(item.Product, rows, inputContracts).ToArray(),
                    materials);
                var release = new ProductReleaseContractService().LoadRequired(item.Product);
                item.ProductionReleased = release.ProductionReleased;
                item.ConceptExportOutputs = release.ConceptExportOutputs;
                item.OpenReleaseItems = release.OpenReleaseItems;
                item.ConceptExportNote = release.ConceptExportNote;
                return item;
            }).ToArray();
        }

        private static void ApplyConfigurationContract(
            ProductCatalogItem item,
            Dictionary<string, string> productRow,
            IList<Dictionary<string, string>> rules,
            IList<Dictionary<string, string>> inputRows,
            IList<Dictionary<string, string>> materials)
        {
            var missing = new List<string>();
            var parameterRule = rules.LastOrDefault(rule =>
                string.Equals(MasterDataRuntimeCatalog.Value(rule, "Domein"), "Parameters", StringComparison.OrdinalIgnoreCase)
                && string.Equals(MasterDataRuntimeCatalog.Value(rule, "Parametertype"), "Parametercontract", StringComparison.OrdinalIgnoreCase));
            item.InputConstraints = parameterRule == null ? new ProductInputConstraint[0] : ParsePrimaryDimensions(parameterRule).ToArray();
            if (item.InputConstraints.Length == 0) missing.Add("Parametergrenzen ontbreken in productmasterdata.");

            item.AllowedSheetMaterialIds = MaterialIds(rules, "Materiaalkeuze");
            item.AllowedProfileMaterialIds = MaterialIds(rules, "Profielkeuze");
            item.DefaultSheetMaterialId = item.AllowedSheetMaterialIds.FirstOrDefault();
            item.DefaultProfileMaterialId = item.AllowedProfileMaterialIds.Length == 1 ? item.AllowedProfileMaterialIds[0] : null;
            if (!item.SupportsProfiles && item.AllowedSheetMaterialIds.Length == 0) missing.Add("Standaard plaatmateriaal ontbreekt in productmasterdata.");
            if (item.SupportsProfiles && item.AllowedProfileMaterialIds.Length == 0) missing.Add("Standaard profielmateriaal ontbreekt in productmasterdata.");
            item.ConfigurationInputs = inputRows
                .Where(row => IsYes(row, "Actief"))
                .Select(row => ParseInput(row, productRow, item, materials, rules))
                .OrderBy(input => input.Section, StringComparer.OrdinalIgnoreCase)
                .ThenBy(input => input.Order)
                .ToArray();
            item.ConfigurationSections = item.ConfigurationInputs.Select(input => input.Section)
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var input in item.ConfigurationInputs)
            {
                if (input.BlocksConfiguration)
                    missing.Add(string.IsNullOrWhiteSpace(input.MissingReason)
                        ? input.ContractId + " blokkeert configuratie." : input.MissingReason);
                if (!input.Required) continue;
                if (string.Equals(input.InputType, "select", StringComparison.OrdinalIgnoreCase) && input.Options.Length == 0)
                    missing.Add(input.Label + ": toegestane keuzes ontbreken in productmasterdata.");
                if (!string.Equals(input.InputType, "checkbox", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(input.InputType, "select", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(input.DefaultValue))
                    missing.Add(input.Label + ": standaardwaarde ontbreekt in productmasterdata.");
            }
            item.CanConfigure = missing.Count == 0;
            item.MissingConfigurationData = missing.ToArray();
        }

        private static IEnumerable<Dictionary<string, string>> ResolveProductRules(
            string productId,
            IList<Dictionary<string, string>> products,
            IList<Dictionary<string, string>> rules)
        {
            var chain = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = productId;
            while (!string.IsNullOrWhiteSpace(current) && seen.Add(current))
            {
                chain.Insert(0, current);
                var row = products.FirstOrDefault(product => string.Equals(
                    MasterDataRuntimeCatalog.Value(product, "Product-ID"), current, StringComparison.OrdinalIgnoreCase));
                current = row == null ? null : MasterDataRuntimeCatalog.Value(row, "Basisproduct-ID").Trim();
            }
            return chain.SelectMany(id => rules.Where(rule => string.Equals(
                MasterDataRuntimeCatalog.Value(rule, "Product-ID"), id, StringComparison.OrdinalIgnoreCase)));
        }

        private static IEnumerable<Dictionary<string, string>> ResolveProductInputs(
            string productId,
            IList<Dictionary<string, string>> products,
            IList<Dictionary<string, string>> inputRows)
        {
            var chain = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = productId;
            while (!string.IsNullOrWhiteSpace(current) && seen.Add(current))
            {
                chain.Add(current);
                var row = products.FirstOrDefault(product => string.Equals(
                    MasterDataRuntimeCatalog.Value(product, "Product-ID"), current, StringComparison.OrdinalIgnoreCase));
                current = row == null ? null : MasterDataRuntimeCatalog.Value(row, "Basisproduct-ID").Trim();
            }
            chain.Reverse();
            var resolved = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in chain)
            foreach (var row in inputRows.Where(input => string.Equals(
                MasterDataRuntimeCatalog.Value(input, "Product-ID"), id, StringComparison.OrdinalIgnoreCase)))
                resolved[MasterDataRuntimeCatalog.Value(row, "Request-veld")] = row;
            return resolved.Values;
        }

        private static ProductConfigurationInput ParseInput(
            Dictionary<string, string> row,
            Dictionary<string, string> productRow,
            ProductCatalogItem product,
            IList<Dictionary<string, string>> materials,
            IList<Dictionary<string, string>> rules)
        {
            var optionSource = MasterDataRuntimeCatalog.Value(row, "Optiebron").Trim();
            var options = ParseOptions(MasterDataRuntimeCatalog.Value(row, "Opties"));
            if (string.Equals(optionSource, "AllowedSheetMaterialIds", StringComparison.OrdinalIgnoreCase))
                options = MaterialOptions(product.AllowedSheetMaterialIds, materials);
            else if (string.Equals(optionSource, "AllowedProfileMaterialIds", StringComparison.OrdinalIgnoreCase))
                options = MaterialOptions(product.AllowedProfileMaterialIds, materials);
            else if (optionSource.StartsWith("RuleMaterialIds:", StringComparison.OrdinalIgnoreCase))
                options = MaterialOptions(MaterialIds(rules, optionSource.Substring("RuleMaterialIds:".Length)), materials);

            return new ProductConfigurationInput
            {
                ContractId = Required(row, "Invoercontract-ID"),
                Section = Required(row, "Sectie"),
                Order = RequiredInt(row, "Volgorde"),
                InputId = Required(row, "Invoer-ID"),
                RequestField = Required(row, "Request-veld"),
                Label = Required(row, "Label"),
                InputType = Required(row, "Invoertype"),
                DefaultValue = ResolveInputValue(MasterDataRuntimeCatalog.Value(row, "Standaardbron"), MasterDataRuntimeCatalog.Value(row, "Standaardwaarde"), productRow, product, rules),
                Minimum = OptionalDouble(MasterDataRuntimeCatalog.Value(row, "Minimum"), productRow),
                Maximum = OptionalDouble(MasterDataRuntimeCatalog.Value(row, "Maximum"), productRow),
                Step = OptionalDouble(MasterDataRuntimeCatalog.Value(row, "Stap"), productRow),
                Unit = MasterDataRuntimeCatalog.Value(row, "Eenheid"),
                Options = options,
                Required = IsYes(row, "Vereist"),
                Status = Required(row, "Status"),
                BlocksConfiguration = IsYes(row, "Blokkeert configuratie"),
                Source = MasterDataRuntimeCatalog.Value(row, "Bron"),
                MissingReason = MasterDataRuntimeCatalog.Value(row, "Toelichting")
            };
        }

        private static ProductConfigurationOption[] ParseOptions(string value)
        {
            return (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(entry => entry.Split(new[] { '|' }, 2))
                .Where(parts => parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                .Select(parts => new ProductConfigurationOption
                {
                    Value = parts[0].Trim(),
                    Label = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : parts[0].Trim()
                }).ToArray();
        }

        private static ProductConfigurationOption[] MaterialOptions(string[] ids, IList<Dictionary<string, string>> materials)
        {
            return (ids ?? new string[0]).Select(id =>
            {
                var material = materials.FirstOrDefault(row => string.Equals(
                    MasterDataRuntimeCatalog.Value(row, "Materiaal-ID"), id, StringComparison.OrdinalIgnoreCase));
                var customerName = material == null ? string.Empty : MasterDataRuntimeCatalog.Value(material, "Klantnaam").Trim();
                var family = material == null ? id : MasterDataRuntimeCatalog.Value(material, "Familie").Trim();
                var quality = material == null ? string.Empty : MasterDataRuntimeCatalog.Value(material, "Kwaliteit/type").Trim();
                return new ProductConfigurationOption { Value = id, Label = string.IsNullOrWhiteSpace(customerName) ? (family + " " + quality).Trim() : customerName };
            }).ToArray();
        }

        private static string ResolveInputValue(string source, string literal, Dictionary<string, string> productRow,
            ProductCatalogItem product, IList<Dictionary<string, string>> rules)
        {
            source = (source ?? string.Empty).Trim();
            if (source.StartsWith("Products.", StringComparison.OrdinalIgnoreCase))
                return MasterDataRuntimeCatalog.Value(productRow, source.Substring("Products.".Length)).Trim();
            if (string.Equals(source, "AllowedSheetMaterialIds:first", StringComparison.OrdinalIgnoreCase))
                return product.AllowedSheetMaterialIds.FirstOrDefault() ?? string.Empty;
            if (string.Equals(source, "AllowedProfileMaterialIds:first", StringComparison.OrdinalIgnoreCase))
                return product.AllowedProfileMaterialIds.FirstOrDefault() ?? string.Empty;
            if (source.StartsWith("RuleMaterialIds:", StringComparison.OrdinalIgnoreCase)
                && source.EndsWith(":first", StringComparison.OrdinalIgnoreCase))
            {
                var parameterType = source.Substring("RuleMaterialIds:".Length,
                    source.Length - "RuleMaterialIds:".Length - ":first".Length);
                return MaterialIds(rules, parameterType).FirstOrDefault() ?? string.Empty;
            }
            return (literal ?? string.Empty).Trim();
        }

        private static double? OptionalDouble(string value, Dictionary<string, string> productRow)
        {
            value = (value ?? string.Empty).Trim();
            if (value.StartsWith("@Products.", StringComparison.OrdinalIgnoreCase))
                value = MasterDataRuntimeCatalog.Value(productRow, value.Substring("@Products.".Length)).Trim();
            if (value.Length == 0) return null;
            double result;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                throw new InvalidOperationException("Ongeldige numerieke waarde in portaal-invoercontract: " + value + ".");
            return result;
        }

        private static IEnumerable<ProductInputConstraint> ParsePrimaryDimensions(Dictionary<string, string> rule)
        {
            var values = MasterDataRuntimeCatalog.Value(rule, "Waarde").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var units = MasterDataRuntimeCatalog.Value(rule, "Eenheid").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var inputs = new[] { "widthMm", "depthMm", "heightMm" };
            for (var index = 0; index < Math.Min(inputs.Length, values.Length); index++)
            {
                var bounds = values[index].Split(new[] { ".." }, StringSplitOptions.None);
                double minimum, maximum;
                if (bounds.Length != 2
                    || !double.TryParse(bounds[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minimum)
                    || !double.TryParse(bounds[1], NumberStyles.Float, CultureInfo.InvariantCulture, out maximum))
                    throw new InvalidOperationException("Ongeldig parametercontract in " + MasterDataRuntimeCatalog.Value(rule, "Regel-ID") + ".");
                yield return new ProductInputConstraint
                {
                    InputId = inputs[index],
                    Minimum = minimum,
                    Maximum = maximum,
                    Unit = index < units.Length ? units[index].Trim() : string.Empty,
                    SourceRuleId = MasterDataRuntimeCatalog.Value(rule, "Regel-ID")
                };
            }
        }

        private static string[] MaterialIds(IList<Dictionary<string, string>> rules, string parameterType)
        {
            var matchingRules = rules.Where(rule => string.Equals(MasterDataRuntimeCatalog.Value(rule, "Domein"), "Materiaal", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(MasterDataRuntimeCatalog.Value(rule, "Parametertype"), parameterType, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(MasterDataRuntimeCatalog.Value(rule, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matchingRules.Length == 0) return new string[0];

            var mostDerivedProductId = MasterDataRuntimeCatalog.Value(matchingRules[matchingRules.Length - 1], "Product-ID");
            return matchingRules
                .Where(rule => string.Equals(MasterDataRuntimeCatalog.Value(rule, "Product-ID"), mostDerivedProductId, StringComparison.OrdinalIgnoreCase))
                .SelectMany(rule => MasterDataRuntimeCatalog.Value(rule, "Referentie-ID(s)").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.Trim()).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string Required(System.Collections.Generic.Dictionary<string, string> row, string field)
        {
            var value = MasterDataRuntimeCatalog.Value(row, field).Trim();
            if (value.Length == 0) throw new InvalidOperationException("Productmasterdata mist " + field + ".");
            return value;
        }

        private static double RequiredDouble(System.Collections.Generic.Dictionary<string, string> row, string field)
        {
            double value;
            if (!double.TryParse(Required(row, field), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Productmasterdata bevat geen geldige waarde voor " + field + ".");
            return value;
        }

        private static int RequiredInt(System.Collections.Generic.Dictionary<string, string> row, string field)
        {
            int value;
            if (!int.TryParse(Required(row, field), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Productmasterdata bevat geen geldige waarde voor " + field + ".");
            return value;
        }

        private static int RequiredPositiveInt(System.Collections.Generic.Dictionary<string, string> row, string field)
        {
            var value = RequiredInt(row, field);
            if (value < 1)
                throw new InvalidOperationException("Productmasterdata bevat geen positieve waarde voor " + field + ".");
            return value;
        }

        private static bool IsYes(System.Collections.Generic.Dictionary<string, string> row, string field)
        {
            return string.Equals(MasterDataRuntimeCatalog.Value(row, field).Trim(), "Ja", StringComparison.OrdinalIgnoreCase);
        }
    }
}
