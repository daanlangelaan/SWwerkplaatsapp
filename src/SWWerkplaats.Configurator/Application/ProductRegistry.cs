using System;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProductRegistry
    {
        private readonly IProductBuilder[] builders;

        public ProductRegistry()
            : this(new IProductBuilder[] { new CabinetProductBuilder(), new WorkbenchProductBuilder() })
        {
        }

        public ProductRegistry(IProductBuilder[] builders)
        {
            this.builders = builders == null || builders.Length == 0
                ? new IProductBuilder[] { new CabinetProductBuilder(), new WorkbenchProductBuilder() }
                : builders;
        }

        public IProductBuilder[] All()
        {
            var copy = new IProductBuilder[builders.Length];
            Array.Copy(builders, copy, builders.Length);
            return copy;
        }

        public IProductBuilder Resolve(PortalQuoteRequest request)
        {
            var productId = ProductId(request);
            foreach (var builder in builders)
            {
                if (string.Equals(builder.ProductId, productId, StringComparison.OrdinalIgnoreCase)) return builder;
            }

            return builders[0];
        }

        public ProductCatalogItem[] CatalogItems()
        {
            var items = new ProductCatalogItem[builders.Length];
            for (var i = 0; i < builders.Length; i++)
            {
                items[i] = builders[i].CatalogItem;
            }

            return items;
        }

        private static string ProductId(PortalQuoteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Product)) return "cabinet";
            return request.Product.Trim();
        }
    }
}
