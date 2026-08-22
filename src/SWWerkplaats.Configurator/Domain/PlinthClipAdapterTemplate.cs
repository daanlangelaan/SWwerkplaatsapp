namespace SWWerkplaats.Configurator.Domain
{
    public sealed class PlinthClipAdapterTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double TongueWidthMm { get; set; }
        public double TongueHeightMm { get; set; }
        public double TongueThicknessMm { get; set; }
        public double FootAxisFromTongueBackMm { get; set; }
        public double PrintClearancePerSideMm { get; set; }
        public double BackPlateWidthMm { get; set; }
        public double BackPlateHeightMm { get; set; }
        public double MinimumBackPlateThicknessMm { get; set; }
        public double GuideWallThicknessMm { get; set; }
        public double GuideLipOverlapMm { get; set; }
        public double GuideLipThicknessMm { get; set; }
        public double BottomStopThicknessMm { get; set; }
        public double MountingHoleDiameterMm { get; set; }
        public double MountingHoleSpacingMm { get; set; }
        public double UpperMountingHoleHorizontalOffsetMm { get; set; }
        public double MountingWingExtensionMm { get; set; }
        public double MountingCountersinkDiameterMm { get; set; }
        public double MountingCountersinkDepthMm { get; set; }
        public double PlinthCenterMarkDiameterMm { get; set; }
        public double PlinthCenterMarkDepthMm { get; set; }
        public double FrontScrewDiameterMm { get; set; }
        public double FrontScrewLengthMm { get; set; }
        public double SideScrewDiameterMm { get; set; }
        public double SideScrewLengthMm { get; set; }
        public bool TonguePatternVerified { get; set; }
        public bool FullDesignVerified { get; set; }

        public double SlotWidthMm
        {
            get { return TongueWidthMm + 2.0 * PrintClearancePerSideMm; }
        }

        public double SlotHeightMm
        {
            get { return TongueHeightMm + 2.0 * PrintClearancePerSideMm; }
        }

        public double SlotDepthMm
        {
            get { return TongueThicknessMm + 2.0 * PrintClearancePerSideMm; }
        }
    }
}
