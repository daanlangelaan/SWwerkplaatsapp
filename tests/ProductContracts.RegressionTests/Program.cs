using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

internal static class Program
{
    private sealed class Contract
    {
        public string ProductId;
        public PortalQuoteRequest Request;
        public int Sheets;
        public int Profiles;
        public int Hardware;
        public int Feet;
        public int EndCaps;
        public int Fasteners;
        public int Placements;
    }

    private static int Main(string[] args)
    {
        try
        {
            var models = Contracts().Select(contract => new
            {
                Contract = contract,
                Model = new ProductModelBuildService().Build(contract.Request)
            }).ToArray();

            if (args.Any(arg => string.Equals(arg, "--dump", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var item in models)
                {
                    Console.WriteLine("{0}|sheets={1}|profiles={2}|hardware={3}|feet={4}|endcaps={5}|fasteners={6}|placements={7}",
                        item.Contract.ProductId,
                        PhysicalSheetCount(item.Model), PhysicalProfileCount(item.Model),
                        PhysicalHardwareCount(item.Model), FootCount(item.Model), EndCapCount(item.Model),
                        FastenerCount(item.Model), item.Model.AssemblyPlacements.Count);
                    if (item.Contract.ProductId == "robotcel" || item.Contract.ProductId == "machinebasis" || item.Contract.ProductId == "materiaalwagen")
                    {
                        foreach (var placement in item.Model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile))
                            Console.WriteLine("  {0}|L={1:0.###}|W={2:0.###}|H={3:0.###}|X={4:0.###}|Y={5:0.###}|Z={6:0.###}",
                                placement.PartName, placement.LengthMm, placement.WidthMm, placement.HeightMm,
                                placement.Xmm, placement.Ymm, placement.Zmm);
                    }
                }
                return 0;
            }

            foreach (var item in models) VerifyCountContract(item.Contract, item.Model);
            VerifyRobotCellGeometry(models.Single(item => item.Contract.ProductId == "robotcel").Model);
            VerifyMachineBaseGeometry(models.Single(item => item.Contract.ProductId == "machinebasis").Model);
            VerifyMaterialCartGeometry(models.Single(item => item.Contract.ProductId == "materiaalwagen").Model);
            VerifyMaterialCartVariants();
            Console.WriteLine("PASS  Productaantallen, voeten, eindkappen, bevestigingen, profieloriëntatie, coplanaire buitenvlakken en moduulbanen voldoen aan de contracten.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  " + ex.Message);
            return 1;
        }
    }

    private static Contract[] Contracts()
    {
        return new[]
        {
            C("cabinet", Request("cabinet", 2400, 600, 900, r => { r.UnitCount = 4; r.DefaultShelfCount = 3; r.DefaultDrawerCount = 1; r.IncludeBackPanel = true; }), 44, 0, 315, 0, 0, 307, 44),
            C("werktafel", Request("werktafel", 1500, 750, 900, null), 1, 12, 64, 0, 0, 44, 1),
            C("machinebasis", Request("machinebasis", 2000, 800, 2000, r => { r.MachineBaseWorktopHeightMm = 900; }), 14, 36, 718, 0, 4, 646, 129),
            C("robotcel", Request("robotcel", 1200, 800, 900, r => { r.RobotCellIntermediateBeamMaxSpacingMm = 700; }), 1, 15, 32, 4, 2, 26, 30),
            C("materiaalwagen", Request("materiaalwagen", 1000, 650, 950, r => { r.MaterialCartShelfCount = 3; r.MaterialCartShelfMaterialId = "hpl_10_lex"; r.MaterialCartHandleSide = "right"; r.MaterialCartSteeringMode = "fixed-and-swivel"; }), 3, 22, 40, 0, 0, 0, 33),
            C("werktafel_lex", Request("werktafel_lex", 1650, 1000, 833, null), 6, 13, 212, 4, 8, 128, 66),
            C("werktafel_lex_revolution", Request("werktafel_lex_revolution", 1650, 1000, 833, null), 6, 13, 212, 4, 8, 128, 66),
            C("werkbankkast", Request("werkbankkast", 2400, 600, 900, r => { r.UnitCount = 4; r.DefaultShelfCount = 3; r.IncludeBackPanel = true; }), 28, 0, 64, 0, 0, 52, 28),
            C("vakjeskast", Request("vakjeskast", 300, 270, 400, r => { r.CubbyCellWidthMm = 100; r.CubbyCellDepthMm = 90; r.CubbyCellHeightMm = 100; r.CubbyColumnCount = 3; r.CubbyRowCount = 4; r.CubbyGridInsetMm = 20; r.IncludeBackPanel = true; }), 10, 0, 38, 0, 0, 0, 10),
            C("shipping_box", Request("shipping_box", 1200, 800, 800, r => { r.SheetMaterialId = "osb_18"; r.ShippingBoxIncludeHandles = true; }), 6, 0, 40, 0, 0, 0, 46),
        };
    }

    private static Contract C(string productId, PortalQuoteRequest request, int sheets, int profiles, int hardware, int feet, int endCaps, int fasteners, int placements)
    {
        return new Contract { ProductId = productId, Request = request, Sheets = sheets, Profiles = profiles, Hardware = hardware, Feet = feet, EndCaps = endCaps, Fasteners = fasteners, Placements = placements };
    }

    private static PortalQuoteRequest Request(string product, double width, double depth, double height, Action<PortalQuoteRequest> customize)
    {
        var request = new PortalQuoteRequest { Product = product, WidthMm = width, DepthMm = depth, HeightMm = height, Quantity = 1 };
        if (customize != null) customize(request);
        return request;
    }

    private static void VerifyCountContract(Contract contract, WorkbenchModel model)
    {
        Require(model.ProductId == contract.ProductId, contract.ProductId + ": verkeerd ProductId");
        Require(contract.Sheets >= 0, contract.ProductId + ": telcontract is nog niet bevroren; voer --dump uit");
        Require(PhysicalSheetCount(model) == contract.Sheets, contract.ProductId + ": plaataantal gewijzigd");
        Require(PhysicalProfileCount(model) == contract.Profiles, contract.ProductId + ": profielaantal gewijzigd");
        Require(PhysicalHardwareCount(model) == contract.Hardware, contract.ProductId + ": beslagtelling gewijzigd");
        Require(FootCount(model) == contract.Feet, contract.ProductId + ": aantal stelvoeten gewijzigd");
        Require(EndCapCount(model) == contract.EndCaps, contract.ProductId + ": aantal eind-/afdekkappen gewijzigd");
        Require(FastenerCount(model) == contract.Fasteners, contract.ProductId + ": aantal bevestigingen gewijzigd");
        Require(model.AssemblyPlacements.Count == contract.Placements, contract.ProductId + ": assemblytelling gewijzigd");
    }

    private static void VerifyRobotCellGeometry(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).ToArray();
        var lower = profiles.Where(p => p.PartName.StartsWith("Onderligger ", StringComparison.Ordinal)).ToArray();
        Require(lower.Length == 4, "Robotcel: onderste omtrek moet exact vier liggers bevatten");
        Require(profiles.Count(p => p.PartName.StartsWith("Onderste dwarsligger 40x80", StringComparison.Ordinal)) == 1,
            "Robotcel: onderste laag moet exact één dwarsligger bevatten");
        foreach (var placement in lower.Concat(profiles.Where(p => p.PartName.StartsWith("Onderste dwarsligger", StringComparison.Ordinal))))
        {
            Require(Close(placement.HeightMm, 80), "Robotcel: " + placement.PartName + " ligt vlak in plaats van 40x80 staand");
            Require(Close(Math.Min(placement.LengthMm, placement.WidthMm), 40), "Robotcel: verkeerde dwarse profielmaat voor " + placement.PartName);
        }

        var uprights = profiles.Where(p => p.PartName.StartsWith("Staander 80x80", StringComparison.Ordinal)).ToArray();
        Require(uprights.Length == 4, "Robotcel: vier 80x80-staanders vereist");
        var uprightX = uprights.Max(p => Math.Abs(p.Xmm));
        var uprightZ = uprights.Max(p => Math.Abs(p.Zmm));
        foreach (var side in lower.Where(p => p.PartName.Contains("links") || p.PartName.Contains("rechts")))
            Require(Close(Math.Abs(Math.Abs(side.Xmm) - uprightX), 20), "Robotcel: zij-onderligger staat niet op een geldige 40-mm moduulbaan");
        foreach (var beam in lower.Where(p => p.PartName.Contains("voor") || p.PartName.Contains("achter")))
            Require(Close(Math.Abs(Math.Abs(beam.Zmm) - uprightZ), 20), "Robotcel: voor/achter-onderligger staat niet op een geldige 40-mm moduulbaan");

        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Zwarte eindkap 8 160x40", StringComparison.Ordinal)) == 2,
            "Robotcel: achterrail vereist exact twee passende eindkappen");
        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("TechXXL adapterplaat / voetplaat 10 80x80 M16", StringComparison.Ordinal)) == 4,
            "Robotcel: vier 80x80/M16-adapterplaten vereist");
        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("TechXXL stelvoet D80 schotel", StringComparison.Ordinal)) == 4,
            "Robotcel: vier M16-stelvoeten vereist");

        foreach (var upright in uprights)
        {
            var side = lower.Single(p => p.PartName.Contains(upright.Xmm < 0 ? "links" : "rechts"));
            var frontBack = lower.Single(p => p.PartName.Contains(upright.Zmm < 0 ? "voor" : "achter"));
            Require(Close(OutsideFace(side.Xmm, side.LengthMm, upright.Xmm), OutsideFace(upright.Xmm, upright.LengthMm, upright.Xmm)),
                "Robotcel: zij-onderligger ligt niet gelijk aan het buitenvlak van de staander");
            Require(Close(OutsideFace(frontBack.Zmm, frontBack.WidthMm, upright.Zmm), OutsideFace(upright.Zmm, upright.WidthMm, upright.Zmm)),
                "Robotcel: voor/achter-onderligger ligt niet gelijk aan het buitenvlak van de staander");
        }
    }

    private static void VerifyMachineBaseGeometry(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).ToArray();
        var uprights = profiles.Where(p => p.PartName.StartsWith("Staander ", StringComparison.Ordinal)
            && Close(p.LengthMm, 40) && Close(p.WidthMm, 80)).ToArray();
        Require(uprights.Length == 4, "Machinebasis: vier staanders vereist");
        foreach (var upright in uprights)
            Require(Close(upright.LengthMm, 40) && Close(upright.WidthMm, 80), "Machinebasis: staanderdoorsnede moet X=40 en Z=80 zijn");
        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Zwarte eindkap 40x80", StringComparison.Ordinal)) == 4,
            "Machinebasis: vier eindkappen op het bovenframe vereist");
        Require(profiles.All(p => p.LengthMm > 0 && p.WidthMm > 0 && p.HeightMm > 0), "Machinebasis: profiel met nulafmeting gevonden");
    }

    private static void VerifyMaterialCartGeometry(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).ToArray();
        Require(profiles.Count(p => p.PartName.StartsWith("Hoekstaander 40x40", StringComparison.Ordinal)) == 4,
            "Materiaalwagen: vier doorlopende hoekstaanders vereist");
        for (var layer = 1; layer <= 3; layer++)
        {
            var prefix = "Legbordlaag " + layer + " ";
            var layerProfiles = profiles.Where(p => p.PartName.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            Require(layerProfiles.Length == 5, "Materiaalwagen: elke standaardlaag moet vier omtrekliggers en één middenligger bevatten");
            Require(layerProfiles.All(p => Close(p.HeightMm, 40)), "Materiaalwagen: alle laagprofielen moeten 40 mm hoog liggen");
        }
        Require(model.AssemblyPlacements.Count(p => p.Kind == AssemblyComponentKind.Sheet && p.PartName.StartsWith("Legbord ", StringComparison.Ordinal)) == 3,
            "Materiaalwagen: exact drie legborden vereist");
        Require(model.AssemblyPlacements.Count(p => p.VisualKind == "hardware-wheel") == 4,
            "Materiaalwagen: exact vier D100-wielen vereist");
        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Duwbeugel ", StringComparison.Ordinal)) == 3,
            "Materiaalwagen: duwbeugel moet uit twee staanders en één greep bestaan");
        Require(EndCapCount(model) == 0, "Materiaalwagen: verbonden of afgedekte profielkoppen mogen geen losse eindkappen krijgen");
    }

    private static void VerifyMaterialCartVariants()
    {
        var compact = new ProductModelBuildService().Build(Request("materiaalwagen", 800, 500, 850, r =>
        {
            r.MaterialCartShelfCount = 2;
            r.MaterialCartShelfMaterialId = "betonplex_18";
            r.MaterialCartHandleSide = "none";
            r.MaterialCartSteeringMode = "four-swivel";
        }));
        Require(compact.Sheets.Sum(p => Math.Max(1, p.Quantity)) == 2, "Materiaalwagen compact: twee legborden vereist");
        Require(compact.AssemblyPlacements.Count(p => p.PartName.StartsWith("Legbordlaag ", StringComparison.Ordinal)) == 8,
            "Materiaalwagen compact: onder 1000 mm breedte zijn vier omtrekliggers per laag en geen middenligger vereist");
        Require(compact.AssemblyPlacements.All(p => !p.PartName.StartsWith("Duwbeugel ", StringComparison.Ordinal)),
            "Materiaalwagen compact: keuze zonder duwbeugel moet alle beugeldelen verwijderen");
        Require(compact.Hardware.Any(h => (h.ArticleNumber ?? "").Contains("101074") && h.Quantity == 2),
            "Materiaalwagen compact: vier-zwenkwielmodus vereist twee vrije D100-zwenkwielen");

        var maximum = new ProductModelBuildService().Build(Request("materiaalwagen", 1800, 1000, 1200, r =>
        {
            r.MaterialCartShelfCount = 4;
            r.MaterialCartShelfMaterialId = "hpl_10_lex";
            r.MaterialCartHandleSide = "left";
            r.MaterialCartSteeringMode = "fixed-and-swivel";
        }));
        Require(maximum.Sheets.Sum(p => Math.Max(1, p.Quantity)) == 4, "Materiaalwagen maximum: vier legborden vereist");
        Require(maximum.AssemblyPlacements.Count(p => p.PartName.Contains("middenligger")) == 4,
            "Materiaalwagen maximum: iedere brede laag vereist één middenligger");
        Require(maximum.AssemblyPlacements.Count(p => p.PartName.StartsWith("Duwbeugel ", StringComparison.Ordinal)) == 3,
            "Materiaalwagen maximum: linker duwbeugel moet uit drie profielen bestaan");
    }

    private static int PhysicalSheetCount(WorkbenchModel model) { return model.Sheets.Sum(p => Math.Max(1, p.Quantity)); }
    private static int PhysicalProfileCount(WorkbenchModel model) { return model.Profiles.Sum(p => Math.Max(1, p.Quantity)); }
    private static int PhysicalHardwareCount(WorkbenchModel model) { return model.Hardware.Sum(p => Math.Max(0, p.Quantity)); }
    private static int FootCount(WorkbenchModel model) { return HardwareCount(model, "stelvoet", "verstelbare voet"); }
    private static int EndCapCount(WorkbenchModel model) { return HardwareCount(model, "eindkap", "afdekkap"); }
    private static int FastenerCount(WorkbenchModel model)
    {
        var tokens = new[] { "schroef", "bout", "moer", "ring", "connector", "bevestig", "hoekanker", "t-slot" };
        return model.Hardware.Where(item => tokens.Any(token => (item.Name ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
            .Sum(item => Math.Max(0, item.Quantity));
    }
    private static int HardwareCount(WorkbenchModel model, params string[] tokens)
    {
        return model.Hardware.Where(item => tokens.Any(token => (item.Name ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
            .Sum(item => Math.Max(0, item.Quantity));
    }
    private static double OutsideFace(double center, double size, double direction) { return center + Math.Sign(direction) * size / 2.0; }
    private static bool Close(double left, double right) { return Math.Abs(left - right) <= 0.01; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
