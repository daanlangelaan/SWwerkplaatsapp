using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;
using SWWerkplaats.Configurator.Manufacturing;

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
            OrderApplicationService.ApplyDeliveryContract(request);
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
                DeliveryForm = request.DeliveryForm,
                ReceiptMethod = request.ReceiptMethod,
                DeliveryPriceNote = DeliveryPriceNote(request),
                SheetPartCount = CountSheets(preview.Model),
                ProfilePartCount = CountProfiles(preview.Model),
                PreviewSvg = visualization.BuildProductSvg(preview.Model, request),
                NestingSvg = preview.NestingSvg
            };
            response.StructuralCalculation = preview.Model.StructuralCalculation;
            if (response.StructuralCalculation != null) response.Files.Add("Constructieberekening.txt");
            if (preview.Model.Profiles.Count > 0)
            {
                var profileConfigurationService = new ProfileProjectConfigurationService();
                var profileConfiguration = profileConfigurationService.Deserialize(
                    profileConfigurationService.Serialize(profileConfigurationService.Build(preview.Model)));
                var profileSequence = profileConfigurationService.ToProductionSequence(profileConfiguration);
                response.Files.Add("Profielconfiguratie.json");
                try
                {
                    var tapRows = new ProfileTapWorklistService().Build(profileSequence);
                    response.ProfileMachiningControlSvg = new ProfileMachiningVisualSvgExporter().Generate(profileSequence, tapRows);
                    response.Files.Add("Profieltappen-werkplaatslijst.xlsx");
                    response.Files.Add("Profielbewerkingen-visuele-controle.svg");
                }
                catch (InvalidOperationException ex)
                {
                    if (!string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)
                        || !ex.Message.Contains("expliciete kernboring K1..Kn")) throw;
                    // Een klantpreview is geen CAM-vrijgave. De losse Werktafel mag renderen en calculeren,
                    // terwijl de niet-vrijgegeven taplijst uitsluitend bij productie-export geblokkeerd blijft.
                }
            }
            response.AssemblyInstructions = new AssemblyInstructionPlanningService().Build(preview.Model);

            response.Files.Add("BOM.csv");
            response.Files.Add("CAM-operaties.csv");
            response.Files.Add("Nesting\\NestVisualisatie.svg");
            response.Files.Add("Nesting\\*.tap na interne vrijgave");
            foreach (var part in assembly3D.Build(preview.Model, request))
            {
                response.Assembly3D.Add(part);
            }
            response.Motion = new PortalMotionContractService().Build(preview.Model, request, response.Assembly3D);

            return response;
        }

        private static string DeliveryPriceNote(PortalQuoteRequest request)
        {
            var open = new System.Collections.Generic.List<string>();
            if (string.Equals(request.DeliveryForm, "gemonteerd", StringComparison.OrdinalIgnoreCase)) open.Add("montage");
            if (string.Equals(request.ReceiptMethod, "verzenden", StringComparison.OrdinalIgnoreCase)) open.Add("verpakking en verzending");
            var description = open.Count == 2 ? "montage, verpakking en verzending" : string.Join(" en ", open);
            return open.Count == 0
                ? "Montage en verzending zijn niet van toepassing op deze keuze."
                : "De getoonde productprijs is exclusief " + description + "; Bedrijfsbeheer stelt deze kosten vast.";
        }

        private static string ProductName(PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            if (request != null && string.Equals(request.Product, "machinebasis", StringComparison.OrdinalIgnoreCase)) return "Parametrische machinebasis";
            if (request != null && string.Equals(request.Product, "robotcel", StringComparison.OrdinalIgnoreCase)) return "Robot cel";
            if (request != null && string.Equals(request.Product, "lineaire_robotcel", StringComparison.OrdinalIgnoreCase)) return "Lineaire robotcel";
            if (request != null && string.Equals(request.Product, "materiaalwagen", StringComparison.OrdinalIgnoreCase)) return "Modulaire materiaal- en gereedschapswagen";
            if (request != null && string.Equals(request.Product, "sim_rig_4080", StringComparison.OrdinalIgnoreCase)) return "Modulaire sim-racing-rig 40x80";
            if (request != null && string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)) return "Workstation";
            if (request != null && string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)) return "Workstation ontwikkelvariant";
            if (request != null && string.Equals(request.Product, "hoogteverstelbare_werktafel", StringComparison.OrdinalIgnoreCase)) return "Hoogteverstelbare werktafel";
            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase)) return "Werkbank met kastonderbouw";
            if (request != null && string.Equals(request.Product, "vakjeskast", StringComparison.OrdinalIgnoreCase)) return "Vakjeskast";
            if (request != null && string.Equals(request.Product, "shipping_box", StringComparison.OrdinalIgnoreCase)) return "Shipping box / clipkist";
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
