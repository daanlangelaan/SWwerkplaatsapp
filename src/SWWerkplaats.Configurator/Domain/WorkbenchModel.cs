using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class WorkbenchModel
    {
        public string ProductId { get; set; }
        public string ProjectName { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public double LowerFrameHeightMm { get; set; }
        public double MiddleLayerHeightMm { get; set; }
        public List<ProfilePart> Profiles { get; private set; }
        public List<ProfileOperation> ProfileOperations { get; private set; }
        public List<SheetPart> Sheets { get; private set; }
        public List<HardwareItem> Hardware { get; private set; }
        public List<AssemblyPlacement> AssemblyPlacements { get; private set; }
        public List<AssemblyConnection> AssemblyConnections { get; private set; }
        public List<ProfileFastenerCalculation> ProfileFastenerCalculations { get; private set; }
        public StructuralCalculationReport StructuralCalculation { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
        public List<string> DesignNotes { get; private set; }

        public WorkbenchModel()
        {
            Profiles = new List<ProfilePart>();
            ProfileOperations = new List<ProfileOperation>();
            Sheets = new List<SheetPart>();
            Hardware = new List<HardwareItem>();
            AssemblyPlacements = new List<AssemblyPlacement>();
            AssemblyConnections = new List<AssemblyConnection>();
            ProfileFastenerCalculations = new List<ProfileFastenerCalculation>();
            DesignNotes = new List<string>();
        }
    }
}
