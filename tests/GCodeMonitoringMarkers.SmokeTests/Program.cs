using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Manufacturing;

internal static class Program
{
    private static int Main()
    {
        try
        {
            VerifyNestedGenerator();
            VerifySinglePartGenerator();
            Console.WriteLine("PASS  RD-markers zijn aanwezig en veranderen geen machine-G-code.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  " + ex.Message);
            return 1;
        }
    }

    private static void VerifyNestedGenerator()
    {
        var fixture = CreateFixture();
        var enabled = CreateOptions(fixture.HoleTool, fixture.ContourTool, true);
        var disabled = CreateOptions(fixture.HoleTool, fixture.ContourTool, false);
        var generator = new NestedMach3GCodeGenerator();

        var withMarkers = generator.Generate(fixture.Stock, fixture.ContourTool, fixture.Machine, enabled, 2, 5, "plaat-03.tap");
        var withoutMarkers = generator.Generate(fixture.Stock, fixture.ContourTool, fixture.Machine, disabled, 2, 5, "plaat-03.tap");

        VerifyCommonContract(withMarkers, withoutMarkers);
        Require(withMarkers.Contains("(RD_META: PLATE=2)"), "Geneste G-code mist plaatnummermetadata.");
        Require(withMarkers.Contains("(RD_META: PLATE_COUNT=5)"), "Geneste G-code mist plaataantalmetadata.");
        Require(withMarkers.Contains("(RD_STEP: 01 Kleine gaten met 3mm-frees)"), "Stap voor kleine gaten ontbreekt.");
        Require(withMarkers.Contains("(RD_STEP: 02 Gaten vanaf 6mm met contourfrees)"), "Stap voor grote gaten ontbreekt.");
        Require(withMarkers.Contains("(RD_STEP: 03 Positioneergroeven en pockets)"), "Pocketstap ontbreekt.");
        Require(!withMarkers.Contains("(RD_STEP: 04 Kopkamers)"), "Lege kopkamerstap mag niet worden gepubliceerd.");
    }

    private static void VerifySinglePartGenerator()
    {
        var fixture = CreateFixture();
        var generator = new Mach3GCodeGenerator();
        var withMarkers = generator.GenerateSheetPart(
            fixture.Part, fixture.HoleTool, fixture.ContourTool, null, false, false, 1,
            fixture.Machine, 18, 10, 1, 1, 15, 1, 1600, 30, true);
        var withoutMarkers = generator.GenerateSheetPart(
            fixture.Part, fixture.HoleTool, fixture.ContourTool, null, false, false, 1,
            fixture.Machine, 18, 10, 1, 1, 15, 1, 1600, 30, false);

        VerifyCommonContract(withMarkers, withoutMarkers);
        Require(withMarkers.Contains("(RD_META: CONTRACT=1)"), "Contractversie ontbreekt in onderdeel-G-code.");
    }

    private static void VerifyCommonContract(string withMarkers, string withoutMarkers)
    {
        Require(withMarkers.Contains("(RD_EVENT: TOOL_CHANGE; TOOL="), "Toolwisselmarker ontbreekt.");
        Require(withMarkers.Contains("(RD_STEP: 07 Buitencontour voorfrezen)"), "Voorcontourstap ontbreekt.");
        Require(withMarkers.Contains("(RD_STEP: 08 Laatste contourlaag)"), "Eindcontourstap ontbreekt.");
        Require(withMarkers.Contains("(RD_EVENT: FINAL_CONTOUR_APPROACHING)"), "Waarschuwing voor de eindlaag ontbreekt.");
        Require(withMarkers.Contains("(RD_EVENT: FINAL_CONTOUR)"), "Actieve eindlaagmarker ontbreekt.");
        Require(!withoutMarkers.Contains("(RD_"), "Uitgeschakelde monitoring bevat nog RD-markers.");

        var markerLines = Lines(withMarkers).Where(IsMarker).ToArray();
        Require(markerLines.Length > 0, "Geen markerregels gevonden.");
        Require(markerLines.All(line => line.StartsWith("(RD_", StringComparison.Ordinal) && line.EndsWith(")", StringComparison.Ordinal)),
            "Monitoringmetadata moet uitsluitend uit volledige G-codecommentaarregels bestaan.");

        var approaching = LineIndex(withMarkers, "(RD_EVENT: FINAL_CONTOUR_APPROACHING)");
        var pause = NextLineIndex(withMarkers, approaching, "M0");
        var finalStep = LineIndex(withMarkers, "(RD_STEP: 08 Laatste contourlaag)");
        var active = LineIndex(withMarkers, "(RD_EVENT: FINAL_CONTOUR)");
        Require(approaching >= 0 && pause > approaching, "FINAL_CONTOUR_APPROACHING moet vóór de bewaakte M0 staan.");
        Require(finalStep > pause, "Stap 08 moet pas na de bewaakte M0 beginnen.");
        Require(active > finalStep, "FINAL_CONTOUR moet in de daadwerkelijke eindcontourstap staan.");

        var commandsWithMarkers = Lines(withMarkers).Where(line => !IsMarker(line)).ToArray();
        var commandsWithoutMarkers = Lines(withoutMarkers).Where(line => !IsMarker(line)).ToArray();
        Require(commandsWithMarkers.SequenceEqual(commandsWithoutMarkers, StringComparer.Ordinal),
            "Monitoring aan/uit wijzigde andere regels dan RD-commentaar.");
        VerifySafeRapidMoves(withMarkers, 15);
    }

    private static void VerifySafeRapidMoves(string program, double safeZmm)
    {
        double? modalZ = null;
        var lines = Lines(program);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (!line.StartsWith("G0 ", StringComparison.Ordinal) && !line.StartsWith("G1 ", StringComparison.Ordinal)) continue;

            foreach (var token in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length > 1 && token[0] == 'Z'
                    && double.TryParse(token.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    modalZ = z;
            }

            var rapidXY = line.StartsWith("G0 ", StringComparison.Ordinal)
                && line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(token => token.StartsWith("X", StringComparison.Ordinal) || token.StartsWith("Y", StringComparison.Ordinal));
            Require(!rapidXY || (modalZ.HasValue && modalZ.Value >= safeZmm - 0.001),
                "Onveilige X/Y-snelverplaatsing op regel " + (index + 1).ToString(CultureInfo.InvariantCulture)
                + ": " + line + " bij modal Z" + (modalZ.HasValue ? modalZ.Value.ToString("0.###", CultureInfo.InvariantCulture) : " onbekend") + ".");
        }
    }

    private static Fixture CreateFixture()
    {
        var material = new Material
        {
            Id = "test-18",
            Name = "Testplaat 18mm",
            Kind = MaterialKind.Sheet,
            ThicknessMm = 18,
            SheetLengthMm = 500,
            SheetWidthMm = 300
        };
        var part = new SheetPart
        {
            Name = "Monitor testdeel",
            Material = material,
            LengthMm = 200,
            WidthMm = 100,
            Quantity = 1,
            UseTabs = true
        };
        part.Holes.Add(new SheetHole { Name = "Pilotgat", Xmm = 25, Ymm = 25, DiameterMm = 3 });
        part.Holes.Add(new SheetHole { Name = "Potgat", Xmm = 70, Ymm = 25, DiameterMm = 35, DepthMm = 13, DepthMode = OperationDepthMode.PocketFromFace });
        part.Pockets.Add(new SheetPocket { Name = "Testpocket", Xmm = 20, Ymm = 55, LengthMm = 60, WidthMm = 12, DepthMm = 6 });

        var stock = new NestedStockSheet
        {
            Name = "Monitor testplaat",
            Material = material,
            StockLengthMm = 500,
            StockWidthMm = 300,
            SheetNumber = 2
        };
        stock.Placements.Add(new NestedSheetPlacement { Part = part, InstanceNumber = 1, Xmm = 30, Ymm = 30 });

        return new Fixture
        {
            Part = part,
            Stock = stock,
            HoleTool = Tool("t3", "Frees 3mm", 3, 18000, 2),
            ContourTool = Tool("t6", "Frees 6mm", 6, 20500, 4),
            Machine = new MachineProfile { Id = "test", Name = "Test Mach3", MaxXmm = 1000, MaxYmm = 1000, SafeZmm = 15 }
        };
    }

    private static CamJobOptions CreateOptions(ToolDefinition holeTool, ToolDefinition contourTool, bool enabled)
    {
        var options = CamJobOptions.FromPrimaryTool(holeTool);
        options.AddTool(contourTool);
        options.EnableMonitoringMarkers = enabled;
        options.ThroughCutOvertravelMm = 1;
        options.SafeTravelZMm = 15;
        options.ContourOnionSkinMm = 1;
        options.FinalContourFeedRateMmMin = 1600;
        options.FinalContourRampLengthMm = 30;
        return options;
    }

    private static ToolDefinition Tool(string id, string name, double diameter, double rpm, double passDepth)
    {
        return new ToolDefinition
        {
            Id = id,
            Name = name,
            Kind = ToolKind.EndMill,
            DiameterMm = diameter,
            SpindleRpm = rpm,
            FeedRateMmMin = 2400,
            PlungeRateMmMin = 500,
            PassDepthMm = passDepth
        };
    }

    private static string[] Lines(string value)
    {
        return value.Replace("\r\n", "\n").Split('\n');
    }

    private static bool IsMarker(string line)
    {
        return line.StartsWith("(RD_", StringComparison.Ordinal);
    }

    private static int LineIndex(string value, string exactLine)
    {
        return Array.IndexOf(Lines(value), exactLine);
    }

    private static int NextLineIndex(string value, int after, string exactLine)
    {
        var lines = Lines(value);
        for (var i = Math.Max(0, after + 1); i < lines.Length; i++)
            if (string.Equals(lines[i], exactLine, StringComparison.Ordinal)) return i;
        return -1;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture
    {
        public SheetPart Part { get; set; }
        public NestedStockSheet Stock { get; set; }
        public ToolDefinition HoleTool { get; set; }
        public ToolDefinition ContourTool { get; set; }
        public MachineProfile Machine { get; set; }
    }
}
