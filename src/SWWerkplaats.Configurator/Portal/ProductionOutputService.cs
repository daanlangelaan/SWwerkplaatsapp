using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Manufacturing;
using SWWerkplaats.Configurator.SolidWorks;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class ProductionOutput
    {
        public WorkbenchModel Model { get; set; }
        public NestingPlan NestingPlan { get; set; }
        public string NestingSvg { get; set; }
        public List<string> Files { get; private set; }

        public ProductionOutput()
        {
            Files = new List<string>();
        }
    }

    public sealed class ProductionOutputService
    {
        public ProductionOutput BuildPreview(PortalQuoteRequest request)
        {
            var factory = new PortalConfigurationFactory();
            var machine = factory.DefaultMachine();
            var contourTool = factory.DefaultTool();
            var model = new ProductModelBuildService().Build(factory, request);
            ApplyRevisionAfterMilledTestSheetOne(model, request);
            ApplyCompletedSheetParts(model, request);
            var settings = AppSettings.Load();
            NestingPlan nestingPlan;
            string nestingSvg;
            try
            {
                nestingPlan = BuildNestingPlan(model, machine, settings, request);
                nestingSvg = new NestingExporter().ExportSvg(nestingPlan, contourTool);
            }
            catch (InvalidOperationException exception)
            {
                var release = new ProductReleaseContractService().LoadRequired(request.Product);
                if (release.ProductionReleased) throw;
                nestingPlan = new NestingPlan();
                model.DesignNotes.Add("Conceptpreview zonder nesting: " + exception.Message);
                nestingSvg = ConceptNestingUnavailableSvg(exception.Message);
            }
            return new ProductionOutput { Model = model, NestingPlan = nestingPlan, NestingSvg = nestingSvg };
        }

        private static string ConceptNestingUnavailableSvg(string reason)
        {
            var safe = System.Security.SecurityElement.Escape(reason ?? "Nesting niet beschikbaar.");
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"900\" height=\"220\" viewBox=\"0 0 900 220\">"
                + "<rect x=\"1\" y=\"1\" width=\"898\" height=\"218\" rx=\"18\" fill=\"#f7f8fa\" stroke=\"#d0d5dd\"/>"
                + "<text x=\"38\" y=\"78\" font-family=\"Arial,sans-serif\" font-size=\"24\" font-weight=\"700\" fill=\"#344054\">Conceptpreview — nesting niet vrijgegeven</text>"
                + "<text x=\"38\" y=\"124\" font-family=\"Arial,sans-serif\" font-size=\"17\" fill=\"#667085\">" + safe + "</text>"
                + "<text x=\"38\" y=\"166\" font-family=\"Arial,sans-serif\" font-size=\"15\" fill=\"#b42318\">Geen CAM-, inkoop- of productievrijgave.</text></svg>";
        }

        public WorkbenchCabinetAuditResult AuditWorkbenchCabinet(PortalQuoteRequest request)
        {
            var preview = BuildPreview(request);
            return new WorkbenchCabinetAuditService().Audit(preview.Model, request, preview.NestingPlan);
        }

        public ProductionOutput GenerateOrderFiles(PortalQuoteRequest request, string outputFolder)
        {
            EnsureProductRelease(request);
            var factory = new PortalConfigurationFactory();
            var contourTool = factory.DefaultTool();
            var holeTool = LibraryCatalog.DefaultEndMill(3, 2.0);
            var enableWoodScrewCountersinks = request.EnableWoodScrewCountersinks == true || request.EnableCountersinkAndEdgeChamfer == true;
            var enableOutsideEdgeChamfer = request.EnableOutsideEdgeChamfer == true || request.EnableCountersinkAndEdgeChamfer == true;
            var vBitTool = enableWoodScrewCountersinks || enableOutsideEdgeChamfer ? LibraryCatalog.WorkbenchCabinetVBit() : null;
            var camJob = CamJobOptions.FromPrimaryTool(holeTool);
            camJob.AddTool(contourTool);
            camJob.EnableWoodScrewCountersinks = enableWoodScrewCountersinks;
            camJob.EnableOutsideEdgeChamfer = enableOutsideEdgeChamfer;
            camJob.EdgeChamferWidthMm = 1.0;
            camJob.AddTool(vBitTool);
            var camMaster = CamMasterSettings.LoadRequired();
            camMaster.ApplyTo(camJob);
            var machine = factory.DefaultMachine();
            var model = new ProductModelBuildService().Build(factory, request);
            var settings = AppSettings.Load();
            WorkbenchCabinetAuditResult fullModelAudit = null;
            if (request.RevisionAfterMilledTestSheetOne || HasCompletedSheetParts(request))
            {
                var fullModelPlan = BuildNestingPlan(model, machine, settings, null);
                fullModelAudit = new WorkbenchCabinetAuditService().Audit(model, request, fullModelPlan);
                if (!fullModelAudit.Passed)
                    throw new InvalidOperationException("Revisie-export afgebroken: het volledige meubelmodel bevat " + fullModelAudit.Errors.Count.ToString() + " fout(en): " + string.Join(" | ", fullModelAudit.Errors));
                ApplyRevisionAfterMilledTestSheetOne(model, request);
                ApplyCompletedSheetParts(model, request);
            }
            var nestingPlan = BuildNestingPlan(model, machine, settings, request);

            Directory.CreateDirectory(outputFolder);
            var output = new ProductionOutput { Model = model, NestingPlan = nestingPlan };
            var csv = new CsvExporter();
            var control = new OrderControlExportService();
            if (request.TestFitFirstSheet)
                Write(output, outputFolder, "Testplaat-overzicht.txt", TestFitOverview(nestingPlan, TestFitPriorityPartNames(model, request)));

            if (string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase))
            {
                var audit = fullModelAudit ?? new WorkbenchCabinetAuditService().Audit(model, request, nestingPlan);
                Write(output, outputFolder, "VrijgaveControle.txt", FormatAudit(audit));
                if (!audit.Passed)
                    throw new InvalidOperationException("Productie-export afgebroken: de geometrie-, frees- of BOM-controle bevat " + audit.Errors.Count.ToString() + " fout(en). Zie VrijgaveControle.txt.");
            }

            if (HasProfiles(model))
            {
                var profileConfigurationService = new ProfileProjectConfigurationService();
                var profileConfigurationJson = profileConfigurationService.Serialize(profileConfigurationService.Build(model));
                Write(output, outputFolder, "Profielconfiguratie.json", profileConfigurationJson);

                // Vanaf dit punt is de opnieuw ingelezen projectconfiguratie de enige bron voor profielproductie-uitvoer.
                var profileConfiguration = profileConfigurationService.Deserialize(profileConfigurationJson);
                var profileProductionSequence = profileConfigurationService.ToProductionSequence(profileConfiguration);
                var profileParts = profileConfigurationService.ToProfileParts(profileConfiguration);
                var profileOperations = profileConfigurationService.ToOperations(profileConfiguration);
                var profilePlacements = profileConfigurationService.ToPlacements(profileConfiguration);
                Write(output, outputFolder, "Profielconfiguratie-validatie.txt", ProfileConfigurationValidationText(profileConfiguration));
                Write(output, outputFolder, "Afkortlijst.csv", csv.ExportCutList(profileParts));
                Write(output, outputFolder, "Boorlijst.csv", csv.ExportDrillList(profileProductionSequence));
                Write(output, outputFolder, "Profielbewerkingen.csv", csv.ExportProfileOperations(profileOperations));
                Write(output, outputFolder, "Profielstickers.csv", csv.ExportProfileStickers(profilePlacements));
                new ProfileOperationsXlsxExporter().Export(Path.Combine(outputFolder, "Profielbewerkingen.xlsx"), profileOperations);
                output.Files.Add("Profielbewerkingen.xlsx");
                new ProfileStickerXlsxExporter().Export(Path.Combine(outputFolder, "Profielstickers-freesvolgorde.xlsx"), profileProductionSequence);
                output.Files.Add("Profielstickers-freesvolgorde.xlsx");
                var profileTapWorklist = new ProfileTapWorklistService().Build(profileProductionSequence);
                new ProfileTapWorklistXlsxExporter().Export(Path.Combine(outputFolder, "Profieltappen-werkplaatslijst.xlsx"), profileTapWorklist);
                output.Files.Add("Profieltappen-werkplaatslijst.xlsx");
                new ProfileMachiningVisualSvgExporter().Export(Path.Combine(outputFolder, "Profielbewerkingen-visuele-controle.svg"), profileProductionSequence, profileTapWorklist);
                output.Files.Add("Profielbewerkingen-visuele-controle.svg");
                Write(output, outputFolder, "ProfielCNC-Operatorprogramma.tap", new ProfileCncOperatorProgramGenerator(profileConfigurationService.ToCncMachineSettings(profileConfiguration)).Generate(profileConfiguration, profileProductionSequence));
                Write(output, outputFolder, "ProfielStationPlan.txt", csv.ExportProfileStationPlan(profileConfiguration));
            }

            var sheetCamParts = model.Sheets
                .Where(sheet => sheet.Material == null || !sheet.Material.IsAdditiveManufactured)
                .ToList();
            if (sheetCamParts.Count > 0)
            {
                Write(output, outputFolder, "Plaatgaten.csv", csv.ExportSheetHoleList(sheetCamParts));
                Write(output, outputFolder, "CAM-operaties.csv", csv.ExportCamOperations(sheetCamParts, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, camJob.EdgeChamferWidthMm, camJob.ThroughCutOvertravelMm));
                Write(output, outputFolder, "ToolLibrary.csv", csv.ExportToolLibrary(camJob));
            }

            if (control.HasRailData(model))
            {
                Write(output, outputFolder, "RailgatenControle.csv", control.ExportRailHoleControl(model));
                Write(output, outputFolder, "RailTemplateControle.csv", control.ExportUsedRailTemplates(model));
                Write(output, outputFolder, "RailTemplateVisualisatie.svg", control.ExportUsedRailTemplatesSvg(model));
            }

            Write(output, outputFolder, "AssemblageControle.txt", control.ExportAssemblyControl(model, request));
            Write(output, outputFolder, "AssemblageControle.csv", control.ExportAssemblyControlCsv(model, request));
            Write(output, outputFolder, "TekencontractControle.csv", control.ExportDrawingContractControl(model));
            Write(output, outputFolder, "TekencontractValidatie.csv", control.ExportDrawingContractValidation(model));
            var pricing = new PortalPricingService();
            var price = pricing.Calculate(model, nestingPlan);
            Write(output, outputFolder, "BOM.csv", csv.ExportBom(model, price));
            Write(output, outputFolder, "PrijsOverzicht.csv", pricing.ExportCsv(price));
            new PriceOverviewXlsxExporter().Export(Path.Combine(outputFolder, "Projectcalculatie.xlsx"), model, price);
            output.Files.Add("Projectcalculatie.xlsx");
            Write(output, outputFolder, "Offerte.txt", pricing.ExportOfferText(request, price, "CONCEPT"));

            if (string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                var adapterFiles = new PlinthClipAdapterExportService().ExportOpenScad(factory.BuildWorkbenchCabinet(request), outputFolder);
                foreach (var adapterFile in adapterFiles) output.Files.Add(adapterFile);
            }

            if (sheetCamParts.Count > 0)
            {
                var gcode = new Mach3GCodeGenerator();
                foreach (var sheet in sheetCamParts)
                {
                    Write(output, outputFolder, SafeFileName(sheet.Name) + ".tap", gcode.GenerateSheetPart(sheet, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, camJob.EdgeChamferWidthMm, machine, sheet.Material.ThicknessMm, camJob.TabWidthMm, camJob.TabHeightMm, camJob.ThroughCutOvertravelMm, camJob.SafeTravelZMm, camJob.ContourOnionSkinMm, camJob.FinalContourFeedRateMmMin, camJob.FinalContourRampLengthMm));
                }

                var nestingFolderName = HasCompletedSheetParts(request)
                    ? "Herstel_na_plaat_2"
                    : (request.RevisionAfterMilledTestSheetOne ? "Revisie_vanaf_plaat_2" : (request.TestFitFirstSheet ? "Testplaat_nesting" : "Nesting"));
                var nestingRelativePrefix = nestingFolderName + "\\";
                var nestingFolder = Path.Combine(outputFolder, nestingFolderName);
                Directory.CreateDirectory(nestingFolder);
                var nestingExporter = new NestingExporter();
                output.NestingSvg = nestingExporter.ExportSvg(nestingPlan, contourTool);
                if (HasCompletedSheetParts(request))
                    Write(output, nestingFolder, nestingRelativePrefix + "Herstel-overzicht.txt", RecoveryOverview(request, nestingPlan));
                Write(output, nestingFolder, nestingRelativePrefix + "NestPlan.csv", nestingExporter.ExportCsv(nestingPlan));
                Write(output, nestingFolder, nestingRelativePrefix + "NestVisualisatie.svg", output.NestingSvg);

                var nestedGcode = new NestedMach3GCodeGenerator();
                var toolpathPreview = new ToolpathPreviewExporter();
                if (camJob.EnablePencilMarking)
                {
                    var pencilMarking = new PencilMarkingGCodeGenerator();
                    Write(output, nestingFolder, nestingRelativePrefix + "PotloodMarkeerPlan.csv", pencilMarking.ExportPlan(nestingPlan, camJob.BuildPencilMarkingOptions()));
                }

                for (var stockIndex = 0; stockIndex < nestingPlan.StockSheets.Count; stockIndex++)
                {
                    var stock = nestingPlan.StockSheets[stockIndex];
                    var nextProgramFile = stockIndex + 1 < nestingPlan.StockSheets.Count
                        ? SafeFileName(nestingPlan.StockSheets[stockIndex + 1].Name) + ".tap"
                        : null;
                    Write(
                        output,
                        nestingFolder,
                        nestingRelativePrefix + SafeFileName(stock.Name) + ".tap",
                        nestedGcode.Generate(stock, contourTool, machine, camJob, stockIndex + 1, nestingPlan.StockSheets.Count, nextProgramFile));
                    Write(output, nestingFolder, nestingRelativePrefix + "ToolpathPreview_" + SafeFileName(stock.Name) + ".svg", toolpathPreview.ExportSvg(stock, contourTool));
                }
            }

            WriteVisualExports(output, outputFolder, model, request);
            Write(output, outputFolder, "SolidWorksExportPlan.txt", FormatPlan(SolidWorksExportPlan.FromWorkbench(model)));
            return output;
        }

        public PortalSolidWorksExportResponse GenerateSolidWorksControlFiles(PortalQuoteRequest request, string rootFolder)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrWhiteSpace(rootFolder)) throw new ArgumentException("Portal-outputmap ontbreekt.");

            var includeCam = request.ExportIncludeCam != false;
            var includeSolidWorks = request.ExportIncludeSolidWorks != false;
            var includeCustomerPackage = request.ExportIncludeCustomerPackage != false;
            var includeInteractiveCustomerModel = request.ExportIncludeInteractiveCustomerModel != false;
            var includeHighDefinitionCustomerModel = request.ExportIncludeHighDefinitionCustomerModel != false;
            var includeThreeDPrint = request.ExportIncludeThreeDPrint != false;
            var includeControls = request.ExportIncludeControls != false;
            var keepSolidWorksArchive = ShouldKeepSolidWorksArchive(request);
            var includeAnyCustomerOutput = includeCustomerPackage || includeInteractiveCustomerModel || includeHighDefinitionCustomerModel;
            var releaseContract = new ProductReleaseContractService().LoadRequired(request.Product);
            var conceptExport = !releaseContract.ProductionReleased;
            if (!includeCam && !includeSolidWorks && !includeCustomerPackage && !includeInteractiveCustomerModel
                && !includeHighDefinitionCustomerModel && !includeThreeDPrint && !includeControls)
                throw new InvalidOperationException("Selecteer minimaal één onderdeel voor de projectexport.");
            EnsureProjectExportSelectionAllowed(request, releaseContract,
                includeCam, includeSolidWorks, includeCustomerPackage, includeInteractiveCustomerModel,
                includeHighDefinitionCustomerModel, includeThreeDPrint, includeControls);

            if (string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase))
            {
                var preflightAudit = AuditWorkbenchCabinet(request);
                if (!preflightAudit.Passed)
                    throw new InvalidOperationException("Projectexport afgebroken vóór SolidWorks: de geometrie-, frees- of BOM-controle bevat "
                        + preflightAudit.Errors.Count.ToString(CultureInfo.InvariantCulture) + " fout(en): "
                        + string.Join(" | ", preflightAudit.Errors));
            }

            var factory = new PortalConfigurationFactory();
            var model = new ProductModelBuildService().Build(factory, request);
            var generatedAt = DateTime.Now;
            var projectLabel = ProjectFolderLabel(request, model);
            var outputFolder = UniqueProjectFolder(
                Path.Combine(rootFolder, "Projecten"),
                generatedAt.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + projectLabel);
            Directory.CreateDirectory(outputFolder);

            var camFolder = Path.Combine(outputFolder, "01_CAM");
            var cadFolder = Path.Combine(outputFolder, "02_SolidWorks");
            var customerFolder = Path.Combine(outputFolder, "03_Klantvoorstel");
            var printFolder = Path.Combine(outputFolder, "04_3D-print");
            var projectDataFolder = Path.Combine(outputFolder, "05_Projectdata");
            var validationFolder = Path.Combine(projectDataFolder, "Validatie");
            var generationFolder = Path.Combine(projectDataFolder, "Generatie");

            string macroPath = null;
            string assemblyPath = null;
            string customerModelPath = null;
            string customerHtmlPath = null;
            string interactiveCustomerHtmlPath = null;
            string customerPowerPointPath = null;
            string customerAppendixPdfPath = null;
            string customerDrawingPath = null;
            string customerDrawingPdfPath = null;

            if (includeSolidWorks || includeCustomerPackage || includeHighDefinitionCustomerModel)
            {
                if (includeControls) macroPath = new SolidWorksMacroExporter().ExportMacro(model, outputFolder);
                assemblyPath = RunSolidWorksWorker(request, outputFolder);
                if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                    throw new InvalidOperationException("SolidWorks heeft geen assembly aangemaakt.");

                customerModelPath = SolidWorksCustomerPresentation.CustomerModelPath(assemblyPath);
                customerHtmlPath = SolidWorksCustomerPresentation.CustomerHtmlPath(assemblyPath);
                var drawingOutput = SolidWorksCustomerDrawingExporter.OutputFor(assemblyPath);
                customerDrawingPath = drawingOutput.DrawingPath;

                if (includeHighDefinitionCustomerModel)
                {
                    if (!File.Exists(customerModelPath)) throw new InvalidOperationException("SolidWorks heeft geen GLB-klantmodel aangemaakt: " + customerModelPath);
                    if (!File.Exists(customerHtmlPath)) throw new InvalidOperationException("SolidWorks heeft geen HTML-klantmodel aangemaakt: " + customerHtmlPath);
                }

                if (includeCustomerPackage)
                {
                    if (!File.Exists(customerModelPath)) throw new InvalidOperationException("SolidWorks heeft geen GLB-klantmodel aangemaakt: " + customerModelPath);
                    if (!File.Exists(customerHtmlPath)) throw new InvalidOperationException("SolidWorks heeft geen HTML-klantmodel aangemaakt: " + customerHtmlPath);
                    if (!File.Exists(drawingOutput.PdfPath)) throw new InvalidOperationException("SolidWorks heeft geen PDF-werktekening aangemaakt: " + drawingOutput.PdfPath);
                    if (!File.Exists(drawingOutput.DrawingPath)) throw new InvalidOperationException("SolidWorks heeft geen bewerkbare werktekening aangemaakt: " + drawingOutput.DrawingPath);
                    customerPowerPointPath = SolidWorksCustomerPowerPointExporter.Export(customerModelPath, request, model, drawingOutput);
                    if (!File.Exists(customerPowerPointPath)) throw new InvalidOperationException("PowerPoint heeft geen klantpresentatie aangemaakt: " + customerPowerPointPath);
                    customerAppendixPdfPath = SolidWorksCustomerPowerPointExporter.CustomerPdfPath(customerModelPath, request);
                    if (!File.Exists(customerAppendixPdfPath)) throw new InvalidOperationException("PowerPoint heeft geen statische klantbijlage aangemaakt: " + customerAppendixPdfPath);
                }
            }

            // Bouw CAM, controles, 3D-printdelen en statische aanzichten pas op
            // nadat de SolidWorks-basis gereed is. Een mislukte COM-export kan zo
            // nooit een half projectpakket achterlaten dat volledig lijkt.
            if (!conceptExport && (includeCam || includeCustomerPackage || includeInteractiveCustomerModel || includeThreeDPrint || includeControls))
            {
                var production = GenerateOrderFiles(request, camFolder);
                model = production.Model;
            }

            if (includeThreeDPrint)
            {
                MoveDirectoryIfPresent(Path.Combine(camFolder, "3D-print"), printFolder);
                if (!Directory.Exists(printFolder))
                {
                    Directory.CreateDirectory(printFolder);
                    File.WriteAllText(
                        Path.Combine(printFolder, "Geen_3D-printonderdelen.txt"),
                        "Voor deze configuratie zijn geen afzonderlijke 3D-printonderdelen gegenereerd." + Environment.NewLine);
                }
            }
            else
                DeleteDirectoryIfPresent(Path.Combine(camFolder, "3D-print"));

            if (includeAnyCustomerOutput)
                Directory.CreateDirectory(customerFolder);

            var sourceViewsFolder = Path.Combine(camFolder, "Aanzichten");
            var customerViewsFolder = Path.Combine(customerFolder, "Aanzichten");
            if (includeCustomerPackage)
            {
                MoveDirectoryIfPresent(sourceViewsFolder, customerViewsFolder);
                if (includeInteractiveCustomerModel)
                    interactiveCustomerHtmlPath = MoveInteractiveCustomerModel(
                        Path.Combine(customerViewsFolder, "3D-model.html"),
                        customerFolder);
                else
                {
                    DeleteFileIfPresent(Path.Combine(customerViewsFolder, "3D-model.html"));
                    DeleteFileIfPresent(Path.Combine(customerViewsFolder, "3D-model.json"));
                }
            }
            else if (includeInteractiveCustomerModel)
            {
                interactiveCustomerHtmlPath = MoveInteractiveCustomerModel(Path.Combine(sourceViewsFolder, "3D-model.html"), customerFolder);
                MoveFileToFolder(Path.Combine(sourceViewsFolder, "3D-model.json"), customerViewsFolder);
                DeleteDirectoryIfPresent(sourceViewsFolder);
            }
            else
                DeleteDirectoryIfPresent(sourceViewsFolder);

            if (includeCustomerPackage || includeHighDefinitionCustomerModel)
            {
                var sourceMaterialFolder = Path.Combine(cadFolder, "Materialen");
                var customerMaterialFolder = Path.Combine(customerFolder, "Render-assets", "Materialen");
                MoveDirectoryIfPresent(sourceMaterialFolder, customerMaterialFolder);
                RelinkRenderMaterialAssets(customerMaterialFolder, sourceMaterialFolder, customerMaterialFolder);
            }
            else
                DeleteDirectoryIfPresent(Path.Combine(cadFolder, "Materialen"));

            if (includeHighDefinitionCustomerModel)
            {
                var highDefinitionFolder = Path.Combine(customerFolder, "3D-high-definition");
                customerModelPath = MoveFileToFolder(customerModelPath, highDefinitionFolder);
                customerHtmlPath = MoveFileToFolder(customerHtmlPath, highDefinitionFolder);
            }
            else
            {
                DeleteFileIfPresent(customerModelPath);
                DeleteFileIfPresent(customerHtmlPath);
                customerModelPath = null;
                customerHtmlPath = null;
            }

            if (includeCustomerPackage)
            {
                customerPowerPointPath = MoveFileToFolder(customerPowerPointPath, customerFolder);
                customerAppendixPdfPath = MoveFileToFolder(customerAppendixPdfPath, customerFolder);
                var drawingOutput = SolidWorksCustomerDrawingExporter.OutputFor(assemblyPath);
                customerDrawingPdfPath = MoveFileToFolder(drawingOutput.PdfPath, customerFolder);
                MoveFileToFolder(
                    drawingOutput.GeneralSheetImagePath,
                    Path.Combine(customerFolder, "Render-assets"));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(assemblyPath))
                {
                    var drawingOutput = SolidWorksCustomerDrawingExporter.OutputFor(assemblyPath);
                    DeleteFileIfPresent(drawingOutput.PdfPath);
                    DeleteFileIfPresent(drawingOutput.GeneralSheetImagePath);
                }
            }

            if (!conceptExport)
                EnsureProfileConfigurationInProject(model, camFolder, projectDataFolder, validationFolder);

            if (includeControls)
            {
                Directory.CreateDirectory(projectDataFolder);
                WriteProjectConfiguration(request, Path.Combine(projectDataFolder, "Configuratie.json"));
                if (conceptExport)
                    WriteConceptReleaseFiles(outputFolder, projectDataFolder, releaseContract, model);
                else
                    MoveProjectDataFiles(camFolder, projectDataFolder, validationFolder, generationFolder);
                macroPath = MoveFileToFolder(macroPath, generationFolder);
                MoveFileToFolder(Path.Combine(outputFolder, "SolidWorksMacroInstructies.txt"), generationFolder);
                MoveFileToFolder(Path.Combine(outputFolder, "SolidWorksWorkerInput.json"), generationFolder);
                MoveFileToFolder(Path.Combine(outputFolder, "SolidWorksWorkerResult.json"), generationFolder);
            }
            else
            {
                DeleteCamControlFiles(camFolder);
                DeleteFileIfPresent(Path.Combine(outputFolder, "SolidWorksMacroInstructies.txt"));
                DeleteFileIfPresent(Path.Combine(outputFolder, "SolidWorksWorkerInput.json"));
                DeleteFileIfPresent(Path.Combine(outputFolder, "SolidWorksWorkerResult.json"));
            }

            if (conceptExport && !includeControls)
                WriteConceptReleaseFiles(outputFolder, null, releaseContract, model);

            if (!includeCam) DeleteDirectoryIfPresent(camFolder);
            if (!keepSolidWorksArchive)
            {
                CloseGeneratedSolidWorksDocuments(cadFolder);
                DeleteDirectoryIfPresent(cadFolder);
                assemblyPath = null;
                customerDrawingPath = null;
            }
            else
            {
                EnsureSolidWorksFolderContainsOnlyNativeDocuments(cadFolder);
            }

            var partCount = keepSolidWorksArchive && Directory.Exists(cadFolder)
                ? Directory.GetFiles(cadFolder, "*.SLDPRT").Length
                : 0;

            File.WriteAllText(
                Path.Combine(outputFolder, "ProjectOutputOverzicht.txt"),
                "PROJECTOUTPUT" + Environment.NewLine
                + "Gegenereerd: " + generatedAt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine
                + "Project: " + projectLabel + Environment.NewLine
                + "Configuratie: " + model.ProjectName + Environment.NewLine
                + "Vrijgavestatus: " + (conceptExport ? "CONCEPT - NIET PRODUCTIEVRIJGEGEVEN" : "productie-export") + Environment.NewLine
                + Environment.NewLine
                + "SELECTIE" + Environment.NewLine
                + OutputOverviewLine("01_CAM", includeCam, camFolder)
                + OutputOverviewLine("02_SolidWorks", keepSolidWorksArchive, cadFolder)
                + OutputOverviewLine("03_Klantvoorstel", includeAnyCustomerOutput, customerFolder)
                + "  Klantpresentatie PDF/PPT en aanzichten: " + (includeCustomerPackage ? "geselecteerd" : "niet geselecteerd") + Environment.NewLine
                + "  Interactief 3D-model met schuifregelaars: " + (includeInteractiveCustomerModel ? "geselecteerd" : "niet geselecteerd") + Environment.NewLine
                + "  High-definition 3D-model + native SW-naslag: " + (includeHighDefinitionCustomerModel ? "geselecteerd" : "niet geselecteerd") + Environment.NewLine
                + OutputOverviewLine("04_3D-print", includeThreeDPrint, printFolder)
                + OutputOverviewLine("05_Projectdata", includeControls, projectDataFolder));

            var fileCount = Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories).Length;
            var response = new PortalSolidWorksExportResponse
            {
                Ok = true,
                Message = (conceptExport ? "Conceptexport — niet productievrijgegeven — gegenereerd met: " : "Projectexport gegenereerd met: ")
                    + SelectedOutputNames(includeCam, includeSolidWorks, includeCustomerPackage, includeInteractiveCustomerModel, includeHighDefinitionCustomerModel, includeThreeDPrint, includeControls) + ".",
                IsConceptExport = conceptExport,
                OutputFolder = outputFolder,
                AssemblyPath = assemblyPath,
                ControlModelPath = assemblyPath,
                CustomerModelPath = customerModelPath,
                CustomerHtmlPath = customerHtmlPath,
                InteractiveCustomerHtmlPath = interactiveCustomerHtmlPath,
                CustomerPowerPointPath = customerPowerPointPath,
                CustomerAppendixPdfPath = customerAppendixPdfPath,
                CustomerDrawingPath = customerDrawingPath,
                CustomerDrawingPdfPath = customerDrawingPdfPath,
                MacroPath = macroPath,
                PartCount = partCount,
                FileCount = fileCount,
                PlacementCount = model.AssemblyPlacements.Count
            };
            response.OpenReleaseItems.AddRange(releaseContract.OpenReleaseItems ?? new string[0]);
            return response;
        }

        private static void EnsureProjectExportSelectionAllowed(
            PortalQuoteRequest request,
            ProductReleaseContract release,
            bool cam,
            bool solidWorks,
            bool customerPackage,
            bool interactiveCustomerModel,
            bool highDefinitionCustomerModel,
            bool threeDPrint,
            bool controls)
        {
            if (release == null || release.ProductionReleased) return;
            var allowed = new HashSet<string>(release.ConceptExportOutputs ?? new string[0], StringComparer.OrdinalIgnoreCase);
            var selected = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { "CAM", cam },
                { "SolidWorks", solidWorks },
                { "Klantvoorstel", customerPackage },
                { "Interactief 3D", interactiveCustomerModel },
                { "High-definition 3D", highDefinitionCustomerModel },
                { "3D-print", threeDPrint },
                { "Projectdata", controls }
            };
            var unsupported = selected.Where(item => item.Value && !allowed.Contains(item.Key)).Select(item => item.Key).ToArray();
            if (unsupported.Length > 0)
                throw new InvalidOperationException("Product is nog niet productievrijgegeven. Alleen conceptexport toegestaan: "
                    + string.Join(", ", allowed.ToArray()) + ". Niet toegestaan: " + string.Join(", ", unsupported) + ".");
        }

        private static void WriteConceptReleaseFiles(
            string outputFolder,
            string projectDataFolder,
            ProductReleaseContract release,
            WorkbenchModel model)
        {
            var lines = new List<string>
            {
                "CONCEPTEXPORT - NIET PRODUCTIEVRIJGEGEVEN",
                "",
                "Product: " + (model == null ? release.ProductId : model.ProjectName),
                "Doel: parametrische machine-/robotbasis voor verdere handmatige samenbouw in SolidWorks.",
                "Deze export is niet geschikt voor CAM, inkoopvrijgave, klantvrijgave of productie.",
                "",
                "WORKFLOW",
                string.IsNullOrWhiteSpace(release.ConceptExportNote) ? "Conceptassembly; toepassingsdelen worden later toegevoegd." : release.ConceptExportNote,
                "",
                "OPENSTAANDE PUNTEN VOOR RELEASE"
            };
            var openItems = release.OpenReleaseItems ?? new string[0];
            if (openItems.Length == 0) lines.Add("- Geen openstaande punten geregistreerd.");
            else lines.AddRange(openItems.Select(item => "- " + item));
            lines.Add("");
            lines.Add("Vrijgave vereist een expliciete masterdatawijziging en geslaagde controles; deze tekst is geen vrijgave.");
            var contents = string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
            File.WriteAllText(Path.Combine(outputFolder, "CONCEPT_NIET_PRODUCTIEVRIJGEGEVEN.txt"), contents);
            if (!string.IsNullOrWhiteSpace(projectDataFolder))
            {
                Directory.CreateDirectory(projectDataFolder);
                File.WriteAllText(Path.Combine(projectDataFolder, "Openstaande-vrijgavepunten.txt"), contents);
            }
            WriteStructuralCalculationReport(outputFolder, projectDataFolder, model == null ? null : model.StructuralCalculation);
        }

        private static void WriteStructuralCalculationReport(string outputFolder, string projectDataFolder, StructuralCalculationReport report)
        {
            if (report == null) return;
            var lines = new List<string>
            {
                "CONSTRUCTIEBEREKENING - INDICATIEF, GEEN PRODUCTIEVRIJGAVE",
                "",
                "Product-ID: " + report.ProductId,
                "Status: " + report.Status,
                "Referentiebelasting totaal: " + F(report.ReferenceLoadN) + " N",
                "Draagprofiel: " + report.ProfileMaterialId,
                "Overspanning: " + F(report.SpanMm) + " mm",
                "Parallelle liggers: " + report.ParallelBeamCount,
                "Elasticiteitsmodulus: " + F(report.ElasticModulusNPerMm2) + " N/mm2",
                "Sterke-as-traagheidsmoment: " + F(report.StrongAxisInertiaCm4) + " cm4",
                "Berekende doorbuiging bij referentiebelasting: " + F(report.CalculatedDeflectionMm) + " mm",
                "Formule: " + report.Formula,
                "",
                "OPEN DATA / NOG VAST TE LEGGEN"
            };
            lines.AddRange(report.OpenData.Select(item => "- " + item));
            var text = string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
            File.WriteAllText(Path.Combine(outputFolder, "Constructieberekening.txt"), text);
            var targetFolder = string.IsNullOrWhiteSpace(projectDataFolder) ? outputFolder : projectDataFolder;
            Directory.CreateDirectory(targetFolder);
            File.WriteAllText(Path.Combine(targetFolder, "Constructieberekening.json"),
                new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(report));
            if (!string.Equals(targetFolder, outputFolder, StringComparison.OrdinalIgnoreCase))
                File.WriteAllText(Path.Combine(targetFolder, "Constructieberekening.txt"), text);
        }

        private static string ProjectFolderLabel(PortalQuoteRequest request, WorkbenchModel model)
        {
            var configured = request == null ? null : request.ProjectName;
            if (string.IsNullOrWhiteSpace(configured)) return SafeFileName(model == null ? "Project" : model.ProjectName);

            configured = SafeFileName(configured.Trim());
            var generated = SafeFileName(model == null ? "" : model.ProjectName);
            if (string.IsNullOrWhiteSpace(generated) || configured.IndexOf(generated, StringComparison.OrdinalIgnoreCase) >= 0)
                return configured;
            return configured + "_" + generated;
        }

        private static string UniqueProjectFolder(string projectsRoot, string folderName)
        {
            Directory.CreateDirectory(projectsRoot);
            var basePath = Path.Combine(projectsRoot, SafeFileName(folderName));
            if (!Directory.Exists(basePath)) return basePath;
            for (var suffix = 2; suffix < 1000; suffix++)
            {
                var candidate = basePath + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                if (!Directory.Exists(candidate)) return candidate;
            }
            return basePath + "_" + Guid.NewGuid().ToString("N");
        }

        private static void MoveProjectDataFiles(
            string camFolder,
            string projectDataFolder,
            string validationFolder,
            string generationFolder)
        {
            foreach (var name in CommercialProjectFileNames())
                MoveFileToFolder(Path.Combine(camFolder, name), projectDataFolder);
            foreach (var name in ValidationFileNames())
                MoveFileToFolder(Path.Combine(camFolder, name), validationFolder);
            foreach (var name in GenerationFileNames())
                MoveFileToFolder(Path.Combine(camFolder, name), generationFolder);
        }

        private static void DeleteCamControlFiles(string camFolder)
        {
            foreach (var name in CommercialProjectFileNames()) DeleteFileIfPresent(Path.Combine(camFolder, name));
            foreach (var name in ValidationFileNames()) DeleteFileIfPresent(Path.Combine(camFolder, name));
            foreach (var name in GenerationFileNames()) DeleteFileIfPresent(Path.Combine(camFolder, name));
        }

        private static string[] CommercialProjectFileNames()
        {
            return new[]
            {
                "BOM.csv",
                "PrijsOverzicht.csv",
                "Projectcalculatie.xlsx",
                "Offerte.txt"
            };
        }

        private static string[] ValidationFileNames()
        {
            return new[]
            {
                "VrijgaveControle.txt",
                "AssemblageControle.txt",
                "AssemblageControle.csv",
                "TekencontractControle.csv",
                "TekencontractValidatie.csv",
                "RailgatenControle.csv",
                "RailTemplateControle.csv",
                "RailTemplateVisualisatie.svg"
            };
        }

        private static string[] GenerationFileNames()
        {
            return new[] { "SolidWorksExportPlan.txt" };
        }

        private static void WriteProjectConfiguration(PortalQuoteRequest request, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            File.WriteAllText(path, serializer.Serialize(request));
        }

        private static void EnsureSolidWorksFolderContainsOnlyNativeDocuments(string cadFolder)
        {
            if (!Directory.Exists(cadFolder)) return;
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".SLDPRT",
                ".SLDASM",
                ".SLDDRW"
            };
            var invalid = Directory.GetFiles(cadFolder, "*", SearchOption.AllDirectories)
                .Where(path => !allowed.Contains(Path.GetExtension(path)))
                .Take(10)
                .ToArray();
            if (invalid.Length == 0) return;
            throw new InvalidOperationException(
                "02_SolidWorks mag uitsluitend native SolidWorks-documenten bevatten. Verkeerd gerouteerd: "
                + string.Join(", ", invalid.Select(Path.GetFileName).ToArray()));
        }

        private static void MoveDirectoryIfPresent(string sourceFolder, string targetFolder)
        {
            if (!Directory.Exists(sourceFolder)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(targetFolder) ?? "");
            if (Directory.Exists(targetFolder)) Directory.Delete(targetFolder, true);
            Directory.Move(sourceFolder, targetFolder);
        }

        private static void RelinkRenderMaterialAssets(string materialFolder, string oldFolder, string newFolder)
        {
            if (!Directory.Exists(materialFolder)) return;
            foreach (var path in Directory.GetFiles(materialFolder, "*.p2m", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(path);
                var updated = content.Replace(oldFolder, newFolder);
                if (!string.Equals(content, updated, StringComparison.Ordinal))
                    File.WriteAllText(path, updated, Encoding.ASCII);
            }
        }

        private static void DeleteDirectoryIfPresent(string folder)
        {
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path);
        }

        private static string OutputOverviewLine(string label, bool selected, string path)
        {
            if (!selected) return label + ": niet geselecteerd" + Environment.NewLine;
            var present = Directory.Exists(path) && Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length > 0;
            return label + ": " + (present ? path : "geselecteerd; geen bestanden voor deze configuratie") + Environment.NewLine;
        }

        internal static bool ShouldKeepSolidWorksArchive(PortalQuoteRequest request)
        {
            return request != null
                && (request.ExportIncludeSolidWorks != false || request.ExportIncludeHighDefinitionCustomerModel != false);
        }

        private static string SelectedOutputNames(
            bool cam,
            bool solidWorks,
            bool customerPackage,
            bool interactiveCustomerModel,
            bool highDefinitionCustomerModel,
            bool threeDPrint,
            bool controls)
        {
            var names = new List<string>();
            if (cam) names.Add("CAM");
            if (solidWorks) names.Add("SolidWorks");
            if (customerPackage) names.Add("klantvoorstel");
            if (interactiveCustomerModel) names.Add("interactief 3D-klantmodel");
            if (highDefinitionCustomerModel) names.Add("high-definition 3D-klantmodel met SW-bronbestanden");
            if (threeDPrint) names.Add("3D-print");
            if (controls) names.Add("projectdata");
            return string.Join(", ", names.ToArray());
        }

        private static string MoveFileToFolder(string sourcePath, string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return sourcePath;
            Directory.CreateDirectory(targetFolder);
            var targetPath = Path.Combine(targetFolder, Path.GetFileName(sourcePath));
            if (File.Exists(targetPath)) File.Delete(targetPath);
            File.Move(sourcePath, targetPath);
            return targetPath;
        }

        private static string InteractiveCustomerModelPath(string customerFolder)
        {
            return Path.Combine(customerFolder, "3D-model.html");
        }

        private static string MoveInteractiveCustomerModel(string sourcePath, string customerFolder)
        {
            var movedPath = MoveFileToFolder(sourcePath, customerFolder);
            var expectedPath = InteractiveCustomerModelPath(customerFolder);
            if (!string.Equals(Path.GetFullPath(movedPath), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase)
                || !File.Exists(expectedPath))
                throw new InvalidOperationException("Het interactieve 3D-klantmodel kon niet in de hoofdmap Klantvoorstel worden geplaatst.");
            return expectedPath;
        }

        private static string RunSolidWorksWorker(PortalQuoteRequest request, string outputFolder)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var workerInput = Path.Combine(outputFolder, "SolidWorksWorkerInput.json");
            var workerResult = Path.Combine(outputFolder, "SolidWorksWorkerResult.json");
            File.WriteAllText(workerInput, serializer.Serialize(request));
            SolidWorksWorkerResult lastResult = null;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                DeleteFileIfPresent(workerResult);
                var result = RunSolidWorksWorkerAttempt(workerInput, workerResult, serializer);
                if (result != null && result.ContractVersion == 1 && result.Ok && !string.IsNullOrWhiteSpace(result.AssemblyPath) && File.Exists(result.AssemblyPath))
                    return result.AssemblyPath;

                lastResult = result;
                var error = result == null ? "Ongeldig SolidWorks-helperresultaat." : result.Error;
                if (attempt >= 2 || !IsTransientSolidWorksRpcFailure(error))
                    throw new InvalidOperationException(error);

                PrepareSolidWorksRetry(outputFolder);
                Thread.Sleep(3000);
            }

            throw new InvalidOperationException(lastResult == null ? "Ongeldig SolidWorks-helperresultaat." : lastResult.Error);
        }

        private static SolidWorksWorkerResult RunSolidWorksWorkerAttempt(
            string workerInput,
            string workerResult,
            JavaScriptSerializer serializer)
        {
            var executable = Process.GetCurrentProcess().MainModule.FileName;
            var start = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--solidworks-worker " + Q(workerInput) + " " + Q(workerResult),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            using (var process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("SolidWorks-helperproces kon niet starten.");
                if (!process.WaitForExit(10 * 60 * 1000))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("SolidWorks-export duurde langer dan 10 minuten.");
                }
                if (!File.Exists(workerResult)) throw new InvalidOperationException("SolidWorks-helper stopte zonder resultaat (exitcode " + process.ExitCode + ").");
                return serializer.Deserialize<SolidWorksWorkerResult>(File.ReadAllText(workerResult));
            }
        }

        private static void CloseGeneratedSolidWorksDocuments(string cadFolder)
        {
            if (string.IsNullOrWhiteSpace(cadFolder) || !Directory.Exists(cadFolder)) return;
            var executable = Process.GetCurrentProcess().MainModule.FileName;
            var start = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--solidworks-close-documents-under " + Q(cadFolder),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            using (var process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("SolidWorks-documentopruiming kon niet starten.");
                if (!process.WaitForExit(60 * 1000))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("SolidWorks-documentopruiming duurde langer dan 60 seconden.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("SolidWorks-documentopruiming stopte met exitcode " + process.ExitCode + ".");
            }
        }

        private static bool IsTransientSolidWorksRpcFailure(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("0x800706BA", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("RPC-server is niet beschikbaar", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("0x80010108", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("disconnected from its clients", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void PrepareSolidWorksRetry(string outputFolder)
        {
            try
            {
                var cadFolder = Path.Combine(outputFolder, "02_SolidWorks");
                if (Directory.Exists(cadFolder)) Directory.Delete(cadFolder, true);
            }
            catch
            {
                // Een verdwenen COM-server kan bestandsvergrendelingen kort laten
                // nalopen. De tweede exporter overschrijft dezelfde doelbestanden.
            }
        }

        private static string Q(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\\\"") + "\"";
        }

        private static string ExceptionDetails(Exception error)
        {
            var messages = new List<string>();
            while (error != null)
            {
                if (!string.IsNullOrWhiteSpace(error.Message)) messages.Add(error.Message);
                error = error.InnerException;
            }
            return string.Join(" -> ", messages.ToArray());
        }

        private static string FormatAudit(WorkbenchCabinetAuditResult audit)
        {
            var sb = new StringBuilder();
            sb.AppendLine(audit.Passed ? "UITKOMST: GESLAAGD" : "UITKOMST: AFGEKEURD");
            sb.AppendLine();
            sb.AppendLine("CONTROLES");
            foreach (var check in audit.Checks) sb.AppendLine("OK - " + check);
            sb.AppendLine();
            sb.AppendLine("FOUTEN");
            if (audit.Errors.Count == 0) sb.AppendLine("Geen.");
            foreach (var error in audit.Errors) sb.AppendLine("FOUT - " + error);
            sb.AppendLine();
            sb.AppendLine("OPENSTAANDE PUNTEN / WAARSCHUWINGEN");
            if (audit.Warnings.Count == 0) sb.AppendLine("Geen.");
            foreach (var warning in audit.Warnings) sb.AppendLine("LET OP - " + warning);
            return sb.ToString();
        }

        private static NestingPlan BuildNestingPlan(WorkbenchModel model, MachineProfile machine, AppSettings settings, PortalQuoteRequest request)
        {
            var priorityNames = TestFitPriorityPartNames(model, request);
            var plan = new SheetNestingEngine().Build(
                model,
                machine,
                EffectiveNestSpacing(settings),
                settings.NestMarginMm,
                settings.NestStockLengthMm,
                settings.NestStockWidthMm,
                priorityNames);
            if (priorityNames != null && priorityNames.Count > 0) ValidateTestFitFirstSheet(plan, priorityNames);
            if (request != null && request.RevisionAfterMilledTestSheetOne && plan.StockSheets.Count > 0)
            {
                var recovery = HasCompletedSheetParts(request);
                for (var index = 0; index < plan.StockSheets.Count; index++)
                {
                    var stock = plan.StockSheets[index];
                    stock.SheetNumber = index + 2;
                    stock.Name = (stock.Material == null ? "Plaat" : stock.Material.Name.Replace(" ", "_"))
                        + (index == 0
                            ? (recovery ? "_HerstelPlaat_02" : "_RevisiePlaat_02")
                            : "_NestPlaat_" + (index + 2).ToString("00", CultureInfo.InvariantCulture));
                }
            }
            return plan;
        }

        private static IList<string> TestFitPriorityPartNames(WorkbenchModel model, PortalQuoteRequest request)
        {
            if (request == null || (!request.TestFitFirstSheet && !request.RevisionAfterMilledTestSheetOne)) return null;
            if (!string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Testplaat-nesting is nu alleen vastgelegd voor de werkbankkast.");

            if (request.RevisionAfterMilledTestSheetOne)
            {
                var revisionParts = new List<string>
                {
                    "Draaideur rechts paar 1",
                    "Volledig tussenschot dubbel links U2",
                    "Bovenlade front U3",
                    "Bovenlade zijde links U3",
                    "Bovenlade zijde rechts U3"
                };
                var completed = CompletedSheetPartNames(request);
                foreach (var name in revisionParts)
                {
                    if (model.Sheets.All(part => !string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase))
                        && !completed.Contains(name))
                        throw new InvalidOperationException("Revisieplaat 2 kan niet worden gemaakt: vereist onderdeel ontbreekt: " + name + ".");
                }
                return revisionParts
                    .Where(name => model.Sheets.Any(part => string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var required = new List<string>
            {
                "Werkbank zijwand links",
                "Draaideur links paar 1",
                "Bovenlade bodem U1",
                "Bovenlade front U1",
                "Bovenlade zijde links U1",
                "Bovenlade zijde rechts U1",
                "Bovenlade achter U1"
            };
            foreach (var name in required)
            {
                if (model.Sheets.All(part => !string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Testplaat kan niet worden gemaakt: vereist onderdeel ontbreekt: " + name + ".");
            }

            var materials = model.Sheets
                .Where(part => required.Any(name => string.Equals(name, part.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(part => (part.Material == null ? "" : part.Material.Id) + "|" + (part.Material == null ? "" : part.Material.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (materials.Length != 1)
                throw new InvalidOperationException("Testplaat kan niet worden gemaakt: zijwand, deur en complete lade moeten uit hetzelfde materiaal en dezelfde dikte komen.");
            return required;
        }

        private static void ApplyRevisionAfterMilledTestSheetOne(WorkbenchModel model, PortalQuoteRequest request)
        {
            if (request == null || !request.RevisionAfterMilledTestSheetOne) return;
            if (!string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Revisie vanaf gefreesde testplaat 1 is alleen voor de werkbankkast vastgelegd.");

            var milledPartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Werkbank zijwand links",
                "Draaideur links paar 1",
                "Bovenlade bodem U1",
                "Bovenlade front U1",
                "Bovenlade zijde links U1",
                "Bovenlade zijde rechts U1",
                "Bovenlade achter U1",
                "Werkbank zijwand rechts",
                "Bovenlade bodem U3",
                "Bovenlade bodem U4",
                "Bovenlade front U4",
                "Bovenlade front U2",
                "T-stijl deuraanslag X-600",
                "T-stijl deuraanslag X600"
            };
            RemoveSheetParts(model, milledPartNames);
        }

        private static void EnsureProfileConfigurationInProject(
            WorkbenchModel model,
            string camFolder,
            string projectDataFolder,
            string validationFolder)
        {
            if (!HasProfiles(model)) return;
            Directory.CreateDirectory(projectDataFolder);
            Directory.CreateDirectory(validationFolder);
            var sourceManifest = Path.Combine(camFolder, "Profielconfiguratie.json");
            var targetManifest = Path.Combine(projectDataFolder, "Profielconfiguratie.json");
            if (File.Exists(sourceManifest))
            {
                if (File.Exists(targetManifest)) File.Delete(targetManifest);
                File.Move(sourceManifest, targetManifest);
            }
            else
            {
                var service = new ProfileProjectConfigurationService();
                File.WriteAllText(targetManifest, service.Serialize(service.Build(model)));
            }

            var serviceForValidation = new ProfileProjectConfigurationService();
            var configuration = serviceForValidation.Deserialize(File.ReadAllText(targetManifest));
            var sourceValidation = Path.Combine(camFolder, "Profielconfiguratie-validatie.txt");
            var targetValidation = Path.Combine(validationFolder, "Profielconfiguratie-validatie.txt");
            if (File.Exists(sourceValidation))
            {
                if (File.Exists(targetValidation)) File.Delete(targetValidation);
                File.Move(sourceValidation, targetValidation);
            }
            else
            {
                File.WriteAllText(targetValidation, ProfileConfigurationValidationText(configuration));
            }
        }

        private static bool HasCompletedSheetParts(PortalQuoteRequest request)
        {
            return request != null
                && request.CompletedSheetPartNames != null
                && request.CompletedSheetPartNames.Any(name => !string.IsNullOrWhiteSpace(name));
        }

        private static HashSet<string> CompletedSheetPartNames(PortalQuoteRequest request)
        {
            return new HashSet<string>(
                request == null || request.CompletedSheetPartNames == null
                    ? Enumerable.Empty<string>()
                    : request.CompletedSheetPartNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void ApplyCompletedSheetParts(WorkbenchModel model, PortalQuoteRequest request)
        {
            var completed = CompletedSheetPartNames(request);
            if (completed.Count == 0) return;

            var missing = completed
                .Where(name => model.Sheets.All(part => !string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Herstel-export afgebroken: reeds bruikbaar gemarkeerde plaatdelen ontbreken in het resterende model: " + string.Join(", ", missing) + ".");

            RemoveSheetParts(model, completed);
        }

        private static void RemoveSheetParts(WorkbenchModel model, HashSet<string> partNames)
        {
            if (model == null || partNames == null || partNames.Count == 0) return;
            model.Sheets.RemoveAll(part => partNames.Contains(part.Name));
            model.AssemblyPlacements.RemoveAll(placement =>
                placement.Kind == AssemblyComponentKind.Sheet && partNames.Contains(placement.PartName));
        }

        private static string RecoveryOverview(PortalQuoteRequest request, NestingPlan plan)
        {
            var completed = CompletedSheetPartNames(request).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            var remaining = plan.StockSheets
                .SelectMany(stock => stock.Placements)
                .Select(placement => placement.Part.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sb = new StringBuilder();
            sb.AppendLine("HERSTEL-EXPORT PLAATDELEN");
            sb.AppendLine();
            sb.AppendLine("Reeds gefreesd en bruikbaar; niet opnieuw opgenomen:");
            foreach (var name in completed) sb.AppendLine("- " + name);
            sb.AppendLine();
            sb.AppendLine("Opnieuw of nog te frezen (" + remaining.Length.ToString(CultureInfo.InvariantCulture) + " delen):");
            foreach (var name in remaining) sb.AppendLine("- " + name);
            sb.AppendLine();
            sb.AppendLine("Aantal voorraadplaten: " + plan.StockSheets.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Controleer voor het frezen NestPlan.csv, NestVisualisatie.svg en iedere ToolpathPreview_*.svg.");
            return sb.ToString();
        }

        private static void ValidateTestFitFirstSheet(NestingPlan plan, IList<string> requiredNames)
        {
            if (plan == null || plan.StockSheets.Count == 0) throw new InvalidOperationException("Testplaat-nesting leverde geen voorraadplaat op.");
            var first = plan.StockSheets[0];
            var missing = requiredNames
                .Where(name => first.Placements.All(placement => !string.Equals(placement.Part.Name, name, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Testplaat-nesting kon de afgesproken testset niet volledig op plaat 1 plaatsen: " + string.Join(", ", missing) + ".");
        }

        private static string TestFitOverview(NestingPlan plan, IList<string> requiredNames)
        {
            var first = plan.StockSheets[0];
            var usedArea = first.Placements.Sum(placement => placement.LengthMm * placement.WidthMm);
            var utilization = first.StockLengthMm > 0 && first.StockWidthMm > 0
                ? usedArea / (first.StockLengthMm * first.StockWidthMm) * 100.0
                : 0;
            var sb = new StringBuilder();
            sb.AppendLine("TESTPLAAT-NESTING");
            sb.AppendLine("Plaat: " + first.Name);
            sb.AppendLine("Voorraadmaat: " + first.StockLengthMm.ToString("0", CultureInfo.InvariantCulture) + " x " + first.StockWidthMm.ToString("0", CultureInfo.InvariantCulture) + " mm");
            sb.AppendLine("Netto onderdeeloppervlak: " + utilization.ToString("0.0", CultureInfo.InvariantCulture) + "% van de bruto plaat");
            sb.AppendLine();
            sb.AppendLine("Verplichte passingstest-set:");
            foreach (var name in requiredNames) sb.AppendLine("- " + name);
            sb.AppendLine();
            sb.AppendLine("Alle onderdelen op plaat 1:");
            foreach (var placement in first.Placements)
            {
                var required = requiredNames.Any(name => string.Equals(name, placement.Part.Name, StringComparison.OrdinalIgnoreCase));
                sb.AppendLine("- " + (required ? "TESTSET | " : "AANVULLING | ") + placement.Label);
            }
            sb.AppendLine();
            sb.AppendLine("De overige onderdelen staan op de volgende nestplaten in dezelfde map.");
            return sb.ToString();
        }

        private static bool HasProfiles(WorkbenchModel model)
        {
            return model != null && ((model.Profiles != null && model.Profiles.Count > 0) || (model.ProfileOperations != null && model.ProfileOperations.Count > 0));
        }

        private static string ProfileConfigurationValidationText(ProfileProjectConfiguration configuration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PROFIELCONFIGURATIE VALIDATIE");
            sb.AppendLine("Schema: " + configuration.SchemaVersion);
            sb.AppendLine("Profielstukken: " + configuration.Profiles.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Verbindingen: " + configuration.Connections.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Productievrijgave: " + (configuration.ProductionReleased ? "JA" : "NEE"));
            if (configuration.ProductionBlockers.Count == 0)
            {
                sb.AppendLine("Blokkades: geen");
            }
            else
            {
                sb.AppendLine("Blokkades:");
                foreach (var blocker in configuration.ProductionBlockers) sb.AppendLine("- " + blocker);
            }
            sb.AppendLine();
            sb.AppendLine("Alle profielproductie-uitvoer in deze map is afgeleid van Profielconfiguratie.json.");
            return sb.ToString();
        }

        private static double EffectiveNestSpacing(AppSettings settings)
        {
            var configured = settings == null ? 0 : settings.NestSpacingMm;
            return Math.Max(configured, 18.0);
        }

        private static bool HasSheets(WorkbenchModel model)
        {
            return model != null && model.Sheets != null && model.Sheets.Count > 0;
        }

        private static void WriteVisualExports(ProductionOutput output, string outputFolder, WorkbenchModel model, PortalQuoteRequest request)
        {
            var folder = Path.Combine(outputFolder, "Aanzichten");
            Directory.CreateDirectory(folder);

            var parts = new PortalAssembly3DService().Build(model, request);
            var motion = new PortalMotionContractService().Build(model, request, parts);
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            Write(output, folder, "Aanzichten\\ProductPreview.svg", new PortalVisualizationService().BuildProductSvg(model, request));
            Write(output, folder, "Aanzichten\\Vooraanzicht.svg", BuildAssemblyViewSvg(parts, "front", "Vooraanzicht"));
            Write(output, folder, "Aanzichten\\Zijaanzicht.svg", BuildAssemblyViewSvg(parts, "side", "Zijaanzicht"));
            Write(output, folder, "Aanzichten\\Bovenaanzicht.svg", BuildAssemblyViewSvg(parts, "top", "Bovenaanzicht"));
            if (motion != null)
            {
                if (motion.Vertical != null) Write(output, folder, "Aanzichten\\Bewegingsbereik-hoogte.svg", BuildMotionRangeSvg(parts, motion, true));
                if (motion.Horizontal != null) Write(output, folder, "Aanzichten\\Bewegingsbereik-links-midden-rechts.svg", BuildMotionRangeSvg(parts, motion, false));
            }
            Write(output, folder, "Aanzichten\\3D-model.json", serializer.Serialize(parts));
            Write(output, folder, "Aanzichten\\3D-model.html", BuildAssembly3DHtml(parts, motion, serializer));
        }

        internal static string BuildMotionRangeSvg(List<PortalAssemblyPart> parts, PortalMotionContract motion, bool heightRange)
        {
            var states = heightRange
                ? new[]
                {
                    new MotionState("Laagste stand", motion.Horizontal == null ? 0 : motion.Horizontal.DefaultValue, motion.Vertical.Minimum),
                    new MotionState("Hoogste stand", motion.Horizontal == null ? 0 : motion.Horizontal.DefaultValue, motion.Vertical.Maximum)
                }
                : new[]
                {
                    new MotionState("Volledig links", motion.Horizontal.Minimum, motion.Vertical.DefaultValue),
                    new MotionState("Midden", motion.Horizontal.DefaultValue, motion.Vertical.DefaultValue),
                    new MotionState("Volledig rechts", motion.Horizontal.Maximum, motion.Vertical.DefaultValue)
                };
            var canvasW = 1200.0;
            var canvasH = heightRange ? 720.0 : 650.0;
            var title = heightRange ? "Werkhoogte · laagste en hoogste stand" : "Bladverplaatsing · links, midden en rechts";
            var panelGap = 28.0;
            var panelW = (canvasW - 80 - panelGap * (states.Length - 1)) / states.Length;
            var panelY = 100.0;
            var panelH = canvasH - 190.0;
            var allRects = states.SelectMany(state => parts.Select(part => ProjectWithMotion(part, "front", state.Horizontal, state.Vertical))).ToArray();
            var minX = allRects.Min(rect => rect.X0);
            var maxX = allRects.Max(rect => rect.X1);
            var minY = allRects.Min(rect => rect.Y0);
            var maxY = allRects.Max(rect => rect.Y1);
            var scale = Math.Min((panelW - 36) / Math.Max(1, maxX - minX), (panelH - 48) / Math.Max(1, maxY - minY));
            var worktop = heightRange ? null : parts.FirstOrDefault(part =>
                part != null && (part.Name ?? string.Empty).IndexOf("kogelpotblad", StringComparison.OrdinalIgnoreCase) >= 0);
            var fixedSupports = heightRange
                ? new PortalAssemblyPart[0]
                : parts.Where(part => part != null && (part.Name ?? string.Empty).StartsWith("Voetprofiel ", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!heightRange && (worktop == null || fixedSupports.Length == 0))
                throw new InvalidOperationException("Werkblad of vaste pootprofielen ontbreken voor de maatvoering van de bladverplaatsing.");
            var fixedSupportMin = heightRange ? 0 : fixedSupports.Min(part => part.Xmm - part.SizeXmm / 2.0);
            var fixedSupportMax = heightRange ? 0 : fixedSupports.Max(part => part.Xmm + part.SizeXmm / 2.0);
            var centeredOverhang = heightRange ? 0 : (motion.WorktopWidthMm - motion.FixedSupportOuterWidthMm) / 2.0;

            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1200\" height=\"" + F(canvasH) + "\" viewBox=\"0 0 1200 " + F(canvasH) + "\">");
            sb.AppendLine("<defs><marker id=\"dimensionArrow\" markerWidth=\"8\" markerHeight=\"8\" refX=\"4\" refY=\"4\" orient=\"auto-start-reverse\"><path d=\"M8,1 L0,4 L8,7 Z\" fill=\"#008c95\"/></marker></defs>");
            sb.AppendLine("<style>text{font-family:Arial,sans-serif}.title{font-size:28px;font-weight:700;fill:#172126}.state{font-size:18px;font-weight:700;fill:#344054}.value{font-size:16px;font-weight:700;fill:#007f87}.part{stroke:#475467;stroke-width:1}.profile{fill:#b9c1c8}.sheet{fill:#f2f1ec}.hardware{fill:#7c8791}.measure{stroke:#008c95;stroke-width:1.5}.dimension{stroke:#008c95;stroke-width:1.4;marker-start:url(#dimensionArrow);marker-end:url(#dimensionArrow)}.dimensionExtension{stroke:#008c95;stroke-width:1;stroke-dasharray:4 4}.dimensionValue{font-size:12px;font-weight:700;fill:#007f87;paint-order:stroke;stroke:#fff;stroke-width:4}.note{font-size:16px;fill:#475467}</style>");
            sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#fbfcfd\"/><text class=\"title\" x=\"40\" y=\"48\">" + Xml(title) + "</text>");
            for (var index = 0; index < states.Length; index++)
            {
                var state = states[index];
                var panelX = 40 + index * (panelW + panelGap);
                var centerX = panelX + panelW / 2.0;
                var baseY = panelY + panelH - 24;
                sb.AppendLine("<rect x=\"" + F(panelX) + "\" y=\"" + F(panelY) + "\" width=\"" + F(panelW) + "\" height=\"" + F(panelH) + "\" rx=\"14\" fill=\"#fff\" stroke=\"#d8dee5\"/>");
                sb.AppendLine("<text class=\"state\" text-anchor=\"middle\" x=\"" + F(centerX) + "\" y=\"" + F(panelY + 30) + "\">" + Xml(state.Label) + "</text>");
                foreach (var part in parts.OrderBy(item => item.Zmm))
                {
                    var rect = ProjectWithMotion(part, "front", state.Horizontal, state.Vertical);
                    var x = heightRange
                        ? centerX + (rect.X0 - (minX + maxX) / 2.0) * scale
                        : centerX - (rect.X1 - (minX + maxX) / 2.0) * scale;
                    var y = baseY - (rect.Y1 - minY) * scale;
                    var w = Math.Max(1, (rect.X1 - rect.X0) * scale);
                    var h = Math.Max(1, (rect.Y1 - rect.Y0) * scale);
                    var css = string.Equals(part.Kind, "profile", StringComparison.OrdinalIgnoreCase) ? "profile" : (string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase) ? "sheet" : "hardware");
                    sb.AppendLine("<rect class=\"part " + css + "\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\"/>");
                }
                if (!heightRange)
                {
                    var worldCenter = (minX + maxX) / 2.0;
                    var topRect = ProjectWithMotion(worktop, "front", state.Horizontal, state.Vertical);
                    var topLeftX = centerX - (topRect.X1 - worldCenter) * scale;
                    var topRightX = centerX - (topRect.X0 - worldCenter) * scale;
                    var supportLeftX = centerX - (fixedSupportMax - worldCenter) * scale;
                    var supportRightX = centerX - (fixedSupportMin - worldCenter) * scale;
                    var topBottomY = baseY - (topRect.Y0 - minY) * scale;
                    var dimensionY = baseY + 10;
                    var leftOverhang = centeredOverhang - state.Horizontal;
                    var rightOverhang = centeredOverhang + state.Horizontal;
                    sb.AppendLine("<line class=\"dimensionExtension\" x1=\"" + F(topLeftX) + "\" y1=\"" + F(topBottomY) + "\" x2=\"" + F(topLeftX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<line class=\"dimensionExtension\" x1=\"" + F(topRightX) + "\" y1=\"" + F(topBottomY) + "\" x2=\"" + F(topRightX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<line class=\"dimensionExtension\" x1=\"" + F(supportLeftX) + "\" y1=\"" + F(baseY - 16) + "\" x2=\"" + F(supportLeftX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<line class=\"dimensionExtension\" x1=\"" + F(supportRightX) + "\" y1=\"" + F(baseY - 16) + "\" x2=\"" + F(supportRightX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<line class=\"dimension\" x1=\"" + F(topLeftX) + "\" y1=\"" + F(dimensionY) + "\" x2=\"" + F(supportLeftX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<line class=\"dimension\" x1=\"" + F(supportRightX) + "\" y1=\"" + F(dimensionY) + "\" x2=\"" + F(topRightX) + "\" y2=\"" + F(dimensionY) + "\"/>");
                    sb.AppendLine("<text class=\"dimensionValue\" text-anchor=\"middle\" x=\"" + F((topLeftX + supportLeftX) / 2.0) + "\" y=\"" + F(dimensionY - 5) + "\">" + Xml(F(leftOverhang) + " mm") + "</text>");
                    sb.AppendLine("<text class=\"dimensionValue\" text-anchor=\"middle\" x=\"" + F((supportRightX + topRightX) / 2.0) + "\" y=\"" + F(dimensionY - 5) + "\">" + Xml(F(rightOverhang) + " mm") + "</text>");
                }
                var value = heightRange
                    ? motion.Vertical.ReferenceValueMm + state.Vertical
                    : state.Horizontal;
                var valueText = heightRange
                    ? F(value) + " mm werkhoogte"
                    : (Math.Abs(value) < 0.001 ? "0 mm" : (value > 0 ? "+" : "−") + F(Math.Abs(value)) + " mm");
                sb.AppendLine("<text class=\"value\" text-anchor=\"middle\" x=\"" + F(centerX) + "\" y=\"" + F(panelY + panelH + 30) + "\">" + Xml(valueText) + "</text>");
            }
            var footer = heightRange
                ? "Totale hoogteverstelling " + F(motion.Vertical.Maximum - motion.Vertical.Minimum) + " mm"
                : "Totale slag " + F(motion.Horizontal.Maximum - motion.Horizontal.Minimum) + " mm · maximale oversteek t.o.v. buitenzijde poten " + F(motion.MaximumOverhangMm) + " mm";
            sb.AppendLine("<text class=\"note\" x=\"40\" y=\"" + F(canvasH - 28) + "\">" + Xml(footer) + "</text></svg>");
            return sb.ToString();
        }

        private static ProjectedRect ProjectWithMotion(PortalAssemblyPart part, string mode, double horizontal, double vertical)
        {
            var x = part.Xmm + horizontal * part.MotionTranslateXPerMm;
            var y = part.Ymm + vertical * part.MotionTranslateYPerMm;
            var sizeY = part.SizeYmm + vertical * part.MotionSizeYPerMm;
            if (mode == "side") return new ProjectedRect(part.Zmm - part.SizeZmm / 2.0, y - sizeY / 2.0, part.Zmm + part.SizeZmm / 2.0, y + sizeY / 2.0);
            if (mode == "top") return new ProjectedRect(x - part.SizeXmm / 2.0, part.Zmm - part.SizeZmm / 2.0, x + part.SizeXmm / 2.0, part.Zmm + part.SizeZmm / 2.0);
            return new ProjectedRect(x - part.SizeXmm / 2.0, y - sizeY / 2.0, x + part.SizeXmm / 2.0, y + sizeY / 2.0);
        }

        private static string BuildAssemblyViewSvg(List<PortalAssemblyPart> parts, string mode, string title)
        {
            var bounds = Bounds.FromParts(parts);
            var horizontal = mode == "side" ? new Axis(bounds.MinZ, bounds.MaxZ) : new Axis(bounds.MinX, bounds.MaxX);
            var vertical = mode == "top" ? new Axis(bounds.MinZ, bounds.MaxZ) : new Axis(bounds.MinY, bounds.MaxY);
            if (mode == "side") vertical = new Axis(bounds.MinY, bounds.MaxY);

            var canvasW = 1000.0;
            var canvasH = 680.0;
            var margin = 70.0;
            var scaleX = (canvasW - 2 * margin) / Math.Max(1, horizontal.Max - horizontal.Min);
            var scaleY = (canvasH - 2 * margin) / Math.Max(1, vertical.Max - vertical.Min);
            var scale = Math.Min(scaleX, scaleY);
            if (double.IsInfinity(scale) || double.IsNaN(scale) || scale <= 0) scale = 1;

            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1000\" height=\"680\" viewBox=\"0 0 1000 680\">");
            sb.AppendLine("<style>text{font-family:Arial,sans-serif}.title{font-size:24px;font-weight:700;fill:#111827}.part{stroke:#667085;stroke-width:1.2}.sheet{fill:#eadcc7}.profile{fill:#cfd6df}.hole{fill:#334155}.pocket{fill:rgba(120,78,28,.2);stroke:#8a5a20;stroke-dasharray:5 4}.label{font-size:11px;fill:#344054}</style>");
            sb.AppendLine("<rect x=\"0\" y=\"0\" width=\"1000\" height=\"680\" fill=\"#f8fafc\"/>");
            sb.AppendLine("<text class=\"title\" x=\"36\" y=\"42\">" + Xml(title) + "</text>");

            foreach (var part in parts)
            {
                var rect = Project(part, mode);
                var x = margin + (rect.X0 - horizontal.Min) * scale;
                var y = canvasH - margin - (rect.Y1 - vertical.Min) * scale;
                var w = Math.Max(1, (rect.X1 - rect.X0) * scale);
                var h = Math.Max(1, (rect.Y1 - rect.Y0) * scale);
                sb.AppendLine("<rect class=\"part " + (part.Kind == "profile" ? "profile" : "sheet") + "\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\"/>");
                if (w > 38 && h > 18)
                {
                    sb.AppendLine("<text class=\"label\" x=\"" + F(x + 5) + "\" y=\"" + F(y + 14) + "\">" + Xml(part.Name) + "</text>");
                }
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static ProjectedRect Project(PortalAssemblyPart part, string mode)
        {
            if (mode == "side")
            {
                return new ProjectedRect(part.Zmm - part.SizeZmm / 2.0, part.Ymm - part.SizeYmm / 2.0, part.Zmm + part.SizeZmm / 2.0, part.Ymm + part.SizeYmm / 2.0);
            }

            if (mode == "top")
            {
                return new ProjectedRect(part.Xmm - part.SizeXmm / 2.0, part.Zmm - part.SizeZmm / 2.0, part.Xmm + part.SizeXmm / 2.0, part.Zmm + part.SizeZmm / 2.0);
            }

            return new ProjectedRect(part.Xmm - part.SizeXmm / 2.0, part.Ymm - part.SizeYmm / 2.0, part.Xmm + part.SizeXmm / 2.0, part.Ymm + part.SizeYmm / 2.0);
        }

        private static string BuildAssembly3DHtml(List<PortalAssemblyPart> parts, PortalMotionContract motion, JavaScriptSerializer serializer)
        {
            var json = SafeScriptJson(serializer.Serialize(parts));
            var motionJson = SafeScriptJson(serializer.Serialize(motion));
            var presentationJson = SafeScriptJson(serializer.Serialize(PortalPresentationContract.LoadRequired()));
            var sb = new StringBuilder(PortalAssemblyViewerSource.RendererJavaScript.Length + PortalAssemblyViewerSource.ThreeModuleDataUrl.Length + json.Length + presentationJson.Length + 16000);
            sb.AppendLine("<!doctype html><html lang=\"nl\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>3D klantmodel</title>");
            sb.AppendLine("<style>:root{font-family:Inter,'Segoe UI',Arial,sans-serif;color:#17202a;background:#eef1f4}*{box-sizing:border-box}html,body{width:100%;height:100%;margin:0;overflow:hidden}body{display:grid;grid-template-rows:auto 1fr;background:radial-gradient(circle at 48% 16%,#fff 0,#f4f6f8 52%,#e7ebef 100%)}header{display:grid;gap:10px;padding:12px 18px;border-bottom:1px solid #d9dee4;background:rgba(255,255,255,.94);box-shadow:0 5px 22px rgba(20,24,33,.05);z-index:2}.top,.controls,.group{display:flex;align-items:center;gap:9px;flex-wrap:wrap}.top{justify-content:space-between}.title{font-size:17px;font-weight:760}.hint{font-size:12px;color:#697581}.controls{justify-content:space-between}.group{padding:6px 9px;border-radius:11px;background:#f2f4f7}.group strong{font-size:11px;color:#667085}button{appearance:none;border:1px solid #d0d5dd;background:#fff;color:#344054;border-radius:8px;padding:7px 10px;font:650 12px 'Segoe UI',Arial;cursor:pointer}button.active{border-color:#1f4b73;background:#1f4b73;color:#fff}label{display:flex;align-items:center;gap:8px;font-size:12px;font-weight:700;color:#344054}input[type=range]{width:clamp(120px,16vw,230px);padding:0;accent-color:#0071e3}.value{min-width:92px;color:#1f4b73;font-weight:760}main{position:relative;min-height:0}canvas{display:block;width:100%;height:100%;cursor:grab;touch-action:none}canvas:active{cursor:grabbing}.status{position:absolute;left:50%;bottom:16px;transform:translateX(-50%);padding:7px 12px;border-radius:999px;background:rgba(31,41,51,.8);color:#fff;font-size:11px;pointer-events:none}.error{display:none;position:absolute;inset:24px;place-items:center;text-align:center;background:#fff;border:1px solid #ebc6c1;border-radius:14px;color:#8a1f17}.hasError .error{display:grid}.hasError canvas{visibility:hidden}@media(max-width:720px){header{padding:9px}.hint{display:none}.controls{align-items:stretch}.group{width:100%}label{width:100%}input[type=range]{flex:1}}</style></head><body>");
            sb.AppendLine("<header><div class=\"top\"><div class=\"title\">3D klantmodel</div><div class=\"hint\">Dezelfde assembly en renderer als de configurator · sleep om te draaien · scrol om te zoomen</div></div><div class=\"controls\"><div class=\"group\"><strong>Aanzicht</strong><button type=\"button\" data-view=\"iso\" class=\"active\">Iso</button><button type=\"button\" data-view=\"front\">Voor</button><button type=\"button\" data-view=\"side\">Zij</button><button type=\"button\" data-view=\"underside\">Onderzijde</button></div><div class=\"group\"><strong>Kleur</strong><button type=\"button\" data-color=\"realistic\" class=\"active\">Echte kleuren</button><button type=\"button\" data-color=\"technical\">Constructiekleuren</button></div><div class=\"group\" id=\"horizontalControl\"><label>Bladpositie <input id=\"horizontal\" type=\"range\"><span id=\"horizontalValue\" class=\"value\"></span></label></div><div class=\"group\" id=\"verticalControl\"><label>Werkhoogte <input id=\"vertical\" type=\"range\"><span id=\"verticalValue\" class=\"value\"></span></label></div><div class=\"group\"><label>Zoom <input id=\"zoom\" type=\"range\" min=\"55\" max=\"190\" value=\"100\"></label></div></div></header>");
            sb.AppendLine("<main id=\"stage\"><canvas id=\"assemblyCanvas\"></canvas><div class=\"status\">Interactief offline klantmodel</div><div class=\"error\"><div><strong>Het 3D-model kon niet worden geopend.</strong><br><br>Open dit bestand in een actuele versie van Edge of Chrome.</div></div></main>");
            sb.AppendLine("<script type=\"module\">");
            sb.Append("const baseParts=").Append(json).AppendLine(";");
            sb.Append("const motion=").Append(motionJson).AppendLine(";");
            sb.Append("const presentationData=").Append(presentationJson).AppendLine(";");
            sb.Append("const THREE=await import('").Append(PortalAssemblyViewerSource.ThreeModuleDataUrl).AppendLine("');");
            sb.AppendLine("let ghostLexTop=false,assemblyColorMode='realistic',viewMode='iso',rotationDeg=215,motionHorizontal=0,motionVertical=0,fitZoom=1,dragging=false,lastX=0;");
            sb.AppendLine(PortalAssemblyViewerSource.RendererJavaScript);
            sb.AppendLine("const stage=document.getElementById('stage'),canvas=document.getElementById('assemblyCanvas'),horizontal=document.getElementById('horizontal'),vertical=document.getElementById('vertical'),zoom=document.getElementById('zoom');");
            sb.AppendLine("const renderer=new THREE.WebGLRenderer({canvas,antialias:true,alpha:true});renderer.setPixelRatio(Math.min(devicePixelRatio||1,2));renderer.setClearColor(0xf5f7fa,1);renderer.outputColorSpace=THREE.SRGBColorSpace;const scene=new THREE.Scene(),camera=new THREE.OrthographicCamera(-1,1,1,-1,.1,10000),group=new THREE.Group();scene.add(group);scene.add(new THREE.HemisphereLight(0xffffff,0x7d8996,2.35));const keyLight=new THREE.DirectionalLight(0xffffff,3.1);keyLight.position.set(3,5,4);scene.add(keyLight);");
            sb.AppendLine("function setupMotion(input,axis,host){if(!axis){host.style.display='none';return}input.min=axis.Minimum;input.max=axis.Maximum;input.step=axis.Step;input.value=axis.DefaultValue;input.addEventListener('input',()=>{updateMotionLabels();rebuild(false)})}setupMotion(horizontal,motion&&motion.Horizontal,document.getElementById('horizontalControl'));setupMotion(vertical,motion&&motion.Vertical,document.getElementById('verticalControl'));");
            sb.AppendLine("function adjustedParts(){if(!motion)return baseParts;const h=Number(horizontal.value),v=Number(vertical.value);return baseParts.map(source=>{const p=JSON.parse(JSON.stringify(source)),dx=h*Number(p.MotionTranslateXPerMm||0),dy=v*Number(p.MotionTranslateYPerMm||0),dsy=v*Number(p.MotionSizeYPerMm||0);p.Xmm+=dx;p.Ymm+=dy;p.SizeYmm+=dsy;(p.Holes||[]).forEach(x=>{x.Xmm+=dx;x.Ymm+=dy});(p.Pockets||[]).forEach(x=>{x.Xmm+=dx;x.Ymm+=dy});(p.CoreHoles||[]).forEach(x=>{if(x.Xmm!=null)x.Xmm+=dx;if(x.Ymm!=null)x.Ymm+=dy});return p})}");
            sb.AppendLine("function updateMotionLabels(){if(!motion)return;const h=Number(horizontal.value),v=Number(vertical.value),direction=h<0?'links':h>0?'rechts':'midden';document.getElementById('horizontalValue').textContent=direction+' · '+Math.abs(Math.round(h))+' '+motion.Horizontal.Unit;document.getElementById('verticalValue').textContent=Math.round(motion.Vertical.ReferenceValueMm+v)+' '+motion.Vertical.Unit}");
            sb.AppendLine("function bounds(){group.updateMatrixWorld(true);const box=new THREE.Box3();group.children.filter(x=>!x.userData.excludeFromFit).forEach(x=>box.expandByObject(x));return box}");
            sb.AppendLine("function fit(){const box=bounds();if(box.isEmpty())return;const size=box.getSize(new THREE.Vector3()),center=box.getCenter(new THREE.Vector3()),span=Math.max(size.x,size.y,size.z,1),w=Math.max(320,stage.clientWidth),h=Math.max(260,stage.clientHeight);camera.left=-w/2;camera.right=w/2;camera.top=h/2;camera.bottom=-h/2;camera.up.set(0,1,0);if(viewMode==='front'||viewMode==='side')camera.position.set(center.x,center.y,center.z+span*2);else if(viewMode==='underside')camera.position.set(center.x+span*.82,center.y-span*.38,center.z+span);else camera.position.set(center.x+span*.88,center.y+span*.44,center.z+span);camera.lookAt(center);const diagonal=viewMode==='iso'||viewMode==='underside',wf=diagonal?.70:.84,hf=diagonal?.74:.82;fitZoom=Math.min(3,Math.max(.06,Math.min(w*wf/Math.max(size.x,size.z,1),h*hf/Math.max(size.y,1))));camera.zoom=fitZoom*Number(zoom.value)/100;camera.updateProjectionMatrix();render()}");
            sb.AppendLine("function render(){group.rotation.y=rotationDeg*Math.PI/180;renderer.render(scene,camera)}function rebuild(refit){group.clear();buildThreeParts(THREE,group,adjustedParts());if(refit)fit();else render()}");
            sb.AppendLine("function resize(){const w=Math.max(320,stage.clientWidth),h=Math.max(260,stage.clientHeight);renderer.setSize(w,h,false);fit()}addEventListener('resize',resize);zoom.addEventListener('input',()=>{camera.zoom=fitZoom*Number(zoom.value)/100;camera.updateProjectionMatrix();render()});");
            sb.AppendLine("document.querySelectorAll('[data-view]').forEach(button=>button.addEventListener('click',()=>{viewMode=button.dataset.view;rotationDeg=viewMode==='side'?90:viewMode==='front'?180:215;document.querySelectorAll('[data-view]').forEach(x=>x.classList.toggle('active',x===button));rebuild(true)}));document.querySelectorAll('[data-color]').forEach(button=>button.addEventListener('click',()=>{assemblyColorMode=button.dataset.color;document.querySelectorAll('[data-color]').forEach(x=>x.classList.toggle('active',x===button));rebuild(false)}));");
            sb.AppendLine("canvas.addEventListener('pointerdown',event=>{dragging=true;lastX=event.clientX;canvas.setPointerCapture(event.pointerId)});canvas.addEventListener('pointermove',event=>{if(!dragging)return;rotationDeg=(rotationDeg+(event.clientX-lastX)*.45+360)%360;lastX=event.clientX;render()});canvas.addEventListener('pointerup',()=>dragging=false);canvas.addEventListener('pointercancel',()=>dragging=false);canvas.addEventListener('wheel',event=>{event.preventDefault();zoom.value=Math.max(Number(zoom.min),Math.min(Number(zoom.max),Number(zoom.value)+(event.deltaY<0?8:-8)));zoom.dispatchEvent(new Event('input'))},{passive:false});");
            sb.AppendLine("try{updateMotionLabels();resize();rebuild(true)}catch(error){console.error(error);stage.classList.add('hasError')}</script></body></html>");
            return sb.ToString();
        }

        private static string SafeScriptJson(string value)
        {
            return (value ?? "null").Replace("</", "<\\/");
        }

        private static string BuildLegacyAssembly3DHtml(List<PortalAssemblyPart> parts, PortalMotionContract motion, JavaScriptSerializer serializer)
        {
            var json = serializer.Serialize(parts);
            var motionJson = serializer.Serialize(motion);
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>3D model</title>");
            sb.AppendLine("<style>body{margin:0;font-family:Arial,sans-serif;background:#f4f6f8;color:#111827}.bar{display:flex;gap:18px;align-items:center;padding:12px 18px;background:#fff;border-bottom:1px solid #d0d5dd;flex-wrap:wrap}canvas{display:block;width:100vw;height:calc(100vh - 76px)}label{font-size:13px;font-weight:700;display:flex;align-items:center;gap:8px}input{width:180px}.value{min-width:74px;color:#1f4b73}</style></head><body>");
            sb.AppendLine("<div class=\"bar\"><strong>3D klantmodel</strong><label>Rotatie <input id=\"rot\" type=\"range\" min=\"-180\" max=\"180\" value=\"35\"></label><label>Zoom <input id=\"zoom\" type=\"range\" min=\"30\" max=\"160\" value=\"80\"></label><label id=\"horizontalControl\">Blad <input id=\"horizontal\" type=\"range\"><span id=\"horizontalValue\" class=\"value\"></span></label><label id=\"verticalControl\">Hoogte <input id=\"vertical\" type=\"range\"><span id=\"verticalValue\" class=\"value\"></span></label></div><canvas id=\"c\"></canvas>");
            sb.AppendLine("<script>const parts=");
            sb.AppendLine(json);
            sb.AppendLine(";const motion=" + motionJson + ",c=document.getElementById('c'),ctx=c.getContext('2d'),rot=document.getElementById('rot'),zoom=document.getElementById('zoom'),horizontal=document.getElementById('horizontal'),vertical=document.getElementById('vertical');function setup(input,axis,host){if(!axis){host.style.display='none';return}input.min=axis.Minimum;input.max=axis.Maximum;input.step=axis.Step;input.value=axis.DefaultValue;input.oninput=draw}setup(horizontal,motion&&motion.Horizontal,document.getElementById('horizontalControl'));setup(vertical,motion&&motion.Vertical,document.getElementById('verticalControl'));function resize(){c.width=innerWidth*devicePixelRatio;c.height=(innerHeight-76)*devicePixelRatio;draw()}addEventListener('resize',resize);rot.oninput=draw;zoom.oninput=draw;function p3(x,y,z,a,s){const ca=Math.cos(a),sa=Math.sin(a);const xr=x*ca-z*sa,zr=x*sa+z*ca;return [c.width/2+xr*s,(c.height*0.62)-y*s+zr*s*.35]}function adjusted(source){const p={...source},h=motion?Number(horizontal.value):0,v=motion?Number(vertical.value):0;p.Xmm+=h*(p.MotionTranslateXPerMm||0);p.Ymm+=v*(p.MotionTranslateYPerMm||0);p.SizeYmm+=v*(p.MotionSizeYPerMm||0);return p}function box(part,a,s){const x=part.Xmm,y=part.Ymm,z=part.Zmm,sx=part.SizeXmm/2,sy=part.SizeYmm/2,sz=part.SizeZmm/2;const pts=[p3(x-sx,y-sy,z-sz,a,s),p3(x+sx,y-sy,z-sz,a,s),p3(x+sx,y+sy,z-sz,a,s),p3(x-sx,y+sy,z-sz,a,s),p3(x-sx,y-sy,z+sz,a,s),p3(x+sx,y-sy,z+sz,a,s),p3(x+sx,y+sy,z+sz,a,s),p3(x-sx,y+sy,z+sz,a,s)];const faces=[[0,1,2,3],[4,5,6,7],[0,4,7,3],[1,5,6,2],[3,2,6,7],[0,1,5,4]];ctx.strokeStyle='#64748b';ctx.lineWidth=1.2*devicePixelRatio;ctx.fillStyle=part.Kind==='profile'?'rgba(174,184,196,.72)':'rgba(228,205,170,.72)';for(const f of faces){ctx.beginPath();ctx.moveTo(...pts[f[0]]);for(let i=1;i<f.length;i++)ctx.lineTo(...pts[f[i]]);ctx.closePath();ctx.fill();ctx.stroke()}}function draw(){ctx.clearRect(0,0,c.width,c.height);ctx.fillStyle='#f8fafc';ctx.fillRect(0,0,c.width,c.height);const a=Number(rot.value)*Math.PI/180,s=Number(zoom.value)/100*devicePixelRatio*.55,current=parts.map(adjusted);current.sort((a,b)=>(a.Zmm+a.Xmm)-(b.Zmm+b.Xmm)).forEach(part=>box(part,a,s));if(motion){const h=Number(horizontal.value),v=Number(vertical.value);document.getElementById('horizontalValue').textContent=(h<0?'links ':h>0?'rechts ':'midden ')+Math.abs(Math.round(h))+' mm';document.getElementById('verticalValue').textContent=Math.round(motion.Vertical.ReferenceValueMm+v)+' mm'}}resize();</script></body></html>");
            return sb.ToString();
        }

        private sealed class MotionState
        {
            public MotionState(string label, double horizontal, double vertical)
            {
                Label = label;
                Horizontal = horizontal;
                Vertical = vertical;
            }

            public string Label { get; private set; }
            public double Horizontal { get; private set; }
            public double Vertical { get; private set; }
        }

        private static void EnsureProductRelease(PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "lineaire_robotcel", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Productie-export geblokkeerd: FAIRINO FR5 en HIWIN HGR20/HGH20CA zijn vastgelegd, maar de lineaire robotcel mist nog de volledige HIWIN-bestelcode en raillengte-uitvoering, motor/reductor/tandheugelkeuze, volledige adapterplaatberekening en bevestigers, vloerverankering en veiligheids-PL/SIL-validatie.");
        }

        private static void Write(ProductionOutput output, string folder, string relativeName, string contents)
        {
            var fileName = relativeName;
            if (relativeName.IndexOf("\\", StringComparison.Ordinal) >= 0)
            {
                fileName = relativeName.Substring(relativeName.LastIndexOf("\\", StringComparison.Ordinal) + 1);
            }

            File.WriteAllText(Path.Combine(folder, fileName), contents);
            output.Files.Add(relativeName);
        }

        private static string FormatPlan(SolidWorksExportPlan plan)
        {
            var text = "Assembly: " + plan.AssemblyName + Environment.NewLine + "Parts:" + Environment.NewLine;
            foreach (var part in plan.PartNames)
            {
                text += "- " + part + Environment.NewLine;
            }

            return text;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private struct Axis
        {
            public readonly double Min;
            public readonly double Max;

            public Axis(double min, double max)
            {
                Min = min;
                Max = max;
            }
        }

        private struct ProjectedRect
        {
            public readonly double X0;
            public readonly double Y0;
            public readonly double X1;
            public readonly double Y1;

            public ProjectedRect(double x0, double y0, double x1, double y1)
            {
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }
        }

        private sealed class Bounds
        {
            public double MinX { get; private set; }
            public double MaxX { get; private set; }
            public double MinY { get; private set; }
            public double MaxY { get; private set; }
            public double MinZ { get; private set; }
            public double MaxZ { get; private set; }

            public static Bounds FromParts(List<PortalAssemblyPart> parts)
            {
                var bounds = new Bounds
                {
                    MinX = 0,
                    MaxX = 1,
                    MinY = 0,
                    MaxY = 1,
                    MinZ = 0,
                    MaxZ = 1
                };

                if (parts == null || parts.Count == 0) return bounds;

                bounds.MinX = double.MaxValue;
                bounds.MinY = double.MaxValue;
                bounds.MinZ = double.MaxValue;
                bounds.MaxX = double.MinValue;
                bounds.MaxY = double.MinValue;
                bounds.MaxZ = double.MinValue;

                foreach (var part in parts)
                {
                    bounds.MinX = Math.Min(bounds.MinX, part.Xmm - part.SizeXmm / 2.0);
                    bounds.MaxX = Math.Max(bounds.MaxX, part.Xmm + part.SizeXmm / 2.0);
                    bounds.MinY = Math.Min(bounds.MinY, part.Ymm - part.SizeYmm / 2.0);
                    bounds.MaxY = Math.Max(bounds.MaxY, part.Ymm + part.SizeYmm / 2.0);
                    bounds.MinZ = Math.Min(bounds.MinZ, part.Zmm - part.SizeZmm / 2.0);
                    bounds.MaxZ = Math.Max(bounds.MaxZ, part.Zmm + part.SizeZmm / 2.0);
                }

                return bounds;
            }
        }

        public static string SafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "bestand";
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }
    }
}
