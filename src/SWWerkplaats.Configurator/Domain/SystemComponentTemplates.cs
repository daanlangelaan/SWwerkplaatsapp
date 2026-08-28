namespace SWWerkplaats.Configurator.Domain
{
    public enum ProfileSawingMode
    {
        SupplierCutToLength,
        InHouse
    }

    public sealed class LinearGuideTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SelectedSupplierUrl { get; set; }
        public string ReferenceManufacturer { get; set; }
        public string ReferenceModel { get; set; }
        public string ReferenceSourceUrl { get; set; }
        public string VerificationStatus { get; set; }
        public double RailLengthMm { get; set; }
        public int RailQuantity { get; set; }
        public double RailWidthMm { get; set; }
        public double RailHeightMm { get; set; }
        public double RailMountingPitchMm { get; set; }
        public double RailEndDistanceMm { get; set; }
        public double RailHoleThroughDiameterMm { get; set; }
        public double RailHoleCounterboreDiameterMm { get; set; }
        public double RailHoleCounterboreDepthMm { get; set; }
        public int RailHoleCount { get; set; }
        public int CarriageQuantity { get; set; }
        public double CarriageWidthMm { get; set; }
        public double CarriageLengthMm { get; set; }
        public double AssemblyHeightMm { get; set; }
        public double CarriageMountingPitchXmm { get; set; }
        public double CarriageMountingPitchZmm { get; set; }
        public string CarriageMountingThread { get; set; }
        public double ReferenceDynamicLoadKn { get; set; }
        public double ReferenceStaticLoadKn { get; set; }
    }

    public sealed class LiftColumnTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string VerificationStatus { get; set; }
        public int Quantity { get; set; }
        public double RetractedLengthMm { get; set; }
        public double StrokeMm { get; set; }
        public double BodyWidthMm { get; set; }
        public double BodyDepthMm { get; set; }
        public double EndPlateLengthMm { get; set; }
        public double EndPlateWidthMm { get; set; }
        public double EndPlateThicknessMm { get; set; }
        public double SlotCenterPitchMm { get; set; }
        public double SlotLengthMm { get; set; }
        public double SlotWidthMm { get; set; }
    }

    public sealed class LevelingFootCornerAdapterTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArticleNumber { get; set; }
        public string SupplierUrl { get; set; }
        public double WidthMm { get; set; }
        public double ReachMm { get; set; }
        public double MountingPlateHeightMm { get; set; }
        public double MountingPlateThicknessMm { get; set; }
        public double SupportArmThicknessMm { get; set; }
        public double FootAxisFromMountingFaceMm { get; set; }
        public double ThreadDiameterMm { get; set; }
        public double MountingHoleDiameterMm { get; set; }
        public double MountingHolePitchMm { get; set; }
    }

    public sealed class LevelingFootTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArticleNumber { get; set; }
        public string SupplierUrl { get; set; }
        public double NominalDiameterMm { get; set; }
        public double ActualFootDiameterMm { get; set; }
        public double OverallHeightMm { get; set; }
        public double FootHeightMm { get; set; }
        public double ThreadDiameterMm { get; set; }
        public double ThreadLengthMm { get; set; }
        public double NutAcrossFlatsMm { get; set; }
        public double NutHeightMm { get; set; }
        public double MaxLoadKg { get; set; }
    }

    public sealed class SwingLatchTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArticleNumber { get; set; }
        public string SupplierUrl { get; set; }
        public double OverallLengthMm { get; set; }
        public double WidthMm { get; set; }
        public double OverallProjectionMm { get; set; }
        public double MountingBaseDiameterMm { get; set; }
        public double BaseProjectionMm { get; set; }
        public double ThreadDiameterMm { get; set; }
        public double RotationStepDeg { get; set; }
        public double HexKeyAcrossFlatsMm { get; set; }
        public double WeightGrams { get; set; }
        public string RenderStatus { get; set; }
        public string OpenRenderData { get; set; }

        public double NoseCenterDistanceMm
        {
            get { return OverallLengthMm - MountingBaseDiameterMm / 2.0 - WidthMm / 2.0; }
        }
    }
}
