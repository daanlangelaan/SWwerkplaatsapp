using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ShippingBoxMasterDataSettings
    {
        private const string ProductId = "shipping_box";

        public double MinimumWidthMm { get; private set; }
        public double MaximumWidthMm { get; private set; }
        public double MinimumDepthMm { get; private set; }
        public double MaximumDepthMm { get; private set; }
        public double MinimumHeightMm { get; private set; }
        public double MaximumHeightMm { get; private set; }
        public double StockAllowanceMm { get; private set; }

        public static ShippingBoxMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var rules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductId, StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase))
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Parametercontract", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (rules.Length != 1)
                throw new InvalidOperationException("Shipping box vereist precies één parametercontract in productmasterdata.");

            var bounds = MasterDataRuntimeCatalog.Value(rules[0], "Waarde")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseBounds).ToArray();
            if (bounds.Length != 3)
                throw new InvalidOperationException("Shipping box vereist drie parametergrenzen voor binnenbreedte, binnendiepte en binnenhoogte.");

            var allowanceRules = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), ProductId, StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase))
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Plaatmarge", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (allowanceRules.Length != 1)
                throw new InvalidOperationException("Shipping box vereist precies één plaatmarge in productmasterdata.");
            double stockAllowance;
            if (!double.TryParse(MasterDataRuntimeCatalog.Value(allowanceRules[0], "Waarde"), NumberStyles.Float, CultureInfo.InvariantCulture, out stockAllowance)
                || stockAllowance < 0)
                throw new InvalidOperationException("Shipping box heeft een ongeldige plaatmarge in productmasterdata.");

            return new ShippingBoxMasterDataSettings
            {
                MinimumWidthMm = bounds[0].Item1,
                MaximumWidthMm = bounds[0].Item2,
                MinimumDepthMm = bounds[1].Item1,
                MaximumDepthMm = bounds[1].Item2,
                MinimumHeightMm = bounds[2].Item1,
                MaximumHeightMm = bounds[2].Item2,
                StockAllowanceMm = stockAllowance
            };
        }

        private static Tuple<double, double> ParseBounds(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { ".." }, StringSplitOptions.None);
            double minimum;
            double maximum;
            if (parts.Length != 2
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minimum)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out maximum)
                || minimum <= 0 || maximum < minimum)
                throw new InvalidOperationException("Ongeldige shipping-box-parametergrens: " + value + ".");
            return Tuple.Create(minimum, maximum);
        }
    }
}
