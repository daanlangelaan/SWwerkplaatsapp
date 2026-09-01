using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class PortalSiteCatalog
    {
        private readonly PortalSiteCatalogContract contract;

        public PortalSiteCatalog()
        {
            contract = LoadRequired();
        }

        public List<PortalSiteDefinition> List()
        {
            return contract.Sites.OrderBy(site => site.SiteId, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public PortalSiteDefinition GetRequired(string siteId)
        {
            var site = contract.Sites.FirstOrDefault(value => string.Equals(value.SiteId, siteId, StringComparison.OrdinalIgnoreCase));
            if (site == null) throw new InvalidOperationException("Onbekende portalsite: " + siteId);
            return site;
        }

        public void EnsureProductAllowed(PortalSiteDefinition site, string productId)
        {
            if (site == null) throw new ArgumentNullException("site");
            if (site.AllowAllProducts) return;
            if (site.AllowedProductIds.Any(value => string.Equals(value, productId, StringComparison.OrdinalIgnoreCase))) return;
            throw new InvalidOperationException("Product " + productId + " is niet beschikbaar op site " + site.SiteId + ".");
        }

        private static PortalSiteCatalogContract LoadRequired()
        {
            var path = FindUpwards(AppDomain.CurrentDomain.BaseDirectory) ?? FindUpwards(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Portal-sitecontract ontbreekt: config/portal-sites.json is niet gevonden.");
            var value = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                .Deserialize<PortalSiteCatalogContract>(File.ReadAllText(path));
            if (value == null || value.ContractVersion != 1 || value.Sites == null || value.Sites.Count == 0)
                throw new InvalidOperationException("Portal-sitecontract heeft geen ondersteunde contractVersion 1 of bevat geen sites.");
            var duplicate = value.Sites.GroupBy(site => (site.SiteId ?? "").Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
            if (duplicate != null) throw new InvalidOperationException("Portal-sitecontract bevat een ontbrekend of dubbel SiteId: " + duplicate.Key);
            foreach (var site in value.Sites)
            {
                if (string.IsNullOrWhiteSpace(site.Name) || string.IsNullOrWhiteSpace(site.Status) || string.IsNullOrWhiteSpace(site.PresentationProfile))
                    throw new InvalidOperationException("Portal-sitecontract is onvolledig voor " + site.SiteId + ".");
                if (site.AllowedProductIds == null) site.AllowedProductIds = new List<string>();
                if (site.OpenData == null) site.OpenData = new List<string>();
                if (!site.AllowAllProducts && site.AllowedProductIds.Count == 0)
                    throw new InvalidOperationException("Portal-site " + site.SiteId + " heeft geen toegestane Product-ID's.");
            }
            var knownProducts = new HashSet<string>(new ProductCatalogApplicationService().ListProducts().Select(product => product.Product), StringComparer.OrdinalIgnoreCase);
            var unknownProduct = value.Sites.SelectMany(site => site.AllowedProductIds.Select(productId => new { site.SiteId, ProductId = productId }))
                .FirstOrDefault(valueItem => !knownProducts.Contains(valueItem.ProductId));
            if (unknownProduct != null) throw new InvalidOperationException("Portal-site " + unknownProduct.SiteId + " verwijst naar onbekend Product-ID " + unknownProduct.ProductId + ".");
            return value;
        }

        private static string FindUpwards(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var index = 0; index < 8 && !string.IsNullOrWhiteSpace(folder); index++)
            {
                var candidate = Path.Combine(folder, "config", "portal-sites.json");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }
    }
}
