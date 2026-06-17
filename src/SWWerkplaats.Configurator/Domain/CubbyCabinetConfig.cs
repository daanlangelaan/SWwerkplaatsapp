namespace SWWerkplaats.Configurator.Domain
{
    public sealed class CubbyCabinetConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
        public double PlinthHeightMm { get; set; }
        public double PlinthDepthMm { get; set; }
        public bool IncludeBackPanel { get; set; }
        public bool IncludeAdjustableShelfHoles { get; set; }
        public Material CarcassMaterial { get; set; }
        public Material BackMaterial { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public ShelfSupportTemplate ShelfSupport { get; set; }
        public bool AutoTabs { get; set; }
        public double SmallPartAreaThresholdMm2 { get; set; }
        public double TabWidthMm { get; set; }
        public double TabHeightMm { get; set; }
        public double ShelfClearanceMm { get; set; }
        public double DoorGapMm { get; set; }
    }
}
