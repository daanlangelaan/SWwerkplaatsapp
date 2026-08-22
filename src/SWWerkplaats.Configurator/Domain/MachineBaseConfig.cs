namespace SWWerkplaats.Configurator.Domain
{
    public sealed class MachineBaseConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public double WorktopHeightMm { get; set; }
        public double ReservedWorktopThicknessMm { get; set; }
        public Material WorktopMaterial { get; set; }
        public Material UprightProfile { get; set; }
        public Material LowerBeamProfile { get; set; }
        public Material WorktopBeamProfile { get; set; }
        public double WorktopIntermediateBeamMaxSpacingMm { get; set; }
        public Material TopBeamProfile { get; set; }
        public Material LowerPanelMaterial { get; set; }
        public Material UpperPanelMaterial { get; set; }
        public string FrontProtectionMode { get; set; }
        public double ControlCabinetWidthMm { get; set; }
        public double ControlCabinetDepthMm { get; set; }
        public double ControlCabinetHeightMm { get; set; }
        public string ControlCabinetPosition { get; set; }
        public int ControlCabinetDoorCount { get; set; }
        public string ControlCabinetHingeSide { get; set; }
        public int FrontDoorCount { get; set; }
        public string FrontSingleDoorHingeSide { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
    }
}
