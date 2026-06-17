using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public static class ProductDefaults
    {
        public const double WorkbenchWidthMm = 1500;
        public const double WorkbenchDepthMm = 750;
        public const double WorkbenchHeightMm = 900;
        public const int WorkbenchUnitCount = 1;
        public const int WorkbenchDefaultShelfCount = 0;
        public const int WorkbenchDefaultDrawerCount = 0;

        public const double CabinetWidthMm = 2400;
        public const double CabinetDepthMm = 600;
        public const double CabinetHeightMm = 900;
        public const int CabinetUnitCount = 4;
        public const int CabinetDefaultShelfCount = 3;
        public const int CabinetDefaultDrawerCount = 1;
        public const string CabinetDefaultShelfStartMode = "top";

        public const double CubbyCabinetCellWidthMm = 400;
        public const double CubbyCabinetCellDepthMm = 350;
        public const double CubbyCabinetCellHeightMm = 350;
        public const int CubbyCabinetColumnCount = 3;
        public const int CubbyCabinetRowCount = 4;
        public const double CubbyCabinetPlinthHeightMm = 80;
        public const double CubbyCabinetPlinthDepthMm = 40;
        public const double CubbyCabinetGridInsetMm = 20;
        public const double CubbyCabinetCombSlotClearanceMm = 0.3;
        public const double CubbyCabinetBackGrooveDepthMm = 6;
        public const double CubbyCabinetBackGrooveClearanceMm = 0.5;
        public const double CubbyCabinetBackFastenerMaxSpacingMm = 220;
        public const double CubbyCabinetDividerBackFastenerMaxSpacingMm = 260;

        public const int DefaultSheetIndex = 2;
        public const int DefaultProfileIndex = 1;
        public const int DefaultFastenerIndex = 0;
        public const int DefaultDrawerRailIndex = 1;
        public const int DefaultShelfSupportIndex = 0;
        public const double DefaultSheetThicknessMm = 18;

        public const string DefaultDrawerMaterialId = "multiplex_15";
        public const string DefaultBackMaterialId = "multiplex_15";

        public const double CabinetPlinthHeightMm = 100;
        public const double CabinetPlinthDepthMm = 60;
        public const double FullWidthTopDrawerHeightMm = 160;
        public const double AdjustableShelfHoleEndMarginMm = 80;
        public const double ShelfClearanceMm = 2;
        public const double DrawerBackClearanceMm = 30;
        public const double DoorGapMm = 2;

        public const double DefaultToolDiameterMm = 6;
        public const double DefaultToolPassDepthMm = 6.25;

        public static MachineProfile DefaultMachine()
        {
            return new MachineProfile
            {
                Id = "mach3_portaal_3020x1520",
                Name = "Mach3 portaalfrees 3020x1520",
                MaxXmm = 3020,
                MaxYmm = 1520,
                FileExtension = ".tap",
                SafeZmm = 15,
                Origin = "links onder"
            };
        }
    }
}
