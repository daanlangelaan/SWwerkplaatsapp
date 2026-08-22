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
            var settings = AppSettings.Load();
            var nestingPlan = BuildNestingPlan(model, machine, settings, request);
            var nestingSvg = new NestingExporter().ExportSvg(nestingPlan, contourTool);
            return new ProductionOutput { Model = model, NestingPlan = nestingPlan, NestingSvg = nestingSvg };
        }

        public WorkbenchCabinetAuditResult AuditWorkbenchCabinet(PortalQuoteRequest request)
        {
            var preview = BuildPreview(request);
            return new WorkbenchCabinetAuditService().Audit(preview.Model, request, preview.NestingPlan);
        }

        public ProductionOutput GenerateOrderFiles(PortalQuoteRequest request, string outputFolder)
        {
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
            if (request.RevisionAfterMilledTestSheetOne)
            {
                var fullModelPlan = BuildNestingPlan(model, machine, settings, null);
                fullModelAudit = new WorkbenchCabinetAuditService().Audit(model, request, fullModelPlan);
                if (!fullModelAudit.Passed)
                    throw new InvalidOperationException("Revisie-export afgebroken: het volledige meubelmodel bevat " + fullModelAudit.Errors.Count.ToString() + " fout(en): " + string.Join(" | ", fullModelAudit.Errors));
                ApplyRevisionAfterMilledTestSheetOne(model, request);
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
                Write(output, outputFolder, "Afkortlijst.csv", csv.ExportCutList(model.Profiles));
                Write(output, outputFolder, "Boorlijst.csv", csv.ExportDrillList(model.Profiles));
                Write(output, outputFolder, "Profielbewerkingen.csv", csv.ExportProfileOperations(model.ProfileOperations));
                new ProfileOperationsXlsxExporter().Export(Path.Combine(outputFolder, "Profielbewerkingen.xlsx"), model.ProfileOperations);
                output.Files.Add("Profielbewerkingen.xlsx");
                Write(output, outputFolder, "ProfielStationPlan.txt", csv.ExportProfileStationPlan(model));
            }

            if (HasSheets(model))
            {
                Write(output, outputFolder, "Plaatgaten.csv", csv.ExportSheetHoleList(model.Sheets));
                Write(output, outputFolder, "CAM-operaties.csv", csv.ExportCamOperations(model.Sheets, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, camJob.EdgeChamferWidthMm, camJob.ThroughCutOvertravelMm));
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
            new PriceOverviewXlsxExporter().Export(Path.Combine(outputFolder, "PrijsOverzicht.xlsx"), price);
            output.Files.Add("PrijsOverzicht.xlsx");
            Write(output, outputFolder, "Offerte.txt", pricing.ExportOfferText(request, price, "CONCEPT"));

            if (string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                var adapterFiles = new PlinthClipAdapterExportService().ExportOpenScad(factory.BuildWorkbenchCabinet(request), outputFolder);
                foreach (var adapterFile in adapterFiles) output.Files.Add(adapterFile);
            }

            if (HasSheets(model))
            {
                var gcode = new Mach3GCodeGenerator();
                foreach (var sheet in model.Sheets)
                {
                    Write(output, outputFolder, SafeFileName(sheet.Name) + ".tap", gcode.GenerateSheetPart(sheet, holeTool, contourTool, vBitTool, enableWoodScrewCountersinks, enableOutsideEdgeChamfer, camJob.EdgeChamferWidthMm, machine, sheet.Material.ThicknessMm, camJob.TabWidthMm, camJob.TabHeightMm, camJob.ThroughCutOvertravelMm, camJob.SafeTravelZMm, camJob.ContourOnionSkinMm, camJob.FinalContourFeedRateMmMin, camJob.FinalContourRampLengthMm));
                }

                var nestingFolderName = request.RevisionAfterMilledTestSheetOne ? "Revisie_vanaf_plaat_2" : (request.TestFitFirstSheet ? "Testplaat_nesting" : "Nesting");
                var nestingRelativePrefix = nestingFolderName + "\\";
                var nestingFolder = Path.Combine(outputFolder, nestingFolderName);
                Directory.CreateDirectory(nestingFolder);
                var nestingExporter = new NestingExporter();
                output.NestingSvg = nestingExporter.ExportSvg(nestingPlan, contourTool);
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
            var includeThreeDPrint = request.ExportIncludeThreeDPrint != false;
            var includeControls = request.ExportIncludeControls != false;
            if (!includeCam && !includeSolidWorks && !includeCustomerPackage && !includeThreeDPrint && !includeControls)
                throw new InvalidOperationException("Selecteer minimaal één onderdeel voor de projectexport.");

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
            string customerPowerPointPath = null;
            string customerAppendixPdfPath = null;
            string customerDrawingPath = null;
            string customerDrawingPdfPath = null;

            if (includeSolidWorks || includeCustomerPackage)
            {
                if (includeControls) macroPath = new SolidWorksMacroExporter().ExportMacro(model, outputFolder);
                assemblyPath = RunSolidWorksWorker(request, outputFolder);
                if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                    throw new InvalidOperationException("SolidWorks heeft geen assembly aangemaakt.");

                customerModelPath = SolidWorksCustomerPresentation.CustomerModelPath(assemblyPath);
                customerHtmlPath = SolidWorksCustomerPresentation.CustomerHtmlPath(assemblyPath);
                var drawingOutput = SolidWorksCustomerDrawingExporter.OutputFor(assemblyPath);
                customerDrawingPath = drawingOutput.DrawingPath;

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
            if (includeCam || includeCustomerPackage || includeThreeDPrint || includeControls)
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

            if (includeCustomerPackage)
            {
                Directory.CreateDirectory(customerFolder);
                MoveDirectoryIfPresent(Path.Combine(camFolder, "Aanzichten"), Path.Combine(customerFolder, "Aanzichten"));
                var sourceMaterialFolder = Path.Combine(cadFolder, "Materialen");
                var customerMaterialFolder = Path.Combine(customerFolder, "Render-assets", "Materialen");
                MoveDirectoryIfPresent(sourceMaterialFolder, customerMaterialFolder);
                RelinkRenderMaterialAssets(customerMaterialFolder, sourceMaterialFolder, customerMaterialFolder);
                customerModelPath = MoveFileToFolder(customerModelPath, customerFolder);
                customerHtmlPath = MoveFileToFolder(customerHtmlPath, customerFolder);
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
                DeleteDirectoryIfPresent(Path.Combine(camFolder, "Aanzichten"));
                DeleteDirectoryIfPresent(Path.Combine(cadFolder, "Materialen"));
                DeleteFileIfPresent(customerModelPath);
                DeleteFileIfPresent(customerHtmlPath);
                if (!string.IsNullOrWhiteSpace(assemblyPath))
                {
                    var drawingOutput = SolidWorksCustomerDrawingExporter.OutputFor(assemblyPath);
                    DeleteFileIfPresent(drawingOutput.PdfPath);
                    DeleteFileIfPresent(drawingOutput.GeneralSheetImagePath);
                }
                customerModelPath = null;
                customerHtmlPath = null;
            }

            if (includeControls)
            {
                Directory.CreateDirectory(projectDataFolder);
                WriteProjectConfiguration(request, Path.Combine(projectDataFolder, "Configuratie.json"));
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

            if (!includeCam) DeleteDirectoryIfPresent(camFolder);
            if (!includeSolidWorks)
            {
                DeleteDirectoryIfPresent(cadFolder);
                assemblyPath = null;
                customerDrawingPath = null;
            }
            else
            {
                EnsureSolidWorksFolderContainsOnlyNativeDocuments(cadFolder);
            }

            var partCount = includeSolidWorks && Directory.Exists(cadFolder)
                ? Directory.GetFiles(cadFolder, "*.SLDPRT").Length
                : 0;

            File.WriteAllText(
                Path.Combine(outputFolder, "ProjectOutputOverzicht.txt"),
                "PROJECTOUTPUT" + Environment.NewLine
                + "Gegenereerd: " + generatedAt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine
                + "Project: " + projectLabel + Environment.NewLine
                + "Configuratie: " + model.ProjectName + Environment.NewLine
                + Environment.NewLine
                + "SELECTIE" + Environment.NewLine
                + OutputOverviewLine("01_CAM", includeCam, camFolder)
                + OutputOverviewLine("02_SolidWorks", includeSolidWorks, cadFolder)
                + OutputOverviewLine("03_Klantvoorstel", includeCustomerPackage, customerFolder)
                + OutputOverviewLine("04_3D-print", includeThreeDPrint, printFolder)
                + OutputOverviewLine("05_Projectdata", includeControls, projectDataFolder));

            var fileCount = Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories).Length;
            return new PortalSolidWorksExportResponse
            {
                Ok = true,
                Message = "Projectexport gegenereerd met: " + SelectedOutputNames(includeCam, includeSolidWorks, includeCustomerPackage, includeThreeDPrint, includeControls) + ".",
                OutputFolder = outputFolder,
                AssemblyPath = assemblyPath,
                ControlModelPath = assemblyPath,
                CustomerModelPath = customerModelPath,
                CustomerHtmlPath = customerHtmlPath,
                CustomerPowerPointPath = customerPowerPointPath,
                CustomerAppendixPdfPath = customerAppendixPdfPath,
                CustomerDrawingPath = customerDrawingPath,
                CustomerDrawingPdfPath = customerDrawingPdfPath,
                MacroPath = macroPath,
                PartCount = partCount,
                FileCount = fileCount,
                PlacementCount = model.AssemblyPlacements.Count
            };
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
                "PrijsOverzicht.xlsx",
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

        private static string SelectedOutputNames(bool cam, bool solidWorks, bool customerPackage, bool threeDPrint, bool controls)
        {
            var names = new List<string>();
            if (cam) names.Add("CAM");
            if (solidWorks) names.Add("SolidWorks");
            if (customerPackage) names.Add("klantvoorstel");
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
                if (result != null && result.Ok && !string.IsNullOrWhiteSpace(result.AssemblyPath) && File.Exists(result.AssemblyPath))
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

        private sealed class SolidWorksWorkerResult
        {
            public bool Ok { get; set; }
            public string AssemblyPath { get; set; }
            public string Error { get; set; }
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
                for (var index = 0; index < plan.StockSheets.Count; index++)
                {
                    var stock = plan.StockSheets[index];
                    stock.SheetNumber = index + 2;
                    stock.Name = (stock.Material == null ? "Plaat" : stock.Material.Name.Replace(" ", "_"))
                        + (index == 0 ? "_RevisiePlaat_02" : "_NestPlaat_" + (index + 2).ToString("00", CultureInfo.InvariantCulture));
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
                foreach (var name in revisionParts)
                {
                    if (model.Sheets.All(part => !string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("Revisieplaat 2 kan niet worden gemaakt: vereist onderdeel ontbreekt: " + name + ".");
                }
                return revisionParts;
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
            model.Sheets.RemoveAll(part => milledPartNames.Contains(part.Name));
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
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            Write(output, folder, "Aanzichten\\ProductPreview.svg", new PortalVisualizationService().BuildProductSvg(model, request));
            Write(output, folder, "Aanzichten\\Vooraanzicht.svg", BuildAssemblyViewSvg(parts, "front", "Vooraanzicht"));
            Write(output, folder, "Aanzichten\\Zijaanzicht.svg", BuildAssemblyViewSvg(parts, "side", "Zijaanzicht"));
            Write(output, folder, "Aanzichten\\Bovenaanzicht.svg", BuildAssemblyViewSvg(parts, "top", "Bovenaanzicht"));
            Write(output, folder, "Aanzichten\\3D-model.json", serializer.Serialize(parts));
            Write(output, folder, "Aanzichten\\3D-model.html", BuildAssembly3DHtml(parts, serializer));
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

        private static string BuildAssembly3DHtml(List<PortalAssemblyPart> parts, JavaScriptSerializer serializer)
        {
            var json = serializer.Serialize(parts);
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>3D model</title>");
            sb.AppendLine("<style>body{margin:0;font-family:Arial,sans-serif;background:#f4f6f8;color:#111827}.bar{display:flex;gap:18px;align-items:center;padding:14px 18px;background:#fff;border-bottom:1px solid #d0d5dd}canvas{display:block;width:100vw;height:calc(100vh - 58px)}label{font-size:13px;font-weight:700}input{width:220px}</style></head><body>");
            sb.AppendLine("<div class=\"bar\"><strong>3D model</strong><label>Rotatie <input id=\"rot\" type=\"range\" min=\"-180\" max=\"180\" value=\"35\"></label><label>Zoom <input id=\"zoom\" type=\"range\" min=\"30\" max=\"160\" value=\"80\"></label></div><canvas id=\"c\"></canvas>");
            sb.AppendLine("<script>const parts=");
            sb.AppendLine(json);
            sb.AppendLine(";const c=document.getElementById('c'),ctx=c.getContext('2d'),rot=document.getElementById('rot'),zoom=document.getElementById('zoom');function resize(){c.width=innerWidth*devicePixelRatio;c.height=(innerHeight-58)*devicePixelRatio;draw()}addEventListener('resize',resize);rot.oninput=draw;zoom.oninput=draw;function p3(x,y,z,a,s){const ca=Math.cos(a),sa=Math.sin(a);const xr=x*ca-z*sa,zr=x*sa+z*ca;return [c.width/2+xr*s,(c.height*0.58)-y*s+zr*s*.35]}function box(part,a,s){const x=part.Xmm,y=part.Ymm,z=part.Zmm,sx=part.SizeXmm/2,sy=part.SizeYmm/2,sz=part.SizeZmm/2;const pts=[p3(x-sx,y-sy,z-sz,a,s),p3(x+sx,y-sy,z-sz,a,s),p3(x+sx,y+sy,z-sz,a,s),p3(x-sx,y+sy,z-sz,a,s),p3(x-sx,y-sy,z+sz,a,s),p3(x+sx,y-sy,z+sz,a,s),p3(x+sx,y+sy,z+sz,a,s),p3(x-sx,y+sy,z+sz,a,s)];const faces=[[0,1,2,3],[4,5,6,7],[0,4,7,3],[1,5,6,2],[3,2,6,7],[0,1,5,4]];ctx.strokeStyle='#64748b';ctx.lineWidth=1.2*devicePixelRatio;ctx.fillStyle=part.Kind==='profile'?'rgba(174,184,196,.72)':'rgba(228,205,170,.72)';for(const f of faces){ctx.beginPath();ctx.moveTo(...pts[f[0]]);for(let i=1;i<f.length;i++)ctx.lineTo(...pts[f[i]]);ctx.closePath();ctx.fill();ctx.stroke()}}function draw(){ctx.clearRect(0,0,c.width,c.height);ctx.fillStyle='#f8fafc';ctx.fillRect(0,0,c.width,c.height);const a=Number(rot.value)*Math.PI/180,s=Number(zoom.value)/100*devicePixelRatio*.55;[...parts].sort((a,b)=>(a.Zmm+a.Xmm)-(b.Zmm+b.Xmm)).forEach(part=>box(part,a,s));}resize();</script></body></html>");
            return sb.ToString();
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
