using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public enum CabinetDoorHand
    {
        Geen,
        Links,
        Rechts
    }

    public sealed class CabinetConfig
    {
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double WorktopHeightMm { get; set; }
        public int UnitCount { get; set; }
        public double PlinthHeightMm { get; set; }
        public double PlinthDepthMm { get; set; }
        public bool IncludeBackPanel { get; set; }
        public Material CarcassMaterial { get; set; }
        public Material WorktopMaterial { get; set; }
        public Material DrawerMaterial { get; set; }
        public Material FrontMaterial { get; set; }
        public Material BackMaterial { get; set; }
        public FastenerDefinition SheetFastener { get; set; }
        public RailTemplate DrawerRail { get; set; }
        public bool AutoTabs { get; set; }
        public double SmallPartAreaThresholdMm2 { get; set; }
        public double TabWidthMm { get; set; }
        public double TabHeightMm { get; set; }
        public double ShelfClearanceMm { get; set; }
        public double DrawerSideClearanceMm { get; set; }
        public double DoorGapMm { get; set; }
        public List<CabinetUnitConfig> Units { get; private set; }

        public CabinetConfig()
        {
            Units = new List<CabinetUnitConfig>();
        }
    }

    public sealed class CabinetUnitConfig
    {
        public int UnitNumber { get; set; }
        public int ShelfCount { get; set; }
        public string ShelfHeightsMm { get; set; }
        public int DrawerCount { get; set; }
        public double DrawerHeightMm { get; set; }
        public CabinetDoorHand Door { get; set; }
        public bool SlidingDoors { get; set; }
        public double SlidingDoorMaxWidthMm { get; set; }
    }
}
