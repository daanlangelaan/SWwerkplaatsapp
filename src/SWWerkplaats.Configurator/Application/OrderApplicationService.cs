using System;
using System.Collections.Generic;
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

            var orderId = "SW-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var orderFolder = repository.CreateOrderFolder(orderId);
            var output = production.GenerateOrderFiles(request, orderFolder);
            var price = pricing.Calculate(output.Model, output.NestingPlan);
            var record = new PortalOrderRecord
            {
                OrderId = orderId,
                Status = PortalOrderStatus.TeControleren,
                CreatedAt = DateTime.Now.ToString("s"),
                ProductName = ProductName(request),
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                PriceExVat = price.ExVat,
                PriceIncVat = price.IncVat,
                OutputFolder = orderFolder
            };

            foreach (var file in output.Files)
            {
                record.Files.Add(file);
            }

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

            record.Status = PortalOrderStatus.InFreeswachtrij;
            record.QueueFolder = repository.CopyOrderToQueue(record);
            repository.SaveRecord(record);
            return record;
        }

        private static string ProductName(PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            return "Cabinet";
        }
    }
}
