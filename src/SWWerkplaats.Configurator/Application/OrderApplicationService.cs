using System;
using System.Collections.Generic;
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

            var orderId = "SW-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var orderFolder = repository.CreateOrderFolder(orderId);
            var output = production.GenerateOrderFiles(request, orderFolder);
            var price = pricing.Calculate(output.Model, output.NestingPlan);
            OrderWorkflowPolicy.EnsureCanTransition(OrderWorkflowStatus.Nieuw, OrderWorkflowStatus.TeControleren, OrderWorkflowRole.System);
            var record = new PortalOrderRecord
            {
                OrderId = orderId,
                Status = OrderWorkflowStatus.TeControleren,
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
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            return "Cabinet";
        }
    }
}
