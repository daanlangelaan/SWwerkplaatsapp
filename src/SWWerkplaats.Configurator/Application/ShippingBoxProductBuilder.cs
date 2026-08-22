using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ShippingBoxProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "shipping_box"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Shipping box / clipkist",
                    Category = "transportverpakking",
                    DefaultWidthMm = ProductDefaults.ShippingBoxInternalWidthMm,
                    DefaultDepthMm = ProductDefaults.ShippingBoxInternalDepthMm,
                    DefaultHeightMm = ProductDefaults.ShippingBoxInternalHeightMm,
                    DefaultUnitCount = 1,
                    DefaultShelfCount = 0,
                    DefaultDrawerCount = 0,
                    DefaultShelfStartMode = "none",
                    SupportsProfiles = false,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = false,
                    SupportsAdjustableShelfHoles = false
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            request = request ?? new PortalQuoteRequest();
            return new ShippingBoxEngine().Build(factory.BuildShippingBox(request));
        }
    }
}
