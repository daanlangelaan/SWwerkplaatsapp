using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductModelBuildService
    {
        public WorkbenchModel Build(PortalQuoteRequest request)
        {
            return Build(new PortalConfigurationFactory(), request);
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");

            WorkbenchModel model;
            if (IsCabinet(request))
            {
                model = new CabinetEngine().Build(factory.BuildCabinet(request));
            }
            else
            {
                model = new WorkbenchEngine().Build(factory.BuildWorkbench(request));
            }

            ApplyOrderQuantity(model, request);
            return model;
        }

        public static bool IsCabinet(PortalQuoteRequest request)
        {
            return request == null || string.IsNullOrWhiteSpace(request.Product) || request.Product.ToLowerInvariant() != "werktafel";
        }

        private static void ApplyOrderQuantity(WorkbenchModel model, PortalQuoteRequest request)
        {
            if (model == null || request == null) return;
            var quantity = Math.Max(1, request.Quantity);
            if (quantity <= 1) return;

            foreach (var sheet in model.Sheets)
            {
                sheet.Quantity = Math.Max(1, sheet.Quantity) * quantity;
            }

            foreach (var profile in model.Profiles)
            {
                profile.Quantity = Math.Max(1, profile.Quantity) * quantity;
            }

            foreach (var operation in model.ProfileOperations)
            {
                operation.Quantity = Math.Max(1, operation.Quantity) * quantity;
            }

            foreach (var hardware in model.Hardware)
            {
                hardware.Quantity = Math.Max(0, hardware.Quantity) * quantity;
            }
        }
    }
}
