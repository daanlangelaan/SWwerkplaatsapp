namespace SWWerkplaats.Configurator.Domain
{
    public sealed class SimRigConfig
    {
        public string ProjectName { get; set; }
        public double OutsideWidthMm { get; set; }
        public double LengthMm { get; set; }
        public double SteeringBridgeHeightMm { get; set; }
        public double SteeringBridgePositionMm { get; set; }
        public double PedalDeckPositionMm { get; set; }
        public double PedalAngleDeg { get; set; }
        public string WheelMountPattern { get; set; }
        public Material Profile4080 { get; set; }
        public Material AdapterPlateMaterial { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
    }
}
