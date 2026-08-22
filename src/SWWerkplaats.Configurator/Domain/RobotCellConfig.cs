namespace SWWerkplaats.Configurator.Domain
{
    public sealed class RobotCellConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double WorktopHeightMm { get; set; }
        public double IntermediateBeamMaxSpacingMm { get; set; }
        public Material UprightProfile { get; set; }
        public Material FrameBeamProfile { get; set; }
        public Material RearRailProfile { get; set; }
        public Material WorktopMaterial { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
    }
}
