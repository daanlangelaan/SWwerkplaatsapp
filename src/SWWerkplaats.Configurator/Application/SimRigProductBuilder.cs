using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class SimRigProductBuilder : IProductBuilder
    {
        public string ProductId { get { return "sim_rig_4080"; } }

        public ProductCatalogItem CatalogItem
        {
            get
            {
                return new ProductCatalogItem
                {
                    Product = ProductId,
                    Name = "Modulaire sim-racing-rig 40x80",
                    Category = "profielconstructie",
                    DefaultWidthMm = ProductDefaults.SimRigOutsideWidthMm,
                    DefaultDepthMm = ProductDefaults.SimRigLengthMm,
                    DefaultHeightMm = ProductDefaults.SimRigSteeringBridgeHeightMm,
                    DefaultUnitCount = 1,
                    SupportsProfiles = true,
                    SupportsDrawers = false,
                    SupportsDoors = false,
                    SupportsBackPanel = false,
                    SupportsAdjustableShelfHoles = false
                };
            }
        }

        public WorkbenchModel Build(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            return new SimRigEngine().Build(factory.BuildSimRig(request));
        }
    }
}
