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
                    HoleCount = 4,
                    FirstHoleOffsetMm = 37,
                    HoleSpacingMm = 96,
                    VerticalOffsetMm = 32,
                    HoleDiameterMm = 6.5,
                    FastenerName = "4x16 bolkopschroef"
                },
                new RailTemplate
                {
                    Id = "generic_450",
                    Name = "AliExpress ladegeleider 450mm basis",
                    LengthMm = 450,
                    HoleCount = 5,
                    FirstHoleOffsetMm = 37,
                    HoleSpacingMm = 96,
                    VerticalOffsetMm = 32,
                    HoleDiameterMm = 6.5,
                    FastenerName = "4x16 bolkopschroef"
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
