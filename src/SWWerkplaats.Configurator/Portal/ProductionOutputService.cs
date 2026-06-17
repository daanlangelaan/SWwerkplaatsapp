using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var model = new ProductModelBuildService().Build(factory, request);
            var settings = AppSettings.Load();
            var nestingPlan = new SheetNestingEngine().Build(model, machine, settings.NestSpacingMm, settings.NestMarginMm, settings.NestStockLengthMm, settings.NestStockWidthMm);
            var nestingSvg = new NestingExporter().ExportSvg(nestingPlan);
            return new ProductionOutput { Model = model, NestingPlan = nestingPlan, NestingSvg = nestingSvg };
        }

        public ProductionOutput GenerateOrderFiles(PortalQuoteRequest request, string outputFolder)
        {
            var factory = new PortalConfigurationFactory();
            var tool = factory.DefaultTool();
            var camJob = CamJobOptions.FromPrimaryTool(tool);
            var machine = factory.DefaultMachine();
            var model = new ProductModelBuildService().Build(factory, request);
            var settings = AppSettings.Load();
            var nestingPlan = new SheetNestingEngine().Build(model, machine, settings.NestSpacingMm, settings.NestMarginMm, settings.NestStockLengthMm, settings.NestStockWidthMm);

            Directory.CreateDirectory(outputFolder);
            var output = new ProductionOutput { Model = model, NestingPlan = nestingPlan };
            var csv = new CsvExporter();
            var control = new OrderControlExportService();

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
                Write(output, outputFolder, "CAM-operaties.csv", csv.ExportCamOperations(model.Sheets, tool));
                Write(output, outputFolder, "ToolLibrary.csv", csv.ExportToolLibrary(camJob));
            }

            if (control.HasRailData(model))
            {
                Write(output, outputFolder, "RailgatenControle.csv", control.ExportRailHoleControl(model));
                Write(output, outputFolder, "RailTemplateControle.csv", control.ExportUsedRailTemplates(model));
            }

            Write(output, outputFolder, "AssemblageControle.txt", control.ExportAssemblyControl(model, request));
            Write(output, outputFolder, "AssemblageControle.csv", control.ExportAssemblyControlCsv(model, request));
            Write(output, outputFolder, "TekencontractControle.csv", control.ExportDrawingContractControl(model));
            Write(output, outputFolder, "TekencontractValidatie.csv", control.ExportDrawingContractValidation(model));
            Write(output, outputFolder, "BOM.csv", csv.ExportBom(model));
            var pricing = new PortalPricingService();
            var price = pricing.Calculate(model, nestingPlan);
            Write(output, outputFolder, "PrijsOverzicht.csv", pricing.ExportCsv(price));
            new PriceOverviewXlsxExporter().Export(Path.Combine(outputFolder, "PrijsOverzicht.xlsx"), price);
            output.Files.Add("PrijsOverzicht.xlsx");
            Write(output, outputFolder, "Offerte.txt", pricing.ExportOfferText(request, price, "CONCEPT"));

            if (HasSheets(model))
            {
                var gcode = new Mach3GCodeGenerator();
                foreach (var sheet in model.Sheets)
                {
                    Write(output, outputFolder, SafeFileName(sheet.Name) + ".tap", gcode.GenerateSheetPart(sheet, tool, machine, sheet.Material.ThicknessMm, 8, 1.5));
                }

                var nestingFolder = Path.Combine(outputFolder, "Nesting");
                Directory.CreateDirectory(nestingFolder);
                var nestingExporter = new NestingExporter();
                output.NestingSvg = nestingExporter.ExportSvg(nestingPlan);
                Write(output, nestingFolder, "Nesting\\NestPlan.csv", nestingExporter.ExportCsv(nestingPlan));
                Write(output, nestingFolder, "Nesting\\NestVisualisatie.svg", output.NestingSvg);

                var nestedGcode = new NestedMach3GCodeGenerator();
                var toolpathPreview = new ToolpathPreviewExporter();
                if (camJob.EnablePencilMarking)
                {
                    var pencilMarking = new PencilMarkingGCodeGenerator();
                    Write(output, nestingFolder, "Nesting\\PotloodMarkeerPlan.csv", pencilMarking.ExportPlan(nestingPlan, camJob.BuildPencilMarkingOptions()));
                }

                foreach (var stock in nestingPlan.StockSheets)
                {
                    Write(output, nestingFolder, "Nesting\\" + SafeFileName(stock.Name) + ".tap", nestedGcode.Generate(stock, tool, machine, camJob));
                    Write(output, nestingFolder, "Nesting\\ToolpathPreview_" + SafeFileName(stock.Name) + ".svg", toolpathPreview.ExportSvg(stock, tool));
                }
            }

            WriteVisualExports(output, outputFolder, model, request);
            Write(output, outputFolder, "SolidWorksExportPlan.txt", FormatPlan(SolidWorksExportPlan.FromWorkbench(model)));
            return output;
        }

        private static bool HasProfiles(WorkbenchModel model)
        {
            return model != null && ((model.Profiles != null && model.Profiles.Count > 0) || (model.ProfileOperations != null && model.ProfileOperations.Count > 0));
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
