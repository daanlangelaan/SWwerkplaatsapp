using System;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CatalogData
    {
        public Material[] Sheets { get; set; }
        public Material[] Profiles { get; set; }
        public RailTemplate[] Rails { get; set; }
        public ShelfSupportTemplate[] ShelfSupports { get; set; }
        public string[] Statuses { get; set; }
        public ProductCatalogItem[] Products { get; set; }
        public LinearGuideTemplate[] LinearGuides { get; set; }
        public LiftColumnTemplate[] LiftColumns { get; set; }
        public PortalPresentationContract Presentation { get; set; }
    }

    public sealed class CatalogApplicationService
    {
        private readonly ICatalogRepository catalog;

        public CatalogApplicationService()
            : this(new LibraryCatalogRepository())
        {
        }

        public CatalogApplicationService(ICatalogRepository catalog)
        {
            this.catalog = catalog ?? new LibraryCatalogRepository();
        }

        public CatalogData GetCatalog()
        {
            return new CatalogData
            {
                Sheets = catalog.Sheets(),
                Profiles = catalog.Profiles().Select(ToProfileCatalogOption).ToArray(),
                Rails = catalog.DrawerRails(),
                ShelfSupports = catalog.ShelfSupports(),
                Statuses = OrderWorkflowStatus.All(),
                Products = new ProductCatalogApplicationService().ListProducts(),
                LinearGuides = new[] { ProductDefaults.LexHsr15LinearGuide() },
                LiftColumns = new[] { ProductDefaults.LexHte2LiftColumn() },
                Presentation = PortalPresentationContract.LoadRequired()
            };
        }

        private static Material ToProfileCatalogOption(Material source)
        {
            if (source == null) return null;
            var dimensions = source.WidthMm > 0 && source.HeightMm > 0
                ? Math.Min(source.WidthMm, source.HeightMm).ToString("0.##", CultureInfo.InvariantCulture)
                    + "×" + Math.Max(source.WidthMm, source.HeightMm).ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;
            return new Material
            {
                Id = source.Id,
                Name = string.IsNullOrWhiteSpace(dimensions) ? source.Name : dimensions + " — " + source.Name,
                Kind = source.Kind,
                WidthMm = source.WidthMm,
                HeightMm = source.HeightMm,
                ThicknessMm = source.ThicknessMm,
                StockLengthMm = source.StockLengthMm,
                SheetLengthMm = source.SheetLengthMm,
                SheetWidthMm = source.SheetWidthMm,
                RenderAppearance = source.RenderAppearance,
                IsAdditiveManufactured = source.IsAdditiveManufactured
            };
        }
    }
}
