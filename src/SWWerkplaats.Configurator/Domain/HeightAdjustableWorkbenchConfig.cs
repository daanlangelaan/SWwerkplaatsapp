using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class HeightAdjustableWorkbenchConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public double ColumnCenterDistanceMm { get; set; }
        public double StabilizationPlateHeightMm { get; set; }
        public double StabilizationPlateWidthMm { get; set; }
        public Material FootProfile { get; set; }
        public Material TopFrameProfile { get; set; }
        public Material TopSheet { get; set; }
        public Material StabilizationSheet { get; set; }
        public LiftColumnTemplate LiftColumn { get; set; }
        public LevelingFootCornerAdapterTemplate LevelingFootCornerAdapter { get; set; }
        public LevelingFootTemplate LevelingFoot { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
        public List<string> DesignNotes { get; private set; }

        public HeightAdjustableWorkbenchConfig()
        {
            DesignNotes = new List<string>();
        }
    }
}
