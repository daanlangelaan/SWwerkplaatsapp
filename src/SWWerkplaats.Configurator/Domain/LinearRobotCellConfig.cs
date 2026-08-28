namespace SWWerkplaats.Configurator.Domain
{
    public sealed class LinearRobotCellConfig
    {
        public string ProjectName { get; set; }
        public double LengthMm { get; set; }
        public double WorktopDepthMm { get; set; }
        public double WorktopHeightMm { get; set; }
        public int WorktopSideCount { get; set; }
        public double GuardHeightAboveWorktopMm { get; set; }
        public double IntermediateSupportMaxSpacingMm { get; set; }
        public double RailZoneWidthMm { get; set; }
        public double RailCenterSpacingMm { get; set; }
        public double RailWidthMm { get; set; }
        public double RailHeightMm { get; set; }
        public double CarriageLengthMm { get; set; }
        public double CarriageWidthMm { get; set; }
        public double CarriageHeightMm { get; set; }
        public double RobotAdapterLengthMm { get; set; }
        public double RobotAdapterWidthMm { get; set; }
        public double RobotAdapterThicknessMm { get; set; }
        public double MotorAdapterLengthMm { get; set; }
        public double MotorAdapterHeightMm { get; set; }
        public double MotorAdapterThicknessMm { get; set; }
        public double RackWidthMm { get; set; }
        public double RackHeightMm { get; set; }
        public double FootVisibleHeightMm { get; set; }
        public double FootDiameterMm { get; set; }
        public double FootPlateThicknessMm { get; set; }
        public bool LowerFrameEndCrossmembersUseOuterLane { get; set; }
        public int TwoSidedEndWallIntermediatePostCount { get; set; }
        public int ThroughCornerUprightCount { get; set; }
        public int TwoSidedCenterSupportRowCount { get; set; }
        public string LightCurtainSetComponentId { get; set; }
        public string LightCurtainEmitterComponentId { get; set; }
        public string LightCurtainReceiverComponentId { get; set; }
        public string LightCurtainDisplayName { get; set; }
        public string LightCurtainArticleNumber { get; set; }
        public double LightCurtainProtectedHeightMm { get; set; }
        public double LightCurtainWidthMm { get; set; }
        public double LightCurtainOverallHeightMm { get; set; }
        public double LightCurtainDepthMm { get; set; }
        public Material UprightProfile { get; set; }
        public Material FrameBeamProfile { get; set; }
        public Material RailCarrierProfile { get; set; }
        public Material GuardProfile { get; set; }
        public Material WorktopMaterial { get; set; }
        public Material GuardPanelMaterial { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
    }
}
