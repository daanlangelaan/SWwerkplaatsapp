using System;
using System.Collections.Generic;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductReleaseContract
    {
        public string ProductId { get; set; }
        public string ProductStatus { get; set; }
        public bool ProductionReleased { get; set; }
        public string[] ConceptExportOutputs { get; set; }
        public string[] OpenReleaseItems { get; set; }
        public string ConceptExportNote { get; set; }
    }

    public sealed class ProductReleaseContractService
    {
        public ProductReleaseContract LoadRequired(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("Product-ID ontbreekt.");
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var product = catalog.Records("products").SingleOrDefault(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), productId, StringComparison.OrdinalIgnoreCase));
            if (product == null) throw new InvalidOperationException("Product ontbreekt in runtime-masterdata: " + productId);

            var rules = catalog.Records("productRules").Where(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Product-ID"), productId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase)).ToArray();
            var releaseBlock = rules.LastOrDefault(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Vrijgaveblokkade", StringComparison.OrdinalIgnoreCase)
                && IsYes(row, "Blokkeert export"));
            var conceptExport = rules.LastOrDefault(row => string.Equals(
                MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Conceptexport", StringComparison.OrdinalIgnoreCase));

            return new ProductReleaseContract
            {
                ProductId = productId,
                ProductStatus = MasterDataRuntimeCatalog.Value(product, "Status"),
                ProductionReleased = releaseBlock == null,
                ConceptExportOutputs = conceptExport == null
                    ? new string[0]
                    : Split(MasterDataRuntimeCatalog.Value(conceptExport, "Waarde")),
                OpenReleaseItems = releaseBlock == null
                    ? new string[0]
                    : Split(MasterDataRuntimeCatalog.Value(releaseBlock, "Waarde")),
                ConceptExportNote = conceptExport == null
                    ? string.Empty
                    : MasterDataRuntimeCatalog.Value(conceptExport, "Voorwaarde/uitleg")
            };
        }

        private static string[] Split(string value)
        {
            return (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static bool IsYes(Dictionary<string, string> row, string field)
        {
            return string.Equals(MasterDataRuntimeCatalog.Value(row, field), "Ja", StringComparison.OrdinalIgnoreCase);
        }
    }
}
