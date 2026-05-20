namespace SWWerkplaats.Configurator.Domain
{
    public static class LibraryCatalog
    {
        public static Material[] Profiles()
        {
            return new[]
            {
                new Material { Id = "alu_profile_30x30", Name = "Alu profiel 30x30", Kind = MaterialKind.Profile, WidthMm = 30, HeightMm = 30, StockLengthMm = 6000 },
                new Material { Id = "alu_profile_40x40", Name = "Alu profiel 40x40", Kind = MaterialKind.Profile, WidthMm = 40, HeightMm = 40, StockLengthMm = 6000 },
                new Material { Id = "alu_profile_45x45", Name = "Alu profiel 45x45", Kind = MaterialKind.Profile, WidthMm = 45, HeightMm = 45, StockLengthMm = 6000 },
                new Material { Id = "steel_tube_40x40", Name = "Stalen koker 40x40", Kind = MaterialKind.Profile, WidthMm = 40, HeightMm = 40, StockLengthMm = 6000 }
            };
        }

        public static Material[] Sheets()
        {
            return new[]
            {
                new Material { Id = "osb_18", Name = "OSB 18mm", Kind = MaterialKind.Sheet, ThicknessMm = 18, SheetLengthMm = 2440, SheetWidthMm = 1220 },
                new Material { Id = "betonplex_12", Name = "Betonplex 12mm", Kind = MaterialKind.Sheet, ThicknessMm = 12, SheetLengthMm = 2500, SheetWidthMm = 1250 },
                new Material { Id = "betonplex_18", Name = "Betonplex 18mm", Kind = MaterialKind.Sheet, ThicknessMm = 18, SheetLengthMm = 2500, SheetWidthMm = 1250 },
                new Material { Id = "multiplex_15", Name = "Multiplex 15mm", Kind = MaterialKind.Sheet, ThicknessMm = 15, SheetLengthMm = 2500, SheetWidthMm = 1250 }
            };
        }

        public static FastenerDefinition[] SheetFasteners()
        {
            return new[]
            {
                new FastenerDefinition
                {
                    Id = "M8_ISO4762",
                    Name = "M8 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 8,
                    ClearanceHoleDiameterMm = 8,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 13,
                    HeadHeightMm = 8,
                    HeadClearanceMm = 1
                },
                new FastenerDefinition
                {
                    Id = "M6_ISO4762",
                    Name = "M6 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 6,
                    ClearanceHoleDiameterMm = 6.5,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 10,
                    HeadHeightMm = 6,
                    HeadClearanceMm = 1
                },
                new FastenerDefinition
                {
                    Id = "M10_ISO4762",
                    Name = "M10 inbusbout cilinderkop",
                    Standard = "ISO 4762 / DIN 912",
                    NominalDiameterMm = 10,
                    ClearanceHoleDiameterMm = 10.5,
                    HeadKind = FastenerHeadKind.SocketHeadCap,
                    HeadDiameterMm = 16,
                    HeadHeightMm = 10,
                    HeadClearanceMm = 1
                }
            };
        }

        public static RailTemplate[] DrawerRails()
        {
            return new[]
            {
                new RailTemplate
                {
                    Id = "generic_350",
                    Name = "AliExpress ladegeleider 350mm basis",
                    LengthMm = 350,
                    ThicknessMm = 12.7,
                    CabinetHoleCount = 4,
                    CabinetFirstHoleOffsetMm = 37,
                    CabinetHoleSpacingMm = 96,
                    CabinetHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 4,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
                },
                new RailTemplate
                {
                    Id = "generic_450",
                    Name = "AliExpress ladegeleider 450mm basis",
                    LengthMm = 450,
                    ThicknessMm = 12.7,
                    CabinetHoleCount = 5,
                    CabinetFirstHoleOffsetMm = 37,
                    CabinetHoleSpacingMm = 96,
                    CabinetHolePositionsMm = "",
                    CabinetVerticalOffsetMm = 32,
                    CabinetHoleDiameterMm = 6.5,
                    DrawerHoleCount = 5,
                    DrawerFirstHoleOffsetMm = 37,
                    DrawerHoleSpacingMm = 96,
                    DrawerHolePositionsMm = "",
                    DrawerVerticalOffsetMm = 32,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
                },
                new RailTemplate
                {
                    Id = "measured_500",
                    Name = "Gemeten ladegeleider 500mm",
                    LengthMm = 500,
                    ThicknessMm = 13,
                    CabinetHoleCount = 5,
                    CabinetFirstHoleOffsetMm = 34,
                    CabinetHoleSpacingMm = 0,
                    CabinetHolePositionsMm = "34;98;226;354;418",
                    CabinetVerticalOffsetMm = 21,
                    CabinetHoleDiameterMm = 5,
                    DrawerHoleCount = 3,
                    DrawerFirstHoleOffsetMm = 29,
                    DrawerHoleSpacingMm = 0,
                    DrawerHolePositionsMm = "29;220;453",
                    DrawerVerticalOffsetMm = 21,
                    DrawerHoleDiameterMm = 4.5,
                    FastenerName = "4x16 bolkopschroef"
                }
            };
        }

        public static ShelfSupportTemplate[] ShelfSupports()
        {
            return new[]
            {
                new ShelfSupportTemplate
                {
                    Id = "shelf_pin_5mm_32",
                    Name = "Legplankdrager pin 5mm, systeem 32",
                    ThicknessMm = 5,
                    HeightMm = 12,
                    HoleDiameterMm = 5,
                    HoleSpacingMm = 32,
                    FrontInsetMm = 50,
                    BackInsetMm = 50,
                    FirstHoleHeightMm = 160
                },
                new ShelfSupportTemplate
                {
                    Id = "shelf_pin_5mm_64",
                    Name = "Legplankdrager pin 5mm, grove stap 64",
                    ThicknessMm = 5,
                    HeightMm = 12,
                    HoleDiameterMm = 5,
                    HoleSpacingMm = 64,
                    FrontInsetMm = 50,
                    BackInsetMm = 50,
                    FirstHoleHeightMm = 160
                }
            };
        }

        public static ToolDefinition DefaultEndMill(double diameterMm, double passDepthMm)
        {
            return new ToolDefinition
            {
                Id = "endmill_" + diameterMm.ToString("0.##").Replace(",", ".") + "mm",
                Name = "Frees " + diameterMm.ToString("0.##") + "mm",
                Kind = ToolKind.EndMill,
                DiameterMm = diameterMm,
                FeedRateMmMin = 1800,
                PlungeRateMmMin = 400,
                SpindleRpm = 18000,
                PassDepthMm = passDepthMm
            };
        }
    }
}
