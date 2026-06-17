using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public interface ICatalogRepository
    {
        Material[] Sheets();
        Material[] Profiles();
        FastenerDefinition[] SheetFasteners();
        RailTemplate[] DrawerRails();
        ShelfSupportTemplate[] ShelfSupports();
        ToolDefinition DefaultEndMill(double diameterMm, double passDepthMm);
    }

    public sealed class LibraryCatalogRepository : ICatalogRepository
    {
        public Material[] Sheets()
        {
            return LibraryCatalog.Sheets();
        }

        public Material[] Profiles()
        {
            return LibraryCatalog.Profiles();
        }

        public FastenerDefinition[] SheetFasteners()
        {
            return LibraryCatalog.SheetFasteners();
        }

        public RailTemplate[] DrawerRails()
        {
            return LibraryCatalog.DrawerRails();
        }

        public ShelfSupportTemplate[] ShelfSupports()
        {
            return LibraryCatalog.ShelfSupports();
        }

        public ToolDefinition DefaultEndMill(double diameterMm, double passDepthMm)
        {
            return LibraryCatalog.DefaultEndMill(diameterMm, passDepthMm);
        }
    }
}
