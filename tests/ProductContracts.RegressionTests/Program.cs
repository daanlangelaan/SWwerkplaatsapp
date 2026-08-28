using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Manufacturing;
using SWWerkplaats.Configurator.Portal;
using SWWerkplaats.Configurator.SolidWorks;

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
                    if (item.Contract.ProductId == "robotcel" || item.Contract.ProductId == "lineaire_robotcel" || item.Contract.ProductId == "machinebasis" || item.Contract.ProductId == "materiaalwagen" || item.Contract.ProductId == "sim_rig_4080" || item.Contract.ProductId == "hoogteverstelbare_werktafel")
                    {
                        foreach (var placement in item.Model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile))
                            Console.WriteLine("  {0}|L={1:0.###}|W={2:0.###}|H={3:0.###}|X={4:0.###}|Y={5:0.###}|Z={6:0.###}",
                                placement.PartName, placement.LengthMm, placement.WidthMm, placement.HeightMm,
                                placement.Xmm, placement.Ymm, placement.Zmm);
                    }
                }
                return 0;
            }

            foreach (var item in models)
            {
                VerifyCountContract(item.Contract, item.Model);
                VerifyProfileTraceability(item.Contract.ProductId, item.Model);
                VerifyProfileStickerPolicy(item.Contract.ProductId, item.Model);
                VerifyProfileProductionSequence(item.Contract.ProductId, item.Model);
                VerifyProfileProjectConfiguration(item.Contract.ProductId, item.Model);
            }
            VerifyRobotCellGeometry(models.Single(item => item.Contract.ProductId == "robotcel").Model);
            VerifyLinearRobotCellGeometry(models.Single(item => item.Contract.ProductId == "lineaire_robotcel").Model);
            VerifyLinearRobotCellConceptReleaseContract(models.Single(item => item.Contract.ProductId == "lineaire_robotcel").Contract.Request);
            VerifyMachineBaseGeometry(models.Single(item => item.Contract.ProductId == "machinebasis").Model);
            VerifyMachineBaseAssemblyInstructions(models.Single(item => item.Contract.ProductId == "machinebasis").Model);
            VerifyMachineBaseStickerPolicy(models.Single(item => item.Contract.ProductId == "machinebasis").Model);
            VerifyProfileProjectOutput(models.Single(item => item.Contract.ProductId == "machinebasis").Contract.Request);
            VerifyGenericCustomerPresentationDrawing(
                models.Single(item => item.Contract.ProductId == "machinebasis").Contract.Request,
                models.Single(item => item.Contract.ProductId == "machinebasis").Model);
            VerifyMaterialCartGeometry(models.Single(item => item.Contract.ProductId == "materiaalwagen").Model);
            VerifyMaterialCartVariants();
            VerifySimRigGeometry(models.Single(item => item.Contract.ProductId == "sim_rig_4080").Model);
            VerifySimRigVariants();
            VerifyLexMotionContract(models.Single(item => item.Contract.ProductId == "werktafel_lex").Contract.Request,
                models.Single(item => item.Contract.ProductId == "werktafel_lex").Model);
            VerifyLexGeometry(models.Single(item => item.Contract.ProductId == "werktafel_lex").Model);
            VerifyLexReleaseAudit(models.Single(item => item.Contract.ProductId == "werktafel_lex").Contract.Request);
            VerifyLexMotionExports(models.Single(item => item.Contract.ProductId == "werktafel_lex").Contract.Request);
            VerifyHeightAdjustableWorkbenchGeometry(
                models.Single(item => item.Contract.ProductId == "hoogteverstelbare_werktafel").Contract.Request,
                models.Single(item => item.Contract.ProductId == "hoogteverstelbare_werktafel").Model);
            VerifyHeightAdjustableWorkbenchVariants();
            VerifyProfileSlotGeometryMasterdata();
            VerifyWorkbenchBackPanelConnections(models.Single(item => item.Contract.ProductId == "werkbankkast").Model);
            VerifyPortalPresentationBoundary();
            VerifyWorkbenchQuotePreview();
            VerifySolidWorksGlbDeliveryContract();
            VerifyDamagedSheetRecovery();
            Console.WriteLine("PASS  Productaantallen, voeten, eindkappen, bevestigingen, profieloriëntatie, coplanaire buitenvlakken en moduulbanen voldoen aan de contracten.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  " + ex.Message);
            return 1;
        }
    }

    private static void VerifyWorkbenchQuotePreview()
    {
        var response = new QuoteApplicationService().BuildQuote(Request("werktafel", 1500, 750, 900, null));
        Require(response.ProfilePartCount == 12
                && response.Assembly3D.Count(part => string.Equals(part.Kind, "profile", StringComparison.OrdinalIgnoreCase)) == 12
                && response.Assembly3D.Any(part => string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase))
                && response.PriceIncVat > 0
                && response.NestingSvg.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
                && !response.Files.Contains("Profieltappen-werkplaatslijst.xlsx"),
            "Werktafelpreview: prijs, nesting en 12 getraceerde profielen moeten zonder niet-vrijgegeven CAM-taplijst renderen");
    }

    private static void VerifyGenericCustomerPresentationDrawing(PortalQuoteRequest request, WorkbenchModel model)
    {
        var exporterType = typeof(SolidWorksCustomerPresentation).Assembly.GetType(
            "SWWerkplaats.Configurator.SolidWorks.SolidWorksCustomerPowerPointExporter",
            true);
        var method = exporterType.GetMethod(
            "BuildOrthographicSvg",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Require(method != null, "Klantpresentatie: orthografische SVG-export ontbreekt");

        var parts = new PortalAssembly3DService().Build(model, request);
        var folder = Path.Combine(Path.GetTempPath(), "sww-customer-presentation-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = (string)method.Invoke(null, new object[]
            {
                parts, request, folder, "machinebasis-voor.svg", "front",
                1000d, 460d, false, 0d, 0d
            });
            Require(File.Exists(path) && File.ReadAllText(path).StartsWith("<svg", StringComparison.OrdinalIgnoreCase),
                "Klantpresentatie: een product zonder LEX-beweging moet een vooraanzicht kunnen exporteren");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static void VerifySolidWorksGlbDeliveryContract()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "SWWerkplaats-GlbContract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        try
        {
            var controlPath = Path.Combine(tempFolder, "WORKSTATION_TEST_CONTROLE.SLDPRT");
            var validPath = Path.Combine(tempFolder, "valid.glb");
            WriteGlbJson(validPath,
                "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"name\":\"current camera\"},{\"name\":\"WORKSTATION_TEST_CONTROLE\"}]," +
                "\"materials\":[{\"name\":\"Aluminium_profiel_sleuf_L2_N1\",\"pbrMetallicRoughness\":{\"baseColorTexture\":{\"index\":0}}}]}" );
            SolidWorksCustomerPresentation.ValidateGlbForDelivery(validPath, controlPath, true);

            var regressedPath = Path.Combine(tempFolder, "regressed.glb");
            WriteGlbJson(regressedPath,
                "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"name\":\"current camera\"},{\"name\":\"Part69\"}]," +
                "\"materials\":[{\"name\":\"color\",\"pbrMetallicRoughness\":{}}]}" );
            var rejected = false;
            try
            {
                SolidWorksCustomerPresentation.ValidateGlbForDelivery(regressedPath, controlPath, true);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Require(rejected,
                "HD-klantmodelcontract: een generieke PartXX-root zonder systeemprofielappearance moet worden afgekeurd");
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    private static void WriteGlbJson(string path, string json)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var paddedLength = (jsonBytes.Length + 3) / 4 * 4;
        var bytes = new byte[12 + 8 + paddedLength];
        Encoding.ASCII.GetBytes("glTF").CopyTo(bytes, 0);
        BitConverter.GetBytes(2).CopyTo(bytes, 4);
        BitConverter.GetBytes(bytes.Length).CopyTo(bytes, 8);
        BitConverter.GetBytes(paddedLength).CopyTo(bytes, 12);
        BitConverter.GetBytes(0x4E4F534A).CopyTo(bytes, 16);
        Buffer.BlockCopy(jsonBytes, 0, bytes, 20, jsonBytes.Length);
        for (var index = 20 + jsonBytes.Length; index < bytes.Length; index++) bytes[index] = 0x20;
        File.WriteAllBytes(path, bytes);
    }

    private static void VerifyLexMotionContract(PortalQuoteRequest request, WorkbenchModel model)
    {
        var parts = new PortalAssembly3DService().Build(model, request);
        var motion = new PortalMotionContractService().Build(model, request, parts);
        Require(motion != null, "LEX: bewegingscontract ontbreekt");
        Require(Close(motion.Horizontal.Minimum, -220) && Close(motion.Horizontal.Maximum, 220),
            "LEX: horizontale eindstanden wijken af van de drie borgposities");
        Require(Close(motion.Vertical.ReferenceValueMm, 848) && Close(motion.Vertical.Maximum, 400),
            "LEX: werkhoogtebereik is niet 848–1248 mm");
        Require(Close(motion.MaximumOverhangMm, 560),
            "LEX: maximale oversteek ten opzichte van de buitenzijde van de poten is niet 560 mm");
        Require(parts.Any(part => part.MotionTranslateXPerMm == -1 && (part.Name ?? "").Contains("Kogelpotblad")),
            "LEX: werkblad is niet gekoppeld aan de horizontale bewegingsas");
        Require(parts.Where(part => (part.Name ?? "").StartsWith("Voetprofiel ", StringComparison.OrdinalIgnoreCase)
                    || (part.Name ?? "").StartsWith("Vast railframe ", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part.Name, "HTE2 kolom", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part.Name, "HSR15R wagen", StringComparison.OrdinalIgnoreCase))
                .All(part => Close(part.MotionTranslateXPerMm, 0)),
            "LEX: onderstel, vaste railframe, hefkolommen en vaste HSR15R-wagens moeten horizontaal wereldvast blijven");
        Require(parts.Where(part => (part.Name ?? "").Contains("Kogelpotblad")
                    || (part.Name ?? "").StartsWith("Bewegend buitenframe ", StringComparison.OrdinalIgnoreCase)
                    || (part.Name ?? "").StartsWith("Werkbladhouder ", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part.Name, "HSR15 rail 1440", StringComparison.OrdinalIgnoreCase))
                .All(part => Close(part.MotionTranslateXPerMm, -1)),
            "LEX: blad, bewegend frame, werkbladhouders en rails moeten samen over de vaste wagens verschuiven");
        var worktopBrackets = model.AssemblyPlacements.Where(part => string.Equals(
            part.ComponentId, WorktopBracketPlacementService.ComponentId, StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(worktopBrackets.Length == 10
                && worktopBrackets.GroupBy(part => Math.Round(part.Zmm, 3)).All(group => group.Count() == 2)
                && model.Hardware.Single(item => item.ArticleNumber == WorktopBracketPlacementService.ArticleNumber).Quantity == 10,
            "LEX: vijf X-draagprofielen moeten elk een symmetrisch paar TIN 100391-werkbladbeugels en een gesynchroniseerde BOM-regel hebben");
        Require(model.StructuralCalculation != null && model.StructuralCalculation.Status == "IndicativeOnly"
                && model.StructuralCalculation.ParallelBeamCount == 5 && model.StructuralCalculation.CalculatedDeflectionMm > 0,
            "LEX: indicatieve belasting-/stijfheidsberekening voor vijf draagliggers ontbreekt");
        var rails = parts.Where(part => string.Equals(part.Name, "HSR15 rail 1440", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(rails.Length == 2 && rails.All(part => Close(part.SizeXmm, 1440) && part.Holes.Count == 24),
            "LEX: beide HSR15-rails moeten op 1440 mm met 24 behouden patroonboringen zijn afgezaagd");
        var stops = parts.Where(part => string.Equals(part.Name, "HSR15 mechanische eindstop", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(stops.Length == 4 && stops.All(part => Close(Math.Abs(part.Xmm), 699.3)),
            "LEX: de vier mechanische eindstops moeten op de afgeleide wagen-contactposities staan");
        var carriages = parts.Where(part => string.Equals(part.Name, "HSR15R wagen", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(carriages.Length == 4 && carriages.All(part => Close(Math.Abs(part.Xmm), 445)),
            "LEX: de vier HSR15R-wagens moeten op 890 mm h.o.h. boven de poot- en vaste zijframeharten staan");
        Require(parts.Where(part => (part.Name ?? "").Contains("stabilisatieplaat"))
                .All(part => Close(part.MotionTranslateYPerMm, 0)),
            "LEX: de HPL-stabilisatieplaat is aan de vaste kolomlichamen bevestigd en mag niet met de hoogte-as meebewegen");
        Require(parts.Count(part => part.MotionSizeYPerMm == 1 && string.Equals(part.Name, "HTE2 kolom", StringComparison.OrdinalIgnoreCase)) == 2,
            "LEX: beide HTE2-kolommen moeten telescopisch aan de hoogte-as zijn gekoppeld");
    }

    private static void VerifyHeightAdjustableWorkbenchGeometry(PortalQuoteRequest request, WorkbenchModel model)
    {
        var profilePlacements = model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile).ToArray();
        var upper = profilePlacements.Where(item => (item.PartName ?? string.Empty).Contains("bovenframe")).ToArray();
        var connectors = profilePlacements.Where(item => (item.PartName ?? string.Empty).StartsWith("Onderstelaansluiting bovenframe", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(profilePlacements.Length == 8 && upper.Length == 6 && connectors.Length == 2,
            "Hoogteverstelbare werktafel: manifest vereist 2 voetprofielen, 4 randprofielen en exact 2 kolomaansluitprofielen");
        Require(connectors.Select(item => item.Xmm).OrderBy(value => value).SequenceEqual(new[] { -445.0, 445.0 })
            && connectors.All(item => Close(item.HeightMm, 40) && Close(item.LengthMm, 40) && Close(item.WidthMm, 920)),
            "Hoogteverstelbare werktafel: beide Z-aansluitprofielen moeten op de HTE2-kolomharten staan");
        Require(model.Sheets.Single(item => item.Name == "Vast werkblad").Holes.Count == 0,
            "Hoogteverstelbare werktafel: het vaste blad mag geen kogelpotgaten bevatten");
        var forbidden = model.AssemblyPlacements.Where(item =>
            (item.PartName ?? string.Empty).IndexOf("HSR15", StringComparison.OrdinalIgnoreCase) >= 0
            || (item.PartName ?? string.Empty).IndexOf("kogelpot", StringComparison.OrdinalIgnoreCase) >= 0
            || (item.PartName ?? string.Empty).IndexOf("borgpositie", StringComparison.OrdinalIgnoreCase) >= 0
            || (item.PartName ?? string.Empty).IndexOf("eindstop", StringComparison.OrdinalIgnoreCase) >= 0
            || (item.PartName ?? string.Empty).IndexOf("Schwenkriegel", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(forbidden.Length == 0, "Hoogteverstelbare werktafel: horizontale Workstation-keten of kogelpotten zijn niet volledig verwijderd");
        Require(model.AssemblyConnections.Count(item => item.JointType == AssemblyJointType.StandardConnector) == 8,
            "Hoogteverstelbare werktafel: de vier randknooppunten en vier aansluitprofielknooppunten moeten acht standaardverbinders opleveren");
        var worktopBrackets = model.AssemblyPlacements.Where(part => string.Equals(
            part.ComponentId, WorktopBracketPlacementService.ComponentId, StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(worktopBrackets.Length == 8
                && worktopBrackets.GroupBy(part => Math.Round(part.Xmm, 3)).All(group => group.Count() == 2)
                && model.Hardware.Single(item => item.ArticleNumber == WorktopBracketPlacementService.ArticleNumber).Quantity == 8,
            "Hoogteverstelbare werktafel: vier Z-draagprofielen moeten elk een symmetrisch paar TIN 100391-werkbladbeugels en een gesynchroniseerde BOM-regel hebben");
        Require(model.StructuralCalculation != null && model.StructuralCalculation.Status == "IndicativeOnly"
                && model.StructuralCalculation.ParallelBeamCount == 2 && model.StructuralCalculation.CalculatedDeflectionMm > 0
                && model.StructuralCalculation.OpenData.Count >= 4,
            "Hoogteverstelbare werktafel: transparante indicatieve belasting-/stijfheidsberekening met open data ontbreekt");
        var hte2Offer = MasterDataRuntimeCatalog.LoadRequired().Records("offers").Single(row =>
            string.Equals(MasterDataRuntimeCatalog.Value(row, "Aanbieding-ID"), "PRI-BESLAG-GEMING-HTE2-O1-400-2COL-SET", StringComparison.OrdinalIgnoreCase));
        Require(string.IsNullOrWhiteSpace(MasterDataRuntimeCatalog.Value(hte2Offer, "Product-ID")),
            "HTE2-setprijs moet generiek zijn en mag niet aan Workstation-product-ID's zijn gekoppeld");
        var price = new PortalPricingService().Calculate(model);
        Require(price.Lines.Any(line => line.Key == WorktopBracketPlacementService.ComponentId
                    && line.Quantity == 8 && line.PurchaseUnitPrice == 1.62m && line.OfferId == "CAT-TECHXXL-100391")
                && price.Lines.Any(line => line.Key == "geming_hte2_o1_400"
                    && line.Quantity == 1 && line.PurchaseUnitPrice == 371.63m),
            "Hoogteverstelbare werktafel: TIN 100391 en de generieke HTE2-set moeten met leveranciersprijs in de calculatie staan");

        var parts = new PortalAssembly3DService().Build(model, request);
        var motion = new PortalMotionContractService().Build(model, request, parts);
        Require(motion != null && motion.Horizontal == null && motion.Vertical != null
            && Close(motion.Vertical.ReferenceValueMm, 850)
            && Close(motion.Vertical.Minimum, -80) && Close(motion.Vertical.Maximum, 280),
            "Hoogteverstelbare werktafel: alleen het verticale 770..1130-mm bewegingscontract mag worden aangeboden");
        Require(parts.Where(item => (item.Name ?? string.Empty).StartsWith("Voetprofiel ", StringComparison.OrdinalIgnoreCase)).All(item => Close(item.MotionTranslateYPerMm, 0))
            && parts.Where(item => string.Equals(item.Name, "Vast werkblad", StringComparison.OrdinalIgnoreCase)).All(item => Close(item.MotionTranslateYPerMm, 1)),
            "Hoogteverstelbare werktafel: onderstelbasis moet vast blijven en het blad moet met de hefhoogte meebewegen");
    }

    private static void VerifyHeightAdjustableWorkbenchVariants()
    {
        var model4080 = new ProductModelBuildService().Build(Request("hoogteverstelbare_werktafel", 1650, 1000, 850, request =>
        {
            request.ProfileMaterialId = "alu_system_80x40";
            request.SheetMaterialId = "hpl_10_lex";
        }));
        var upper = model4080.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile
            && (item.PartName ?? string.Empty).Contains("bovenframe")).ToArray();
        Require(upper.Length == 6 && upper.All(item => Close(item.HeightMm, 80))
            && upper.All(item => Close(Math.Min(item.LengthMm, item.WidthMm), 40)),
            "Hoogteverstelbare werktafel: 40x80-keuze moet voor alle zes bovenframeleden numeriek staand zijn (Y=80)");
        Require(model4080.Hardware.Any(item => string.Equals(item.Name, "Afdekkap 8 80x40 zwart", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.ArticleNumber, "TIN 100192 / S208AK8040", StringComparison.OrdinalIgnoreCase)
                && item.Quantity == 4),
            "Hoogteverstelbare werktafel: 40x80-variant moet de vrijgegeven 80x40-afdekkap uit masterdata gebruiken");

        var plywoodRequest = Request("hoogteverstelbare_werktafel", 1650, 1000, 850, request =>
        {
            request.ProfileMaterialId = "alu_system_40x40";
            request.SheetMaterialId = "multiplex_okoume_grandplex_40";
        });
        var plywoodModel = new ProductModelBuildService().Build(plywoodRequest);
        var plywoodTop = plywoodModel.Sheets.Single(item => string.Equals(item.Name, "Vast werkblad", StringComparison.OrdinalIgnoreCase));
        Require(plywoodTop.Material.Id == "multiplex_okoume_grandplex_40"
                && Close(plywoodTop.Material.ThicknessMm, 40)
                && Close(plywoodTop.Material.SheetLengthMm, 2500)
                && Close(plywoodTop.Material.SheetWidthMm, 1220)
                && plywoodTop.Material.RenderAppearance == "multiplex-gelaagd",
            "Hoogteverstelbare werktafel: Okoumé-keuze moet het volledige 40-mm materiaalcontract uit masterdata gebruiken");
        var plywoodAssemblyTop = new PortalAssembly3DService().Build(plywoodModel, plywoodRequest)
            .Single(item => string.Equals(item.Name, "Vast werkblad", StringComparison.OrdinalIgnoreCase));
        Require(plywoodAssemblyTop.MaterialAppearance == "multiplex-gelaagd"
                && plywoodAssemblyTop.MaterialThicknessAxis == "y",
            "Hoogteverstelbare werktafel: de backend moet materiaalweergave en plaatdikte-as aan de viewer leveren");
        var plywoodCatalogOption = new CatalogApplicationService().GetCatalog().Sheets
            .Single(item => item.Id == "multiplex_okoume_grandplex_40");
        Require(plywoodCatalogOption.Name == "Okoumé 40 mm",
            "Hoogteverstelbare werktafel: de klantkeuze moet de korte masterdatanaam tonen");
        var plywoodPreview = new ProductionOutputService().BuildPreview(plywoodRequest);
        var plywoodPrice = new PortalPricingService().Calculate(plywoodPreview.Model, plywoodPreview.NestingPlan);
        Require(plywoodPrice.Lines.Any(line => line.Key == "multiplex_okoume_grandplex_40"
                    && line.Quantity == 1m && line.Unit == "plaat"
                    && line.PurchaseUnitPrice == 237.35m
                    && line.PurchaseTotal == 237.35m
                    && line.OfferId == "CAT-GGOEDKOOP-0000000732"
                    && line.SupplierId == "SUP-GOEDKOOP"),
            "Hoogteverstelbare werktafel: Okoumé 40 mm moet de generieke Goedkoop-aanbieding uit masterdata gebruiken");

        var rejected = false;
        try
        {
            new ProductModelBuildService().Build(Request("hoogteverstelbare_werktafel", 1650, 1000, 850,
                request => request.ProfileMaterialId = "alu_system_80x80"));
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Require(rejected, "Hoogteverstelbare werktafel: een profiel buiten 40x40/40x80 moet door het backendcontract worden geweigerd");
    }

    private static void VerifyLinearRobotCellConceptReleaseContract(PortalQuoteRequest request)
    {
        var release = new ProductReleaseContractService().LoadRequired(request.Product);
        Require(!release.ProductionReleased
                && release.ConceptExportOutputs.SequenceEqual(new[] { "SolidWorks", "Projectdata" })
                && release.OpenReleaseItems.Length >= 6
                && release.OpenReleaseItems.Any(item => item.Contains("adapterplaat")),
            "Lineaire robotcel: conceptexport en openstaande vrijgavepunten moeten uit productmasterdata komen");

        var catalogItem = new ProductCatalogApplicationService().ListProducts()
            .Single(item => string.Equals(item.Product, request.Product, StringComparison.OrdinalIgnoreCase));
        Require(!catalogItem.ProductionReleased
                && catalogItem.ConceptExportOutputs.SequenceEqual(release.ConceptExportOutputs)
                && catalogItem.OpenReleaseItems.SequenceEqual(release.OpenReleaseItems),
            "Lineaire robotcel: portalcatalogus moet hetzelfde vrijgavecontract leveren als de backend");

        var selectionMethod = typeof(ProductionOutputService).GetMethod(
            "EnsureProjectExportSelectionAllowed",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Require(selectionMethod != null, "Lineaire robotcel: exportselectiecontrole ontbreekt");
        selectionMethod.Invoke(null, new object[] { request, release, false, true, false, false, false, false, true });
        var camBlocked = false;
        try
        {
            selectionMethod.Invoke(null, new object[] { request, release, true, true, false, false, false, false, true });
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            camBlocked = ex.InnerException is InvalidOperationException
                && ex.InnerException.Message.Contains("Niet toegestaan: CAM");
        }
        Require(camBlocked, "Lineaire robotcel: CAM moet geblokkeerd blijven terwijl concept-SolidWorks is toegestaan");

        var productionBlocked = false;
        try
        {
            new ProductionOutputService().GenerateOrderFiles(request, Path.Combine(Path.GetTempPath(), "sww-lrc-blocked-" + Guid.NewGuid().ToString("N")));
        }
        catch (InvalidOperationException ex)
        {
            productionBlocked = ex.Message.Contains("Productie-export geblokkeerd");
        }
        Require(productionBlocked, "Lineaire robotcel: productie-export mag door de conceptexport niet worden vrijgegeven");

        var placementEnvelopeIds = new[]
        {
            "hiwin_hgr20_rail",
            "hiwin_hgh20ca_block",
            "linear_robot_adapter_plate_provisional",
            "linear_motor_adapter_plate_provisional",
            "rack_pinion_drive_provisional"
        };
        var primitiveService = new ComponentPrimitiveRenderContractService();
        Require(placementEnvelopeIds.All(id => primitiveService.BuildRequired(id).Primitives.Any(part => part.InheritPlacementDimensions)),
            "Lineaire robotcel: parametrische inkoop- en conceptcomponenten moeten hun maten uit het backend-placementcontract erven");

        var preview = new ProductionOutputService().BuildPreview(request);
        var renderedParts = new PortalAssembly3DService().Build(preview.Model, request);
        Require(placementEnvelopeIds.All(id => renderedParts.Any(part => string.Equals(part.ComponentId, id, StringComparison.OrdinalIgnoreCase)
                    && part.SizeXmm > 0 && part.SizeYmm > 0 && part.SizeZmm > 0)),
            "Lineaire robotcel: alle plaatsingsenveloppen moeten daadwerkelijk als positieve 3D-onderdelen renderen");
        Require(renderedParts.Count(part => string.Equals(part.ComponentId, "datasensing_sg4_30_105_oo_e_emitter", StringComparison.OrdinalIgnoreCase)) == 1
                && renderedParts.Count(part => string.Equals(part.ComponentId, "datasensing_sg4_30_105_oo_e_receiver", StringComparison.OrdinalIgnoreCase)) == 1,
            "Lineaire robotcel: de 1200-mm standaardcel moet de fysiek passende SG4 BASE 1050-mm zender en ontvanger renderen");
        Require(renderedParts.Count(part => string.Equals(part.ComponentId, "techxxl_leveling_foot_d80_m16x150", StringComparison.OrdinalIgnoreCase)) >= 30,
            "Lineaire robotcel: iedere stelvoet moet als schotel, kantelgewricht en zichtbare M16-spindel renderen");
        Require(preview.Model.DesignNotes.Any(note => note.StartsWith("Conceptpreview zonder nesting:", StringComparison.Ordinal)),
            "Lineaire robotcel: een te groot conceptplaatdeel mag de 3D-render niet blokkeren maar moet als niet-vrijgegeven nestingnotitie terugkomen");

        var portalHtml = PortalHtml.Page();
        Require(portalHtml.Contains("Conceptproduct — niet productievrijgegeven")
                && portalHtml.Contains("Exporteer conceptbasis")
                && portalHtml.Contains("ConceptExportOutputs"),
            "Lineaire robotcel: portal moet conceptstatus, openpunten en toegestane exportkeuzes tonen");
    }

    private static void VerifyLexGeometry(WorkbenchModel model)
    {
        var movingProfiles = model.Profiles.Where(profile =>
            (profile.Name ?? "").StartsWith("Bewegend", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(movingProfiles.Length == 3
                && movingProfiles.All(profile => profile.Material != null
                    && string.Equals(profile.Material.Id, "alu_system_40x40", StringComparison.OrdinalIgnoreCase)),
            "LEX: buitenframe en alle drie bladliggers moeten uit het globale 40x40-profiel komen");
        Require(model.AssemblyPlacements.Where(part =>
                    (part.PartName ?? "").StartsWith("Bewegend buitenframe ", StringComparison.OrdinalIgnoreCase)
                    || (part.PartName ?? "").StartsWith("Werkbladhouder ", StringComparison.OrdinalIgnoreCase))
                .All(part => Close(part.HeightMm, 40)),
            "LEX: de verticale renderhoogte van het volledige bewegende frame moet 40 mm zijn");

        var lower = model.AssemblyPlacements.First(part => string.Equals(part.PartName, "HTE2 O1 onderplaat 280x65", StringComparison.OrdinalIgnoreCase));
        var body = model.AssemblyPlacements.First(part => string.Equals(part.PartName, "HTE2 kolom", StringComparison.OrdinalIgnoreCase));
        var upper = model.AssemblyPlacements.First(part => string.Equals(part.PartName, "HTE2 O1 bovenplaat 280x65", StringComparison.OrdinalIgnoreCase));
        var fixedFrame = model.AssemblyPlacements.First(part => string.Equals(part.PartName, "Vast railframe voor", StringComparison.OrdinalIgnoreCase));
        Require(Close(lower.Ymm - lower.HeightMm / 2.0, 80)
                && Close(lower.Ymm + lower.HeightMm / 2.0, body.Ymm - body.HeightMm / 2.0)
                && Close(body.Ymm + body.HeightMm / 2.0, upper.Ymm - upper.HeightMm / 2.0)
                && Close(upper.Ymm + upper.HeightMm / 2.0, fixedFrame.Ymm - fixedFrame.HeightMm / 2.0),
            "LEX: HTE2-platen, kolomlichaam en vast frame moeten zonder overlap of spleet op elkaar aansluiten");
        Require(Close(body.HeightMm + lower.HeightMm + upper.HeightMm, 600),
            "LEX: HTE2-installatiemaat inclusief beide O1-platen moet 600 mm zijn");

        var top = model.AssemblyPlacements.First(part => string.Equals(part.PartName, "Kogelpotblad HPL", StringComparison.OrdinalIgnoreCase));
        var topSheet = model.Sheets.First(sheet => string.Equals(sheet.Name, "Kogelpotblad HPL", StringComparison.OrdinalIgnoreCase));
        Require(Close(top.Ymm + topSheet.Material.ThicknessMm / 2.0, 848),
            "LEX: bovenzijde van het werkblad moet in de lage stand exact 848 mm zijn");
        Require(model.Hardware.Any(item => string.Equals(item.ArticleNumber, "TIN 100184 / S208AK4040", StringComparison.OrdinalIgnoreCase)
                && item.Quantity == 4),
            "LEX: vier exacte TechXXL 40x40-afdekkappen moeten in de BOM staan");
        Require(model.Hardware.Any(item => string.Equals(item.ArticleNumber, "TIN 100342 / S208ZP", StringComparison.OrdinalIgnoreCase)
                    && item.Quantity == 26)
                && model.Hardware.Any(item => string.Equals(item.ArticleNumber, "TIN 100673 / S208HS825", StringComparison.OrdinalIgnoreCase)
                    && item.Quantity == 26),
            "LEX: de 26 fysiek afgeleide standaardverbinders en 26 bijbehorende M8x25-bouten moeten als afzonderlijke exacte leveranciersartikelen in de BOM staan");

        var connections = model.AssemblyConnections.Where(connection => connection.JointType == AssemblyJointType.StandardConnector).ToArray();
        Require(connections.Length == 26
                && connections.Count(connection => (connection.TappedPartName ?? "").StartsWith("Vast railframe", StringComparison.OrdinalIgnoreCase)) == 16
                && connections.Count(connection => !(connection.TappedPartName ?? "").StartsWith("Vast railframe", StringComparison.OrdinalIgnoreCase)) == 10
                && connections.All(connection => connection.Status == AssemblyDataStatus.Confirmed
                    && !string.IsNullOrWhiteSpace(connection.AccessFaceId) && connection.AccessSlotIndex > 0),
            "LEX: fysieke profielcontacten moeten 16 vaste-frame- en 10 bewegende verbindingen met exacte D0-D3/S-baan opleveren");
        Require(model.ProfileOperations.Count(operation => operation.Kind == ProfileOperationKind.Tap) == 26
                && model.ProfileOperations.Count(operation => operation.Kind == ProfileOperationKind.Drill
                    && (operation.Note ?? "").StartsWith("Toegangsgat standaardverbinder", StringComparison.OrdinalIgnoreCase)) == 26,
            "LEX: iedere standaardverbinder moet exact één M8-kopse tap en één Ø7-sleuteltoegangsgat leveren");
        var plan = new AssemblyInstructionPlanningService().Build(model);
        Require(plan.Available && plan.SequenceConfirmed
                && plan.Steps.SelectMany(step => step.ConnectionIds).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 26,
            "LEX: de projectbrede assemblageplanner moet alle 26 fysieke verbindingen zonder product-eigen instructielogica opnemen");
        var rendered = new PortalAssembly3DService().Build(model, Request("werktafel_lex", 1650, 1000, 848, null));
        Require(rendered.SelectMany(part => part.Holes).Count(hole => string.Equals(hole.VisualRole, "connector-access", StringComparison.OrdinalIgnoreCase)) == 26,
            "LEX: dezelfde 26 verbindingen moeten als echte profielgaten in het rendercontract zichtbaar zijn");

        var structuralBolt = new FastenerDefinition
        {
            Id = "TEST_M8",
            UsageKind = FastenerUsageKind.StructuralBolt,
            AvailableLengthsMm = new[] { 12.0, 16.0, 20.0, 25.0, 30.0 }
        };
        Require(Close(FastenerSelectionService.SelectTSlotBoltLength(structuralBolt, 10, 2, 8, 12, 1), 20),
            "Projectbreed boutbeleid: tel de gemonteerde draadinlaat op en kies de langste verkrijgbare maat die vóór de sleufbodem stopt");
        try
        {
            FastenerSelectionService.SelectTSlotBoltLength(structuralBolt, 10, 2, 8, 0, 1);
            Require(false, "Projectbreed boutbeleid: ontbrekende maximale insteekdiepte mag nooit een fallbacklengte geven");
        }
        catch (ArgumentOutOfRangeException) { }
        var nutThreadZones = ProfileNutThreadZoneCatalog.LoadRequired();
        Require(Close(nutThreadZones.Required("techxxl_t_nut_8_m8", 8).UsableThreadZoneMm, 6.25)
                && Close(nutThreadZones.Required("techxxl_t_nut_8_m8", 8).ThreadInletOffsetMm, 3.7)
                && nutThreadZones.Required("techxxl_t_nut_8_m8", 8).ThroughThread
                && Close(nutThreadZones.Required("techxxl_t_nut_8_sliding_m4", 4).UsableThreadZoneMm, 9.8)
                && Close(nutThreadZones.Required("techxxl_t_nut_8_sliding_m4", 4).ThreadInletOffsetMm, 1.0)
                && nutThreadZones.Required("techxxl_t_nut_8_sliding_m4", 4).ThroughThread,
            "Projectbreed boutbeleid: M8-inklikmoer en M4-schuifmoer moeten draadzone, gemonteerde draadinlaat en doorlopend gat uit runtime-masterdata leveren");
        Require(model.ProfileFastenerCalculations.Count == 4
                && model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-hsr15-carriage-m4").SelectedLengthMm == 12
                && model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-adapter-m8").SelectedLengthMm == 20
                && model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-rail-m4").SelectedLengthMm == 20
                && model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-hte2-m8").SelectedLengthMm == 16
                && Close(model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-hte2-m8").SelectedThreadEngagementMm.Value, 6.25)
                && Close(model.ProfileFastenerCalculations.Single(value => value.CalculationId == "lex-hte2-m8").RemainingBottomClearanceMm.Value, 1.25)
                && model.ProfileFastenerCalculations.All(value => value.Status == AssemblyDataStatus.Confirmed)
                && model.Hardware.Where(item => new[] { "LEX_HSR15_ADAPTER_M8_TNUT", "LEX_HSR15_RAIL_TNUT_M4", "LEX_HTE2_ENDPLATE_M8_TNUT" }
                    .Contains(item.ArticleNumber, StringComparer.OrdinalIgnoreCase))
                    .All(item => !string.Equals(item.BomStatus, "OPEN - boutlengte geblokkeerd", StringComparison.OrdinalIgnoreCase)
                        && item.Note.IndexOf("Centrale boutberekening", StringComparison.OrdinalIgnoreCase) >= 0),
            "Projectbreed boutbeleid: leveranciersinterface en profiel-sleufbodem moeten M4x12, M8x20, M4x20 en HTE2 M8x16 kiezen zonder resterende blokkade");
    }

    private static void VerifyLexMotionExports(PortalQuoteRequest request)
    {
        var model = new ProductModelBuildService().Build(request);
        var parts = new PortalAssembly3DService().Build(model, request);
        var motion = new PortalMotionContractService().Build(model, request, parts);
        var envelopeAudit = new WorkbenchCabinetAuditResult();
        var envelopeMethod = typeof(WorkbenchCabinetAuditService).GetMethod("CheckAssemblyEnvelope", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var rangeMethod = typeof(ProductionOutputService).GetMethod("BuildMotionRangeSvg", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var htmlMethod = typeof(ProductionOutputService).GetMethod("BuildAssembly3DHtml", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Require(envelopeMethod != null && rangeMethod != null && htmlMethod != null, "LEX: interne envelop- of bereikbeeldexport ontbreekt");
        envelopeMethod.Invoke(null, new object[] { model, request, envelopeAudit });
        Require(envelopeAudit.Passed,
            "LEX: leveranciersgeometrie van kogelpotten en Item-aanslagen moet binnen de vrijgegeven externe product-envelop vallen: "
            + string.Join(" | ", envelopeAudit.Errors.ToArray()));
        var heightSvg = (string)rangeMethod.Invoke(null, new object[] { parts, motion, true });
        var horizontalSvg = (string)rangeMethod.Invoke(null, new object[] { parts, motion, false });
        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        var html = (string)htmlMethod.Invoke(null, new object[] { parts, motion, serializer });
        Require(heightSvg.Contains("848 mm werkhoogte") && heightSvg.Contains("1248 mm werkhoogte"),
            "LEX: laagste en hoogste werkhoogte ontbreken in het bereikbeeld");
        Require(horizontalSvg.Contains("maximale oversteek") && horizontalSvg.Contains("560 mm"),
            "LEX: 560 mm maximale oversteek ontbreekt in het horizontale bereikbeeld");
        Require(horizontalSvg.Contains(">120 mm</text>")
                && horizontalSvg.Contains(">340 mm</text>")
                && horizontalSvg.Contains(">560 mm</text>")
                && horizontalSvg.Contains("marker-start:url(#dimensionArrow)"),
            "LEX: poot-tot-bladuiteinde-maten 120/340/560 mm ontbreken bij links, midden en rechts");
        Require(html.Contains("id=\"horizontal\"") && html.Contains("id=\"vertical\"")
                && html.Contains("MotionTranslateXPerMm") && html.Contains("-1"),
            "LEX: het zelfstandige 3D-klantmodel mist de twee bewegingsregelaars of partkoppelingen");
        Require(html.Contains("updateMotionLabels();rebuild(false)")
                && !html.Contains("updateMotionLabels();rebuild(true)")
                && html.Contains("(p.CoreHoles||[]).forEach"),
            "LEX: het zelfstandige 3D-klantmodel moet bij beweging dezelfde vaste camera en geometriekoppelingen als de portal houden");
        Require(html.Contains("WebGLRenderer")
                && html.Contains("buildThreeParts(THREE,group,adjustedParts())")
                && html.Contains("openTSlotModuleShape")
                && html.Contains("data:text/javascript;base64,")
                && !html.Contains("function box(part,a,s)"),
            "LEX: het zelfstandige 3D-klantmodel gebruikt niet dezelfde WebGL-profielrenderer als de portal");

        var customerModelPathMethod = typeof(ProductionOutputService).GetMethod(
            "InteractiveCustomerModelPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var customerFolder = Path.Combine("C:\\Project", "03_Klantvoorstel");
        Require(customerModelPathMethod != null
                && string.Equals(
                    (string)customerModelPathMethod.Invoke(null, new object[] { customerFolder }),
                    Path.Combine(customerFolder, "3D-model.html"),
                    StringComparison.OrdinalIgnoreCase),
            "Portalexport: het interactieve 3D-klantmodel hoort rechtstreeks in 03_Klantvoorstel, niet onder Aanzichten");
    }

    private static void VerifyLexReleaseAudit(PortalQuoteRequest request)
    {
        var audit = new ProductionOutputService().AuditWorkbenchCabinet(request);
        Require(audit.Passed,
            "LEX: de volledige vrijgavecontrole blokkeert een geldige standaardexport: "
            + string.Join(" | ", audit.Errors.ToArray()));
    }

    private static void VerifyPortalPresentationBoundary()
    {
        var catalog = new CatalogApplicationService().GetCatalog();
        Require(catalog.Presentation != null && catalog.Presentation.ContractVersion == 2,
            "Portal: het versieerbare UI-presentatiecontract moet via /api/catalog beschikbaar zijn");
        Require(catalog.Presentation.DesignTokens != null
            && catalog.Presentation.DesignTokens.Colors.ContainsKey("assemblyActive")
            && catalog.Presentation.DesignTokens.Colors.ContainsKey("plywoodFaceBase")
            && catalog.Presentation.DesignTokens.Typography.FontSizesPx.ContainsKey("body")
            && catalog.Presentation.DesignTokens.SpacingPx.ContainsKey("large")
            && catalog.Presentation.DesignTokens.ControlsPx["minimumTarget"] == 44
            && catalog.Presentation.DesignTokens.BreakpointsPx.ContainsKey("compact")
            && catalog.Presentation.Assembly.Animation.ContainsKey("hardwareMoveMs")
            && catalog.Presentation.Assembly.Camera.ContainsKey("detailWidthFill")
            && catalog.Presentation.Assembly.Materials.ContainsKey("plywoodLayerBands"),
            "Portal: kleur, typografie, spacing, bediening, animatie en camerafit moeten centraal in het presentatiecontract staan");

        var portalHtml = PortalHtml.Page();
        Require(portalHtml.Contains("id=\"exportIncludeInteractiveCustomerModel\"")
                && portalHtml.Contains("id=\"exportIncludeHighDefinitionCustomerModel\"")
                && typeof(PortalQuoteRequest).GetProperty("ExportIncludeInteractiveCustomerModel") != null
                && typeof(PortalQuoteRequest).GetProperty("ExportIncludeHighDefinitionCustomerModel") != null,
            "Portalexport: interactief 3D en high-definition 3D + SW-bron moeten afzonderlijk selecteerbaar zijn");
        var archiveMethod = typeof(ProductionOutputService).GetMethod("ShouldKeepSolidWorksArchive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Require(archiveMethod != null
                && (bool)archiveMethod.Invoke(null, new object[] { new PortalQuoteRequest { ExportIncludeSolidWorks = false, ExportIncludeHighDefinitionCustomerModel = true } })
                && !(bool)archiveMethod.Invoke(null, new object[] { new PortalQuoteRequest { ExportIncludeSolidWorks = false, ExportIncludeHighDefinitionCustomerModel = false } }),
            "Portalexport: high-definition 3D moet de native SolidWorks-map als naslag behouden");
        Require(portalHtml.Contains("<option value=\"\" selected disabled>")
                && !portalHtml.Contains("<option value=\" selected disabled>"),
            "Portal: een lege select-placeholder moet een geldige lege HTML-waarde hebben");
        var forbiddenAssemblyFallbacks = new[]
        {
            "ProvisionalRenderEnvelope",
            "techxxl_standard_connector_8_40",
            "techxxl_button_head_iso7380_m8x25",
            "AccessHoleDiameterMm||7",
            "access.diameter||7",
            "diameter:7,depth:12",
            "function profileModuleCenters",
            "CircleGeometry(3.25",
            "headCenter=endpoint+info.sign*22",
            "clipHalf=2.6",
            "const intro=360,duration=820",
            "Math.sin(time*.0062)",
            ">SW5 · 4×</text>",
            ">Ø7</text>",
            "hpl_",
            "multiplex_okoume",
            "alu_system_",
            "selectedSheetThickness",
            "syncWorkbenchCabinetFootGeometry",
            "setCubbyDefaults",
            "syncCubbyDimensions",
            "syncSlidingDoorUi",
            "machinebase-",
            "addMachineBaseFootplate",
            "addPa40Hinge",
            "addCrateSpringClip",
            "addLevelingCaster",
            "addMaterialCartCaster",
            "BoxGeometry(40,15,80)",
            "faceHalf-4.14",
            "Math.max(40,Number(data.travel)||96)",
            "/^Zijwand/i",
            "/^Toegangsgat standaardverbinder/i",
            "crossB[1]>=75&&crossA[1]<=45?20:0",
            "OffsetFromAnchorEndMm)||30",
            "LongitudinalSizeMm)||48",
            "TransverseSizeMm)||16",
            "item.Quantity||1",
            "value=\"1\" min=\"1\" max=\"99\""
        };
        Require(forbiddenAssemblyFallbacks.All(token => !portalHtml.Contains(token)),
            "Portal: assemblagetechniek en presentatiewaarden mogen niet opnieuw als UI-fallback worden ingebouwd");
        Require(portalHtml.Contains("applyPresentationContract(c.presentation)")
            && portalHtml.Contains("presentationNumber('animation','hardwareMoveMs')")
            && portalHtml.Contains("presentationNumber('camera','detailWidthFill')")
            && portalHtml.Contains("MaterialAppearance==='multiplex-gelaagd'")
            && portalHtml.Contains("presentationNumber('materials','plywoodLayerBands')")
            && portalHtml.Contains("presentationColor('plywoodFaceBase')"),
            "Portal: de assemblageweergave moet het centrale presentatiecontract werkelijk gebruiken");
        Require(portalHtml.Contains("renderProductCards(c.products)")
            && portalHtml.Contains("id=\"productChoices\"")
            && portalHtml.Contains("renderConfigurationFields(meta)")
            && portalHtml.Contains("data-request-field")
            && !portalHtml.Contains("onclick=\"chooseProduct('machinebasis')\""),
            "Portal: productkaarten en configuratievelden moeten uit /api/catalog komen en niet als productdata in HTML staan");
        var materialCart = catalog.Products.Single(product => product.Product == "materiaalwagen");
        var machineBase = catalog.Products.Single(product => product.Product == "machinebasis");
        var workbench = catalog.Products.Single(product => product.Product == "werktafel");
        var lexWorkbench = catalog.Products.Single(product => product.Product == "werktafel_lex");
        var lexRevolution = catalog.Products.Single(product => product.Product == "werktafel_lex_revolution");
        var heightAdjustableWorkbench = catalog.Products.Single(product => product.Product == "hoogteverstelbare_werktafel");
        var simRig = catalog.Products.Single(product => product.Product == "sim_rig_4080");
        var shippingBox = catalog.Products.Single(product => product.Product == "shipping_box");
        var cabinet = catalog.Products.Single(product => product.Product == "cabinet");
        var workbenchCabinet = catalog.Products.Single(product => product.Product == "werkbankkast");
        var robotCell = catalog.Products.Single(product => product.Product == "robotcel");
        Require(cabinet.CardImageStatus == "Beschikbaar"
            && cabinet.CardImageUrl == "/images/product-cabinet.png"
            && cabinet.CardImageAlt.Contains("cabinet")
            && machineBase.CardImageUrl == "/images/product-machinebase.png"
            && lexWorkbench.CardImageUrl == "/images/product-lex-workbench.jpg"
            && workbenchCabinet.CardImageUrl == "/images/product-workbench-cabinet.jpg"
            && robotCell.CardImageStatus == "Ontbreekt"
            && string.IsNullOrWhiteSpace(robotCell.CardImageUrl)
            && portalHtml.Contains("product.CardImageUrl")
            && portalHtml.Contains("Productrender nog niet beschikbaar")
            && !portalHtml.Contains("initials=name.split")
            && !portalHtml.Contains("choiceGlyph"),
            "Portalcatalogus: kaartafbeeldingen moeten uit productmasterdata komen en initialen mogen geen afbeeldingsfallback zijn");
        Require(catalog.Profiles.Single(profile => profile.Id == "alu_system_40x40").Name.StartsWith("40×40 — ", StringComparison.Ordinal)
            && catalog.Profiles.Single(profile => profile.Id == "alu_system_80x40").Name.StartsWith("40×80 — ", StringComparison.Ordinal),
            "Portalcatalogus: profielkeuzes moeten hun doorsnedemaat expliciet in het backendlabel tonen");
        Require(materialCart.InputConstraints.Length == 3
            && Close(materialCart.InputConstraints.Single(rule => rule.InputId == "widthMm").Minimum, 600)
            && Close(materialCart.InputConstraints.Single(rule => rule.InputId == "widthMm").Maximum, 1800)
            && materialCart.DefaultProfileMaterialId == "alu_system_40x40",
            "Portalcatalogus: grenzen en standaardprofiel van de materiaalwagen moeten uit productmasterdata komen");
        Require(workbench.CanConfigure && workbench.MissingConfigurationData.Length == 0
            && workbench.DefaultQuantity == 1
            && workbench.DefaultProfileMaterialId == "alu_system_40x40"
            && Close(workbench.InputConstraints.Single(rule => rule.InputId == "widthMm").Minimum, 300)
            && Close(workbench.InputConstraints.Single(rule => rule.InputId == "widthMm").Maximum, 3020)
            && Close(workbench.InputConstraints.Single(rule => rule.InputId == "depthMm").Maximum, 1520)
            && Close(workbench.InputConstraints.Single(rule => rule.InputId == "heightMm").Maximum, 2400),
            "Portalcatalogus: de losse Werktafel moet uit expliciete maat- en groef-8-regels configureren");
        var machineBaseWorktop = machineBase.ConfigurationInputs.Single(input => input.RequestField == "MachineBaseWorktopMaterialId");
        var machineBaseLowerBeam = machineBase.ConfigurationInputs.Single(input => input.RequestField == "MachineBaseLowerBeamProfileId");
        Require(machineBase.CanConfigure && machineBase.MissingConfigurationData.Length == 0
            && machineBase.DefaultQuantity == 1
            && Close(machineBase.InputConstraints.Single(rule => rule.InputId == "widthMm").Maximum, 3050)
            && Close(machineBase.InputConstraints.Single(rule => rule.InputId == "depthMm").Maximum, 1300)
            && machineBaseWorktop.DefaultValue == "hpl_10_lex"
            && machineBaseWorktop.Options.Select(option => option.Value).SequenceEqual(new[] { "hpl_10_lex" })
            && machineBaseLowerBeam.DefaultValue == "alu_system_40x40"
            && machineBaseLowerBeam.Options.Select(option => option.Value).SequenceEqual(new[] { "alu_system_40x40", "alu_system_80x40" }),
            "Portalcatalogus: machinebasis moet configureerbaar zijn vanuit exacte werkblad-, onderligger- en maatregels");
        Require(!materialCart.CanConfigure
            && materialCart.MissingConfigurationData.Any(value => value.Contains("legbordmateriaalkeuze")),
            "Portalcatalogus: ontbrekend legbordmateriaal van de materiaalwagen moet configuratie expliciet blokkeren");
        Require(lexWorkbench.CanConfigure && lexWorkbench.MissingConfigurationData.Length == 0
            && lexRevolution.CanConfigure && lexRevolution.MissingConfigurationData.Length == 0
            && lexWorkbench.InputConstraints.Length == 3
            && lexRevolution.InputConstraints.Length == 3
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "widthMm").Minimum, 1200)
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "widthMm").Maximum, 2200)
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "depthMm").Minimum, 700)
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "depthMm").Maximum, 1400)
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "heightMm").Minimum, 848)
            && Close(lexWorkbench.InputConstraints.Single(rule => rule.InputId == "heightMm").Maximum, 1248)
            && lexWorkbench.InputConstraints.Select(rule => (rule.InputId, rule.Minimum, rule.Maximum))
                .SequenceEqual(lexRevolution.InputConstraints.Select(rule => (rule.InputId, rule.Minimum, rule.Maximum))),
            "Portalcatalogus: beide identieke Workstation-varianten moeten hetzelfde erfbare maatcontract gebruiken");
        Require(heightAdjustableWorkbench.CanConfigure && heightAdjustableWorkbench.MissingConfigurationData.Length == 0
            && heightAdjustableWorkbench.AllowedProfileMaterialIds.SequenceEqual(new[] { "alu_system_40x40", "alu_system_80x40" })
            && heightAdjustableWorkbench.AllowedSheetMaterialIds.SequenceEqual(new[] { "hpl_10_lex", "multiplex_okoume_grandplex_40" })
            && heightAdjustableWorkbench.DefaultSheetMaterialId == "hpl_10_lex"
            && Close(heightAdjustableWorkbench.InputConstraints.Single(rule => rule.InputId == "heightMm").Minimum, 770)
            && Close(heightAdjustableWorkbench.InputConstraints.Single(rule => rule.InputId == "heightMm").Maximum, 1130)
            && !heightAdjustableWorkbench.ProductionReleased
            && !heightAdjustableWorkbench.OpenReleaseItems.Contains("werkbladbevestiging")
            && heightAdjustableWorkbench.OpenReleaseItems.Contains("HTE2-zijgroefdata-voor-stabilisatieplaat")
            && heightAdjustableWorkbench.OpenReleaseItems.Contains("belasting-en-stijfheidscontrole"),
            "Portalcatalogus: hoogteverstelbare werktafel moet volledig uit het eigen profiel-, hoogte- en vrijgavecontract configureren");
        Require(simRig.InputConstraints.Length == 3
            && Close(simRig.InputConstraints.Single(rule => rule.InputId == "depthMm").Minimum, 1200)
            && Close(simRig.InputConstraints.Single(rule => rule.InputId == "depthMm").Maximum, 1800)
            && simRig.DefaultProfileMaterialId == "alu_system_80x40",
            "Portalcatalogus: sim-riggrenzen en profieldefault moeten uit productmasterdata komen");
        Require(shippingBox.DefaultSheetMaterialId == "osb_18"
            && portalHtml.Contains("applyProductCatalogContract(meta)")
            && portalHtml.Contains("if(meta.CanConfigure)quote();")
            && portalHtml.Contains("threeState.preserveCameraOnNextRebuild=true")
            && portalHtml.Contains("if(!threeState.preserveCameraOnNextRebuild)threeState.forceFit=true")
            && portalHtml.Contains("$('quantity').value=meta.DefaultQuantity")
            && !portalHtml.Contains("$('quantity').value=''")
            && portalHtml.Contains("let offset=Number(sticker.OffsetFromAnchorEndMm)")
            && !portalHtml.Contains("$('sheetMaterialId').value='osb_18'")
            && !portalHtml.Contains("$('profileMaterialId').value='alu_profile_40x40'"),
            "Portalcatalogus: materiaaldefaults mogen uitsluitend uit /api/catalog komen en een geldige standaardconfiguratie moet direct renderen");
    }

    private static void VerifyProfileSlotGeometryMasterdata()
    {
        var catalog = new ProfileSlotGeometryCatalog();
        VerifyProfileSlotGeometry(catalog, "alu_system_40x40", 4, 1, new[] { 20.0 }, new[] { 20.0 });
        VerifyProfileSlotGeometry(catalog, "alu_system_80x40", 6, 2, new[] { 20.0, 60.0 }, new[] { 20.0 });
        VerifyProfileSlotGeometry(catalog, "alu_system_80x80", 8, 4, new[] { 20.0, 60.0 }, new[] { 20.0, 60.0 });
        VerifyProfileSlotGeometry(catalog, "alu_system_160x40", 10, 4, new[] { 20.0, 60.0, 100.0, 140.0 }, new[] { 20.0 });
        var profile40 = catalog.FindRequired("alu_system_40x40");
        Require(profile40.Status == "ExactSupplierGeometry"
            && profile40.SlotMouthWidthMm.HasValue && Close(profile40.SlotMouthWidthMm.Value, 16)
            && profile40.SlotMouthDepthMm.HasValue && Close(profile40.SlotMouthDepthMm.Value, 4.5)
            && profile40.SlotCavityWidthMm.HasValue && Close(profile40.SlotCavityWidthMm.Value, 20)
            && profile40.SlotCavityDepthMm.HasValue && Close(profile40.SlotCavityDepthMm.Value, 12)
            && profile40.OutsideCornerRadiusMm.HasValue && Close(profile40.OutsideCornerRadiusMm.Value, 4)
            && profile40.CoreHoleDiameterMm.HasValue && Close(profile40.CoreHoleDiameterMm.Value, 6.8),
            "40x40 groef-8 F14-profiel moet de fysieke TechXXL sleufmond, kamer, R4 en kernboring gebruiken");

        var blocked = false;
        try { catalog.FindRequired("alu_profile_30x30"); }
        catch (InvalidOperationException) { blocked = true; }
        Require(blocked, "30-mm profielserie mag niet stilzwijgend de 40-mm sleufasregel erven");
    }

    private static void VerifyProfileProjectOutput(PortalQuoteRequest request)
    {
        var folder = Path.Combine(Path.GetTempPath(), "sww-profile-configuration-" + Guid.NewGuid().ToString("N"));
        try
        {
            var output = new ProductionOutputService().GenerateOrderFiles(request, folder);
            var manifestPath = Path.Combine(folder, "Profielconfiguratie.json");
            var validationPath = Path.Combine(folder, "Profielconfiguratie-validatie.txt");
            Require(File.Exists(manifestPath) && File.Exists(validationPath),
                "Projectexport: canonieke profielconfiguratie of validatierapport ontbreekt");
            var service = new ProfileProjectConfigurationService();
            var manifest = service.Deserialize(File.ReadAllText(manifestPath));
            Require(output.Files.Contains("Profielconfiguratie.json") && manifest.Profiles.Count > 0 && manifest.Connections.Count > 0,
                "Projectexport: manifest bevat niet de volledige profiel- en verbindingsconfiguratie");
            foreach (var file in new[]
            {
                "Afkortlijst.csv", "Boorlijst.csv", "Profielbewerkingen.csv", "Profielstickers.csv",
                "Profielstickers-freesvolgorde.xlsx", "Profieltappen-werkplaatslijst.xlsx",
                "Profielbewerkingen-visuele-controle.svg", "ProfielCNC-Operatorprogramma.tap"
            }) Require(File.Exists(Path.Combine(folder, file)), "Projectexport: afgeleide profieluitvoer ontbreekt: " + file);
            var program = File.ReadAllText(Path.Combine(folder, "ProfielCNC-Operatorprogramma.tap"));
            var connectorAccess = manifest.Connections
                .Where(connection => connection.JointType == AssemblyJointType.StandardConnector.ToString()).ToArray();
            Require(!manifest.ProductionReleased && program.Contains("NIET VRIJGEGEVEN")
                && manifest.ProductionBlockers.Any(blocker => blocker.Contains("nog niet fysiek gevalideerd"))
                && connectorAccess.Length > 0 && connectorAccess.All(connection => connection.AccessHoleProductionReady
                    && connection.AccessHoleDiameterMm > 0 && connection.AccessHoleOffsetMm > 0
                    && (connection.AccessFaceId ?? "").StartsWith("D", StringComparison.Ordinal)
                    && connection.AccessSlotIndex > 0),
                "Projectexport: gatdata moet exact zijn, maar uitvoerbare CNC blijft geblokkeerd zolang de profielmaat niet fysiek is vrijgegeven");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static void VerifyProfileProjectConfiguration(string productId, WorkbenchModel model)
    {
        if (model.Profiles.Count == 0) return;
        var expectedTraceIds = model.Profiles.SelectMany(profile => profile.PieceTraceIds).ToArray();
        var stickerTraceIds = model.AssemblyPlacements.Where(value => value.Sticker != null)
            .SelectMany(value => value.Sticker.TraceIds).ToArray();
        if (expectedTraceIds.Any(traceId => !stickerTraceIds.Contains(traceId, StringComparer.OrdinalIgnoreCase))) return;
        var service = new ProfileProjectConfigurationService();
        var configuration = service.Build(model);
        var json = service.Serialize(configuration);
        var loaded = service.Deserialize(json);
        var sequence = service.ToProductionSequence(loaded);

        Require(loaded.SchemaVersion == ProfileProjectConfigurationService.CurrentSchemaVersion,
            productId + ": profielconfiguratie heeft verkeerde schemaversie");
        Require(loaded.Profiles.Count == PhysicalProfileCount(model),
            productId + ": profielconfiguratie bevat niet ieder fysiek profielstuk precies eenmaal");
        Require(sequence.Select(value => value.TraceId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == loaded.Profiles.Count,
            productId + ": profielconfiguratie bevat dubbele trace-ID's");
        Require(loaded.Connections.Count == model.AssemblyConnections.Count,
            productId + ": profielconfiguratie mist verbindingen");
        Require(sequence.All(value => value.Sticker != null && value.MachiningFrame != null && value.MachiningFrame.Faces.Count == 4),
            productId + ": sticker- of D0-D3-machineframe ging bij JSON-roundtrip verloren");
        var expectedOperations = model.ProfileOperations.SelectMany(value => value.PieceTraceIds).Count();
        Require(sequence.Sum(value => value.Operations.Count) == expectedOperations,
            productId + ": fysieke profielbewerkingen gingen bij JSON-roundtrip verloren");
        Require(json.Contains("\"Profiles\"") && json.Contains("\"Connections\"") && json.Contains(Environment.NewLine),
            productId + ": profielconfiguratie is niet als leesbare complete JSON opgeslagen");
        if (!loaded.ProductionReleased)
        {
            var blockedProgram = new ProfileCncOperatorProgramGenerator(service.ToCncMachineSettings(loaded)).Generate(loaded, sequence);
            Require(blockedProgram.Contains("NIET VRIJGEGEVEN") && !blockedProgram.Contains("G0 ")
                && !blockedProgram.Contains("G1 ") && !blockedProgram.Contains(" M3"),
                productId + ": geblokkeerd profielmanifest mag geen uitvoerbare boorbewegingen leveren");
        }

        foreach (var connection in loaded.Connections.Where(value => value.JointType == AssemblyJointType.StandardConnector.ToString()))
        {
            Require(connection.CoreHoleIndex > 0, productId + ": manifestverbinding mist kernboring");
            Require(connection.Instances.Count > 0 && connection.Instances.All(value => !string.IsNullOrWhiteSpace(value.TappedTraceId) && !string.IsNullOrWhiteSpace(value.SlotTraceId)),
                productId + ": manifestverbinding mist fysieke profielstuk-ID's");
            if (!connection.AccessHoleProductionReady)
                Require(loaded.ProductionBlockers.Any(value => value.IndexOf(connection.ConnectionId, StringComparison.OrdinalIgnoreCase) >= 0),
                    productId + ": onvolledig sleuteltoegangsgat blokkeert productie niet");
        }
    }

    private static void VerifyProfileSlotGeometry(ProfileSlotGeometryCatalog catalog, string materialId, int expectedCount, int expectedCoreCount, double[] widthAxes, double[] heightAxes)
    {
        var geometry = catalog.FindRequired(materialId);
        Require(geometry.ExpectedPerimeterSlotCount == expectedCount, materialId + ": verkeerd opgeslagen sleufaantal");
        Require(geometry.CalculatedPerimeterSlotCount == expectedCount, materialId + ": verkeerd berekend sleufaantal");
        Require(geometry.ExpectedCoreHoleCountPerEnd == expectedCoreCount && geometry.CalculatedCoreHoleCountPerEnd == expectedCoreCount,
            materialId + ": verkeerd aantal kopse kernboringen");
        Require(geometry.EndTapThread == "M8", materialId + ": kopse tapdraad is niet M8");
        Require(geometry.WidthFaceAxisOffsetsMm.SequenceEqual(widthAxes), materialId + ": verkeerde sleufassen over breedte");
        Require(geometry.HeightFaceAxisOffsetsMm.SequenceEqual(heightAxes), materialId + ": verkeerde sleufassen over hoogte");
        Require(Math.Abs(geometry.EdgeOffsetMm - 20) < 0.001 && Math.Abs(geometry.PitchMm - 40) < 0.001,
            materialId + ": randafstand/raster gewijzigd");
    }

    private static Contract[] Contracts()
    {
        return new[]
        {
            C("cabinet", Request("cabinet", 2400, 600, 900, r => { r.UnitCount = 4; r.DefaultShelfCount = 3; r.DefaultDrawerCount = 1; r.IncludeBackPanel = true; }), 44, 0, 315, 0, 0, 307, 44),
            C("werktafel", Request("werktafel", 1500, 750, 900, null), 1, 12, 64, 0, 0, 44, 13),
            C("machinebasis", Request("machinebasis", 2000, 800, 2000, r => { r.MachineBaseWorktopHeightMm = 900; }), 14, 36, 766, 0, 4, 686, 129),
            C("robotcel", Request("robotcel", 1200, 800, 900, r => { r.RobotCellIntermediateBeamMaxSpacingMm = 700; }), 1, 15, 32, 4, 2, 26, 30),
            C("lineaire_robotcel", Request("lineaire_robotcel", 3000, 700, 900, r => { r.LinearRobotCellWorktopSideCount = 1; r.LinearRobotCellGuardHeightAboveWorktopMm = 1200; r.LinearRobotCellIntermediateSupportMaxSpacingMm = 750; }), 7, 45, 30, 10, 0, 0, 83),
            C("materiaalwagen", Request("materiaalwagen", 1000, 650, 950, r => { r.MaterialCartShelfCount = 3; r.MaterialCartShelfMaterialId = "hpl_10_lex"; r.MaterialCartHandleSide = "right"; r.MaterialCartSteeringMode = "fixed-and-swivel"; }), 3, 22, 40, 0, 0, 0, 33),
            C("sim_rig_4080", Request("sim_rig_4080", 680, 1350, 660, r => { r.SimRigSteeringBridgePositionMm = 610; r.SimRigPedalDeckPositionMm = 250; r.SimRigPedalAngleDeg = 12; r.SimRigWheelMountPattern = "csl-dd"; }), 6, 11, 56, 0, 6, 50, 23),
            C("werktafel_lex", Request("werktafel_lex", 1650, 1000, 848, null), 6, 13, 279, 4, 8, 143, 82),
            C("werktafel_lex_revolution", Request("werktafel_lex_revolution", 1650, 1000, 848, null), 6, 13, 279, 4, 8, 143, 82),
            C("hoogteverstelbare_werktafel", Request("hoogteverstelbare_werktafel", 1650, 1000, 850, r => { r.ProfileMaterialId = "alu_system_40x40"; r.SheetMaterialId = "hpl_10_lex"; }), 2, 8, 86, 4, 4, 49, 48),
            C("werkbankkast", Request("werkbankkast", 2400, 600, 900, r => { r.UnitCount = 4; r.DefaultShelfCount = 3; r.IncludeBackPanel = true; }), 28, 0, 80, 0, 0, 68, 28),
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

    private static void VerifyDamagedSheetRecovery()
    {
        var request = Request("werkbankkast", 2400, 600, 900, r =>
        {
            r.UnitCount = 4;
            r.DefaultShelfCount = 1;
            r.IncludeBackPanel = true;
            r.IncludeTopDrawer = true;
            r.IncludeDrawerPullCutouts = true;
            r.IncludeAdjustableShelfHoles = true;
            r.WorkbenchCabinetIncludeLeftSidePlinth = true;
            r.WorkbenchCabinetIncludeRightSidePlinth = true;
            r.AdjustableShelfPositionCount = 6;
            r.SheetMaterialId = "betonplex_18";
            r.DrawerMaterialId = "betonplex_18";
            r.BackMaterialId = "betonplex_18";
            r.RevisionAfterMilledTestSheetOne = true;
            r.CompletedSheetPartNames = new List<string>
            {
                "Draaideur rechts paar 1",
                "Volledig tussenschot dubbel links U2",
                "Bovenlade zijde links U3"
            };
        });

        var recovery = new ProductionOutputService().BuildPreview(request);
        Require(recovery.NestingPlan.StockSheets.Count > 0, "Herstel plaat 2: geen nestplaten gegenereerd");
        Require(recovery.NestingPlan.StockSheets[0].Name.Contains("HerstelPlaat_02"), "Herstel plaat 2: eerste plaat heeft geen herkenbare herstelnaam");
        foreach (var completed in request.CompletedSheetPartNames)
            Require(recovery.Model.Sheets.All(part => !string.Equals(part.Name, completed, StringComparison.OrdinalIgnoreCase)), "Herstel plaat 2: bruikbaar deel opnieuw opgenomen: " + completed);
        Require(recovery.Model.AssemblyPlacements.Where(placement => placement.Kind == AssemblyComponentKind.Sheet)
                .All(placement => recovery.Model.Sheets.Any(part => string.Equals(part.Name, placement.PartName, StringComparison.OrdinalIgnoreCase))),
            "Herstel plaat 2: renderplaatsing zonder resterend plaatrecord gevonden");
        Require(recovery.Model.Sheets.Any(part => part.Name == "Volledig tussenschot dubbel rechts U2"), "Herstel plaat 2: beschadigd tussenschot ontbreekt");
        Require(recovery.Model.Sheets.Any(part => part.Name == "Bovenlade bodem U2"), "Herstel plaat 2: beschadigde ladebodem ontbreekt");
        Require(recovery.Model.Sheets.Any(part => part.Name == "Volledig tussenschot T U1"), "Herstel plaat 2: beschadigd T-tussenschot ontbreekt");
    }

    private static void VerifyWorkbenchBackPanelConnections(WorkbenchModel model)
    {
        var settings = WorkbenchCabinetBackPanelSettings.LoadRequired();
        Require(Close(settings.GrooveDepthMm, 3) && Close(settings.GrooveClearanceMm, 1)
                && Close(settings.FastenerEndInsetMm, 45) && Close(settings.FastenerMaxSpacingMm, 260),
            "Werkbankkast achterwand: productmastercontract wijkt af van de vrijgegeven proefwaarden");

        var back = model.Sheets.Single(part => part.Name == "Achterwand werkbankkast");
        var grooves = back.Pockets.Where(pocket => pocket.Name.StartsWith("Achterwandgroef ", StringComparison.Ordinal)).ToArray();
        Require(grooves.Length == 4, "Werkbankkast achterwand: vier groeven voor de werkelijke interne tussenschotten vereist");
        Require(grooves.All(pocket => Close(pocket.LengthMm, 19) && Close(pocket.WidthMm, 750)
                && Close(pocket.DepthMm, 3) && pocket.Face == OperationFace.NegativeZ
                && pocket.DepthMode == OperationDepthMode.PocketFromFace),
            "Werkbankkast achterwand: groeven moeten 19x750x3mm vanaf de voorzijde zijn");
        var expectedCenters = new[] { 582.0, 1173.0, 1191.0, 1782.0 };
        var actualCenters = grooves.Select(pocket => pocket.Xmm + pocket.LengthMm / 2.0).OrderBy(value => value).ToArray();
        Require(actualCenters.SequenceEqual(expectedCenters), "Werkbankkast achterwand: groefhartlijnen volgen de tussenschotten niet exact");

        var mountingHoles = back.Holes.Where(hole => hole.Name.StartsWith("Montagegat achterwand naar ", StringComparison.Ordinal)).ToArray();
        Require(mountingHoles.Length == 16, "Werkbankkast achterwand: vier montagegaten per tussenschotlijn vereist");
        Require(mountingHoles.All(hole => Close(hole.DiameterMm, 4)
                && hole.DepthMode == OperationDepthMode.Through
                && hole.SupportKind == SheetHoleSupportKind.PanelScrew),
            "Werkbankkast achterwand: montagegaten moeten doorlopend Ø4 uit de hout-houtstandaard zijn");
        foreach (var x in expectedCenters)
        {
            var line = mountingHoles.Where(hole => Close(hole.Xmm, x)).OrderBy(hole => hole.Ymm).ToArray();
            Require(line.Length == 4 && Close(line.First().Ymm, 45) && Close(line.Last().Ymm, 705)
                    && line.Zip(line.Skip(1), (left, right) => right.Ymm - left.Ymm).All(spacing => spacing <= 260.01),
                "Werkbankkast achterwand: gatlijn mist eindinset of maximale gatafstand op X=" + x.ToString(CultureInfo.InvariantCulture));
        }

        var placement = model.AssemblyPlacements.Single(item => item.PartName == "Achterwand werkbankkast");
        Require(Close(placement.Zmm, 300 - back.Material.ThicknessMm / 2.0 - settings.GrooveDepthMm),
            "Werkbankkast achterwand: 3mm voorwaartse insteekpositie ontbreekt");
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

    private static void VerifyLinearRobotCellGeometry(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile).ToArray();
        var uprights = profiles.Where(item => item.PartName.StartsWith("Staander 80x80 ", StringComparison.Ordinal)).ToArray();
        Require(uprights.Length == 10 && uprights.All(item => Close(item.LengthMm, 80) && Close(item.WidthMm, 80)),
            "Lineaire robotcel: standaardconfiguratie vereist vijf steunspanten en tien 80x80-staanders");
        var continuousCorners = uprights.Where(item => Close(item.Ymm + item.HeightMm / 2.0, 2100)).ToArray();
        Require(continuousCorners.Length == 4
                && continuousCorners.All(item => Close(item.Ymm - item.HeightMm / 2.0, 95)),
            "Lineaire robotcel: de vier buitenste 80x80-hoekstaanders moeten ononderbroken van voetplaat tot bovenzijde topframe doorlopen");

        var frameBeams = profiles.Where(item => item.PartName.StartsWith("Onderframe ", StringComparison.Ordinal)
            || item.PartName.StartsWith("Werkbladframe ", StringComparison.Ordinal)).ToArray();
        Require(frameBeams.Length == 26, "Lineaire robotcel: beide 40x80-framelagen moeten elk 8 langsliggers en 5 dwarsliggers bevatten");
        Require(frameBeams.All(item => Close(item.HeightMm, 80)
                && (Close(item.LengthMm, 40) || Close(item.WidthMm, 40))),
            "Lineaire robotcel: alle onder- en werkbladframeprofielen moeten 40x80 staand zijn");
        var lowerCrossmembers = profiles.Where(item => item.PartName.StartsWith("Onderframe dwarsligger ", StringComparison.Ordinal))
            .OrderBy(item => item.Xmm).ToArray();
        Require(lowerCrossmembers.Length == 5
                && Close(lowerCrossmembers.First().Xmm - lowerCrossmembers.First().LengthMm / 2.0,
                    uprights.Min(item => item.Xmm - item.LengthMm / 2.0))
                && Close(lowerCrossmembers.Last().Xmm + lowerCrossmembers.Last().LengthMm / 2.0,
                    uprights.Max(item => item.Xmm + item.LengthMm / 2.0)),
            "Lineaire robotcel: de twee uiterste onderste dwarsliggers moeten met het linker en rechter celbuitenvlak gelijkliggen");

        Require(profiles.Count(item => item.PartName.StartsWith("Raildrager 80x80 ", StringComparison.Ordinal)) == 2,
            "Lineaire robotcel: twee doorlopende 80x80-raildragers vereist");
        Require(model.AssemblyPlacements.Count(item => item.ComponentId == "hiwin_hgr20_rail") == 2,
            "Lineaire robotcel: twee rails moeten aan HIWIN HGR20 gekoppeld zijn");
        Require(model.AssemblyPlacements.Count(item => item.ComponentId == "hiwin_hgh20ca_block") == 4,
            "Lineaire robotcel: vier wagens moeten aan HIWIN HGH20CA gekoppeld zijn");
        Require(model.AssemblyPlacements.Count(item => item.ComponentId == "linear_robot_adapter_plate_provisional") == 1
                && model.AssemblyPlacements.Count(item => item.ComponentId == "linear_motor_adapter_plate_provisional") == 1,
            "Lineaire robotcel: robot- en motoradapter moeten afzonderlijke manifestleden blijven");
        Require(model.AssemblyPlacements.Count(item => item.ComponentId == "datasensing_sg4_30_105_oo_e_emitter") == 1
                && model.AssemblyPlacements.Count(item => item.ComponentId == "datasensing_sg4_30_105_oo_e_receiver") == 1,
            "Lineaire robotcel: eenzijdige 1200-mm standaardconfiguratie vereist één SG4 BASE 1050-mm zender/ontvangerset");
        var lightCurtainPrice = new PortalPricingService().Calculate(model).Lines.SingleOrDefault(item => item.Category == "Veiligheid");
        Require(lightCurtainPrice != null && lightCurtainPrice.Key == "datasensing_sg4_30_105_oo_e_set"
                && lightCurtainPrice.Quantity == 1 && lightCurtainPrice.PurchaseUnitPrice == 1364.60m,
            "Lineaire robotcel: de geselecteerde 1050-mm lichtgordijnset moet tegen de bijbehorende Automation24-masterdataprijs calculeren");
        var railCarriers = model.AssemblyPlacements.Where(item => item.PartName.StartsWith("Raildrager 80x80 ", StringComparison.Ordinal)).ToArray();
        Require(railCarriers.Length == 2 && Close(railCarriers.Average(item => item.Zmm), 350),
            "Lineaire robotcel: bij eenzijdig werkblad moet de railas in de achterste werkbladzone liggen");
        Require(model.Sheets.Count(item => item.Name.StartsWith("LRC werkblad zijde ", StringComparison.Ordinal)) == 1
                && model.Sheets.Count(item => item.Name.StartsWith("LRC acrylaat kopwand ", StringComparison.Ordinal)) == 2
                && model.Sheets.Count(item => item.Name.StartsWith("LRC acrylaat achterwand vak ", StringComparison.Ordinal)) == 4,
            "Lineaire robotcel: eenzijdige werkblad- en acrylaatindeling wijkt af van het manifest");
        Require(model.AssemblyPlacements.Where(item => item.PartName.StartsWith("LRC acrylaat ", StringComparison.Ordinal)).All(item => item.Shape == "acrylic-panel")
                && profiles.Count(item => item.PartName.StartsWith("Topframe achterstaander ", StringComparison.Ordinal)) == 3
                && !profiles.Any(item => item.PartName.StartsWith("Topframe voorstaander ", StringComparison.Ordinal)),
            "Lineaire robotcel: acrylaat moet transparant renderen; alleen de drie tussenstaanders van de achterwand blijven 40x40 naast de doorlopende 80x80-hoeken");
        Require(model.DesignNotes.Any(note => note.Contains("Productie-export is daarom geblokkeerd")),
            "Lineaire robotcel: open robot-, aandrijf-, adapter- en veiligheidsdata moeten expliciet blokkeren");

        var twoSided = new ProductModelBuildService().Build(Request("lineaire_robotcel", 3000, 700, 900, request =>
        {
            request.LinearRobotCellWorktopSideCount = 2;
            request.LinearRobotCellGuardHeightAboveWorktopMm = 1200;
            request.LinearRobotCellIntermediateSupportMaxSpacingMm = 750;
        }));
        Require(twoSided.Sheets.Count(item => item.Name.StartsWith("LRC werkblad zijde ", StringComparison.Ordinal)) == 2
                && twoSided.AssemblyPlacements.Count(item => item.ComponentId == "datasensing_sg4_30_105_oo_e_emitter") == 2
                && twoSided.AssemblyPlacements.Count(item => item.ComponentId == "datasensing_sg4_30_105_oo_e_receiver") == 2
                && twoSided.AssemblyPlacements.Count(item => item.PartName.StartsWith("Topframe kopwand middenstaander ", StringComparison.Ordinal)) == 2
                && twoSided.Sheets.Count(item => item.Name.StartsWith("LRC acrylaat kopwand ", StringComparison.Ordinal)) == 4
                && !twoSided.Sheets.Any(item => item.Name.StartsWith("LRC acrylaat achterwand vak ", StringComparison.Ordinal)),
            "Lineaire robotcel: tweezijdige variant vereist twee werkbladen, twee lichtschermen en één middenstaander met twee acrylaatvelden per kopwand");
        var centerSupports = twoSided.AssemblyPlacements
            .Where(item => item.PartName.StartsWith("Middensteun 80x80 ", StringComparison.Ordinal)).ToArray();
        var twoSidedLowerCrossmembers = twoSided.AssemblyPlacements
            .Where(item => item.PartName.StartsWith("Onderframe dwarsligger ", StringComparison.Ordinal)).ToArray();
        var twoSidedUpperCrossmembers = twoSided.AssemblyPlacements
            .Where(item => item.PartName.StartsWith("Werkbladframe dwarsligger ", StringComparison.Ordinal)).ToArray();
        var centerSupportBottom = twoSidedLowerCrossmembers.Average(beam => beam.Ymm + beam.HeightMm / 2.0);
        var centerSupportTop = twoSidedUpperCrossmembers.Average(beam => beam.Ymm - beam.HeightMm / 2.0);
        Require(centerSupports.Length == 5
                && centerSupports.All(item => Close(item.Zmm, 0)
                    && Close(item.Ymm - item.HeightMm / 2.0, centerSupportBottom)
                    && Close(item.Ymm + item.HeightMm / 2.0, centerSupportTop))
                && twoSided.AssemblyPlacements.Count(item => item.PartName.StartsWith("Middensteun voetplaat 80x80 M16 ", StringComparison.Ordinal)) == 5
                && twoSided.AssemblyPlacements.Count(item => item.PartName.StartsWith("Middensteun stelvoet D80 M16 ", StringComparison.Ordinal)) == 5,
            "Lineaire robotcel: tweezijdig vereist op ieder lengtestation een centrale 80x80-steun met eigen voetplaat en stelvoet, sluitend tussen beide framelagen");

        var lowGuard = new ProductModelBuildService().Build(Request("lineaire_robotcel", 3000, 700, 900, request =>
        {
            request.LinearRobotCellWorktopSideCount = 1;
            request.LinearRobotCellGuardHeightAboveWorktopMm = 800;
            request.LinearRobotCellIntermediateSupportMaxSpacingMm = 750;
        }));
        Require(lowGuard.AssemblyPlacements.Any(item => item.ComponentId == "datasensing_sg4_30_060_oo_e_emitter")
                && lowGuard.AssemblyPlacements.Any(item => item.ComponentId == "datasensing_sg4_30_060_oo_e_receiver"),
            "Lineaire robotcel: een 800-mm afschermhoogte moet op basis van huishoogte en montagespeling de SG4 BASE 600-mm variant kiezen");
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

        Require(model.AssemblyPlacements.Count(p => p.ComponentId == "techxxl_footplate_8_80x40_m12") == 4
            && model.AssemblyPlacements.Count(p => p.ComponentId == "gd60s_leveling_caster_m12") == 4
            && model.AssemblyPlacements.Count(p => p.ComponentId == "techxxl_hinge_8_40x40_light") == 4,
            "Machinebasis: voetplaten, stelwielen en scharnieren moeten via globale component-ID's geplaatst worden");
        Require(model.Hardware.Any(item => item.ArticleNumber == "S208FP804012WA" && item.Quantity == 4)
            && !model.Hardware.Any(item => item.ArticleNumber == "S210FP804012WA" || item.ArticleNumber == "101442"),
            "Machinebasis: de groef-8 voetplaat TIN 101030 moet de oude groef-10 voetplaat vervangen");

        var footplateContract = new ComponentPrimitiveRenderContractService().BuildRequired("techxxl_footplate_8_80x40_m12");
        var hingeContract = new ComponentPrimitiveRenderContractService().BuildRequired("techxxl_hinge_8_40x40_light");
        var casterContract = new ComponentPrimitiveRenderContractService().BuildRequired("gd60s_leveling_caster_m12");
        Require(footplateContract.ContractVersion == 1 && footplateContract.Status == "ProvisionalRenderEnvelope"
            && footplateContract.OpenData.Count > 0 && footplateContract.Primitives.Count == 1
            && footplateContract.Primitives.Single().Holes.Count == 7,
            "Machinebasis: de TechXXL groef-8 voetplaat moet als globale 80x40-component met zeven zichtbare boringen beschikbaar zijn");
        Require(hingeContract.Status == "ProvisionalRenderEnvelope" && hingeContract.OpenData.Count > 0
            && hingeContract.Primitives.Count == 3
            && hingeContract.Primitives.Count(part => part.Shape == "box" && part.Holes.Count == 2) == 2
            && hingeContract.Primitives.Count(part => part.Shape == "cylinder") == 1,
            "Machinebasis: het globale scharnier moet twee afzonderlijk geboorde bladen en een scharnierpen bevatten");
        Require(casterContract.Status == "ProvisionalRenderEnvelope" && casterContract.OpenData.Count > 0
            && casterContract.Primitives.Count == 6
            && casterContract.Primitives.Count(part => part.Shape == "cylinder") >= 4,
            "Machinebasis: het globale GD-60S component moet wiel, behuizing, pad, bediening, draaikrans en M12-stift bevatten");

        var rendered = new PortalAssembly3DService().Build(model, null);
        var footplates = rendered.Where(part => part.ComponentId == footplateContract.ComponentId).ToArray();
        var hinges = rendered.Where(part => part.ComponentId == hingeContract.ComponentId).ToArray();
        var casters = rendered.Where(part => part.ComponentId == casterContract.ComponentId).ToArray();
        Require(footplates.Length == 4 && footplates.All(part => part.Holes.Count == 7
                && part.Holes.All(hole => hole.VisualRole == "component-mounting-hole")
                && part.ComponentRenderOpenData.Count > 0),
            "Machinebasisweergave: vier geboorde TechXXL-voetplaten moeten uit het globale componentcontract komen");
        Require(hinges.Length == 12 && hinges.Count(part => part.Shape == "box" && part.Holes.Count == 2) == 8
                && hinges.Count(part => part.Shape == "cylinder") == 4
                && hinges.All(part => part.ComponentRenderOpenData.Count > 0),
            "Machinebasisweergave: vier echte driedelige scharnieren moeten uit het globale componentcontract komen");
        Require(casters.Length == 24 && casters.Count(part => part.Shape == "cylinder") >= 16
                && casters.All(part => part.ComponentRenderOpenData.Count > 0),
            "Machinebasisweergave: vier herbruikbare zesdelige GD-60S stelwielen moeten worden opgebouwd");
        Require(rendered.Where(part => footplates.Contains(part) || hinges.Contains(part) || casters.Contains(part))
                .All(part => part.Shape == "box" || part.Shape == "cylinder"),
            "Machinebasisweergave: de UI mag voor globale hardware uitsluitend ontvangen generieke primitives hoeven tekenen");
    }

    private static void VerifyMachineBaseAssemblyInstructions(WorkbenchModel model)
    {
        Require(model.AssemblyConnections.Count > 0, "Machinebasis: gerichte profielverbindingen ontbreken");
        Require(model.AssemblyConnections.Select(c => c.ConnectionId).Distinct(StringComparer.Ordinal).Count() == model.AssemblyConnections.Count,
            "Machinebasis: verbindings-ID's moeten uniek en stabiel zijn");
        var profileIds = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).Select(p => p.MemberId).ToArray();
        var tracedProfiles = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).ToArray();
        Require(tracedProfiles.All(p => !string.IsNullOrWhiteSpace(p.TraceId)),
            "Machinebasis: ieder profielstuk moet een traceerbaar profielnummer hebben");
        Require(tracedProfiles.Select(p => p.TraceId).Distinct(StringComparer.Ordinal).Count() == tracedProfiles.Length,
            "Machinebasis: profielnummers moeten uniek zijn");
        Require(model.Profiles.SelectMany(p => p.PieceTraceIds).OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(tracedProfiles.Select(p => p.TraceId).OrderBy(id => id, StringComparer.Ordinal)),
            "Machinebasis: profielstuknummers moeten tussen BOM en assembly overeenkomen");
        Require(model.ProfileOperations.All(operation => operation.PieceTraceIds.Count == operation.Quantity),
            "Machinebasis: iedere zaag-/boor-/tapbewerking moet dezelfde profielstuknummers dragen");
        foreach (var connection in model.AssemblyConnections)
        {
            Require(profileIds.Contains(connection.TappedMemberId), "Machinebasis: getapt kopprofiel van verbinding ontbreekt in assembly");
            Require(profileIds.Contains(connection.SlotMemberId), "Machinebasis: sleufprofiel van verbinding ontbreekt in assembly");
            if (connection.JointType == AssemblyJointType.StandardConnector)
            {
                Require(Close(connection.FastenerThreadMm, 8) && Close(connection.HexKeyAcrossFlatsMm, 5)
                    && Close(connection.AccessHoleDiameterMm, 7) && connection.AccessHoleOffsetMm > 0
                    && connection.ToolPassageClearanceMm > 0 && Close(connection.DrillIncrementMm, 0),
                    "Machinebasis: exacte M8/SW5/Ø7-standaardverbinder en leveranciersspeling zijn gewijzigd");
                Require(connection.FastenerStandardId == "standard-profile-connector-groove8-m8"
                    && connection.ConnectorId == "techxxl_standard_connector_8_40"
                    && connection.FastenerId == "techxxl_button_head_iso7380_m8x25",
                    "Machinebasis: de TechXXL groef-8/M8-keten moet via stabiele masterdata-ID's zijn toegewezen");
                Require((connection.FastenerAxisOrder ?? "").StartsWith("inbuskop → standaardverbinder", StringComparison.Ordinal),
                    "Machinebasis: fysieke laagvolgorde langs de boutas ontbreekt");
                Require((connection.AccessFace ?? "").StartsWith("D", StringComparison.Ordinal)
                    && (connection.SlotLane ?? "").Contains("/S")
                    && (connection.AccessHoleReference ?? "").Contains("vanaf kop A"),
                    "Machinebasis: toegangsgat moet exact aan D0-D3, S-baan en profielkopreferentie zijn gekoppeld");
            }
            else
            {
                Require(connection.JointType == AssemblyJointType.HingeSlidingNut
                    && connection.ConnectorId == "techxxl_hinge_8_40x40_light"
                    && connection.FastenerStandardId == "techxxl-hinge-8-40x40-4xm6x12-4x-nut-m6"
                    && connection.FastenerId == "techxxl_countersunk_socket_m6x12_galvanized"
                    && Close(connection.FastenerThreadMm, 6) && Close(connection.HexKeyAcrossFlatsMm, 4)
                    && (connection.FastenerAxisOrder ?? "").Contains("S208NSMS6-moeren in groef 8"),
                    "Machinebasis: deurscharnier moet de volledige TechXXL groef-8 M6-montageketen gebruiken");
            }
            Require(connection.OpenData.Count == 0 && connection.Status == AssemblyDataStatus.Confirmed
                && !connection.FinalTorqueNm.HasValue,
                "Machinebasis: vrijgegeven verbindingen moeten bevestigd en open-datavrij zijn; moment is bewust geen eis");
        }
        Require(model.AssemblyConnections.Count(connection => connection.JointType == AssemblyJointType.HingeSlidingNut) == 4,
            "Machinebasis: twee deuren vereisen samen vier gerichte scharnier-/schuifmoerverbindingen");
        Require(model.AssemblyConnections.Count(connection => (connection.InstructionGroup ?? "").EndsWith(" frame", StringComparison.Ordinal)) == 8,
            "Machinebasis: beide deurframes vereisen vier gerichte standaardverbindingen");

        var plan = new AssemblyInstructionPlanningService().Build(model);
        Require(plan.Available && plan.SequenceConfirmed && plan.WorkflowId == "machinebase-subassemblies-v1" && plan.Steps.Count == 42,
            "Machinebasis: de vijf subassemblies moeten inclusief vijf verzamelstappen in 42 compacte handelingen zijn vastgelegd");
        var worktopIntermediatePrepare = plan.Steps.Single(step => step.GroupId == "machinebase-worktop"
            && step.Title == "Voorzie tussenliggers aan beide koppen van verbinders");
        var worktopIntermediateTighten = plan.Steps.Single(step => step.GroupId == "machinebase-worktop"
            && step.Title == "Positioneer en draai tussenliggers vast");
        var worktopFramePrepare = plan.Steps.Single(step => step.GroupId == "machinebase-worktop"
            && step.Title == "Voorzie laagframe van verbinders voor de staanders");
        Require(worktopIntermediatePrepare.ConnectionPoints.Count == 12
            && worktopIntermediatePrepare.MaterialItems.All(item => item.Quantity == 12),
            "Machinebasis 40x80: drie werkbladtussenliggers moeten op twee koppen ieder twee verbinders krijgen (12 totaal)");
        Require(worktopIntermediateTighten.ConnectionPoints.Count == 12
            && worktopIntermediateTighten.MaterialItems.Count == 0,
            "Machinebasis 40x80: vastzetten moet dezelfde twaalf verbindingsnodes gebruiken zonder reeds gemonteerde hardware opnieuw te introduceren");
        Require(worktopFramePrepare.ConnectionPoints.Count == 8
            && worktopFramePrepare.MaterialItems.All(item => item.Quantity == 8),
            "Machinebasis 40x80: twee laagframeliggers moeten op twee koppen ieder twee nieuwe verbinders krijgen (8 totaal)");
        foreach (var step in new[] { worktopIntermediatePrepare, worktopIntermediateTighten, worktopFramePrepare })
        {
            foreach (var profileEnd in step.ConnectionPoints.GroupBy(point => point.TappedTraceId + "|" + point.TappedEnd, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = profileEnd.OrderBy(point => point.CoreHoleIndex).ToArray();
                Require(ordered.Length == 2
                    && ordered.Select(point => point.CoreHoleIndex).SequenceEqual(new[] { 1, 2 })
                    && ordered.Select(point => point.CoreWidthOffsetMm).SequenceEqual(new[] { 20.0, 60.0 })
                    && ordered.All(point => Close(point.CoreHeightOffsetMm, 20))
                    && ordered.All(point => Close(point.AccessHoleDiameterMm, 7))
                    && (!Close(ordered[0].AccessXmm, ordered[1].AccessXmm)
                        || !Close(ordered[0].AccessYmm, ordered[1].AccessYmm)
                        || !Close(ordered[0].AccessZmm, ordered[1].AccessZmm)),
                    "Machinebasis 40x80: iedere gebruikte kop moet twee afzonderlijke K1/K2-nodes op 20/60 mm en twee afzonderlijke toegangsgaten hebben");
            }
        }
        var portalProfileParts = new PortalAssembly3DService().Build(model, null)
            .Where(part => new[] { "MB-P016", "MB-P017", "MB-P018" }.Contains(part.TraceId, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        Require(portalProfileParts.Length == 3
            && portalProfileParts.All(part => part.CoreHoles.Count == 2
                && part.CoreHoles.OrderBy(hole => hole.CoreHoleIndex).Select(hole => hole.WidthOffsetMm).SequenceEqual(new[] { 20.0, 60.0 })
                && part.CoreHoles.All(hole => Close(hole.HeightOffsetMm, 20))
                && part.ProfileRender != null
                && part.ProfileRender.ContractVersion == 1
                && part.ProfileRender.Status == "ExactSupplierGeometry"
                && part.ProfileRender.OpenData.Count == 0
                && part.ProfileRender.SlotMouthDepthMm.HasValue && Close(part.ProfileRender.SlotMouthDepthMm.Value, 4.5)
                && part.ProfileRender.SlotCavityWidthMm.HasValue && Close(part.ProfileRender.SlotCavityWidthMm.Value, 20)
                && part.ProfileRender.SlotCavityDepthMm.HasValue && Close(part.ProfileRender.SlotCavityDepthMm.Value, 12.25)
                && part.ProfileRender.OutsideCornerRadiusMm.HasValue && Close(part.ProfileRender.OutsideCornerRadiusMm.Value, 4)
                && part.ProfileRender.CoreHoleDiameterMm.HasValue && Close(part.ProfileRender.CoreHoleDiameterMm.Value, 6.8)
                && part.ProfileRender.SlotAxisCentersLocalMm.Count(axis => axis.Length == 2) == 1
                && part.ProfileRender.SlotAxisCentersLocalMm.Single(axis => axis.Length == 2).SequenceEqual(new[] { -20.0, 20.0 })),
            "Machinebasis 40x80: portal en instructies moeten dezelfde canonieke twee kernboringen op 20/60 mm gebruiken");
        Require(plan.Steps.SelectMany(step => step.ConnectionPoints).All(point => point.HardwareRender != null
                && point.HardwareRender.ContractVersion == 1
                && point.HardwareRender.FastenerHeadStyle == "button-head-hex-socket"
                && point.HardwareRender.BoltShankDiameterMm.HasValue
                && Close(point.HardwareRender.BoltShankDiameterMm.Value, 8)
                && point.HardwareRender.SocketAcrossFlatsMm.HasValue && Close(point.HardwareRender.SocketAcrossFlatsMm.Value, 5)
                && point.HardwareRender.ConnectorPlateThicknessMm.HasValue && Close(point.HardwareRender.ConnectorPlateThicknessMm.Value, 2.2)
                && point.HardwareRender.ConnectorPlateWidthMm.HasValue && Close(point.HardwareRender.ConnectorPlateWidthMm.Value, 17)
                && point.HardwareRender.ConnectorPlateHeightMm.HasValue && Close(point.HardwareRender.ConnectorPlateHeightMm.Value, 35)
                && point.HardwareRender.ConnectorJawLengthMm.HasValue && Close(point.HardwareRender.ConnectorJawLengthMm.Value, 8)
                && point.HardwareRender.ConnectorJawWidthMm.HasValue && Close(point.HardwareRender.ConnectorJawWidthMm.Value, 13)
                && point.HardwareRender.ConnectorJawHeightMm.HasValue && Close(point.HardwareRender.ConnectorJawHeightMm.Value, 8)
                && point.HardwareRender.ConnectorJawSpacingMm.HasValue && Close(point.HardwareRender.ConnectorJawSpacingMm.Value, 22)
                && point.HardwareRender.ConnectorCenterFromProfileEndMm.HasValue && Close(point.HardwareRender.ConnectorCenterFromProfileEndMm.Value, 1.1)
                && point.HardwareRender.ShankCenterFromProfileEndMm.HasValue && Close(point.HardwareRender.ShankCenterFromProfileEndMm.Value, 10.3)
                && point.HardwareRender.HeadCenterFromProfileEndMm.HasValue && Close(point.HardwareRender.HeadCenterFromProfileEndMm.Value, 4.2)
                && point.HardwareRender.InsertionTravelMm.HasValue && Close(point.HardwareRender.InsertionTravelMm.Value, 8)
                && point.HardwareRender.BoltHeadDiameterMm.HasValue && Close(point.HardwareRender.BoltHeadDiameterMm.Value, 14)
                && point.HardwareRender.BoltHeadHeightMm.HasValue && Close(point.HardwareRender.BoltHeadHeightMm.Value, 4)
                && point.HardwareRender.BoltShankLengthMm.HasValue && Close(point.HardwareRender.BoltShankLengthMm.Value, 25)
                && point.HardwareRender.Status == "ExactSupplierGeometry"
                && point.HardwareRender.OpenData.Count == 0),
            "Machinebasis: iedere standaardverbinding moet een expliciet backend-hardware-rendercontract met bolkop-inbusbout dragen");
        Require(plan.Grouping == AssemblyInstructionGrouping.EquivalentProfiles && plan.CanShowIndividualSteps,
            "Machinebasis: equivalente groepering en de mogelijkheid om individuele stappen te tonen ontbreken");
        Require(plan.CanReleaseForProduction && plan.MissingData.Count == 0,
            "Machinebasis: vrijgegeven leveranciersgeometrie en exacte D/S-toegang mogen geen assemblageblokkade achterlaten");
        var groups = plan.Steps.GroupBy(step => step.GroupId).ToArray();
        Require(groups.Select(group => group.Key).SequenceEqual(new[]
        {
            "machinebase-lower", "machinebase-worktop", "machinebase-top", "machinebase-door-1", "machinebase-door-2"
        }), "Machinebasis: subassemblies moeten van onderlaag via werkblad en toplaag naar beide deuren lopen");
        foreach (var group in groups.Take(3))
        {
            var phases = group.Select(step => step.Phase).ToArray();
            Require(phases.SequenceEqual(new[]
            {
                AssemblyInstructionPhase.Prepare,
                AssemblyInstructionPhase.Preassemble, AssemblyInstructionPhase.Insert, AssemblyInstructionPhase.Tighten,
                AssemblyInstructionPhase.Preassemble, AssemblyInstructionPhase.Insert, AssemblyInstructionPhase.Tighten,
                AssemblyInstructionPhase.Preassemble, AssemblyInstructionPhase.Insert, AssemblyInstructionPhase.Tighten
            }), "Machinebasis: binnen-naar-buiten-laagvolgorde voor " + group.Key + " is gewijzigd");
            Require(group.All(step => step.PrimaryTraceIds.Count > 0),
                "Machinebasis: assemblagestappen moeten het profielstuknummer tonen");
            var gather = group.First();
            Require(gather.VisualKind == "gather-subassembly" && gather.MaterialItems.Count == 2
                && gather.MaterialItems.All(item => item.Quantity > 0),
                "Machinebasis: iedere laag-subassembly moet beginnen met profielen en geteld verbindingsmateriaal");
            Require(group.Where(step => step.Phase == AssemblyInstructionPhase.Preassemble)
                .All(step => step.MaterialItems.Count == 2 && step.MarkerTraceIds.Count > 0),
                "Machinebasis: voormontagestappen moeten verbinders, bouten en relevante stickermarkeringen tonen");
        }
        foreach (var group in groups.Skip(3))
        {
            Require(group.Select(step => step.Phase).SequenceEqual(new[]
            {
                AssemblyInstructionPhase.Prepare,
                AssemblyInstructionPhase.Preassemble, AssemblyInstructionPhase.Insert, AssemblyInstructionPhase.Tighten,
                AssemblyInstructionPhase.Preassemble, AssemblyInstructionPhase.Tighten
            }), "Machinebasis: deurframe moet vóór scharniermontage worden gebouwd voor " + group.Key);
            Require(group.Count(step => step.VisualKind == "hinge-sliding-nut") == 2,
                "Machinebasis: iedere deur moet twee expliciete scharnier-/schuifmoerhandelingen bevatten");
            Require(group.First().VisualKind == "gather-subassembly" && group.First().MaterialItems.Count == 5
                && group.First().MaterialItems.Any(item => item.ItemId == "techxxl_hinge_8_40x40_light" && item.Quantity == 2)
                && group.First().MaterialItems.Any(item => item.ItemId == "techxxl_countersunk_socket_m6x12_galvanized" && item.Quantity == 8)
                && group.First().MaterialItems.Any(item => item.ItemId == "techxxl_t_slot_nut_8_bridge_m6" && item.Quantity == 8),
                "Machinebasis: iedere deur-subassembly moet beginnen met profielen en de volledige groef-8 scharnier-/bout-/T-moerketen");
        }
        var lowerFrameInsert = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Title == "Schuif laagframe in de vier staanders");
        var lowerFrameTighten = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Title == "Positioneer en draai laagframe vast");
        var lowerGather = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Phase == AssemblyInstructionPhase.Prepare);
        var lowerOuterRailPreparation = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Title == "Voorzie twee buitenliggers aan beide koppen van verbinders");
        var lowerOuterRailInsert = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Title == "Plaats buitenliggers tussen de staanders");
        var lowerOuterRailTighten = plan.Steps.Single(step => step.GroupId == "machinebase-lower"
            && step.Title == "Positioneer en draai buitenliggers vast");
        Require(lowerGather.PrimaryTraceIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
                new[] { "MB-P005", "MB-P006", "MB-P009", "MB-P010", "MB-P011" })
            && lowerGather.MaterialItems.All(item => item.Quantity == 10),
            "Machinebasis: de eerste verzamelstap mag alleen de vijf profielen en tien verbindingen van de kern-subassembly bevatten");
        Require(lowerOuterRailPreparation.PrimaryTraceIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
                new[] { "MB-P007", "MB-P008" })
            && lowerOuterRailPreparation.MaterialItems.All(item => item.Quantity == 4),
            "Machinebasis: de twee buitenliggers en hun vier verbindingen mogen pas na plaatsing van de kern-subassembly worden geïntroduceerd");
        Require(lowerFrameInsert.ShowAssemblyDetail && lowerFrameTighten.ShowAssemblyDetail,
            "Machinebasis: inschuiven en vastzetten van een compleet laagframe moeten de exacte subassembly tonen");
        Require(lowerFrameInsert.MoveAsRigidGroup && !lowerOuterRailInsert.MoveAsRigidGroup,
            "Machinebasis: alleen het reeds voorgemonteerde laagframe moet als één star geheel worden ingeschoven");
        Require(lowerOuterRailInsert.ShowAssemblyDetail && lowerOuterRailTighten.ShowAssemblyDetail,
            "Machinebasis: de buitenliggers moeten in detail op hun echte positie tussen de vier staanders worden getoond");
        Require(lowerFrameInsert.PrimaryTraceIds.Count == 5 && lowerFrameInsert.SecondaryTraceIds.Count == 4
            && lowerFrameInsert.MarkerTraceIds.Count == 6
            && lowerFrameInsert.MarkerTraceIds.All(id => lowerFrameInsert.SecondaryTraceIds.Contains(id)
                || id == "MB-P005" || id == "MB-P006"),
            "Machinebasis: het complete laagframe mag bewegen, maar alleen de twee aansluitliggers en vier staanders krijgen stickers in beeld");
        foreach (var step in plan.Steps.Where(candidate => candidate.RepeatCount > 1))
        {
            var groupedConnections = step.ConnectionIds.Select(id => model.AssemblyConnections.Single(connection => connection.ConnectionId == id)).ToArray();
            var referenceConnection = groupedConnections[0];
            var referenceTapped = model.AssemblyPlacements.Single(placement => placement.MemberId == referenceConnection.TappedMemberId);
            var referenceReceiver = model.AssemblyPlacements.Single(placement => placement.MemberId == referenceConnection.SlotMemberId);
            var pairedEnds = groupedConnections.GroupBy(connection => connection.TappedMemberId)
                .All(group => group.Select(connection => connection.TappedEnd).Distinct().Count() == 2);
            foreach (var connection in groupedConnections.Skip(1))
            {
                var tapped = model.AssemblyPlacements.Single(placement => placement.MemberId == connection.TappedMemberId);
                var receiver = model.AssemblyPlacements.Single(placement => placement.MemberId == connection.SlotMemberId);
                Require(SamePlacementGeometry(referenceTapped, tapped) && SamePlacementGeometry(referenceReceiver, receiver),
                    "Machinebasis: alleen profielen met gelijke lengte en numerieke oriëntatie mogen worden gegroepeerd");
                Require(connection.ConnectorId == referenceConnection.ConnectorId
                    && connection.FastenerStandardId == referenceConnection.FastenerStandardId
                    && (pairedEnds || (connection.TappedEnd == referenceConnection.TappedEnd
                        && InstallationDirection(referenceTapped, referenceReceiver) == InstallationDirection(tapped, receiver)
                        && connection.AccessFace == referenceConnection.AccessFace
                        && connection.SlotFace == referenceConnection.SlotFace
                        && connection.SlotLane == referenceConnection.SlotLane)),
                    "Machinebasis: alleen verbindingen met dezelfde installatie mogen worden gegroepeerd: "
                    + referenceConnection.ConnectionId + " versus " + connection.ConnectionId);
            }
        }
        var individualPlan = new AssemblyInstructionPlanningService().Build(model, false);
        Require(individualPlan.Grouping == AssemblyInstructionGrouping.Individual && !individualPlan.CanShowIndividualSteps
            && individualPlan.Steps.Count == model.AssemblyConnections.Count * 5
            && individualPlan.Steps.All(step => step.RepeatCount == 1 && step.ConnectionIds.Count == 1),
            "Machinebasis: de planner moet groepering volledig kunnen uitschakelen");
        Require(plan.ScopeLabel == "Assemblage van machinaal voorbereid bouwpakket",
            "Machinebasis: assemblagehandleiding mag profielbewerking niet als assemblagestap presenteren");
        Require(Close(FastenerToolAccessCalculator.RoundHoleDiameterMm(5, 0.5, 1), 7),
            "Standaardverbinder: SW5 moet met 0,5-mm speling naar boormaat Ø7 afronden");
        Require(Close(FastenerToolAccessCalculator.RoundHoleDiameterMm(6, 0.5, 1), 8),
            "Standaardverbinder: een boutuitzondering met SW6 moet het toegangsgat naar Ø8 herberekenen");
        foreach (var connection in model.AssemblyConnections.Where(connection => connection.JointType == AssemblyJointType.StandardConnector))
        {
            var tapped = model.AssemblyPlacements.Single(placement => placement.MemberId == connection.TappedMemberId);
            var receiver = model.AssemblyPlacements.Single(placement => placement.MemberId == connection.SlotMemberId);
            var drill = model.ProfileOperations.Single(operation => operation.Kind == ProfileOperationKind.Drill
                && string.Equals(operation.Note, "Toegangsgat standaardverbinder " + connection.ConnectionId, StringComparison.OrdinalIgnoreCase));
            Require(drill.PieceTraceIds.Contains(receiver.TraceId, StringComparer.OrdinalIgnoreCase)
                    && !drill.PieceTraceIds.Contains(tapped.TraceId, StringComparer.OrdinalIgnoreCase),
                "Machinebasis: toegangsgat hoort uitsluitend bij het ontvangende sleufprofiel, niet bij het getapte kopprofiel");
        }
        var portalHtml = PortalHtml.Page();
        Require(portalHtml.Contains("if(!addOpenTSlotProfile(THREE,group,p,material))")
            && portalHtml.Contains("addProfileEndDetails(THREE,group,p);")
            && portalHtml.Contains("addInstructionAccessHoles(selected,step);")
            && portalHtml.Contains("renderableHardware(h)"),
            "Machinebasis: profiel- en toegangsgaten blijven contractgestuurd; onbekende detailgeometrie valt alleen terug op de backend-envelope");
        Require(!portalHtml.Contains("function addProfileSlotLines")
            && !portalHtml.Contains("grooveMaterial=new THREE.MeshBasicMaterial"),
            "Machinebasis: echte T-sleuven mogen niet opnieuw met semitransparante sleufvlakken worden bedekt");
        Require(CountOccurrences(portalHtml, "function renderInstructionThree(") == 1,
            "Machinebasis: de detailweergave moet precies één actuele renderer hebben; een legacykopie maakt visuele regressies waarschijnlijk");
        Require(portalHtml.Contains("setInstructionLinked3d(!instructionLinked3d)")
            && portalHtml.Contains("syncInstructionCamera(source,target,targetIsDetail)")
            && portalHtml.Contains("offscreenAnchor"),
            "Machinebasis: fullscreen 3D moet beide instructiecamera's koppelen en stickerlijnen bij buitenbeeldankers behouden");
        Require(portalHtml.Contains("id=\"profileMachiningControlPanel\"")
            && portalHtml.Contains("lastQuote.ProfileMachiningControlSvg")
            && portalHtml.Contains("openViewer('profile-control')")
            && portalHtml.Contains("kind==='profile-control'"),
            "Machinebasis: de aparte visuele profielbewerkingscontrole en grootbeeldviewer moeten in de portal beschikbaar blijven");
        Require(portalHtml.Contains("@media(min-width:1600px) and (min-height:850px)")
            && portalHtml.Contains("#assemblyAssistFullscreenBtn,.instructionPanel.assemblyAssistFullscreen .instructionOverviewHead button")
            && portalHtml.Contains("minmax(260px,clamp(280px,15vw,390px))")
            && portalHtml.Contains(".instructionPanel.assemblyAssistFullscreen .instructionGroupProgress{font-size:clamp")
            && portalHtml.Contains(".instructionPanel.assemblyAssistFullscreen .instructionPartSummary b{font-size:clamp"),
            "Machinebasis: fullscreenbediening, belangrijke staptekst, onderdelen en voortgang moeten op grote resoluties begrensd responsief meegroeien");
        Require(portalHtml.Contains("grid-template-columns:minmax(0,1fr) minmax(0,1fr) minmax(190px,230px)")
            && portalHtml.Contains(".instructionPanel.assemblyAssistFullscreen .instructionDetailLabel{grid-column:2/4")
            && portalHtml.Contains(".instructionPanel.assemblyAssistFullscreen .instructionWorkspace{grid-column:2/4")
            && portalHtml.Contains("#instructionOverviewExpanded{row-gap:15px}")
            && portalHtml.Contains("instructionViewportResizeTimer=setTimeout")
            && portalHtml.Contains("detail.focusSize=toolDetail?360:(slideDetail?310:420)")
            && portalHtml.Contains("toolScale=Math.max(.46,Math.min(.68,w/1100))")
            && portalHtml.Contains("toolDetail?.78")
            && portalHtml.Contains("toolDetail?.68"),
            "Machinebasis: fullscreen moet twee even grote hoofdvensters met een aparte infokolom behouden, bij resize opnieuw fitten en verbindingsdetails dichterbij tonen met een normaal geschaalde sleutel");
        Require(portalHtml.Contains("box=host.getBoundingClientRect(),w=Math.max(1,box.width),h=Math.max(1,box.height)")
            && portalHtml.Contains("rect=host.getBoundingClientRect(),w=Math.max(1,rect.width),h=Math.max(1,rect.height)")
            && !portalHtml.Contains("w=Math.max(320,rect.width),h=Math.max(260,rect.height)")
            && !portalHtml.Contains("w=Math.max(300,box.width),h=Math.max(220,box.height)"),
            "Machinebasis: overzicht en detail moeten hun orthografische projectie altijd uit de echte CSS-viewportverhouding afleiden; minimale renderafmetingen mogen een smal venster nooit uitrekken");
        Require(portalHtml.Contains("profileAxisStart:instructionWorldPoint")
            && portalHtml.Contains("profileAxisEnd:instructionWorldPoint")
            && portalHtml.Contains("projectedHorizontal=Math.abs(axisDx)>=Math.abs(axisDy)")
            && portalHtml.Contains("const fullscreenStickers=fullscreen?")
            && portalHtml.Contains("fullscreenStickerSmartLayout")
            && portalHtml.Contains("if(item.projectedHorizontal)")
            && portalHtml.Contains("const preferredSide=item.anchorX<rect.width/2?1:-1")
            && portalHtml.Contains("const cardEdge=item=>")
            && portalHtml.Contains("renderInstructionCameraState(state,detail)"),
            "Machinebasis: liggende stickerkaarten moeten boven het profiel en staanderkaarten ernaast worden geplaatst, na iedere 3D-camerawijziging opnieuw geprojecteerd en exact met anker en kaartrand verbonden");
        Require(portalHtml.Contains("function stickerEndSign(sticker)")
            && portalHtml.Contains("function detailStickerFaceSign(sourceA,sourceB,sideInsert)")
            && portalHtml.Contains("detailStickerForProfileEnd(originalA.Sticker,detailStickerFaceSign")
            && portalHtml.Contains("contact=-recognitionEnd*primarySlotHalf"),
            "Machinebasis: een gelijkwaardige detailverbinding moet het stickeruiteinde als herkenningspunt behouden");
        Require(portalHtml.Contains("connectionEnd=preassemble?-recognitionEnd:1")
            && portalHtml.Contains("Sticker:preassemble?null:a.Sticker")
            && portalHtml.Contains("hardwareView=!exact&&step.VisualKind==='preassemble-connector'")
            && portalHtml.Contains("else if(kind==='preassemble-connector')marks=''"),
            "Machinebasis: verbinders plaatsen toont de stickerloze profielkop en kijkt recht op de volledig zichtbare bout en standaardverbinder zonder overbodige bewegingspijl");
        Require(portalHtml.Contains("function startInstructionHardwareFlash(state,visualKind)")
            && portalHtml.Contains("if(!parts||!['preassemble-connector','slide-into-slot'].includes(step.VisualKind||''))return;")
            && portalHtml.Contains("instructionHardware=true;connector.userData.connectionId=point.ConnectionId")
            && portalHtml.Contains("(step.ConnectionPoints||[]).forEach(point=>")
            && CountOccurrences(portalHtml, "startInstructionHardwareFlash(") >= 3
            && portalHtml.Contains("prefers-reduced-motion: reduce"),
            "Machinebasis: verbinder-aanbrengstappen moeten connectoren en bouten in overzicht en detail pulserend markeren met een toegankelijke fallback");
        Require(portalHtml.Contains("function instructionCoreLocal(profile,core)")
            && portalHtml.Contains("instructionCoreLocal(part,{WidthOffsetMm:point.CoreWidthOffsetMm,HeightOffsetMm:point.CoreHeightOffsetMm})")
            && portalHtml.Contains("VisualRole:'connector-access'")
            && !portalHtml.Contains("function instructionCoreLaneOffsets"),
            "Machinebasis 40x80: overzicht, detail en toegangsgaten moeten de door de backend geleverde K1/K2-geometrie gebruiken en mogen 20/60 niet opnieuw uit de bounding box afleiden");
        Require(portalHtml.Contains("function startInstructionHardwareInsertAnimation(THREE,state,step,hostId,sequenceBypass)")
            && portalHtml.Contains("step.VisualKind!=='preassemble-connector'")
            && portalHtml.Contains("instructionHardwareInsert")
            && portalHtml.Contains(".sort((left,right)=>left[0].localeCompare(right[0]))")
            && portalHtml.Contains("if(data.rotateVector&&object.quaternion)vector.applyQuaternion(object.quaternion)")
            && portalHtml.Contains("startInstructionHardwareInsertAnimation(THREE,state,step,'instructionOverview')")
            && portalHtml.Contains("startInstructionHardwareInsertAnimation(THREE,instructionThreeState,step,'instructionVisual')"),
            "Machinebasis: verbinder-aanbrengstappen moeten bout en verbinder per profiel als een star geheel en een voor een invoeren, eerst in overzicht en daarna in detail");
        Require(portalHtml.Contains("function markInstructionSlideSlots(parts,step)")
            && portalHtml.Contains("floor.userData.instructionSlideSlot=true")
            && portalHtml.Contains("includeSlot=visualKind==='slide-into-slot'")
            && portalHtml.Contains("addInstructionOverviewConnectors(THREE,group,detailParts,step)"),
            "Machinebasis: inschuifstappen moeten alle betrokken verbinders met bout en alleen de werkelijk ontvangende T-sleuf in overzicht en detail laten pulseren");
        Require(portalHtml.Contains("function startInstructionSlideAnimation(THREE,state,step,vector")
            && portalHtml.Contains("movers.length*(duration+gap)")
            && portalHtml.Contains("mover.endProgress*(1-back)")
            && portalHtml.Contains("start.lerp(end,Math.max(0,Math.min(1,progress||0)))")
            && portalHtml.Contains("role!=='active'"),
            "Machinebasis: herhaalde inschuifstappen moeten profielen een voor een naar hun doel bewegen, de pijl met de resterende afstand verkorten en daarna naar het beginbeeld terugkeren");
        Require(portalHtml.Contains("const insertionAxis=instructionAxis(receivers[0])")
            && portalHtml.Contains("fromMinimum=receiverMin-clearance-activeMax")
            && portalHtml.Contains("fromMaximum=receiverMax+clearance-activeMin")
            && portalHtml.Contains("slotAxis:insertionAxis")
            && portalHtml.Contains("targets.length?targets:"),
            "Machinebasis: inschuiven moet vanaf het dichtstbijzijnde uiteinde langs de echte langsas van de ontvangende T-sleuf lopen en het detail rond het echte contact uitsnijden");
        Require(portalHtml.Contains("entrySign=detail.moveVector?.startSide==='minimum'?-1:1")
            && portalHtml.Contains("slideEntryPoint[receiverAxis]=center[receiverAxis]+entrySign*sizes[receiverAxis]/2")
            && portalHtml.Contains("slideStartDistance=Math.hypot")
            && portalHtml.Contains("pair.slideStartDistance*4")
            && portalHtml.Contains("scoringPoint=slideDetail?pair.slideEntryPoint:pair.point")
            && portalHtml.Contains("detail.focusSize=toolDetail?360:(slideDetail?310:420)")
            && portalHtml.Contains("detail.focusAtInsertionEnd=slideDetail")
            && portalHtml.Contains("detail.focusAtInsertionEnd?new THREE.Vector3(...detail.focusPoint).add"),
            "Machinebasis: een inschuifdetail moet de echte vrije invoerhoek uit het overzicht uitsnijden, zodat de ontvangende T-sleuf en beide aansluitende profieldelen zichtbaar blijven");
        Require(portalHtml.Contains("instructionSlidePendingDetail=()=>")
            && portalHtml.Contains("overview&&rawElapsed>=cycle")
            && portalHtml.Contains("object.visible=progress<.985&&!finished")
            && portalHtml.Contains("stopInstructionHardwareFlash(state)")
            && portalHtml.Contains("startInstructionHardwareFlash(state,step.VisualKind)"),
            "Machinebasis: speel eerst links exact één volledige inschuifcyclus af, toon op het doel alleen het blauwe profiel en start daarna pas detailanimatie plus hardware- en T-sleufflash rechts");
        Require(portalHtml.Contains("targetInset=Math.min(70,secondaryLength*.2)")
            && portalHtml.Contains("slideTargetX:targetX")
            && portalHtml.Contains("jointSign:-recognitionEnd")
            && portalHtml.Contains("info.slideTargetX==null?receiverEnd-7:info.slideTargetX"),
            "Machinebasis: het lokale inschuifdetail moet de ontvangende sleuf zichtbaar voorbij het aansluitpunt laten doorlopen zodat het kop-tegen-zijvlakcontact niet als doorsnijding leest");
        Require(portalHtml.Contains("instructionRepeatCounter")
            && portalHtml.Contains("function updateInstructionRepeatCounter(element,number,elapsed,intro,duration)")
            && portalHtml.Contains("detailRuns=Math.round(Number(step.RepeatCount));if(!(detailRuns>0))return")
            && portalHtml.Contains("repeatIndex+1")
            && portalHtml.Contains("rawElapsed>=detailStop"),
            "Machinebasis: de rechter herhaalanimatie speelt het opgegeven aantal keren af, toont centraal concentrisch groeiende volgnummers en stopt na de laatste plaatsing");
        Require(portalHtml.Contains("function instructionAccessHoleVisibleForTrace(THREE,state,candidates,trace)")
            && portalHtml.Contains("function instructionExitProgress(THREE,state,objects,movement)")
            && portalHtml.Contains("stopsAtHole?1:instructionExitProgress")
            && portalHtml.Contains("item.object.visible=!(finished&&!entry.stopsAtHole)")
            && portalHtml.Contains("InstructionTraceId:point.TappedTraceId")
            && portalHtml.Contains("ConnectionId:point.ConnectionId"),
            "Machinebasis: iedere inschuifanimatie stopt bij het trace-gekoppelde zichtbare toegangsgat of loopt volledig uit beeld voor het volgende profiel");
        Require(portalHtml.Contains("rigid=!!step.MoveAsRigidGroup")
            && portalHtml.Contains("trace=rigid?'__rigid_subassembly__':sourceTrace")
            && portalHtml.Contains("const stopsAtHole=rigid||")
            && portalHtml.Contains("state.slideMarkers.filter(marker=>rigid||")
            && portalHtml.Contains("if(!step.MoveAsRigidGroup)"),
            "Machinebasis: een reeds gemonteerd laagframe moet inclusief liggers, markeringen en hardware als één star object bewegen zonder eerdere deelanimaties te herhalen");
        Require(portalHtml.Contains("some(point=>renderableHardware(point.HardwareRender))")
            && portalHtml.Contains("shank.position[axisName]=sign*(half-Number(h.ShankCenterFromProfileEndMm))")
            && portalHtml.Contains("(Number(h.ConnectorPlateThicknessMm)+Number(h.ConnectorJawLengthMm))/2")
            && !portalHtml.Contains("function addInstructionConnector")
            && portalHtml.Contains("class='tool allenKey'")
            && portalHtml.Contains("class='turnArrow3d'")
            && portalHtml.Contains("marker id='iTurnArrow'")
            && !portalHtml.Contains("if(kind==='allen-through-hole'){const turnMaterial"),
            "Machinebasis: vastzetten via een toegangsgat mag geen tweede verbinder of bout tonen; alleen de instekende inbussleutel en ruimtelijke draaipijl zijn toegestaan");
        Require(portalHtml.Contains("pairs.push({active,receiver,point,slideEntryPoint,slideStartDistance,distance})")
            && portalHtml.Contains("detail.detailActiveTraceId=activeTrace")
            && portalHtml.Contains("detail.detailReceiverTraceId=receiverTrace")
            && portalHtml.Contains("detail.focusPoint=[...(slideDetail?chosen.slideEntryPoint:chosen.point)]")
            && portalHtml.Contains("focusedConnection=step.FocusAssemblyView===true")
            && portalHtml.Contains("overviewDirection=exact&&instructionOverviewState")
            && portalHtml.Contains("fitInstructionDetailCamera(THREE,camera,fitBounds,center")
            && portalHtml.Contains("toolDetail?(1-projected.y)*900")
            && portalHtml.Contains("candidates.sort((left,right)=>(Number(right.inside)-Number(left.inside))")
            && portalHtml.Contains("overlay.dataset.toolTraceId=best.point.TappedTraceId"),
            "Machinebasis: ieder schuif- en vastzetdetail moet een lokale camera-uitsnede van één echt overzichtsknooppunt zijn; bij vastzetten krijgt de zichtbare onderste voethoek voorrang, met dezelfde trace-ID's, wereldgeometrie en kijkrichting");
    }

    private static void VerifyProfileTraceability(string productId, WorkbenchModel model)
    {
        var traceIds = model.Profiles.SelectMany(profile => profile.PieceTraceIds).ToArray();
        Require(model.Profiles.All(profile => profile.PieceTraceIds.Count == profile.Quantity),
            productId + ": ieder fysiek profielstuk moet een profielnummer hebben");
        Require(traceIds.Distinct(StringComparer.Ordinal).Count() == traceIds.Length,
            productId + ": profielnummers moeten productbreed uniek zijn");
        Require(model.ProfileOperations.All(operation => operation.PieceTraceIds.Count == operation.Quantity),
            productId + ": iedere profielbewerking moet het profielnummer blijven dragen");
        Require(model.ProfileOperations.SelectMany(operation => operation.PieceTraceIds).All(id => traceIds.Contains(id)),
            productId + ": bewerkingsregel verwijst naar een onbekend profielnummer");
        Require(model.AssemblyPlacements.Where(placement => !string.IsNullOrWhiteSpace(placement.TraceId)).All(placement => traceIds.Contains(placement.TraceId)),
            productId + ": 3D-plaatsing verwijst naar een onbekend profielnummer");
    }

    private static void VerifyProfileStickerPolicy(string productId, WorkbenchModel model)
    {
        var placements = model.AssemblyPlacements.Where(placement => placement.Kind == AssemblyComponentKind.Profile).ToArray();
        if (placements.Length == 0) return;
        Require(placements.All(placement => placement.Sticker != null),
            productId + ": iedere profielplaatsing moet exact één stickerplaatsing hebben");
        Require(placements.All(placement => placement.Sticker.FaceId == "D0"
            && !string.IsNullOrWhiteSpace(placement.Sticker.LocalFace)),
            productId + ": iedere sticker moet D0 en de echte lokale assemblagezijde vastleggen");
        Require(placements.All(placement => MachiningFrame(model, placement).Face("D0") != null
            && MachiningFrame(model, placement).Face("D0").FaceSpanMm > 0),
            productId + ": doorsnedevlak en vlakbreedte moeten uitsluitend uit dezelfde stickerplaatsing afleidbaar zijn");
        Require(placements.All(placement => placement.Sticker.LongitudinalSizeMm > 0
            && placement.Sticker.TransverseSizeMm > 0
            && !string.IsNullOrWhiteSpace(placement.Sticker.OrientationInstruction)),
            productId + ": stickermaat of orientatie-instructie ontbreekt");

        if (placements.All(placement => !string.IsNullOrWhiteSpace(placement.TraceId)))
        {
            var stickerTraceIds = placements.SelectMany(placement => placement.Sticker.TraceIds).ToArray();
            var physicalTraceIds = model.Profiles.SelectMany(profile => profile.PieceTraceIds).ToArray();
            Require(stickerTraceIds.OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(physicalTraceIds.OrderBy(id => id, StringComparer.Ordinal)),
                productId + ": ieder fysiek profielstuk moet precies een productiesticker krijgen");
        }
    }

    private static void VerifyMachineBaseStickerPolicy(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(placement => placement.Kind == AssemblyComponentKind.Profile).ToArray();
        var uprights = profiles.Where(placement => placement.PartName.StartsWith("Staander ", StringComparison.Ordinal)).ToArray();
        Require(uprights.Length == 4, "Machinebasis stickers: vier hoofdstaanders verwacht");
        Require(uprights.All(placement => placement.Sticker.Rule == ProfileStickerPlacementRule.AssemblyViewSide
            && placement.Sticker.LongitudinalAxis == 1
            && Math.Abs(placement.Sticker.WorldNormalY) < 0.01
            && placement.Sticker.ObstructionFree
            && placement.Sticker.AnchorEnd == ProfileEnd.B),
            "Machinebasis stickers: staanders moeten D0 aan de montage-/zichtzijde en nabij het boveneinde krijgen: "
            + string.Join(" | ", uprights.Select(placement => placement.PartName + "=" + placement.Sticker.LocalFace
                + "/N=" + placement.Sticker.WorldNormalX.ToString("0.##") + "," + placement.Sticker.WorldNormalY.ToString("0.##")
                + "," + placement.Sticker.WorldNormalZ.ToString("0.##") + "/vrij=" + placement.Sticker.ObstructionFree)));

        var horizontal = profiles.Where(placement => placement.PartName.StartsWith("Onderframe tussenligger ", StringComparison.Ordinal)).ToArray();
        Require(horizontal.Length > 0
            && horizontal.All(placement => placement.Sticker.Rule == ProfileStickerPlacementRule.UpperFace
                && placement.Sticker.WorldNormalY > 0.9),
            "Machinebasis stickers: horizontale tussenliggers moeten altijd op de gemonteerde bovenzijde blijven; afdekking mag alleen de langspositie wijzigen: "
            + string.Join(" | ", horizontal.Select(placement => placement.PartName + "=" + placement.Sticker.Rule + "/"
                + placement.Sticker.LocalFace + "/Ny=" + placement.Sticker.WorldNormalY.ToString("0.###")
                + "/vrij=" + placement.Sticker.ObstructionFree)));

        var worktopLayer = profiles.Where(placement => string.Equals(placement.PartName, "Bladligger voor", StringComparison.Ordinal)
            || string.Equals(placement.PartName, "Bladligger achter", StringComparison.Ordinal)
            || placement.PartName.StartsWith("Bladframe tussenligger ", StringComparison.Ordinal)).ToArray();
        Require(worktopLayer.Length == 5
            && worktopLayer.All(placement => placement.Sticker.Rule == ProfileStickerPlacementRule.UpperFace
                && placement.Sticker.LocalFace == "+Y"
                && MachiningFrame(model, placement).Face("D0").CrossSectionFace == "+W"
                && Close(MachiningFrame(model, placement).Face("D0").FaceSpanMm, 40)
                && placement.Sticker.WorldNormalY > 0.9),
            "Machinebasis stickers: MB-P012/P013/P016-P018 moeten op het gemonteerde korte 40-mm-bovenvlak van het staande 40x80-profiel liggen: "
            + string.Join(" | ", worktopLayer.Select(placement => placement.TraceId + "=" + placement.Sticker.LocalFace
                + "/" + MachiningFrame(model, placement).Face("D0").CrossSectionFace
                + "/span=" + MachiningFrame(model, placement).Face("D0").FaceSpanMm.ToString("0.##"))));

        var portalProfiles = new PortalAssembly3DService().Build(model, null).Where(part => part.Kind == "profile").ToArray();
        Require(portalProfiles.Length == profiles.Length
            && portalProfiles.All(part => part.Sticker != null && !string.IsNullOrWhiteSpace(part.MemberId)),
            "Machinebasis stickers: 3D-portaal moet stickerplaatsing en stabiel member-ID ontvangen");
        var worktopTraceIds = new HashSet<string>(worktopLayer.Select(placement => placement.TraceId), StringComparer.OrdinalIgnoreCase);
        Require(portalProfiles.Where(part => worktopTraceIds.Contains(part.TraceId)).All(part => part.Sticker.LocalFace == "+Y"
                && part.Sticker.FaceAxis == 1 && part.Sticker.FaceSign == 1),
            "Machinebasis stickers: portal en assemblage-instructie moeten hetzelfde korte 40-mm-stickervlak ontvangen als productie");

        var csv = new CsvExporter().ExportProfileStickers(model.AssemblyPlacements);
        var rows = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var expectedRows = 1 + profiles.Sum(placement => placement.Sticker.TraceIds.Count);
        Require(rows.Length == expectedRows && csv.Contains("Montage-/zichtzijde staander") && csv.Contains("Bovenzijde ligger"),
            "Machinebasis stickers: productie-export moet exact een regel per fysieke profielsticker bevatten");
    }

    private static void VerifyProfileProductionSequence(string productId, WorkbenchModel model)
    {
        if (model.Profiles.Count == 0) return;
        var expectedTraceIds = model.Profiles.SelectMany(profile => profile.PieceTraceIds).ToArray();
        var validatedStickerTraceIds = model.AssemblyPlacements
            .Where(placement => placement.Sticker != null)
            .SelectMany(placement => placement.Sticker.TraceIds)
            .ToArray();
        var missingStickerGeometry = expectedTraceIds
            .Where(traceId => !validatedStickerTraceIds.Contains(traceId, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingStickerGeometry.Length > 0)
        {
            var blocked = false;
            try { new ProfileProductionSequenceService().Build(model); }
            catch (InvalidOperationException ex)
            {
                blocked = ex.Message.Contains("gevalideerde stickerplaatsing ontbreekt");
            }
            Require(blocked, productId + ": CNC/stickeroutput zonder volledige assemblageoriëntatie moet blokkeren in plaats van een stickervlak te verzinnen");
            return;
        }

        var sequence = new ProfileProductionSequenceService().Build(model).ToArray();
        var physicalTraceIds = expectedTraceIds;
        Require(sequence.Length == physicalTraceIds.Length
            && sequence.Select(item => item.TraceId).OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(physicalTraceIds.OrderBy(id => id, StringComparer.Ordinal)),
            productId + ": profielproductievolgorde moet ieder fysiek profielstuk exact eenmaal bevatten");
        Require(sequence.Select(item => item.ProductionOrder).SequenceEqual(Enumerable.Range(1, sequence.Length)),
            productId + ": freesvolgorde moet aaneengesloten vanaf 1 zijn");
        for (var index = 1; index < sequence.Length; index++)
        {
            var previous = sequence[index - 1];
            var current = sequence[index];
            var previousMax = Math.Max(previous.Material.WidthMm, previous.Material.HeightMm);
            var currentMax = Math.Max(current.Material.WidthMm, current.Material.HeightMm);
            var previousMin = Math.Min(previous.Material.WidthMm, previous.Material.HeightMm);
            var currentMin = Math.Min(current.Material.WidthMm, current.Material.HeightMm);
            Require(previousMax > currentMax || (Close(previousMax, currentMax) && (previousMin > currentMin
                    || (Close(previousMin, currentMin) && previous.ProfileLengthMm >= current.ProfileLengthMm))),
                productId + ": freesvolgorde moet groot naar klein en daarna lang naar kort lopen");
        }
        Require(sequence.All(item => item.Sticker != null && item.StickerInstruction.Contains(item.TraceId)
                && item.ClampInstruction.Contains(item.TraceId)
                && item.ClampInstruction.Contains("afgezaagde kop waar de sticker komt")
                && item.ClampInstruction.Contains("vaste aanslag")
                && item.ClampInstruction.Contains("boven")
                && item.ClampInstruction.Contains("mm hoog")
                && item.StickerInstruction.Contains("dwars in het midden")
                && item.StickerInstruction.Contains("cm vanaf de vaste aanslag")
                && !item.ClampInstruction.Contains("D0") && !item.ClampInstruction.Contains("X=0")
                && !item.StickerInstruction.Contains("D0") && !item.StickerInstruction.Contains("X=0")),
            productId + ": iedere freesregel moet een gekoppelde klem- en stickerinstructie in gewone mensentaal hebben");
        Require(sequence.All(item => Close(item.Sticker.OffsetFromAnchorEndMm % 10.0, 0)),
            productId + ": stickerafstand moet op hele centimeters zijn afgerond");

        string gcode = null;
        try { gcode = new ProfileCncOperatorProgramGenerator().Generate(sequence); }
        catch (InvalidOperationException ex)
        {
            Require((string.Equals(productId, "werktafel", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(productId, "machinebasis", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(productId, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(productId, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(productId, "hoogteverstelbare_werktafel", StringComparison.OrdinalIgnoreCase))
                    && ex.Message.Contains("nog niet fysiek gevalideerd"),
                productId + ": alleen profielmaten zonder fysiek vrijgegeven CNC-parameters mogen na geldige stickerplanning geblokkeerd blijven");
        }
        if (gcode != null)
        {
            var operatorStops = gcode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("M0 (", StringComparison.Ordinal)).ToArray();
            Require(gcode.Contains("PROFIEL CNC BOORPROGRAMMA")
                && gcode.Contains("KEER HET PROFIEL NOOIT IN DE LENGTERICHTING OM")
                && sequence.All(item => gcode.Contains("TRACE_ID=" + item.TraceId)
                    && operatorStops.Count(line => line.Contains("STICKER " + item.TraceId + " OP")) == 1)
                && operatorStops.All(line => !line.Contains("D0") && !line.Contains("D1") && !line.Contains("D2")
                    && !line.Contains("D3") && !line.Contains("S1") && !line.Contains("X0") && !line.Contains("Y20"))
                && operatorStops.All(line => line.Length <= 96),
                productId + ": CNC-operatorstops moeten kort, veilig en vrij van interne vlak-, sleuf- en coordinaatcodes zijn");
            if (string.Equals(productId, "machinebasis", StringComparison.OrdinalIgnoreCase))
            {
                var mbP012 = sequence.Single(item => item.TraceId == "MB-P012");
                var stickerCentimeters = Math.Round(mbP012.Sticker.OffsetFromAnchorEndMm / 10.0, MidpointRounding.AwayFromZero)
                    .ToString("0", CultureInfo.InvariantCulture);
                Require(gcode.Contains("TRACE_ID=MB-P012;TYPE=80X40")
                    && gcode.Contains("M0 (MB-P012: KORTE 40-MM-KANT BOVEN. KOP TEGEN AANSLAG. KLEM.)")
                    && gcode.Contains("M0 (STICKER MB-P012 OP " + stickerCentimeters + " CM. MIDDEN BOVEN. START.)"),
                    "Machinebasis CNC: MB-P012 moet het korte 40-mm-stickervlak en de afgeronde stickerafstand in mensentaal tonen");
                VerifyProfileCncFaceAndSlotContract(mbP012);
            }
        }

        var temporaryFile = Path.Combine(Path.GetTempPath(), "sww-profielstickers-" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            new ProfileStickerXlsxExporter().Export(temporaryFile, sequence);
            using (var archive = ZipFile.OpenRead(temporaryFile))
                Require(archive.GetEntry("xl/worksheets/sheet1.xml") != null,
                    productId + ": sticker-Excel is geen geldig OOXML-werkboek");
        }
        finally
        {
            if (File.Exists(temporaryFile)) File.Delete(temporaryFile);
        }

        ProfileTapWorklistRow[] tapRows;
        try { tapRows = new ProfileTapWorklistService().Build(sequence).ToArray(); }
        catch (InvalidOperationException ex)
        {
            Require(string.Equals(productId, "werktafel", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("expliciete kernboring K1..Kn"),
                productId + ": alleen de expliciet niet-vrijgegeven Werktafel-taplijst mag na geldige render- en stickerplanning geblokkeerd blijven");
            return;
        }
        if (string.Equals(productId, "machinebasis", StringComparison.OrdinalIgnoreCase))
        {
            var expectedTapHoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var connection in model.AssemblyConnections.Where(connection => connection.JointType == AssemblyJointType.StandardConnector))
            {
                var placement = model.AssemblyPlacements.Single(item => string.Equals(item.MemberId, connection.TappedMemberId, StringComparison.OrdinalIgnoreCase));
                Require(placement.Sticker != null && placement.Sticker.TraceIds.Count > 0,
                    "Machinebasis tapcontract: getapt verbindingsprofiel mist traceerbare stickerplaatsing voor " + connection.ConnectionId);
                foreach (var traceId in placement.Sticker.TraceIds)
                    expectedTapHoles.Add(traceId + "|" + (connection.TappedEnd == ProfileEnd.A ? "Kop A" : "Kop B") + "|K" + connection.CoreHoleIndex);
            }
            var actualTapHoles = new HashSet<string>(tapRows.Select(row => row.TraceId + "|" + row.End + "|" + row.CoreHole), StringComparer.OrdinalIgnoreCase);
            Require(actualTapHoles.SetEquals(expectedTapHoles),
                "Machinebasis tapcontract: ieder fysiek standaardverbinderpunt moet exact één bijbehorende kopse kernboring tappen");
            Require(tapRows.Length == expectedTapHoles.Count
                    && tapRows.GroupBy(row => row.TraceId + "|" + row.End + "|" + row.CoreHole, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1),
                "Machinebasis tapcontract: 40x80-koppen moeten K1 en K2 afzonderlijk op 20/60 mm bevatten; geen middenverbinder");
            Require(tapRows.Select(row => row.TraceId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 25
                    && tapRows.Select(row => row.TraceId + "|" + row.End).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 50
                    && tapRows.Length == 64,
                "Machinebasis tapcontract: verwacht 25 tapprofielen, 50 te tappen koppen en 64 fysieke M8-tapgaten");
            Require(tapRows.Where(row => Close(row.WidthMm, 80) && Close(row.HeightMm, 40))
                    .GroupBy(row => row.TraceId + "|" + row.End, StringComparer.OrdinalIgnoreCase)
                    .All(group => group.Select(row => row.CoreHole).OrderBy(value => value).SequenceEqual(new[] { "K1", "K2" })
                        && group.Select(row => row.CoreXmm).OrderBy(value => value).SequenceEqual(new[] { 20.0, 60.0 })),
                "Machinebasis tapcontract: iedere gebruikte 40x80-kop moet twee taps op 20 en 60 mm hebben");
            Require(model.AssemblyConnections.Count(connection => connection.JointType == AssemblyJointType.StandardConnector) == tapRows.Length,
                "Machinebasis tapcontract: iedere verbinder heeft één kopse M8-tap en één afzonderlijk sleuteltoegangsgat in het ontvangende zijvlak");
        }
        if (tapRows.Length > 0)
        {
            Require(tapRows.All(row => row.TapRequired && row.Instruction.Contains(row.StickerEnd ? "X=0 · stickerzijde" : "X=L · eindzijde")),
                productId + ": taplijst mag alleen echte tapbewerkingen bevatten en moet stickerzijde als X=0 gebruiken");
            Require(tapRows.All(row => sequence.Single(item => item.TraceId == row.TraceId).Operations.Any(operation => operation.Kind == ProfileOperationKind.Tap
                    && (((operation.Side ?? string.Empty).IndexOf("A/B", StringComparison.OrdinalIgnoreCase) >= 0)
                        || ((operation.Side ?? string.Empty).IndexOf(row.End, StringComparison.OrdinalIgnoreCase) >= 0)))),
                productId + ": bestaande kernboring mag nooit zelfstandig een tapbewerking veroorzaken");
            var visual = new ProfileMachiningVisualSvgExporter().Generate(sequence, tapRows);
            Require(visual.StartsWith("<svg", StringComparison.Ordinal)
                    && visual.Contains(">STICKER</text>") && visual.Contains("class='slot'")
                    && tapRows.All(row => visual.Contains(row.TraceId))
                    && !visual.Contains("NIET TAPPEN") && !visual.Contains("Bewerkingen en lengtematen")
                    && !visual.Contains("Stickerpositie tijdens bewerking") && !visual.Contains("X=0 · stickerzijde")
                    && !visual.Contains("X=L · eindzijde") && !visual.Contains(" mm vanaf X=0")
                    && !visual.Contains(" tappen · ") && !visual.Contains(" / y"),
                productId + ": minimale visuele tapcontrole mist profiel/sticker of bevat overbodige instructietekst");
            foreach (var item in sequence.Where(item => tapRows.Any(row => string.Equals(row.TraceId, item.TraceId, StringComparison.OrdinalIgnoreCase))))
            {
                var geometry = new ProfileSlotGeometryCatalog().FindRequired(item.Material.Id);
                var stickerFace = item.Sticker.LocalFace ?? string.Empty;
                var d0 = item.MachiningFrame.Face("D0");
                var sectionFace = d0.CrossSectionFace ?? string.Empty;
                var expectedSlots = Math.Abs(d0.FaceSpanMm - item.Material.WidthMm)
                    <= Math.Abs(d0.FaceSpanMm - item.Material.HeightMm)
                    ? geometry.WidthFaceAxisOffsetsMm.Count : geometry.HeightFaceAxisOffsetsMm.Count;
                Require(visual.Contains("data-trace-id='" + item.TraceId + "' data-material='" + item.Material.Id
                        + "' data-sticker-face='" + stickerFace + "' data-section-face='" + sectionFace
                        + "' data-face-span='" + d0.FaceSpanMm.ToString("0.##", CultureInfo.InvariantCulture)
                        + "' data-slot-count='" + expectedSlots + "'"),
                    productId + ": langsvisualisatie moet het fysieke stickervlak en de bijbehorende sleufassen volgen voor " + item.TraceId);
            }

            var tapFile = Path.Combine(Path.GetTempPath(), "sww-profieltappen-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                new ProfileTapWorklistXlsxExporter().Export(tapFile, tapRows);
                using (var archive = ZipFile.OpenRead(tapFile))
                {
                    Require(archive.GetEntry("xl/worksheets/sheet1.xml") != null,
                        productId + ": tap-Excel is geen geldig OOXML-werkboek");
                    using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml").Open()))
                    {
                        var worksheet = reader.ReadToEnd();
                        Require(worksheet.Contains("Machinezijde") && !worksheet.Contains("Stickerafstand vanaf X=0 mm")
                                && !worksheet.Contains("X vanaf rand mm") && !worksheet.Contains("Y vanaf rand mm")
                                && !worksheet.Contains("Sticker op deze kop"),
                            productId + ": tap-Excel moet compact zijn, stickerzijde als machine-X=0 gebruiken en geen stickerafstand herhalen");
                    }
                }
            }
            finally
            {
                if (File.Exists(tapFile)) File.Delete(tapFile);
            }
        }
    }

    private static void VerifyProfileCncFaceAndSlotContract(ProfileProductionSequenceItem source)
    {
        var settings = ProfileCncMasterSettings.LoadRequired();
        Require(settings.ContractId == "legacy-profielboorstrategie-v1"
                && Close(settings.SpindleRpm, 6000)
                && Close(settings.SpindleSpinUpSeconds, 20)
                && settings.ValidatedProfileTypes.SequenceEqual(new[] { "20X20", "20X40" })
                && settings.X0AnchorRule == ProfileCncMachineSettings.StickerEndAtMachineX0Rule
                && settings.RollDirectionRule == ProfileCncMachineSettings.ClockwiseFromX0Rule,
            "Profiel-CNC: machinewaarden, vrijgegeven maten en opspancontract moeten uit CAM-masterdata komen");
        settings.ValidatedProfileTypes.Add("80X40");
        var item = new ProfileProductionSequenceItem
        {
            ProductionOrder = 1,
            TraceId = source.TraceId,
            ProfileId = source.ProfileId,
            PartName = source.PartName,
            Material = source.Material,
            ProfileLengthMm = source.ProfileLengthMm,
            Sticker = source.Sticker,
            MachiningFrame = source.MachiningFrame,
            ClampInstruction = source.ClampInstruction,
            StickerInstruction = source.StickerInstruction
        };
        item.Operations.Add(new ProfileOperation
        {
            Kind = ProfileOperationKind.Drill, FaceId = "D0", SlotIndex = 1, PositionFromEndAMm = 100,
            DiameterMm = 7, ThroughHole = true
        });
        item.Operations.Add(new ProfileOperation
        {
            Kind = ProfileOperationKind.Drill, FaceId = "D1", SlotIndex = 2, PositionFromEndAMm = 150,
            DiameterMm = 7, ThroughHole = true
        });
        var cnc = new ProfileCncOperatorProgramGenerator(settings).Generate(new[] { item });
        Require(cnc.Contains("(BOOR: SLEUF 1 VAN LINKS; 100 MM VAN AANSLAG; DIA 7; DOOR)")
                && cnc.Contains("(SWW_HOLE;FACE=D0;SLOT=1;X_MM=100;Y_MM=20;DIA_MM=7)")
                && cnc.Contains("(BOOR: SLEUF 2 VAN LINKS; 150 MM VAN AANSLAG; DIA 7; DOOR)")
                && cnc.Contains("M0 (KIJK VANAF AANSLAG. DRAAI 1/4 RECHTSOM.)")
                && cnc.Contains("M0 (LANGE 80-MM-KANT BOVEN. KOP TEGEN AANSLAG. KLEM. START.)")
                && cnc.Contains("S6000 M3\r\n(WACHT 20 SEC OP TOERENTAL)\r\nG4 P20\r\n")
                && CountOccurrences(cnc, "S6000 M3") == CountOccurrences(cnc, "G4 P20")
                && cnc.Contains("G0 Z95") && cnc.Contains("G0 X100 Y20") && cnc.Contains("G1 Z-1 F150"),
            "Profiel-CNC: interne D0-D3/sleufgeometrie, menselijke draaistop en echte X/Y/Z-boorbaan moeten een contract vormen");

        var originalStickerAnchor = item.Sticker.AnchorEnd;
        var originalFrameAnchor = item.MachiningFrame.X0AnchorEnd;
        try
        {
            item.Sticker.AnchorEnd = ProfileEnd.B;
            item.MachiningFrame.X0AnchorEnd = ProfileEnd.B;
            var fromB = new ProfileCncPlanningService(settings).Build(item);
            Require(Close(fromB.Setups[0].Holes[0].MachineXmm, item.ProfileLengthMm - 100),
                "Profiel-CNC: sticker bij Kop B moet machine-X vanaf dezelfde fysieke X0-aanslag spiegelen zonder het profiel in de lengte om te keren");
        }
        finally
        {
            item.Sticker.AnchorEnd = originalStickerAnchor;
            item.MachiningFrame.X0AnchorEnd = originalFrameAnchor;
        }

        var unvalidatedBlocked = false;
        try { new ProfileCncOperatorProgramGenerator().Generate(new[] { item }); }
        catch (InvalidOperationException ex) { unvalidatedBlocked = ex.Message.Contains("nog niet fysiek gevalideerd"); }
        Require(unvalidatedBlocked, "Profiel-CNC: 40x80 mag geen echte boor-G-code krijgen voordat de profielmaat fysiek is gevalideerd");

        item.Operations[0].FaceId = null;
        var missingFaceBlocked = false;
        try { new ProfileCncPlanningService(settings).Build(item); }
        catch (InvalidOperationException ex) { missingFaceBlocked = ex.Message.Contains("mist fysiek vlak D0-D3"); }
        Require(missingFaceBlocked, "Profiel-CNC: vrije tekst of ontbrekend fysiek vlak mag nooit stilzwijgend een boring bepalen");
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

    private static void VerifySimRigGeometry(WorkbenchModel model)
    {
        var profiles = model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile).ToArray();
        var longRails = profiles.Where(p => p.PartName.StartsWith("Basislangsligger", StringComparison.Ordinal)).ToArray();
        Require(longRails.Length == 2 && longRails.All(p => Close(p.LengthMm, 80) && Close(p.HeightMm, 40)),
            "Sim-rig: beide basislangsliggers moeten 40x80 vlak liggen met X=80 en Y=40");
        var crossmembers = profiles.Where(p => p.PartName.StartsWith("Basisdwarsligger", StringComparison.Ordinal)).ToArray();
        Require(crossmembers.Length == 3 && crossmembers.All(p => Close(p.HeightMm, 40) && Close(p.WidthMm, 80)),
            "Sim-rig: exact drie basisdwarsliggers moeten 40x80 vlak liggen met Y=40 en Z=80");
        var uprights = profiles.Where(p => p.PartName.StartsWith("Stuurstaander", StringComparison.Ordinal)).ToArray();
        Require(uprights.Length == 2 && uprights.All(p => Close(p.LengthMm, 40) && Close(p.WidthMm, 80)),
            "Sim-rig: twee verticale stuurstaanders met doorsnede X=40 en Z=80 vereist");
        var bridge = profiles.Single(p => p.PartName.StartsWith("Stuurbrug", StringComparison.Ordinal));
        Require(Close(bridge.HeightMm, 80) && Close(bridge.WidthMm, 40),
            "Sim-rig: stuurbrug moet 40x80 staand liggen met Y=80 en Z=40");
        Require(model.AssemblyPlacements.Count(p => p.PartName.StartsWith("Custom ", StringComparison.Ordinal)) == 6,
            "Sim-rig: exact zes custom adapterplaten vereist");
        Require(model.Sheets.Where(p => p.Name.StartsWith("Custom ", StringComparison.Ordinal)).All(p => p.CustomContour.Count >= 5),
            "Sim-rig: custom platen moeten een expliciete vereenvoudigde contour hebben");
        Require(model.Sheets.Where(p => p.Name.StartsWith("Custom ", StringComparison.Ordinal)).All(p => p.Pockets.Any(slot => slot.DepthMode == OperationDepthMode.Through)),
            "Sim-rig: iedere custom plaatfamilie moet minstens één functionele doorlopende instelsleuf hebben");
        Require(profiles.Count(p => p.PartName.StartsWith("Pedaalplatform profiel", StringComparison.Ordinal) && Close(p.RotationXDeg, 12)) == 3,
            "Sim-rig: pedaalhoek moet op alle drie pedaalprofielen worden uitgevoerd");
        Require(EndCapCount(model) == 6, "Sim-rig: vier basisuiteinden en twee staanderkoppen vereisen eindkappen");
    }

    private static void VerifySimRigVariants()
    {
        var compact = new ProductModelBuildService().Build(Request("sim_rig_4080", 600, 1200, 550, r =>
        {
            r.SimRigSteeringBridgePositionMm = 500;
            r.SimRigPedalDeckPositionMm = 200;
            r.SimRigPedalAngleDeg = 0;
            r.SimRigWheelMountPattern = "blank";
        }));
        Require(compact.AssemblyPlacements.Count(p => p.PartName.StartsWith("Pedaalplatform profiel", StringComparison.Ordinal) && Close(p.RotationXDeg, 0)) == 3,
            "Sim-rig compact: vlakke pedaalkeuze moet op alle drie profielen worden uitgevoerd");
        Require(compact.Sheets.Single(p => p.Name.StartsWith("Custom stuurzijplaat", StringComparison.Ordinal)).Holes.Count == 0,
            "Sim-rig compact: blanco stuurplaat mag geen productspecifieke CSL-DD-gaten krijgen");

        var maximum = new ProductModelBuildService().Build(Request("sim_rig_4080", 800, 1800, 850, r =>
        {
            r.SimRigSteeringBridgePositionMm = 1000;
            r.SimRigPedalDeckPositionMm = 600;
            r.SimRigPedalAngleDeg = 25;
            r.SimRigWheelMountPattern = "csl-dd";
        }));
        Require(maximum.AssemblyPlacements.Count(p => p.PartName.StartsWith("Pedaalplatform profiel", StringComparison.Ordinal) && Close(p.RotationXDeg, 25)) == 3,
            "Sim-rig maximum: maximale pedaalhoek moet op alle drie profielen worden uitgevoerd");
        Require(maximum.Sheets.Single(p => p.Name.StartsWith("Custom stuurzijplaat", StringComparison.Ordinal)).Holes.Count == 2,
            "Sim-rig maximum: CSL-DD-zijplaat vereist twee M6-gaten per plaat");
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
    private static ProfileMachiningFrame MachiningFrame(WorkbenchModel model, AssemblyPlacement placement)
    {
        var material = model.Profiles.Where(profile => profile.Material != null)
            .First(profile => profile.PieceTraceIds.Contains(placement.TraceId, StringComparer.OrdinalIgnoreCase)).Material;
        return new ProfileMachiningFrameService().Build(placement.TraceId, placement, material);
    }
    private static bool SamePlacementGeometry(AssemblyPlacement left, AssemblyPlacement right)
    {
        return Close(left.LengthMm, right.LengthMm) && Close(left.WidthMm, right.WidthMm) && Close(left.HeightMm, right.HeightMm)
            && left.Orientation == right.Orientation && Close(left.RotationXDeg, right.RotationXDeg)
            && Close(left.RotationYDeg, right.RotationYDeg) && Close(left.RotationZDeg, right.RotationZDeg);
    }
    private static string InstallationDirection(AssemblyPlacement tapped, AssemblyPlacement receiver)
    {
        var values = new[] { receiver.Xmm - tapped.Xmm, receiver.Ymm - tapped.Ymm, receiver.Zmm - tapped.Zmm };
        var absolute = values.Select(Math.Abs).ToArray();
        var axis = Array.IndexOf(absolute, absolute.Max());
        return axis.ToString() + (values[axis] < 0 ? "-" : "+");
    }
    private static bool Close(double left, double right) { return Math.Abs(left - right) <= 0.01; }
    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        for (var index = 0; (index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0; index += needle.Length) count++;
        return count;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
