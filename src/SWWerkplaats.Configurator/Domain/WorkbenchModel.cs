using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class WorkbenchModel
    {
        public string ProjectName { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public double LowerFrameHeightMm { get; set; }
        public double MiddleLayerHeightMm { get; set; }
        public List<ProfilePart> Profiles { get; private set; }
        public List<ProfileOperation> ProfileOperations { get; private set; }
        public List<SheetPart> Sheets { get; private set; }
        public List<HardwareItem> Hardware { get; private set; }
        public List<AssemblyPlacement> AssemblyPlacements { get; private set; }

        public WorkbenchModel()
        {
            Profiles = new List<ProfilePart>();
            ProfileOperations = new List<ProfileOperation>();
            Sheets = new List<SheetPart>();
            Hardware = new List<HardwareItem>();
            AssemblyPlacements = new List<AssemblyPlacement>();
        }
    }
}
