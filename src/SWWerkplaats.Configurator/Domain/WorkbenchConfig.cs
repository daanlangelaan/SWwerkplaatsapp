namespace SWWerkplaats.Configurator.Domain
{
    public sealed class WorkbenchConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public Material FrameProfile { get; set; }
        public Material TopSheet { get; set; }
        public Material ShelfSheet { get; set; }
        public bool IncludeLowerFrame { get; set; }
        public double LowerFrameHeightMm { get; set; }
        public bool IncludeLowerShelf { get; set; }
        public bool IncludeMiddleLayer { get; set; }
        public double MiddleLayerHeightMm { get; set; }
        public bool IncludeMiddleShelf { get; set; }
        public double ShelfCornerClearanceMm { get; set; }
        public double BoltMaxSpacingMm { get; set; }
        public double TopOverhangFrontMm { get; set; }
        public double TopOverhangBackMm { get; set; }
        public double TopOverhangLeftMm { get; set; }
        public double TopOverhangRightMm { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public double ConnectorHoleDiameterMm { get; set; }
        public bool CountersinkSheetHoles { get; set; }
        public double CountersinkDiameterMm { get; set; }
        public double CountersinkDepthMm { get; set; }
        public bool AutoTabs { get; set; }
        public double SmallPartAreaThresholdMm2 { get; set; }
        public double TabWidthMm { get; set; }
        public double TabHeightMm { get; set; }
    }
}
