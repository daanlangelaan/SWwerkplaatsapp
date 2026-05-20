namespace SWWerkplaats.Configurator.Domain
{
    public sealed class RailTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double LengthMm { get; set; }
        public double ThicknessMm { get; set; }
        public int CabinetHoleCount { get; set; }
        public double CabinetFirstHoleOffsetMm { get; set; }
        public double CabinetHoleSpacingMm { get; set; }
        public string CabinetHolePositionsMm { get; set; }
        public double CabinetVerticalOffsetMm { get; set; }
        public double CabinetHoleDiameterMm { get; set; }
        public int DrawerHoleCount { get; set; }
        public double DrawerFirstHoleOffsetMm { get; set; }
        public double DrawerHoleSpacingMm { get; set; }
        public string DrawerHolePositionsMm { get; set; }
        public double DrawerVerticalOffsetMm { get; set; }
        public double DrawerHoleDiameterMm { get; set; }
        public string FastenerName { get; set; }

        public int HoleCount
        {
            get { return CabinetHoleCount; }
            set { CabinetHoleCount = value; }
        }

        public double FirstHoleOffsetMm
        {
            get { return CabinetFirstHoleOffsetMm; }
            set { CabinetFirstHoleOffsetMm = value; }
        }

        public double HoleSpacingMm
        {
            get { return CabinetHoleSpacingMm; }
            set { CabinetHoleSpacingMm = value; }
        }

        public double VerticalOffsetMm
        {
            get { return CabinetVerticalOffsetMm; }
            set { CabinetVerticalOffsetMm = value; }
        }

        public double HoleDiameterMm
        {
            get { return CabinetHoleDiameterMm; }
            set { CabinetHoleDiameterMm = value; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
