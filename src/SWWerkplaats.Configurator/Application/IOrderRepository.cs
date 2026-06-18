using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public interface IOrderRepository
    {
        string CreateOrderFolder(string orderId);
        List<PortalOrderRecord> ListOrders();
        PortalOrderRecord LoadOrder(string orderId);
        void SaveRequest(string orderFolder, PortalQuoteRequest request);
        void SaveRecord(PortalOrderRecord record);
        void SaveOfferText(string orderFolder, string contents);
        void WriteNotifications(PortalOrderRecord record, PortalQuoteRequest request);
        string CopyOrderToQueue(PortalOrderRecord record);
    }

    public sealed class FileOrderRepository : IOrderRepository
    {
        private readonly string rootFolder;
        private readonly JavaScriptSerializer serializer;

        public FileOrderRepository(string rootFolder)
        {
            this.rootFolder = rootFolder;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public string OrdersFolder
        {
            get { return Path.Combine(rootFolder, "Orders"); }
        }

        public string QueueFolder
        {
            get { return Path.Combine(rootFolder, "Freeswachtrij"); }
        }

        public string NotificationsFolder
        {
            get { return Path.Combine(rootFolder, "Notifications"); }
        }

        public string CreateOrderFolder(string orderId)
        {
            Directory.CreateDirectory(OrdersFolder);
            Directory.CreateDirectory(NotificationsFolder);
            var orderFolder = Path.Combine(OrdersFolder, ProductionOutputService.SafeFileName(orderId));
            Directory.CreateDirectory(orderFolder);
            return orderFolder;
        }

        public List<PortalOrderRecord> ListOrders()
        {
            var orders = new List<PortalOrderRecord>();
            if (!Directory.Exists(OrdersFolder)) return orders;

            foreach (var folder in Directory.GetDirectories(OrdersFolder))
            {
                var statusFile = Path.Combine(folder, "order-status.json");
                if (!File.Exists(statusFile)) continue;
                try
                {
                    orders.Add(serializer.Deserialize<PortalOrderRecord>(File.ReadAllText(statusFile)));
                }
                catch
                {
                    // Ignore broken experimental records; the UI should keep loading.
                }
            }

            orders.Sort(delegate(PortalOrderRecord left, PortalOrderRecord right)
            {
                return string.Compare(right.CreatedAt, left.CreatedAt, StringComparison.OrdinalIgnoreCase);
            });
            return orders;
        }

        public PortalOrderRecord LoadOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;
            var statusFile = Path.Combine(OrdersFolder, ProductionOutputService.SafeFileName(orderId), "order-status.json");
            if (!File.Exists(statusFile)) return null;
            return serializer.Deserialize<PortalOrderRecord>(File.ReadAllText(statusFile));
        }

        public void SaveRequest(string orderFolder, PortalQuoteRequest request)
        {
            File.WriteAllText(Path.Combine(orderFolder, "klant-configuratie.json"), serializer.Serialize(request));
        }

        public void SaveRecord(PortalOrderRecord record)
        {
            Directory.CreateDirectory(record.OutputFolder);
            File.WriteAllText(Path.Combine(record.OutputFolder, "order-status.json"), serializer.Serialize(record));
        }

        public void SaveOfferText(string orderFolder, string contents)
        {
            File.WriteAllText(Path.Combine(orderFolder, "Offerte.txt"), contents);
        }

        public void WriteNotifications(PortalOrderRecord record, PortalQuoteRequest request)
        {
            Directory.CreateDirectory(NotificationsFolder);
            var text = "Nieuwe order " + record.OrderId + Environment.NewLine
                + "Status: " + record.Status + Environment.NewLine
                + "Klant: " + request.CustomerName + Environment.NewLine
                + "Email: " + request.CustomerEmail + Environment.NewLine
                + "Telefoon: " + request.CustomerPhone + Environment.NewLine
                + "Product: " + record.ProductName + Environment.NewLine
                + "Prijs incl. btw: EUR " + record.PriceIncVat.ToString("0.00") + Environment.NewLine
                + "Output: " + record.OutputFolder + Environment.NewLine
                + Environment.NewLine
                + "Opmerking:" + Environment.NewLine
                + request.Notes + Environment.NewLine;

            File.WriteAllText(Path.Combine(NotificationsFolder, record.OrderId + "-intern.txt"), text);
            File.WriteAllText(Path.Combine(NotificationsFolder, record.OrderId + "-klantbevestiging.txt"),
                "Beste " + request.CustomerName + "," + Environment.NewLine + Environment.NewLine
                + "Bedankt voor je akkoord. We hebben order " + record.OrderId + " ontvangen en controleren de productiegegevens." + Environment.NewLine
                + "Richtprijs incl. btw: EUR " + record.PriceIncVat.ToString("0.00") + Environment.NewLine + Environment.NewLine
                + "Met vriendelijke groet," + Environment.NewLine
                + "SW Werkplaats" + Environment.NewLine);
        }

        public string CopyOrderToQueue(PortalOrderRecord record)
        {
            if (record == null) throw new ArgumentNullException("record");

            Directory.CreateDirectory(QueueFolder);
            var target = Path.Combine(QueueFolder, ProductionOutputService.SafeFileName(record.OrderId));
            Directory.CreateDirectory(target);
            CopyTapFiles(Path.Combine(record.OutputFolder, "Nesting"), target);
            CopyIfExists(Path.Combine(record.OutputFolder, "BOM.csv"), Path.Combine(target, "BOM.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "CAM-operaties.csv"), Path.Combine(target, "CAM-operaties.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "Plaatgaten.csv"), Path.Combine(target, "Plaatgaten.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "RailgatenControle.csv"), Path.Combine(target, "RailgatenControle.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "RailTemplateControle.csv"), Path.Combine(target, "RailTemplateControle.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "RailTemplateVisualisatie.svg"), Path.Combine(target, "RailTemplateVisualisatie.svg"));
            CopyIfExists(Path.Combine(record.OutputFolder, "Nesting", "NestPlan.csv"), Path.Combine(target, "NestPlan.csv"));
            CopyIfExists(Path.Combine(record.OutputFolder, "Nesting", "NestVisualisatie.svg"), Path.Combine(target, "NestVisualisatie.svg"));
            return target;
        }

        private static void CopyTapFiles(string sourceFolder, string targetFolder)
        {
            if (!Directory.Exists(sourceFolder)) return;
            foreach (var file in Directory.GetFiles(sourceFolder, "*.tap"))
            {
                File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)), true);
            }
        }

        private static void CopyIfExists(string source, string target)
        {
            if (File.Exists(source)) File.Copy(source, target, true);
        }
    }
}
