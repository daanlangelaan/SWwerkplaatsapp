namespace SWWerkplaats.Configurator.Domain
{
    public sealed class AdjustableFootMountingHole
    {
        public string Name { get; set; }
        public double XOffsetMm { get; set; }
        public double YOffsetMm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public bool Through { get; set; }
    }

    public sealed class AdjustableFootTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArticleNumber { get; set; }
        public double NominalHeightMm { get; set; }
        public double MinHeightMm { get; set; }
        public double MaxHeightMm { get; set; }
        public double FootDiameterMm { get; set; }
        public double MountingBlockLengthMm { get; set; }
        public double MountingBlockWidthMm { get; set; }
        public double MountingBlockThicknessMm { get; set; }
        public double PinDiameterMm { get; set; }
        public double PinLengthMm { get; set; }
        public double PinSpacingMm { get; set; }
        public double PinCenterFromShortEdgeMm { get; set; }
        public double CentralFastenerClearanceDiameterMm { get; set; }
        public double CentralFastenerNominalDiameterMm { get; set; }
        public double CentralFastenerLengthMm { get; set; }
        public bool CentralFastenerCncPilotHole { get; set; }
        public double CentralFastenerCenterFromShortEdgeMm { get; set; }
        public double SlotLengthMm { get; set; }
        public double SlotWidthMm { get; set; }
        public double ClipStemDiameterMm { get; set; }
        public double[] FootCenterPositionsFromShortEdgeMm { get; set; }
        public double DefaultFootCenterFromShortEdgeMm { get; set; }
        public double MaxLoadKgPerFoot { get; set; }
        public int PackQuantity { get; set; }
        public bool IncludesPlinthClips { get; set; }
        public int PlinthClipQuantityPerPack { get; set; }
        public bool MountingPatternVerified { get; set; }
        public bool PlinthClipPatternVerified { get; set; }
        public PlinthClipAdapterTemplate PlinthClipAdapter { get; set; }
        public AdjustableFootMountingHole[] MountingHoles { get; set; }
    }
}
