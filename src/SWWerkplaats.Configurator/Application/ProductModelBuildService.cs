using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductModelBuildService
    {
        private readonly ProductRegistry products;

        public ProductModelBuildService()
            : this(new ProductRegistry())
        {
        }

        public ProductModelBuildService(ProductRegistry products)
        {
            this.products = products ?? new ProductRegistry();
        }

        public WorkbenchModel Build(PortalQuoteRequest request)
        {
            return Build(new PortalConfigurationFactory(), request);
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            new ProductRequestDimensionValidationService().Validate(request);

            var builder = products.Resolve(request);
            var model = builder.Build(factory, request);
            model.ProductId = builder.ProductId;
            new ProfileFastenerCalculationService().Assign(model);
            ValidateProductFastenerStandard(builder.ProductId, model);
            ApplyOptionalVBitFinishing(builder.ProductId, model, request);
            new ProfileMemberIdentityService().Assign(model);
            ApplyOrderQuantity(model, request);
            var orderQuantity = request == null ? 1 : Math.Max(1, request.Quantity);
            new ProfileTraceabilityService().Assign(model, builder.ProductId, orderQuantity);
            new ProfileStickerPlacementService().Assign(model, orderQuantity);
            // Fysieke eindvlak-op-sleufcontacten zijn de enige bron voor aantallen,
            // BOM, bewerkingen, rendergaten en assemblage-instructies.
            new ProfileAssemblyConnectionDerivationService().Assign(model);
            new ProfileConnectionHardwareSynchronizationService().Assign(model, orderQuantity);
            // De gekozen kernboring wordt afgeleid van het bestaande fysieke stickervlak.
            // Stickerplaatsing moet daarom gereed zijn voordat verbindingstappen worden vertaald naar tapbewerkingen.
            new ProfileConnectionTapOperationService().Assign(model, orderQuantity);
            new ProfileConnectionGeometryService().Assign(model);
            new ProfileConnectionAccessHoleOperationService().Assign(model, orderQuantity);
            return model;
        }

        private static void ApplyOptionalVBitFinishing(string productId, WorkbenchModel model, PortalQuoteRequest request)
        {
            if (model == null || request == null) return;
            var countersinksEnabled = request.EnableWoodScrewCountersinks == true
                || request.EnableCountersinkAndEdgeChamfer == true;
            if (!countersinksEnabled) return;
            if (!string.Equals(productId, "werkbankkast", StringComparison.OrdinalIgnoreCase)) return;

            foreach (var hole in model.Sheets.SelectMany(sheet => sheet.Holes))
            {
                if (hole.SupportKind != SheetHoleSupportKind.PanelScrew) continue;
                hole.Countersunk = true;
                hole.CountersinkDiameterMm = 8.0;
                hole.CountersinkDepthMm = 5.0;
            }
        }

        private static void ValidateProductFastenerStandard(string productId, WorkbenchModel model)
        {
            var standard = ProductFastenerStandards.Resolve(productId);
            if (string.IsNullOrWhiteSpace(standard.WoodToWoodFastenerId)) return;
            if (model == null || model.SheetFastener == null
                || model.SheetFastener.UsageKind != FastenerUsageKind.WoodScrew
                || !string.Equals(model.SheetFastener.Id, standard.WoodToWoodFastenerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Producttype " + productId + " gebruikt niet zijn productgebonden hout-op-hout bevestigerstandaard.");

            var diameter = model.SheetFastener.ClearanceHoleDiameterMm;
            if (model.Sheets.SelectMany(s => s.Holes).Any(h => h.SupportKind == SheetHoleSupportKind.PanelScrew && Math.Abs(h.DiameterMm - diameter) > 0.001))
                throw new InvalidOperationException("Productie-export geblokkeerd: hout-op-hout gat wijkt af van productstandaard Ø" + diameter.ToString("0.##") + "mm.");
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
