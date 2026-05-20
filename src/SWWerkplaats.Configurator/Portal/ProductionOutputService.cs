using System;
using System.Collections.Generic;
using System.IO;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
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
            var model = BuildModel(factory, request);
            var nestingPlan = new SheetNestingEngine().Build(model, machine, 15, 15, machine.MaxXmm, machine.MaxYmm);
            var nestingSvg = new NestingExporter().ExportSvg(nestingPlan);
            return new ProductionOutput { Model = model, NestingPlan = nestingPlan, NestingSvg = nestingSvg };
        }

        public ProductionOutput GenerateOrderFiles(PortalQuoteRequest request, string outputFolder)
        {
            var factory = new PortalConfigurationFactory();
            var tool = factory.DefaultTool();
            var machine = factory.DefaultMachine();
            var model = BuildModel(factory, request);

            Directory.CreateDirectory(outputFolder);
            var output = new ProductionOutput { Model = model };
            var csv = new CsvExporter();
            Write(output, outputFolder, "Afkortlijst.csv", csv.ExportCutList(model.Profiles));
            Write(output, outputFolder, "Boorlijst.csv", csv.ExportDrillList(model.Profiles));
            Write(output, outputFolder, "Profielbewerkingen.csv", csv.ExportProfileOperations(model.ProfileOperations));
            new ProfileOperationsXlsxExporter().Export(Path.Combine(outputFolder, "Profielbewerkingen.xlsx"), model.ProfileOperations);
            output.Files.Add("Profielbewerkingen.xlsx");
            Write(output, outputFolder, "ProfielStationPlan.txt", csv.ExportProfileStationPlan(model));
            Write(output, outputFolder, "Plaatgaten.csv", csv.ExportSheetHoleList(model.Sheets));
            Write(output, outputFolder, "CAM-operaties.csv", csv.ExportCamOperations(model.Sheets, tool));
            Write(output, outputFolder, "ToolLibrary.csv", csv.ExportToolLibrary(tool));
            Write(output, outputFolder, "BOM.csv", csv.ExportBom(model));
            var pricing = new PortalPricingService();
            var price = pricing.Calculate(model);
            Write(output, outputFolder, "PrijsOverzicht.csv", pricing.ExportCsv(price));
            new PriceOverviewXlsxExporter().Export(Path.Combine(outputFolder, "PrijsOverzicht.xlsx"), price);
            output.Files.Add("PrijsOverzicht.xlsx");
            Write(output, outputFolder, "Offerte.txt", pricing.ExportOfferText(request, price, "CONCEPT"));

            var gcode = new Mach3GCodeGenerator();
            foreach (var sheet in model.Sheets)
            {
                Write(output, outputFolder, SafeFileName(sheet.Name) + ".tap", gcode.GenerateSheetPart(sheet, tool, machine, sheet.Material.ThicknessMm, 8, 1.5));
            }

            var nestingFolder = Path.Combine(outputFolder, "Nesting");
            Directory.CreateDirectory(nestingFolder);
            var nestingPlan = new SheetNestingEngine().Build(model, machine, 15, 15, machine.MaxXmm, machine.MaxYmm);
            output.NestingPlan = nestingPlan;
            var nestingExporter = new NestingExporter();
            output.NestingSvg = nestingExporter.ExportSvg(nestingPlan);
            Write(output, nestingFolder, "Nesting\\NestPlan.csv", nestingExporter.ExportCsv(nestingPlan));
            Write(output, nestingFolder, "Nesting\\NestVisualisatie.svg", output.NestingSvg);

            var nestedGcode = new NestedMach3GCodeGenerator();
            foreach (var stock in nestingPlan.StockSheets)
            {
                Write(output, nestingFolder, "Nesting\\" + SafeFileName(stock.Name) + ".tap", nestedGcode.Generate(stock, tool, machine));
            }

            Write(output, outputFolder, "SolidWorksExportPlan.txt", FormatPlan(SolidWorksExportPlan.FromWorkbench(model)));
            return output;
        }

        private static WorkbenchModel BuildModel(PortalConfigurationFactory factory, PortalQuoteRequest request)
        {
            if (IsCabinet(request))
            {
                return new CabinetEngine().Build(factory.BuildCabinet(request));
            }

            return new WorkbenchEngine().Build(factory.BuildWorkbench(request));
        }

        private static bool IsCabinet(PortalQuoteRequest request)
        {
            return request == null || string.IsNullOrWhiteSpace(request.Product) || request.Product.ToLowerInvariant() != "werktafel";
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
