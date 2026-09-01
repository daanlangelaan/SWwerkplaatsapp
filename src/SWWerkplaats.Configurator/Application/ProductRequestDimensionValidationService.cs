using System;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductInputValidationException : ArgumentException
    {
        public ProductInputValidationException(string message)
            : base(message)
        {
        }
    }

    public sealed class ProductRequestDimensionValidationService
    {
        private static readonly Lazy<ProductCatalogItem[]> CachedProducts = new Lazy<ProductCatalogItem[]>(
            () => new ProductCatalogApplicationService().ListProducts(), true);
        private readonly ProductCatalogItem[] products;

        public ProductRequestDimensionValidationService()
            : this(CachedProducts.Value)
        {
        }

        internal ProductRequestDimensionValidationService(ProductCatalogItem[] products)
        {
            this.products = products ?? new ProductCatalogItem[0];
        }

        public void Validate(PortalQuoteRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var product = products.SingleOrDefault(value => string.Equals(value.Product, request.Product, StringComparison.OrdinalIgnoreCase));
            if (product == null) throw new ProductInputValidationException("Onbekend product voor maatcontrole: " + (request.Product ?? "(leeg)") + ".");
            foreach (var constraint in product.InputConstraints ?? new ProductInputConstraint[0])
            {
                var value = DimensionValue(request, constraint.InputId);
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new ProductInputValidationException(DimensionLabel(constraint.InputId) + " is geen geldige maat.");
                if (value < constraint.Minimum)
                    throw new ProductInputValidationException(
                        DimensionLabel(constraint.InputId) + " " + F(value) + " " + constraint.Unit
                        + " is kleiner dan de minimummaat " + F(constraint.Minimum) + " " + constraint.Unit
                        + " voor " + product.Name + " (" + constraint.SourceRuleId + ").");
                if (value > constraint.Maximum)
                    throw new ProductInputValidationException(
                        DimensionLabel(constraint.InputId) + " " + F(value) + " " + constraint.Unit
                        + " is groter dan de maximummaat " + F(constraint.Maximum) + " " + constraint.Unit
                        + " voor " + product.Name + " (" + constraint.SourceRuleId + ").");
            }
        }

        private static double DimensionValue(PortalQuoteRequest request, string inputId)
        {
            if (string.Equals(inputId, "widthMm", StringComparison.OrdinalIgnoreCase)) return request.WidthMm;
            if (string.Equals(inputId, "depthMm", StringComparison.OrdinalIgnoreCase)) return request.DepthMm;
            if (string.Equals(inputId, "heightMm", StringComparison.OrdinalIgnoreCase)) return request.HeightMm;
            throw new InvalidOperationException("Onbekende primaire maatinvoer in productcontract: " + inputId + ".");
        }

        private static string DimensionLabel(string inputId)
        {
            if (string.Equals(inputId, "widthMm", StringComparison.OrdinalIgnoreCase)) return "Breedte";
            if (string.Equals(inputId, "depthMm", StringComparison.OrdinalIgnoreCase)) return "Diepte";
            if (string.Equals(inputId, "heightMm", StringComparison.OrdinalIgnoreCase)) return "Hoogte";
            return inputId;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
