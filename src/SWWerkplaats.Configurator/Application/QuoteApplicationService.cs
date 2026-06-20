using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class QuoteApplicationService
    {
        private readonly ProductionOutputService production;
        private readonly PortalPricingService pricing;
        private readonly PortalVisualizationService visualization;
        private readonly PortalAssembly3DService assembly3D;

        public QuoteApplicationService()
            : this(new ProductionOutputService(), new PortalPricingService(), new PortalVisualizationService(), new PortalAssembly3DService())
        {
        }

        public QuoteApplicationService(
            ProductionOutputService production,
            PortalPricingService pricing,
            PortalVisualizationService visualization,
            PortalAssembly3DService assembly3D)
        {
            this.production = production ?? new ProductionOutputService();
            this.pricing = pricing ?? new PortalPricingService();
            this.visualization = visualization ?? new PortalVisualizationService();
            this.assembly3D = assembly3D ?? new PortalAssembly3DService();
        }

        public PortalQuoteResponse BuildQuote(PortalQuoteRequest request)
        {
            var preview = production.BuildPreview(request);
            var price = pricing.Calculate(preview.Model, preview.NestingPlan);
            var response = new PortalQuoteResponse
            {
                QuoteId = "Q-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                ProductName = ProductName(request),
                Summary = Summary(preview.Model, request),
                PriceExVat = price.ExVat,
                Material = price.Material,
                Hardware = price.Hardware,
                Machine = price.Machine,
                Labour = price.Labour,
                Margin = price.Margin,
                Vat = price.Vat,
                PriceIncVat = price.IncVat,
                LeadTime = "Indicatie: 5-10 werkdagen na controle",
                SheetPartCount = CountSheets(preview.Model),
                ProfilePartCount = CountProfiles(preview.Model),
                PreviewSvg = visualization.BuildProductSvg(preview.Model, request),
                NestingSvg = preview.NestingSvg
            };

            response.Files.Add("BOM.csv");
            response.Files.Add("CAM-operaties.csv");
            response.Files.Add("Nesting\\NestVisualisatie.svg");
            response.Files.Add("Nesting\\*.tap na interne vrijgave");
            foreach (var part in assembly3D.Build(preview.Model, request))
            {
                response.Assembly3D.Add(part);
            }

            return response;
        }

        private static string ProductName(PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            if (request != null && string.Equals(request.Product, "vakjeskast", StringComparison.OrdinalIgnoreCase)) return "Vakjeskast";
            return "Cabinet";
        }

        private static string Summary(WorkbenchModel model, PortalQuoteRequest request)
        {
            var quantity = request == null ? 1 : Math.Max(1, request.Quantity);
            var prefix = quantity > 1 ? quantity.ToString() + " stuks - " : "";
            return prefix + model.ProjectName + ": " + CountSheets(model) + " plaatdelen, " + CountProfiles(model) + " profieldelen, " + model.Hardware.Count + " beslagregels.";
        }

        private static int CountSheets(WorkbenchModel model)
        {
            var count = 0;
            foreach (var sheet in model.Sheets)
            {
                count += Math.Max(1, sheet.Quantity);
            }

            return count;
        }

        private static int CountProfiles(WorkbenchModel model)
        {
            var count = 0;
            foreach (var profile in model.Profiles)
            {
                count += Math.Max(1, profile.Quantity);
            }

            return count;
        }
    }
}
