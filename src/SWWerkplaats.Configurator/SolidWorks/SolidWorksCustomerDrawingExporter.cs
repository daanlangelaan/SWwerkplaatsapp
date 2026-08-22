using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksCustomerDrawingOutput
    {
        public string DrawingPath { get; set; }
        public string PdfPath { get; set; }
        public string GeneralSheetImagePath { get; set; }
        public string WorktopSheetImagePath { get; set; }
    }

    /// <summary>
    /// Maakt vanuit het geopende controlemodel een rustige A3-klanttekening.
    /// De maatlijnen zijn niet-associatieve SOLIDWORKS-tekeningmaten: zij tonen
    /// bewust alleen de geconfigureerde buitenmaten en maken het blad niet onnodig druk.
    /// </summary>
    internal static class SolidWorksCustomerDrawingExporter
    {
        private const int DefaultDrawingTemplatePreference = 10;
        private const int DocumentTemplateFolderPreference = 6;
        private const int A3PaperSize = 8;
        private const int A3Template = 8;
        private const int InsertSheetAfterSelected = 1;
        private const int RenameDuplicateViews = 1;
        private const int SaveAsCurrentVersion = 0;
        private const int SaveAsOptionsSilent = 1;
        private const int ExportPdfData = 1;
        private const int ExportAllSheets = 1;
        private const double A3Width = 0.420;
        private const double A3Height = 0.297;

        public static SolidWorksCustomerDrawingOutput Export(
            SldWorks solidWorks,
            ModelDoc2 sourceModel,
            string controlPath,
            PortalQuoteRequest request,
            WorkbenchModel workbenchModel)
        {
            if (solidWorks == null) throw new ArgumentNullException("solidWorks");
            if (sourceModel == null) throw new ArgumentNullException("sourceModel");
            if (string.IsNullOrWhiteSpace(controlPath) || !File.Exists(controlPath))
                throw new FileNotFoundException("Het SOLIDWORKS-controlemodel voor de werktekening ontbreekt.", controlPath);
            if (request == null) throw new ArgumentNullException("request");
            if (workbenchModel == null) throw new ArgumentNullException("workbenchModel");

            var output = OutputFor(controlPath);
            var template = ResolveDrawingTemplate(solidWorks);

            ModelDoc2 drawingModel = null;
            var drawingTitle = "";
            try
            {
                drawingModel = solidWorks.NewDocument(template.Path, A3PaperSize, A3Width, A3Height) as ModelDoc2;
                if (drawingModel == null)
                    throw new InvalidOperationException("SOLIDWORKS kon geen nieuwe A3-werktekening maken met template: " + template.Path);
                drawingTitle = drawingModel.GetTitle();
                var drawing = drawingModel as DrawingDoc;
                if (drawing == null)
                    throw new InvalidOperationException("Het nieuwe SOLIDWORKS-document is geen tekening.");

                var initialSheet = drawing.GetCurrentSheet() as Sheet;
                if (initialSheet == null) throw new InvalidOperationException("SOLIDWORKS kon het eerste tekenblad niet openen.");
                var initialName = initialSheet.GetName();
                // Gebruik bewust een leeg wit tekenblad. Een lokaal bedrijfssjabloon
                // kan ingevulde voorbeeldvelden bevatten die nooit in een klantstuk
                // terecht mogen komen.
                drawing.SetupSheet6(initialName, A3PaperSize, A3Template, 1.0, 10.0, false, "", A3Width, A3Height, "", false, 0, 0, 0, 0, 0, 0);
                initialSheet.SetName("KLANTTEKENING");

                initialName = "KLANTTEKENING";
                drawing.ActivateSheet(initialName);
                BuildGeneralSheet(drawingModel, drawing, controlPath, request, workbenchModel);

                drawingModel.EditRebuild3();
                SaveDrawing(drawingModel, output.DrawingPath);
                ExportPdf(solidWorks, drawingModel, output.PdfPath);
                ExportSheetImage(drawingModel, drawing, initialName, output.GeneralSheetImagePath);
                SaveDrawing(drawingModel, output.DrawingPath);
            }
            finally
            {
                if (drawingModel != null && !string.IsNullOrWhiteSpace(drawingTitle))
                    solidWorks.CloseDoc(drawingTitle);
            }

            EnsureOutput(output.DrawingPath, "SOLIDWORKS-werktekening");
            EnsureOutput(output.PdfPath, "PDF-werktekening");
            EnsureOutput(output.GeneralSheetImagePath, "afbeelding van het overzichtsblad");
            return output;
        }

        private sealed class DrawingTemplateSelection
        {
            public string Path { get; set; }
            public bool IsCompanyTemplate { get; set; }
        }

        private static DrawingTemplateSelection ResolveDrawingTemplate(SldWorks solidWorks)
        {
            var overridePath = System.Environment.GetEnvironmentVariable("SW_RDABV_DRAWING_TEMPLATE");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                overridePath = System.IO.Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(overridePath.Trim().Trim('"')));
                if (!File.Exists(overridePath))
                    throw new FileNotFoundException("Het via SW_RDABV_DRAWING_TEMPLATE ingestelde drawing template bestaat niet.", overridePath);
                if (!string.Equals(System.IO.Path.GetExtension(overridePath), ".drwdot", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("SW_RDABV_DRAWING_TEMPLATE moet naar een .drwdot-bestand verwijzen: " + overridePath);
                return new DrawingTemplateSelection { Path = overridePath, IsCompanyTemplate = false };
            }

            var defaultPath = solidWorks.GetUserPreferenceStringValue(DefaultDrawingTemplatePreference);
            var templateFolders = solidWorks.GetUserPreferenceStringValue(DocumentTemplateFolderPreference);
            var companyPath = FindCompanyDrawingTemplate(templateFolders, defaultPath);
            if (!string.IsNullOrWhiteSpace(companyPath))
                return new DrawingTemplateSelection { Path = companyPath, IsCompanyTemplate = false };

            if (string.IsNullOrWhiteSpace(defaultPath) || !File.Exists(defaultPath))
                throw new InvalidOperationException("SOLIDWORKS heeft geen geldig RDAbv/RDA drawing template en ook geen geldige standaard drawing template. Controleer Systeemopties > Bestandslocaties > Documentsjablonen.");

            return new DrawingTemplateSelection { Path = defaultPath, IsCompanyTemplate = false };
        }

        private static string FindCompanyDrawingTemplate(string templateFolders, string defaultPath)
        {
            var candidates = new List<string>();
            AddCompanyTemplateCandidate(candidates, defaultPath);

            foreach (var folderValue in (templateFolders ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var folder = System.Environment.ExpandEnvironmentVariables(folderValue.Trim().Trim('"'));
                if (!Directory.Exists(folder)) continue;

                try
                {
                    foreach (var path in Directory.GetFiles(folder, "*.drwdot", SearchOption.TopDirectoryOnly))
                        AddCompanyTemplateCandidate(candidates, path);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            candidates.Sort(CompareCompanyTemplates);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private static void AddCompanyTemplateCandidate(ICollection<string> candidates, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            if (!string.Equals(System.IO.Path.GetExtension(path), ".drwdot", StringComparison.OrdinalIgnoreCase)) return;
            if (CompanyTemplateScore(path) == int.MaxValue) return;

            foreach (var candidate in candidates)
            {
                if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase)) return;
            }
            candidates.Add(path);
        }

        private static int CompareCompanyTemplates(string left, string right)
        {
            var scoreComparison = CompanyTemplateScore(left).CompareTo(CompanyTemplateScore(right));
            if (scoreComparison != 0) return scoreComparison;

            var dateComparison = File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left));
            return dateComparison != 0 ? dateComparison : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompanyTemplateScore(string path)
        {
            var name = (System.IO.Path.GetFileNameWithoutExtension(path) ?? "").ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
            if (name.Contains("rdabv")) return 0;
            if (name.Contains("drawingtemplaterda")) return 1;
            if (name.Contains("rda")) return 2;
            return int.MaxValue;
        }

        private static void CopyCompanyTemplateSheet(ModelDoc2 drawingModel, DrawingDoc drawing, string sourceName, string targetName)
        {
            drawingModel.ClearSelection2(true);
            if (!drawingModel.Extension.SelectByID2(sourceName, "SHEET", 0, 0, 0, false, 0, null, 0))
                throw new InvalidOperationException("SOLIDWORKS kon het RDAbv-tekenblad niet selecteren om de huisstijl te kopiëren.");

            var namesBefore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in SheetNames(drawing)) namesBefore.Add(name);

            drawingModel.EditCopy();
            if (!drawing.PasteSheet(InsertSheetAfterSelected, RenameDuplicateViews))
                throw new InvalidOperationException("SOLIDWORKS kon het tweede tekenblad niet vanuit de RDAbv-template kopiëren.");

            string copiedName = null;
            foreach (var name in SheetNames(drawing))
            {
                if (!namesBefore.Contains(name))
                {
                    copiedName = name;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(copiedName))
                throw new InvalidOperationException("SOLIDWORKS heeft na het kopiëren geen nieuw RDAbv-tekenblad gemeld.");
            if (!drawing.ActivateSheet(copiedName))
                throw new InvalidOperationException("SOLIDWORKS kon het gekopieerde RDAbv-tekenblad niet activeren.");

            var copiedSheet = drawing.GetCurrentSheet() as Sheet;
            if (copiedSheet == null) throw new InvalidOperationException("SOLIDWORKS kon het gekopieerde RDAbv-tekenblad niet openen.");
            copiedSheet.SetName(targetName);
            drawingModel.ClearSelection2(true);
        }

        private static IEnumerable<string> SheetNames(DrawingDoc drawing)
        {
            var names = drawing.GetSheetNames() as Array;
            if (names == null) yield break;
            foreach (var value in names)
            {
                var name = value as string;
                if (!string.IsNullOrWhiteSpace(name)) yield return name;
            }
        }

        public static SolidWorksCustomerDrawingOutput OutputFor(string controlPath)
        {
            var folder = Path.GetDirectoryName(controlPath) ?? "";
            var name = Path.GetFileNameWithoutExtension(controlPath) ?? "Klantmodel";
            if (name.EndsWith("_CONTROLE", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - "_CONTROLE".Length);
            var stem = Path.Combine(folder, name + "_INTERNE_TEKENBRON");
            var sheetImage = stem + ".png";
            return new SolidWorksCustomerDrawingOutput
            {
                DrawingPath = stem + ".SLDDRW",
                PdfPath = stem + ".pdf",
                GeneralSheetImagePath = sheetImage,
                WorktopSheetImagePath = sheetImage
            };
        }

        private static void BuildGeneralSheet(
            ModelDoc2 drawingModel,
            DrawingDoc drawing,
            string modelPath,
            PortalQuoteRequest request,
            WorkbenchModel workbenchModel)
        {
            AddTitle(drawing, "KLANTTEKENING — " + FriendlyProductName(request, workbenchModel), 0.018, 0.285, 0.0045);
            AddTitle(drawing, "CONCEPT BIJ OFFERTE  •  ALLEEN HOOFD- EN CONTROLEMATEN  •  ALLE MATEN IN MM", 0.018, 0.276, 0.0025);

            var scale = GeneralScale(request);
            var front = AddView(drawing, modelPath, "*Front", 0.112, 0.197, scale);
            var right = AddView(drawing, modelPath, "*Right", 0.310, 0.197, scale);
            var top = AddView(drawing, modelPath, "*Top", 0.112, 0.070, scale);
            AddView(drawing, modelPath, "*Isometric", 0.310, 0.070, scale);

            AddViewLabel(drawing, front, "VOORAANZICHT");
            AddViewLabel(drawing, right, "RECHTER ZIJAANZICHT");
            AddViewLabel(drawing, top, "BOVENAANZICHT");
            AddTitle(drawing, "ISOMETRISCH", 0.274, 0.130, 0.0025);

            AddHorizontalDimension(drawing, front, request.WidthMm, false);
            AddVerticalDimension(drawing, front, request.HeightMm, true);
            AddHorizontalDimension(drawing, right, request.DepthMm, false);
            AddHorizontalDimension(drawing, top, request.WidthMm, false);
            AddVerticalDimension(drawing, top, request.DepthMm, true);

            AddTitle(drawing, "Klant: " + CustomerName(request.CustomerName), 0.018, 0.018, 0.0023);
            AddTitle(drawing, "Project: " + ProjectReference(workbenchModel), 0.018, 0.010, 0.0023);
            AddTitle(drawing, "Datum: " + DateTime.Today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) + "  •  Revisie: CONCEPT", 0.175, 0.018, 0.0023);
            AddTitle(drawing, "Materialen: " + MaterialSummary(workbenchModel), 0.175, 0.010, 0.0023);
            drawingModel.EditRebuild3();
            drawingModel.ViewZoomtofit2();
        }

        private static void BuildWorktopSheet(
            ModelDoc2 drawingModel,
            DrawingDoc drawing,
            string modelPath,
            PortalQuoteRequest request,
            WorkbenchModel workbenchModel)
        {
            AddTitle(drawing, "WERKBLAD EN KRITIEKE BUITENMATEN", 0.018, 0.285, 0.0045);
            AddTitle(drawing, "BOVENAANZICHT — GATEN EN UITSPARINGEN VOLGEN HET 3D-MODEL", 0.018, 0.276, 0.0025);
            var scale = WorktopScale(request);
            var top = AddView(drawing, modelPath, "*Top", 0.215, 0.165, scale);
            AddHorizontalDimension(drawing, top, request.WidthMm, false);
            AddVerticalDimension(drawing, top, request.DepthMm, true);
            AddViewLabel(drawing, top, "BOVENAANZICHT WERKBLAD / SAMENSTELLING");

            AddTitle(drawing, "Hoofdmaat: " + Mm(request.WidthMm) + " × " + Mm(request.DepthMm) + " mm", 0.018, 0.038, 0.0030);
            AddTitle(drawing, "Totale hoogte: " + Mm(request.HeightMm) + " mm", 0.018, 0.027, 0.0026);
            AddTitle(drawing, "Plaatmateriaal: " + SheetMaterial(workbenchModel), 0.190, 0.038, 0.0026);
            AddTitle(drawing, "Controleer posities van gaten en uitsparingen vóór vrijgave.", 0.190, 0.027, 0.0026);
            drawingModel.EditRebuild3();
            drawingModel.ViewZoomtofit2();
        }

        private static View AddView(DrawingDoc drawing, string modelPath, string viewName, double x, double y, double scale)
        {
            var view = drawing.CreateDrawViewFromModelView3(modelPath, viewName, x, y, 0);
            if (view == null) throw new InvalidOperationException("SOLIDWORKS kon het aanzicht " + viewName + " niet maken.");
            view.UseSheetScale = 0;
            view.ScaleDecimal = scale;
            return view;
        }

        private static void AddViewLabel(DrawingDoc drawing, View view, string label)
        {
            var outline = ViewOutline(view);
            AddTitle(drawing, label, outline[0], Math.Min(A3Height - 0.012, outline[3] + 0.006), 0.0024);
        }

        private static void AddHorizontalDimension(DrawingDoc drawing, View view, double millimetres, bool above)
        {
            var o = ViewOutline(view);
            var y = above ? o[3] + 0.012 : o[1] - 0.012;
            var refY = above ? o[3] : o[1];
            var p0 = Point(o[0], y);
            var p1 = Point(o[2], y);
            var normal = new double[] { 0, 0, 1 };
            var p3 = Point(o[0], refY);
            var p4 = Point(o[2], refY);
            var text = Point((o[0] + o[2]) / 2.0, y);
            var dimension = drawing.CreateLinearDim4(p0, p1, normal, p3, p4, text, millimetres / 1000.0, 0, 0.0028);
            if (dimension == null) throw new InvalidOperationException("SOLIDWORKS kon de horizontale hoofdmaat niet plaatsen.");
        }

        private static void AddVerticalDimension(DrawingDoc drawing, View view, double millimetres, bool left)
        {
            var o = ViewOutline(view);
            var x = left ? o[0] - 0.014 : o[2] + 0.014;
            var refX = left ? o[0] : o[2];
            var p0 = Point(x, o[1]);
            var p1 = Point(x, o[3]);
            var normal = new double[] { 0, 0, 1 };
            var p3 = Point(refX, o[1]);
            var p4 = Point(refX, o[3]);
            var text = Point(x, (o[1] + o[3]) / 2.0);
            var dimension = drawing.CreateLinearDim4(p0, p1, normal, p3, p4, text, millimetres / 1000.0, Math.PI / 2.0, 0.0028);
            if (dimension == null) throw new InvalidOperationException("SOLIDWORKS kon de verticale hoofdmaat niet plaatsen.");
        }

        private static double[] ViewOutline(View view)
        {
            var outline = view.GetOutline() as double[];
            if (outline == null || outline.Length < 4)
                throw new InvalidOperationException("SOLIDWORKS kon de begrenzing van een tekeningaanzicht niet bepalen.");
            return outline;
        }

        private static double[] Point(double x, double y)
        {
            return new double[] { x, y, 0 };
        }

        private static void AddTitle(DrawingDoc drawing, string text, double x, double y, double height)
        {
            drawing.CreateText2(text ?? "", x, y, 0, height, 0);
        }

        private static double GeneralScale(PortalQuoteRequest request)
        {
            var width = Math.Max(1, request.WidthMm) / 1000.0;
            var depth = Math.Max(1, request.DepthMm) / 1000.0;
            var height = Math.Max(1, request.HeightMm) / 1000.0;
            return Clamp(Math.Min(0.105 / Math.Max(width, depth), 0.085 / height), 0.04, 0.16);
        }

        private static double WorktopScale(PortalQuoteRequest request)
        {
            var width = Math.Max(1, request.WidthMm) / 1000.0;
            var depth = Math.Max(1, request.DepthMm) / 1000.0;
            return Clamp(Math.Min(0.285 / width, 0.175 / depth), 0.06, 0.30);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static void SaveDrawing(ModelDoc2 drawingModel, string path)
        {
            var errors = 0;
            var warnings = 0;
            drawingModel.Extension.SaveAs(path, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            if (errors != 0) throw new InvalidOperationException("SOLIDWORKS kon de werktekening niet opslaan (code " + errors + ").");
        }

        private static void ExportPdf(SldWorks solidWorks, ModelDoc2 drawingModel, string path)
        {
            dynamic exportData = solidWorks.GetExportFileData(ExportPdfData);
            if (exportData == null) throw new InvalidOperationException("SOLIDWORKS kon geen PDF-exportinstellingen maken.");
            exportData.ViewPdfAfterSaving = false;
            exportData.SetSheets(ExportAllSheets, null);
            var errors = 0;
            var warnings = 0;
            drawingModel.Extension.SaveAs(path, SaveAsCurrentVersion, SaveAsOptionsSilent, exportData, ref errors, ref warnings);
            if (errors != 0) throw new InvalidOperationException("SOLIDWORKS kon de PDF-werktekening niet opslaan (code " + errors + ").");
        }

        private static void ExportSheetImage(ModelDoc2 drawingModel, DrawingDoc drawing, string sheetName, string pngPath)
        {
            if (!drawing.ActivateSheet(sheetName))
                throw new InvalidOperationException("SOLIDWORKS kon tekenblad " + sheetName + " niet activeren.");
            drawingModel.ViewZoomtofit2();
            var bmpPath = Path.ChangeExtension(pngPath, ".bmp");
            try
            {
                if (!drawingModel.SaveBMP(bmpPath, 2400, 1700) || !File.Exists(bmpPath))
                    throw new InvalidOperationException("SOLIDWORKS kon tekenblad " + sheetName + " niet als afbeelding opslaan.");
                using (var bitmap = new Bitmap(bmpPath)) bitmap.Save(pngPath, ImageFormat.Png);
            }
            finally
            {
                try { if (File.Exists(bmpPath)) File.Delete(bmpPath); } catch { }
            }
        }

        private static string FriendlyProductName(PortalQuoteRequest request, WorkbenchModel model)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "WERKTAFEL";
            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase)) return "WERKBANKKAST";
            return string.IsNullOrWhiteSpace(model.ProjectName) ? "KLANTUITVOERING" : model.ProjectName.ToUpperInvariant();
        }

        private static string MaterialSummary(WorkbenchModel model)
        {
            var materials = model.Sheets
                .Where(item => item != null && item.Material != null && !string.IsNullOrWhiteSpace(item.Material.Name))
                .Select(item => item.Material.Name.Trim())
                .Concat(model.Profiles
                    .Where(item => item != null && item.Material != null && !string.IsNullOrWhiteSpace(item.Material.Name))
                    .Select(item => item.Material.Name.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return materials.Length == 0 ? "uitvoering volgens configuratie" : string.Join(" / ", materials);
        }

        private static string SheetMaterial(WorkbenchModel model)
        {
            return model.Sheets.Count > 0 && model.Sheets[0].Material != null ? model.Sheets[0].Material.Name : "plaatmateriaal volgens configuratie";
        }

        private static string CustomerName(string value)
        {
            value = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, "Testklant", StringComparison.OrdinalIgnoreCase)
                ? "nog in te vullen"
                : value;
        }

        private static string ProjectReference(WorkbenchModel model)
        {
            var value = model == null ? "" : (model.ProjectName ?? "").Trim();
            return string.IsNullOrWhiteSpace(value) ? "klantuitvoering" : value.Replace("_", "-");
        }

        private static string Mm(double value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static void EnsureOutput(string path, string description)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("SOLIDWORKS heeft geen geldige " + description + " opgeslagen: " + path);
        }
    }
}
