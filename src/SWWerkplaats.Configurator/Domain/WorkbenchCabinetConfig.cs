namespace SWWerkplaats.Configurator.Domain
{
    public sealed class WorkbenchCabinetConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double WorktopHeightMm { get; set; }
        public int UnitCount { get; set; }
        public double PlinthHeightMm { get; set; }
        public double PlinthSetbackMm { get; set; }
        public double PlinthFloorClearanceMm { get; set; }
        public bool IncludeLeftSidePlinth { get; set; }
        public bool IncludeRightSidePlinth { get; set; }
        public AdjustableFootTemplate AdjustableFoot { get; set; }
        public double AdjustableFootInsetMm { get; set; }
        public double PlinthClipCenterBehindBackFaceMm { get; set; }
        public double DoorStopWidthMm { get; set; }
        public double DoorGapMm { get; set; }
        public double DoorToCarcassClearanceMm { get; set; }
        public double FrontPanelCornerRadiusMm { get; set; }
        public int ShelfCountPerUnit { get; set; }
        public string ShelfStartMode { get; set; }
        public double ShelfClearanceMm { get; set; }
        public double ShelfFrontInsetMm { get; set; }
        public double AdjustableShelfHoleEndMarginMm { get; set; }
        public int AdjustableShelfPositionCount { get; set; }
        public bool IncludeAdjustableShelfHoles { get; set; }
        public bool IncludeTopDrawer { get; set; }
        public bool IncludeDrawerPullCutouts { get; set; }
        public double TopDrawerHeightMm { get; set; }
        public double DrawerSideClearanceMm { get; set; }
        public double DrawerBackClearanceMm { get; set; }
        public bool IncludeBackPanel { get; set; }
        public Material CarcassMaterial { get; set; }
        public Material WorktopMaterial { get; set; }
        public Material FrontMaterial { get; set; }
        public Material DrawerMaterial { get; set; }
        public Material BackMaterial { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public ShelfSupportTemplate ShelfSupport { get; set; }
        public RailTemplate DrawerRail { get; set; }
    }
}
