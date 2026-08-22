namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ShippingBoxConfig
    {
        public string ProjectName { get; set; }
        public double InternalWidthMm { get; set; }
        public double InternalDepthMm { get; set; }
        public double InternalHeightMm { get; set; }
        public Material PanelMaterial { get; set; }
        public CrateClipTemplate Clip { get; set; }
        public string JointMode { get; set; }
        public bool IncludeHandles { get; set; }
        public double HandleLengthMm { get; set; }
        public double HandleHeightMm { get; set; }
        public double HandleCenterHeightRatio { get; set; }
        public double RabbetClearanceMm { get; set; }
        public double RabbetDepthFactor { get; set; }
    }

    public sealed class CrateClipTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Supplier { get; set; }
        public string SupplierModel { get; set; }
        public string SupplierUrl { get; set; }
        public string GeometryReference { get; set; }
        public string GeometryReferenceUrl { get; set; }
        public string Material { get; set; }
        public string Finish { get; set; }
        public double ArmLengthAMm { get; set; }
        public double ArmLengthBMm { get; set; }
        public double WidthMm { get; set; }
        public double ThicknessMm { get; set; }
        public double SlotLengthMm { get; set; }
        public double SlotWidthMm { get; set; }
        public double SlotCenterFromEdgeMm { get; set; }
        public double EndMarginMm { get; set; }
        public double MaxSpacingMm { get; set; }
        public string VerificationStatus { get; set; }
    }
}
