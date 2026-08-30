using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class OrderApplicationService
    {
        private readonly IOrderRepository repository;
        private readonly ProductionOutputService production;
        private readonly PortalPricingService pricing;

        public OrderApplicationService(IOrderRepository repository)
            : this(repository, new ProductionOutputService(), new PortalPricingService())
        {
        }

        public OrderApplicationService(IOrderRepository repository, ProductionOutputService production, PortalPricingService pricing)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            this.repository = repository;
            this.production = production ?? new ProductionOutputService();
            this.pricing = pricing ?? new PortalPricingService();
        }

        public PortalOrderRecord CreateOrder(PortalQuoteRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            ApplyDeliveryContract(request);

            var orderId = "SW-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            var orderFolder = repository.CreateOrderFolder(orderId);
            var output = production.GenerateOrderFiles(request, orderFolder);
            var price = pricing.Calculate(output.Model, output.NestingPlan);
            OrderWorkflowPolicy.EnsureCanTransition(OrderWorkflowStatus.Nieuw, OrderWorkflowStatus.TeControleren, OrderWorkflowRole.System);
            var record = new PortalOrderRecord
            {
                ProjectId = "P-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
                SourceSiteId = string.IsNullOrWhiteSpace(request.SourceSiteId) ? "internal" : request.SourceSiteId,
                OrganizationId = string.IsNullOrWhiteSpace(request.OrganizationId) ? "unassigned" : request.OrganizationId,
                ProductId = request.Product,
                ProjectName = request.ProjectName,
                OrderId = orderId,
                Status = OrderWorkflowStatus.TeControleren,
                CreatedAt = DateTime.Now.ToString("s"),
                ProductName = ProductName(request),
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                DeliveryForm = request.DeliveryForm,
                ReceiptMethod = request.ReceiptMethod,
                AssemblyPriceStatus = string.Equals(request.DeliveryForm, "gemonteerd", StringComparison.OrdinalIgnoreCase) ? "Op aanvraag" : "Niet van toepassing",
                ShippingPriceExVat = string.Equals(request.ReceiptMethod, "afhalen", StringComparison.OrdinalIgnoreCase) ? (decimal?)0m : null,
                ShippingPriceStatus = string.Equals(request.ReceiptMethod, "afhalen", StringComparison.OrdinalIgnoreCase) ? "Niet van toepassing" : "Op aanvraag",
                PriceExVat = price.ExVat,
                PriceIncVat = price.IncVat,
                OutputFolder = orderFolder
            };

            foreach (var file in output.Files)
            {
                record.Files.Add(file);
            }

            foreach (var line in price.Lines.Where(line => line.Category != "Machine" && line.Category != "Arbeid"))
            {
                record.PurchaseLines.Add(new PortalPurchaseSnapshotLine
                {
                    StableItemId = line.Key,
                    Category = line.Category,
                    Description = line.Description,
                    RequiredQuantity = line.Quantity,
                    Unit = line.Unit,
                    PurchaseUnitPrice = line.PurchaseUnitPrice,
                    PurchaseTotal = line.PurchaseTotal,
                    SupplierId = line.SupplierId,
                    Supplier = line.Supplier,
                    SupplierArticleCode = line.SupplierArticleCode,
                    OfferId = line.OfferId,
                    PriceStatus = line.PriceStatus,
                    OrderUrl = line.OrderUrl
                });
            }

            var profileCount = output.Model.Profiles.Sum(item => Math.Max(1, item.Quantity));
            if (profileCount > 0) record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "profile-machine", Label = "Profielenmachine", ItemCount = profileCount });
            var sheetCount = output.Model.Sheets.Where(item => item.Material == null || !item.Material.IsAdditiveManufactured).Sum(item => Math.Max(1, item.Quantity));
            if (sheetCount > 0) record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "sheet-cnc", Label = "Plaat-CNC", ItemCount = sheetCount });
            var printCount = output.Model.Sheets.Where(item => item.Material != null && item.Material.IsAdditiveManufactured).Sum(item => Math.Max(1, item.Quantity));
            if (printCount > 0) record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "3d-print", Label = "3D-print", ItemCount = printCount });
            if (string.Equals(request.DeliveryForm, "gemonteerd", StringComparison.OrdinalIgnoreCase) && output.Model.AssemblyPlacements.Count > 0)
                record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "assembly", Label = "Assemblage", ItemCount = output.Model.AssemblyPlacements.Count });
            record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "dispatch", Label = "Completeren & verzending", ItemCount = Math.Max(1, request.Quantity) });

            repository.SaveOfferText(orderFolder, pricing.ExportOfferText(request, price, orderId));
            repository.SaveRequest(orderFolder, request);
            repository.SaveRecord(record);
            repository.WriteNotifications(record, request);
            return record;
        }

        public List<PortalOrderRecord> ListOrders()
        {
            return repository.ListOrders();
        }

        public PortalOrderRecord ReleaseToQueue(string orderId)
        {
            var record = repository.LoadOrder(orderId);
            if (record == null) throw new InvalidOperationException("Order niet gevonden: " + orderId);

            OrderWorkflowPolicy.EnsureCanTransition(record.Status, OrderWorkflowStatus.InFreeswachtrij, OrderWorkflowRole.Werkvoorbereider);
            record.Status = OrderWorkflowStatus.InFreeswachtrij;
            record.QueueFolder = repository.CopyOrderToQueue(record);
            repository.SaveRecord(record);
            return record;
        }

        public PortalOrderRecord ChangeStatus(string orderId, string nextStatus, OrderWorkflowRole role)
        {
            if (!OrderWorkflowPolicy.IsKnownStatus(nextStatus)) throw new InvalidOperationException("Onbekende orderstatus: " + nextStatus);
            var record = repository.LoadOrder(orderId);
            if (record == null) throw new InvalidOperationException("Order niet gevonden: " + orderId);

            OrderWorkflowPolicy.EnsureCanTransition(record.Status, nextStatus, role);
            record.Status = nextStatus;
            repository.SaveRecord(record);
            return record;
        }

        public PortalOrderRecord ChangeStatus(string orderId, string nextStatus, string role)
        {
            return ChangeStatus(orderId, nextStatus, OrderWorkflowPolicy.ParseRole(role));
        }

        private static string ProductName(PortalQuoteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Product)) return "Onbekend product";
            var product = new ProductCatalogApplicationService().ListProducts()
                .FirstOrDefault(value => string.Equals(value.Product, request.Product, StringComparison.OrdinalIgnoreCase));
            return product == null || string.IsNullOrWhiteSpace(product.Name) ? request.Product : product.Name;
        }

        public PortalOrderRecord UpdateDeliveryPricing(string orderId, PortalDeliveryPricingRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var record = repository.LoadOrder(orderId);
            if (record == null) throw new InvalidOperationException("Order niet gevonden: " + orderId);
            if (request.AssemblyPriceExVat.HasValue && request.AssemblyPriceExVat.Value < 0)
                throw new InvalidOperationException("Montageprijs mag niet negatief zijn.");
            if (request.ShippingPriceExVat.HasValue && request.ShippingPriceExVat.Value < 0)
                throw new InvalidOperationException("Verzendkosten mogen niet negatief zijn.");

            if (string.Equals(record.DeliveryForm, "gemonteerd", StringComparison.OrdinalIgnoreCase))
            {
                record.AssemblyPriceExVat = request.AssemblyPriceExVat;
                record.AssemblyPriceStatus = request.AssemblyPriceExVat.HasValue ? "Vastgesteld" : "Op aanvraag";
            }
            else
            {
                record.AssemblyPriceExVat = null;
                record.AssemblyPriceStatus = "Niet van toepassing";
            }

            if (string.Equals(record.ReceiptMethod, "verzenden", StringComparison.OrdinalIgnoreCase))
            {
                record.ShippingPriceExVat = request.ShippingPriceExVat;
                record.ShippingPriceStatus = request.ShippingPriceExVat.HasValue ? "Vastgesteld" : "Op aanvraag";
            }
            else
            {
                record.ShippingPriceExVat = 0m;
                record.ShippingPriceStatus = "Niet van toepassing";
            }
            repository.SaveRecord(record);
            return record;
        }

        internal static void ApplyDeliveryContract(PortalQuoteRequest request)
        {
            var product = new ProductCatalogApplicationService().ListProducts()
                .FirstOrDefault(value => string.Equals(value.Product, request.Product, StringComparison.OrdinalIgnoreCase));
            if (product == null) throw new InvalidOperationException("Product ontbreekt in runtime-masterdata: " + request.Product);
            request.DeliveryForm = ResolveChoice(product, "DeliveryForm", request.DeliveryForm);
            request.ReceiptMethod = ResolveChoice(product, "ReceiptMethod", request.ReceiptMethod);
        }

        private static string ResolveChoice(ProductCatalogItem product, string requestField, string requestedValue)
        {
            var input = (product.ConfigurationInputs ?? new ProductConfigurationInput[0])
                .FirstOrDefault(candidate => string.Equals(candidate.RequestField, requestField, StringComparison.OrdinalIgnoreCase));
            if (input == null) throw new InvalidOperationException("Productcontract mist " + requestField + ".");
            var value = string.IsNullOrWhiteSpace(requestedValue) ? input.DefaultValue : requestedValue.Trim();
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(input.Label + " is verplicht.");
            var option = (input.Options ?? new ProductConfigurationOption[0])
                .FirstOrDefault(candidate => string.Equals(candidate.Value, value, StringComparison.OrdinalIgnoreCase));
            if (option == null) throw new InvalidOperationException("Ongeldige keuze voor " + input.Label + ": " + value + ".");
            return option.Value;
        }
    }
}
