namespace SWWerkplaats.Configurator.Domain
{
    public sealed class FoldingWorkbenchConfig
    {
        public string ProjectName { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public Material PanelMaterial { get; set; }
        public double StockAllowanceMm { get; set; }
        public double UnderframeInsetLongEdgeMm { get; set; }
        public double UnderframeInsetShortEdgeMm { get; set; }
        public double JointClearanceMm { get; set; }
        public double TabWidthMm { get; set; }
        public double FrameStileWidthMm { get; set; }
        public double TopRailHeightMm { get; set; }
        public double BottomRailHeightMm { get; set; }
        public double IntegratedFootWidthMm { get; set; }
        public double IntegratedFootReliefHeightMm { get; set; }
        public double CornerRadiusMm { get; set; }
        public double WorktopFloatMm { get; set; }
        public double FoldedClearanceMm { get; set; }
        public int TabsPerLongPanel { get; set; }
        public int TabsPerShortPanelHalf { get; set; }
        public double HingeHeightMm { get; set; }
        public double HingeOpenWidthMm { get; set; }
        public double HingeLeafThicknessMm { get; set; }
        public double HingeBarrelDiameterMm { get; set; }
        public double HingeGapMm { get; set; }
        public string HingeComponentId { get; set; }
        public string HingeArticleNumber { get; set; }
        public double DogboneToolDiameterMm { get; set; }
    }
}
