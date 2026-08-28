using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    /// <summary>
    /// Bouwt een klantpresentatie op basis van het GLB-klantmodel. PowerPoint bewaart
    /// het orbit-model in het document; de vaste aanzichten worden als lichte PNG's
    /// uit datzelfde model afgeleid zodat de presentatie compact blijft.
    /// </summary>
    internal static class SolidWorksCustomerPowerPointExporter
    {
        private const int MsoFalse = 0;
        private const int MsoTrue = -1;
        private const int PpSaveAsOpenXmlPresentation = 24;
        private const int PpSaveAsPdf = 32;
        private const int PpLayoutBlank = 12;
        private const int PpShapeFormatPng = 2;
        private const int MsoShapeRoundedRectangle = 5;
        private const int MsoTextOrientationHorizontal = 1;

        public static string Export(
            string glbPath,
            PortalQuoteRequest request,
            WorkbenchModel model,
            SolidWorksCustomerDrawingOutput drawingOutput)
        {
            if (string.IsNullOrWhiteSpace(glbPath) || !File.Exists(glbPath))
                throw new FileNotFoundException("Het GLB-klantmodel voor de PowerPoint ontbreekt.", glbPath);
            if (request == null) throw new ArgumentNullException("request");
            if (model == null) throw new ArgumentNullException("model");

            var profile = SolidWorksCustomerPresentationProfiles.Resolve(request, model);
            var templatePath = ResolveTemplatePath(profile);
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("De standaard klantpresentatie-template ontbreekt.", templatePath);

            var outputPath = CustomerPowerPointPath(glbPath, request);
            var pdfPath = CustomerPdfPath(glbPath, request);
            var temporaryFolder = Path.Combine(Path.GetDirectoryName(outputPath) ?? "", ".klantpresentatie-preview");
            Directory.CreateDirectory(temporaryFolder);

            object applicationObject = null;
            object presentationObject = null;
            try
            {
                var applicationType = Type.GetTypeFromProgID("PowerPoint.Application");
                if (applicationType == null)
                    throw new InvalidOperationException("Microsoft PowerPoint is niet geïnstalleerd; de interactieve klantpresentatie kan niet worden gemaakt.");

                applicationObject = Activator.CreateInstance(applicationType);
                dynamic application = applicationObject;
                presentationObject = application.Presentations.Open(templatePath, MsoFalse, MsoFalse, MsoFalse);
                dynamic presentation = presentationObject;

                ReplaceTokens(presentation, BuildTokens(request, model, profile));
                BuildModelPages(presentation, glbPath, temporaryFolder, profile);
                BuildDrawingPages(presentation, drawingOutput, temporaryFolder, request, model, profile);
                PrepareStaticSlides(presentation, request, model, profile);
                presentation.SaveAs(outputPath, PpSaveAsOpenXmlPresentation);
                presentation.SaveAs(pdfPath, PpSaveAsPdf);
                presentation.Close();
                presentationObject = null;
                application.Quit();
                applicationObject = null;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    "PowerPoint kon de interactieve klantpresentatie niet opbouwen. " + ExceptionDetails(error),
                    error);
            }
            finally
            {
                ReleaseCom(presentationObject);
                ReleaseCom(applicationObject);
                TryRemoveTemporaryFolder(temporaryFolder);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (!File.Exists(outputPath))
                throw new InvalidOperationException("PowerPoint heeft geen klantpresentatie opgeslagen: " + outputPath);
            if (!File.Exists(pdfPath))
                throw new InvalidOperationException("PowerPoint heeft geen statische klantbijlage opgeslagen: " + pdfPath);
            return outputPath;
        }

        private static void BuildDrawingPages(dynamic presentation, SolidWorksCustomerDrawingOutput drawingOutput, string temporaryFolder, PortalQuoteRequest request, WorkbenchModel model, SolidWorksCustomerPresentationProfile profile)
        {
            var parts = new PortalAssembly3DService().Build(model, request);
            var motion = new PortalMotionContractService().Build(model, request, parts);
            const double elevationCanvasHeight = 460;
            const double frontCanvasWidth = 1000;
            const double sideCanvasWidth = 650;
            var heightTravel = profile.ShowHeightAdjustment && motion != null
                ? Math.Max(0, motion.Vertical.Maximum - motion.Vertical.Minimum)
                : 0;
            var sharedElevationScale = Math.Min(
                CalculateOrthographicFitScale(parts, "front", frontCanvasWidth, elevationCanvasHeight, heightTravel),
                CalculateOrthographicFitScale(parts, "side", sideCanvasWidth, elevationCanvasHeight, heightTravel));
            var frontPath = BuildOrthographicSvg(parts, request, temporaryFolder, "klant-voor.svg", "front", frontCanvasWidth, elevationCanvasHeight, false, heightTravel, sharedElevationScale);
            var sidePath = BuildOrthographicSvg(parts, request, temporaryFolder, "klant-zij.svg", "side", sideCanvasWidth, elevationCanvasHeight, false, heightTravel, sharedElevationScale);
            var topPath = BuildOrthographicSvg(parts, request, temporaryFolder, "klant-boven.svg", "top", 1000, 600, false);
            var ballPattern = BuildBallTransferPattern(parts);
            var ballSectionPath = profile.ShowBallTransferDetails && ballPattern != null
                ? BuildBallTransferSectionSvg(ballPattern, temporaryFolder, "klant-kogelpot-doorsnede.svg")
                : null;
            var ballPatternDetailPath = profile.ShowBallTransferDetails && ballPattern != null
                ? BuildBallTransferPatternDetailSvg(ballPattern, temporaryFolder, "klant-kogelpotpatroon-detail.svg")
                : null;
            var heightRangePath = profile.ShowHeightAdjustment && motion != null
                ? WriteUtf8(Path.Combine(temporaryFolder, "klant-bewegingsbereik-hoogte.svg"), ProductionOutputService.BuildMotionRangeSvg(parts, motion, true))
                : null;
            var horizontalRangePath = profile.ShowSlidingMovement && motion != null
                ? WriteUtf8(Path.Combine(temporaryFolder, "klant-bewegingsbereik-links-midden-rechts.svg"), ProductionOutputService.BuildMotionRangeSvg(parts, motion, false))
                : null;

            dynamic slide = presentation.Slides.Item(5);
            DeleteShapesWithPrefixes(slide, "DRAWING_GENERAL", "DRAWING_VIEW_", "BALL_");
            var movementRange = motion == null ? 0 : motion.Horizontal.Maximum - motion.Horizontal.Minimum;
            var topLabel = profile.ShowSlidingMovement ? "BOVENAANZICHT EN VERPLAATSING" : "BOVENAANZICHT";
            var topDimension = Millimetres(request.WidthMm) + " x " + Millimetres(request.DepthMm) + " mm";
            if (profile.ShowSlidingMovement) topDimension += " · slag " + Millimetres(movementRange) + " mm";
            var frontLabel = profile.ShowHeightAdjustment ? "HOOGTEVERSTELLING" : "VOORAANZICHT";
            var frontDimension = profile.ShowHeightAdjustment
                ? Millimetres(motion.Vertical.ReferenceValueMm + motion.Vertical.Minimum) + "–" + Millimetres(motion.Vertical.ReferenceValueMm + motion.Vertical.Maximum) + " mm"
                : "Hoogte " + Millimetres(request.HeightMm) + " mm";
            AddTechnicalSvgView(slide, topPath, 42, 132, 558, 326, topLabel, topDimension, true);
            AddSharedScaleTechnicalSvgView(slide, frontPath, 616, 130, 312, 168, frontCanvasWidth, elevationCanvasHeight, frontLabel, frontDimension);
            AddSharedScaleTechnicalSvgView(slide, sidePath, 616, 300, 312, 168, sideCanvasWidth, elevationCanvasHeight, "ZIJAANZICHT", "Diepte " + Millimetres(request.DepthMm) + " mm");
            if (profile.ShowBallTransferDetails && ballPattern != null)
            {
                dynamic duplicated = slide.Duplicate();
                dynamic detailSlide = duplicated.Item(1);
                while (presentation.Slides.Count > 6) presentation.Slides.Item(7).Delete();
                DeleteShapesWithPrefixes(detailSlide, "DRAWING_GENERAL", "DRAWING_VIEW_", "BALL_");

                AddText(detailSlide, "BALL_PATTERN_LABEL", 42, 132, 876, 18, "HART- EN RANDMATEN", 9.5f, true, OfficeRgb(101, 111, 118));
                dynamic patternDetail = detailSlide.Shapes.AddPicture(ballPatternDetailPath, MsoFalse, MsoTrue, 42, 152, 876, 170);
                patternDetail.Name = "BALL_PATTERN_DETAIL";
                patternDetail.AlternativeText = "Los maatdetail met h.o.h.-afstand, verspringing en randafstanden van het kogelpotpatroon";
                dynamic detailDivider = detailSlide.Shapes.AddLine(42, 342, 918, 342);
                detailDivider.Name = "BALL_DETAIL_DIVIDER";
                detailDivider.Line.ForeColor.RGB = OfficeRgb(220, 224, 229);
                detailDivider.Line.Weight = 1f;
                AddText(detailSlide, "BALL_SECTION_LABEL", 42, 360, 876, 18, "VERZONKEN INBOUW", 9.5f, true, OfficeRgb(101, 111, 118));
                dynamic section = detailSlide.Shapes.AddPicture(ballSectionPath, MsoFalse, MsoTrue, 42, 384, 190, 92);
                section.Name = "BALL_INSTALLATION_SECTION";
                section.AlternativeText = "Doorsnede van een verzonken RVS kogelpot met de kogel 2 mm boven het werkvlak";
                AddText(detailSlide, "BALL_INSTALLATION_NOTE", 264, 390, 654, 34,
                    ballPattern.Count.ToString(CultureInfo.InvariantCulture) + " RVS kogelpotten · verzonken in het HPL-werkvlak",
                    11f, false, OfficeRgb(82, 82, 88));
            }
            else if (presentation.Slides.Count >= 6) presentation.Slides.Item(6).Delete();

            if (!string.IsNullOrWhiteSpace(heightRangePath))
                AddMotionRangeSlide(presentation, heightRangePath, 1200, 720,
                    "Werkhoogte in de laagste en hoogste stand");
            if (!string.IsNullOrWhiteSpace(horizontalRangePath))
                AddMotionRangeSlide(presentation, horizontalRangePath, 1200, 650,
                    "Werkblad volledig links, in het midden en volledig rechts");
        }

        private static string WriteUtf8(string path, string content)
        {
            File.WriteAllText(path, content ?? "", new UTF8Encoding(false));
            return path;
        }

        private static void AddMotionRangeSlide(dynamic presentation, string imagePath, double sourceWidth, double sourceHeight, string alternativeText)
        {
            dynamic slide = presentation.Slides.Add(presentation.Slides.Count + 1, PpLayoutBlank);
            var slideWidth = Convert.ToDouble(presentation.PageSetup.SlideWidth, CultureInfo.InvariantCulture);
            var slideHeight = Convert.ToDouble(presentation.PageSetup.SlideHeight, CultureInfo.InvariantCulture);
            const double margin = 10;
            var scale = Math.Min((slideWidth - 2 * margin) / sourceWidth, (slideHeight - 2 * margin) / sourceHeight);
            var width = sourceWidth * scale;
            var height = sourceHeight * scale;
            var left = (slideWidth - width) / 2.0;
            var top = (slideHeight - height) / 2.0;
            dynamic image = slide.Shapes.AddPicture(imagePath, MsoFalse, MsoTrue, (float)left, (float)top, (float)width, (float)height);
            image.Name = "DRAWING_MOTION_RANGE";
            image.AlternativeText = alternativeText;
            AddText(slide, "DRAWING_MOTION_PAGE", (float)(slideWidth - 42), 18, 24, 12,
                ((int)presentation.Slides.Count).ToString("00", CultureInfo.InvariantCulture), 7f, false, OfficeRgb(101, 111, 118));
        }

        private static string CropDrawingView(string sourcePath, string folder, string fileName, double x, double y, double width, double height)
        {
            var destination = Path.Combine(folder, fileName);
            using (var source = new Bitmap(sourcePath))
            {
                var rectangle = new Rectangle(
                    Math.Max(0, (int)Math.Round(source.Width * x)),
                    Math.Max(0, (int)Math.Round(source.Height * y)),
                    Math.Max(1, (int)Math.Round(source.Width * width)),
                    Math.Max(1, (int)Math.Round(source.Height * height)));
                rectangle.Width = Math.Min(rectangle.Width, source.Width - rectangle.X);
                rectangle.Height = Math.Min(rectangle.Height, source.Height - rectangle.Y);
                using (var crop = source.Clone(rectangle, source.PixelFormat))
                using (var trimmed = TrimWhiteMargins(crop))
                    trimmed.Save(destination, System.Drawing.Imaging.ImageFormat.Png);
            }
            return destination;
        }

        private static Bitmap TrimWhiteMargins(Bitmap source)
        {
            var left = source.Width;
            var top = source.Height;
            var right = -1;
            var bottom = -1;
            for (var y = 0; y < source.Height; y += 2)
            for (var x = 0; x < source.Width; x += 2)
            {
                var pixel = source.GetPixel(x, y);
                if (pixel.R > 242 && pixel.G > 242 && pixel.B > 242) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }

            if (right < left || bottom < top) return new Bitmap(source);
            var padding = Math.Max(8, Math.Min(source.Width, source.Height) / 45);
            left = Math.Max(0, left - padding);
            top = Math.Max(0, top - padding);
            right = Math.Min(source.Width - 1, right + padding);
            bottom = Math.Min(source.Height - 1, bottom + padding);
            return source.Clone(new Rectangle(left, top, right - left + 1, bottom - top + 1), source.PixelFormat);
        }

        private static void AddTechnicalView(dynamic slide, string imagePath, float left, float top, float width, float height, string label, string dimension, bool prominent)
        {
            AddPictureContained(slide, imagePath, left, top + 25, width, height - 51, "DRAWING_VIEW_" + label.Replace(" ", "_"), label);
            dynamic labelBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top, width, 18);
            labelBox.Name = "DRAWING_VIEW_LABEL_" + label.Replace(" ", "_");
            labelBox.TextFrame.TextRange.Text = label;
            FormatText(labelBox, 9.5f, true, OfficeRgb(101, 111, 118));
            dynamic dimensionBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top + height - 23, width, 22);
            dimensionBox.Name = "DRAWING_VIEW_DIM_" + label.Replace(" ", "_");
            dimensionBox.TextFrame.TextRange.Text = dimension;
            FormatText(dimensionBox, prominent ? 17f : 14f, true, OfficeRgb(27, 37, 42));
            if (!prominent) return;
            dynamic divider = slide.Shapes.AddLine(left + width + 14, top, left + width + 14, top + height);
            divider.Name = "DRAWING_VIEW_DIVIDER";
            divider.Line.ForeColor.RGB = OfficeRgb(220, 224, 229);
            divider.Line.Weight = 1f;
        }

        private static void AddTechnicalSvgView(dynamic slide, string imagePath, float left, float top, float width, float height, string label, string dimension, bool prominent)
        {
            dynamic image = slide.Shapes.AddPicture(imagePath, MsoFalse, MsoTrue, left, top + 24, width, height - 49);
            image.Left = left;
            image.Top = top + 24;
            image.Width = width;
            image.Height = height - 49;
            image.Name = "DRAWING_VIEW_" + label.Replace(" ", "_");
            image.AlternativeText = label;
            dynamic labelBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top, width, 18);
            labelBox.Name = "DRAWING_VIEW_LABEL_" + label.Replace(" ", "_");
            labelBox.TextFrame.TextRange.Text = label;
            FormatText(labelBox, 9.5f, true, OfficeRgb(101, 111, 118));
            dynamic dimensionBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top + height - 22, width, 22);
            dimensionBox.Name = "DRAWING_VIEW_DIM_" + label.Replace(" ", "_");
            dimensionBox.TextFrame.TextRange.Text = dimension;
            FormatText(dimensionBox, prominent ? 17f : 14f, true, OfficeRgb(27, 37, 42));
            if (!prominent) return;
            dynamic divider = slide.Shapes.AddLine(left + width + 14, top, left + width + 14, top + height);
            divider.Name = "DRAWING_VIEW_DIVIDER";
            divider.Line.ForeColor.RGB = OfficeRgb(220, 224, 229);
            divider.Line.Weight = 1f;
        }

        private static void AddSharedScaleTechnicalSvgView(
            dynamic slide,
            string imagePath,
            float left,
            float top,
            float width,
            float height,
            double sourceWidth,
            double sourceHeight,
            string label,
            string dimension)
        {
            const float imageHeight = 132f;
            var imageWidth = (float)(imageHeight * sourceWidth / sourceHeight);
            if (imageWidth > width)
                throw new InvalidOperationException("Het technische aanzicht past niet binnen de gereserveerde klantbijlagekolom.");

            var imageLeft = left + (width - imageWidth) / 2f;
            dynamic image = slide.Shapes.AddPicture(imagePath, MsoFalse, MsoTrue, imageLeft, top + 16, imageWidth, imageHeight);
            image.Name = "DRAWING_VIEW_" + label.Replace(" ", "_");
            image.AlternativeText = label + " op dezelfde schaal als het andere hoogteaanzicht";

            dynamic labelBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left + 10, top, width - 20, 16);
            labelBox.Name = "DRAWING_VIEW_LABEL_" + label.Replace(" ", "_");
            labelBox.TextFrame.TextRange.Text = label;
            FormatText(labelBox, 9.5f, true, OfficeRgb(101, 111, 118));

            dynamic dimensionBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left + 10, top + height - 18, width - 20, 18);
            dimensionBox.Name = "DRAWING_VIEW_DIM_" + label.Replace(" ", "_");
            dimensionBox.TextFrame.TextRange.Text = dimension;
            FormatText(dimensionBox, 14f, true, OfficeRgb(27, 37, 42));
        }

        private static string BuildOrthographicSvg(
            IList<PortalAssemblyPart> parts,
            PortalQuoteRequest request,
            string folder,
            string fileName,
            string mode,
            double canvasWidth,
            double canvasHeight,
            bool includeBallPatternDimensions,
            double heightTravelMm = 0,
            double scaleOverride = 0)
        {
            var path = Path.Combine(folder, fileName);
            var list = parts == null ? new List<PortalAssemblyPart>() : parts.Where(item => item != null).ToList();
            if (list.Count == 0) throw new InvalidOperationException("De assembly bevat geen onderdelen voor het klantaanzicht.");

            Func<PortalAssemblyPart, double> horizontalCenter = item => mode == "side" ? item.Zmm : item.Xmm;
            Func<PortalAssemblyPart, double> horizontalSize = item => mode == "side" ? item.SizeZmm : item.SizeXmm;
            Func<PortalAssemblyPart, double> verticalCenter = item => mode == "top" ? item.Zmm : item.Ymm;
            Func<PortalAssemblyPart, double> verticalSize = item => mode == "top" ? item.SizeZmm : item.SizeYmm;
            Func<PortalAssemblyPart, double> depth = item => mode == "front" ? item.Zmm : (mode == "side" ? item.Xmm : item.Ymm);

            var movementRange = DetermineMovementRange(list);
            var movementHalf = movementRange / 2.0;
            var baseMinH = list.Min(item => horizontalCenter(item) - horizontalSize(item) / 2.0);
            var baseMaxH = list.Max(item => horizontalCenter(item) + horizontalSize(item) / 2.0);
            var baseMinV = list.Min(item => verticalCenter(item) - verticalSize(item) / 2.0);
            var baseMaxV = list.Max(item => verticalCenter(item) + verticalSize(item) / 2.0);
            var minH = mode == "top" ? baseMinH - movementHalf : baseMinH;
            var maxH = mode == "top" ? baseMaxH + movementHalf : baseMaxH;
            var minV = baseMinV;
            var maxV = mode == "top" ? baseMaxV : baseMaxV + Math.Max(0, heightTravelMm);
            var spanH = Math.Max(1, maxH - minH);
            var spanV = Math.Max(1, maxV - minV);
            var marginLeft = mode == "top" ? 92.0 : 86.0;
            var marginRight = 52.0;
            var marginTop = mode == "top" ? 74.0 : 34.0;
            var marginBottom = 76.0;
            var fitScale = Math.Min((canvasWidth - marginLeft - marginRight) / spanH, (canvasHeight - marginTop - marginBottom) / spanV);
            var scale = scaleOverride > 0 ? Math.Min(scaleOverride, fitScale) : fitScale;
            var drawingWidth = spanH * scale;
            var drawingHeight = spanV * scale;
            var originX = marginLeft + ((canvasWidth - marginLeft - marginRight) - drawingWidth) / 2.0;
            var originY = marginTop + ((canvasHeight - marginTop - marginBottom) - drawingHeight) / 2.0;

            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(canvasWidth) + "\" height=\"" + F(canvasHeight) + "\" viewBox=\"0 0 " + F(canvasWidth) + " " + F(canvasHeight) + "\">");
            sb.AppendLine("<defs>" +
                "<marker id=\"motionStart\" markerWidth=\"10\" markerHeight=\"8\" refX=\"0.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M0,3.5 L9,0.4 L9,6.6 Z\" fill=\"#0071e3\"/></marker>" +
                "<marker id=\"motionEnd\" markerWidth=\"10\" markerHeight=\"8\" refX=\"8.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M9,3.5 L0,0.4 L0,6.6 Z\" fill=\"#0071e3\"/></marker>" +
                "<marker id=\"cadStart\" markerWidth=\"8\" markerHeight=\"7\" refX=\"0.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M0,3.5 L7,1 L7,6 Z\" fill=\"#008c95\"/></marker>" +
                "<marker id=\"cadEnd\" markerWidth=\"8\" markerHeight=\"7\" refX=\"6.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M7,3.5 L0,1 L0,6 Z\" fill=\"#008c95\"/></marker>" +
                "<radialGradient id=\"steelBall\" cx=\"34%\" cy=\"28%\" r=\"72%\"><stop offset=\"0%\" stop-color=\"#ffffff\"/><stop offset=\"28%\" stop-color=\"#e6ebee\"/><stop offset=\"68%\" stop-color=\"#9ba6ae\"/><stop offset=\"100%\" stop-color=\"#59636b\"/></radialGradient></defs>");
            sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#fbfcfd\"/>");
            var orderedParts = mode == "top"
                ? list.OrderBy(item => IsRoundTopComponent(item) ? 1 : 0).ThenBy(depth)
                : list.OrderBy(depth).ThenBy(item => OrthographicLayerPriority(item));
            foreach (var part in orderedParts)
            {
                var x = originX + (horizontalCenter(part) - horizontalSize(part) / 2.0 - minH) * scale;
                var y = originY + (maxV - (verticalCenter(part) + verticalSize(part) / 2.0)) * scale;
                var width = Math.Max(1.2, horizontalSize(part) * scale);
                var height = Math.Max(1.2, verticalSize(part) * scale);
                var style = OrthographicStyle(part);
                if (mode == "top" && IsRoundTopComponent(part))
                {
                    sb.AppendLine("<ellipse cx=\"" + F(x + width / 2.0) + "\" cy=\"" + F(y + height / 2.0) + "\" rx=\"" + F(Math.Max(2, width / 2.0)) + "\" ry=\"" + F(Math.Max(2, height / 2.0)) + "\" fill=\"" + style + "\" stroke=\"#344054\" stroke-width=\"1.1\"/>");
                }
                else
                {
                    sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" fill=\"" + style + "\" stroke=\"#344054\" stroke-width=\"1.1\"/>");
                }
            }

            var worktop = list.FirstOrDefault(item => (item.Name ?? "").IndexOf("kogelpotblad", StringComparison.OrdinalIgnoreCase) >= 0);
            if (mode == "top" && worktop != null && movementHalf > 0)
            {
                var worktopY = originY + (maxV - (worktop.Zmm + worktop.SizeZmm / 2.0)) * scale;
                var worktopHeight = worktop.SizeZmm * scale;
                foreach (var shift in new[] { -movementHalf, movementHalf })
                {
                    var worktopX = originX + (worktop.Xmm + shift - worktop.SizeXmm / 2.0 - minH) * scale;
                    sb.AppendLine("<rect x=\"" + F(worktopX) + "\" y=\"" + F(worktopY) + "\" width=\"" + F(worktop.SizeXmm * scale) + "\" height=\"" + F(worktopHeight) + "\" fill=\"none\" stroke=\"#0071e3\" stroke-width=\"3\" stroke-dasharray=\"12 9\" opacity=\"0.78\"/>");
                }
                var centerX = originX + (worktop.Xmm - minH) * scale;
                var arrowY = 34.0;
                sb.AppendLine("<line x1=\"" + F(centerX - movementHalf * scale) + "\" y1=\"" + F(arrowY) + "\" x2=\"" + F(centerX + movementHalf * scale) + "\" y2=\"" + F(arrowY) + "\" stroke=\"#0071e3\" stroke-width=\"2.4\" marker-start=\"url(#motionStart)\" marker-end=\"url(#motionEnd)\"/>");
                sb.AppendLine("<text x=\"" + F(centerX) + "\" y=\"62\" text-anchor=\"middle\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"24\" font-weight=\"700\" fill=\"#005bb8\">links ↔ rechts · " + Millimetres(movementRange) + " mm</text>");
            }
            if (mode != "top" && worktop != null && heightTravelMm > 0)
            {
                var ghostWidth = horizontalSize(worktop) * scale;
                var ghostX = originX + (horizontalCenter(worktop) - horizontalSize(worktop) / 2.0 - minH) * scale;
                var ghostY = originY + (maxV - (worktop.Ymm + heightTravelMm + worktop.SizeYmm / 2.0)) * scale;
                sb.AppendLine("<line x1=\"" + F(ghostX) + "\" y1=\"" + F(ghostY) + "\" x2=\"" + F(ghostX + ghostWidth) + "\" y2=\"" + F(ghostY) + "\" stroke=\"#0071e3\" stroke-width=\"3\" stroke-dasharray=\"12 9\" opacity=\"0.82\"/>");
                sb.AppendLine("<text x=\"" + F(ghostX + ghostWidth / 2.0) + "\" y=\"" + F(Math.Max(22, ghostY - 10)) + "\" text-anchor=\"middle\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"21\" font-weight=\"700\" fill=\"#005bb8\">max. werkhoogte</text>");
            }
            if (includeBallPatternDimensions && mode == "top" && worktop != null)
                AppendBallTransferPatternDimensions(sb, list, worktop, originX, originY, minH, maxV, scale);

            var bottomY = canvasHeight - 42;
            var dimensionSpan = mode == "side" ? request.DepthMm : request.WidthMm;
            var dimensionCenter = (baseMinH + baseMaxH) / 2.0;
            var dimensionStartX = originX + (dimensionCenter - dimensionSpan / 2.0 - minH) * scale;
            var dimensionEndX = originX + (dimensionCenter + dimensionSpan / 2.0 - minH) * scale;
            var featureBottomY = originY + drawingHeight;
            sb.AppendLine("<line x1=\"" + F(dimensionStartX) + "\" y1=\"" + F(featureBottomY) + "\" x2=\"" + F(dimensionStartX) + "\" y2=\"" + F(bottomY + 5) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(dimensionEndX) + "\" y1=\"" + F(featureBottomY) + "\" x2=\"" + F(dimensionEndX) + "\" y2=\"" + F(bottomY + 5) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(dimensionStartX) + "\" y1=\"" + F(bottomY) + "\" x2=\"" + F(dimensionEndX) + "\" y2=\"" + F(bottomY) + "\" stroke=\"#008c95\" stroke-width=\"1.15\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            var horizontalValue = mode == "side" ? request.DepthMm : request.WidthMm;
            var horizontalLabel = Millimetres(horizontalValue) + " mm " + (mode == "side" ? "diep" : "breed");
            sb.AppendLine("<text x=\"" + F((dimensionStartX + dimensionEndX) / 2.0) + "\" y=\"" + F(bottomY - 8) + "\" text-anchor=\"middle\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"19\" font-weight=\"500\" fill=\"#007f87\">" + horizontalLabel + "</text>");
            if (mode != "top")
            {
                var dimensionX = originX - 34;
                var verticalLabel = heightTravelMm > 0
                    ? Millimetres(request.HeightMm) + "–" + Millimetres(request.HeightMm + heightTravelMm) + " mm hoog"
                    : Millimetres(request.HeightMm) + " mm hoog";
                AppendOverallVerticalDimension(sb, dimensionX, originY, originY + drawingHeight, originX, verticalLabel);
            }
            else
            {
                var dimensionX = originX - 34;
                AppendOverallVerticalDimension(sb, dimensionX, originY, originY + drawingHeight, originX, Millimetres(request.DepthMm) + " mm diep");
            }
            sb.AppendLine("</svg>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static double CalculateOrthographicFitScale(
            IList<PortalAssemblyPart> parts,
            string mode,
            double canvasWidth,
            double canvasHeight,
            double heightTravelMm)
        {
            var list = parts == null ? new List<PortalAssemblyPart>() : parts.Where(item => item != null).ToList();
            if (list.Count == 0) throw new InvalidOperationException("De assembly bevat geen onderdelen voor het klantaanzicht.");

            Func<PortalAssemblyPart, double> horizontalCenter = item => mode == "side" ? item.Zmm : item.Xmm;
            Func<PortalAssemblyPart, double> horizontalSize = item => mode == "side" ? item.SizeZmm : item.SizeXmm;
            Func<PortalAssemblyPart, double> verticalCenter = item => item.Ymm;
            Func<PortalAssemblyPart, double> verticalSize = item => item.SizeYmm;

            var spanH = Math.Max(1,
                list.Max(item => horizontalCenter(item) + horizontalSize(item) / 2.0) -
                list.Min(item => horizontalCenter(item) - horizontalSize(item) / 2.0));
            var spanV = Math.Max(1,
                list.Max(item => verticalCenter(item) + verticalSize(item) / 2.0) + Math.Max(0, heightTravelMm) -
                list.Min(item => verticalCenter(item) - verticalSize(item) / 2.0));
            const double marginLeft = 86.0;
            const double marginRight = 52.0;
            const double marginTop = 34.0;
            const double marginBottom = 76.0;
            return Math.Min((canvasWidth - marginLeft - marginRight) / spanH, (canvasHeight - marginTop - marginBottom) / spanV);
        }

        private static void AppendBallTransferPatternDimensions(
            StringBuilder sb,
            IList<PortalAssemblyPart> parts,
            PortalAssemblyPart worktop,
            double originX,
            double originY,
            double minH,
            double maxV,
            double scale)
        {
            var balls = parts.Where(IsRoundTopComponent).ToList();
            var pattern = BuildBallTransferPattern(parts);
            if (pattern == null || balls.Count < 2) return;

            Func<double, double> x = value => originX + (value - minH) * scale;
            Func<double, double> y = value => originY + (maxV - value) * scale;
            var rows = balls
                .GroupBy(item => Math.Round(item.Zmm, 3))
                .OrderByDescending(group => group.Key)
                .ToList();
            var topRow = rows.FirstOrDefault(group => group.Count() >= 2);
            if (topRow == null) return;

            var topBalls = topRow.OrderBy(item => item.Xmm).ToList();
            var pitchIndex = Math.Max(0, (topBalls.Count / 2) - 1);
            var pitchStart = topBalls[pitchIndex];
            var pitchEnd = topBalls.Skip(pitchIndex + 1).FirstOrDefault(item => item.Xmm - pitchStart.Xmm > 0.001);
            if (pitchEnd != null)
            {
                var yPitch = y(pitchStart.Zmm) - 24;
                AppendHorizontalPatternDimension(sb, x(pitchStart.Xmm), x(pitchEnd.Xmm), yPitch, y(pitchStart.Zmm) - 5, y(pitchEnd.Zmm) - 5, Millimetres(pattern.PitchXmm) + " mm h.o.h.");
            }

            if (rows.Count >= 2)
            {
                var firstRow = rows[0].OrderBy(item => item.Xmm).ToList();
                var secondRow = rows[1].OrderBy(item => item.Xmm).ToList();
                var firstBall = firstRow[0];
                var secondBall = secondRow[0];
                var xPitch = x(worktop.Xmm - worktop.SizeXmm / 2.0) - 22;
                AppendVerticalPatternDimension(sb, xPitch, y(firstBall.Zmm), y(secondBall.Zmm), x(firstBall.Xmm) - 5, x(secondBall.Xmm) - 5, Millimetres(pattern.PitchZmm) + " mm h.o.h.");

                var staggerY = (y(firstBall.Zmm) + y(secondBall.Zmm)) / 2.0;
                AppendHorizontalPatternDimension(sb, x(firstBall.Xmm), x(secondBall.Xmm), staggerY, y(firstBall.Zmm), y(secondBall.Zmm), Millimetres(pattern.StaggerMm) + " mm verspringing");
            }

            var leftEdge = x(worktop.Xmm - worktop.SizeXmm / 2.0);
            var leftBall = balls.OrderBy(item => item.Xmm).ThenBy(item => item.Zmm).First();
            var bottomEdge = y(worktop.Zmm - worktop.SizeZmm / 2.0);
            var edgeY = Math.Min(bottomEdge - 14, y(leftBall.Zmm) + 28);
            AppendHorizontalPatternDimension(sb, leftEdge, x(leftBall.Xmm), edgeY, bottomEdge, y(leftBall.Zmm), Millimetres(pattern.EdgeXmm) + " mm hart-zijrand");

            var topEdge = y(worktop.Zmm + worktop.SizeZmm / 2.0);
            var topBall = balls.OrderByDescending(item => item.Zmm).ThenByDescending(item => item.Xmm).First();
            var rightEdge = x(worktop.Xmm + worktop.SizeXmm / 2.0);
            AppendVerticalPatternDimension(sb, rightEdge - 24, topEdge, y(topBall.Zmm), rightEdge, x(topBall.Xmm), Millimetres(pattern.EdgeZmm) + " mm hart-rand");
        }

        private static void AppendHorizontalPatternDimension(StringBuilder sb, double x1, double x2, double y, double extensionY1, double extensionY2, string label)
        {
            var start = Math.Min(x1, x2);
            var end = Math.Max(x1, x2);
            var startExtension = x1 <= x2 ? extensionY1 : extensionY2;
            var endExtension = x1 <= x2 ? extensionY2 : extensionY1;
            sb.AppendLine("<line x1=\"" + F(start) + "\" y1=\"" + F(startExtension) + "\" x2=\"" + F(start) + "\" y2=\"" + F(y + (y >= startExtension ? 5 : -5)) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(end) + "\" y1=\"" + F(endExtension) + "\" x2=\"" + F(end) + "\" y2=\"" + F(y + (y >= endExtension ? 5 : -5)) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(start) + "\" y1=\"" + F(y) + "\" x2=\"" + F(end) + "\" y2=\"" + F(y) + "\" stroke=\"#008c95\" stroke-width=\"1.15\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            sb.AppendLine("<text x=\"" + F((start + end) / 2.0) + "\" y=\"" + F(y - 7) + "\" text-anchor=\"middle\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"18\" font-weight=\"500\" fill=\"#007f87\">" + label + "</text>");
        }

        private static void AppendVerticalPatternDimension(StringBuilder sb, double x, double y1, double y2, double extensionX1, double extensionX2, string label)
        {
            var start = Math.Min(y1, y2);
            var end = Math.Max(y1, y2);
            var startExtension = y1 <= y2 ? extensionX1 : extensionX2;
            var endExtension = y1 <= y2 ? extensionX2 : extensionX1;
            sb.AppendLine("<line x1=\"" + F(startExtension) + "\" y1=\"" + F(start) + "\" x2=\"" + F(x + (x >= startExtension ? 5 : -5)) + "\" y2=\"" + F(start) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(endExtension) + "\" y1=\"" + F(end) + "\" x2=\"" + F(x + (x >= endExtension ? 5 : -5)) + "\" y2=\"" + F(end) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(x) + "\" y1=\"" + F(start) + "\" x2=\"" + F(x) + "\" y2=\"" + F(end) + "\" stroke=\"#008c95\" stroke-width=\"1.15\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            sb.AppendLine("<text transform=\"translate(" + F(x - 8) + " " + F((start + end) / 2.0) + ") rotate(-90)\" text-anchor=\"middle\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"18\" font-weight=\"500\" fill=\"#007f87\">" + label + "</text>");
        }

        private static void AppendOverallVerticalDimension(StringBuilder sb, double x, double y1, double y2, double featureX, string label)
        {
            sb.AppendLine("<line x1=\"" + F(featureX) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x - 5) + "\" y2=\"" + F(y1) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(featureX) + "\" y1=\"" + F(y2) + "\" x2=\"" + F(x - 5) + "\" y2=\"" + F(y2) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(x) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x) + "\" y2=\"" + F(y2) + "\" stroke=\"#008c95\" stroke-width=\"1.15\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            sb.AppendLine("<text transform=\"translate(" + F(x - 10) + " " + F((y1 + y2) / 2.0) + ") rotate(-90)\" text-anchor=\"middle\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"19\" font-weight=\"500\" fill=\"#007f87\">" + label + "</text>");
        }

        private static string BuildBallTransferSectionSvg(BallTransferPatternInfo pattern, string folder, string fileName)
        {
            var path = Path.Combine(folder, fileName);
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"260\" height=\"120\" viewBox=\"0 0 260 120\">");
            sb.AppendLine("<defs><marker id=\"sectionStart\" markerWidth=\"8\" markerHeight=\"7\" refX=\"0.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M0,3.5 L7,1 L7,6 Z\" fill=\"#008c95\"/></marker><marker id=\"sectionEnd\" markerWidth=\"8\" markerHeight=\"7\" refX=\"6.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M7,3.5 L0,1 L0,6 Z\" fill=\"#008c95\"/></marker><radialGradient id=\"sectionSteel\" cx=\"34%\" cy=\"28%\" r=\"72%\"><stop offset=\"0%\" stop-color=\"#ffffff\"/><stop offset=\"35%\" stop-color=\"#dfe5e8\"/><stop offset=\"100%\" stop-color=\"#59636b\"/></radialGradient><clipPath id=\"aboveWorktop\"><rect x=\"0\" y=\"0\" width=\"260\" height=\"48\"/></clipPath></defs>");
            sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#fbfcfd\"/>");
            sb.AppendLine("<rect x=\"18\" y=\"48\" width=\"152\" height=\"34\" rx=\"2\" fill=\"#f7f7f4\" stroke=\"#667085\" stroke-width=\"2\"/>");
            sb.AppendLine("<text x=\"24\" y=\"76\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"15\" font-weight=\"700\" fill=\"#475467\">HPL blad</text>");
            sb.AppendLine("<rect x=\"104\" y=\"53\" width=\"34\" height=\"46\" rx=\"3\" fill=\"#87929a\" stroke=\"#344054\" stroke-width=\"2\"/>");
            sb.AppendLine("<rect x=\"99\" y=\"48\" width=\"44\" height=\"5\" rx=\"2\" fill=\"#aab3b9\" stroke=\"#344054\" stroke-width=\"2\"/>");
            sb.AppendLine("<circle cx=\"121\" cy=\"58\" r=\"16\" clip-path=\"url(#aboveWorktop)\" fill=\"url(#sectionSteel)\" stroke=\"#344054\" stroke-width=\"2\"/>");
            sb.AppendLine("<line x1=\"183\" y1=\"42\" x2=\"183\" y2=\"48\" stroke=\"#008c95\" stroke-width=\"1.2\" marker-start=\"url(#sectionStart)\" marker-end=\"url(#sectionEnd)\"/>");
            sb.AppendLine("<line x1=\"169\" y1=\"42\" x2=\"194\" y2=\"42\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"169\" y1=\"48\" x2=\"194\" y2=\"48\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<text x=\"194\" y=\"43\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"18\" font-weight=\"500\" fill=\"#007f87\">+" + Millimetres(pattern.WorkingHeightMm) + " mm</text>");
            sb.AppendLine("<text x=\"194\" y=\"59\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"13\" fill=\"#475467\">boven blad</text>");
            sb.AppendLine("<text x=\"99\" y=\"116\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"14\" font-weight=\"700\" fill=\"#475467\">verzonken</text>");
            sb.AppendLine("</svg>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string BuildBallTransferPatternDetailSvg(BallTransferPatternInfo pattern, string folder, string fileName)
        {
            var path = Path.Combine(folder, fileName);
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1000\" height=\"150\" viewBox=\"0 0 1000 150\">");
            sb.AppendLine("<defs><marker id=\"cadStart\" markerWidth=\"8\" markerHeight=\"7\" refX=\"0.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M0,3.5 L7,1 L7,6 Z\" fill=\"#008c95\"/></marker><marker id=\"cadEnd\" markerWidth=\"8\" markerHeight=\"7\" refX=\"6.5\" refY=\"3.5\" orient=\"auto\" markerUnits=\"userSpaceOnUse\"><path d=\"M7,3.5 L0,1 L0,6 Z\" fill=\"#008c95\"/></marker><radialGradient id=\"detailSteel\" cx=\"34%\" cy=\"28%\" r=\"72%\"><stop offset=\"0%\" stop-color=\"#ffffff\"/><stop offset=\"35%\" stop-color=\"#dfe5e8\"/><stop offset=\"100%\" stop-color=\"#59636b\"/></radialGradient></defs>");
            sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#fbfcfd\"/>");
            sb.AppendLine("<line x1=\"650\" y1=\"12\" x2=\"650\" y2=\"138\" stroke=\"#d9e0e5\" stroke-width=\"2\"/>");
            sb.AppendLine("<text x=\"20\" y=\"23\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"18\" font-weight=\"700\" fill=\"#667085\">HARTMATEN</text>");
            sb.AppendLine("<text x=\"680\" y=\"23\" font-family=\"Aptos,Arial,sans-serif\" font-size=\"18\" font-weight=\"700\" fill=\"#667085\">RANDMATEN</text>");

            foreach (var point in new[] { new[] { 260, 58 }, new[] { 420, 58 }, new[] { 580, 58 }, new[] { 340, 118 }, new[] { 500, 118 } })
                sb.AppendLine("<circle cx=\"" + point[0] + "\" cy=\"" + point[1] + "\" r=\"10\" fill=\"url(#detailSteel)\" stroke=\"#344054\" stroke-width=\"2\"/>");

            AppendDetailHorizontalDimension(sb, 260, 420, 34, 58, 58, Millimetres(pattern.PitchXmm) + " mm h.o.h.");
            AppendDetailVerticalDimension(sb, 220, 58, 118, 260, 340, Millimetres(pattern.PitchZmm) + " mm h.o.h.");
            AppendDetailHorizontalDimension(sb, 260, 340, 92, 58, 118, Millimetres(pattern.StaggerMm) + " mm verspringing", 350, null);

            sb.AppendLine("<path d=\"M705 38 H945 M705 38 V132\" fill=\"none\" stroke=\"#475467\" stroke-width=\"2\"/>");
            sb.AppendLine("<circle cx=\"815\" cy=\"105\" r=\"10\" fill=\"url(#detailSteel)\" stroke=\"#344054\" stroke-width=\"2\"/>");
            AppendDetailHorizontalDimension(sb, 705, 815, 128, 132, 105, Millimetres(pattern.EdgeXmm) + " mm hart-zijrand", null, 144);
            AppendDetailVerticalDimension(sb, 920, 38, 105, 945, 815, Millimetres(pattern.EdgeZmm) + " mm hart-rand", 950, null);
            sb.AppendLine("</svg>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static void AppendDetailHorizontalDimension(StringBuilder sb, double x1, double x2, double y, double extensionY1, double extensionY2, string label, double? labelX = null, double? labelY = null)
        {
            sb.AppendLine("<line x1=\"" + F(x1) + "\" y1=\"" + F(extensionY1) + "\" x2=\"" + F(x1) + "\" y2=\"" + F(y) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(x2) + "\" y1=\"" + F(extensionY2) + "\" x2=\"" + F(x2) + "\" y2=\"" + F(y) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(x1) + "\" y1=\"" + F(y) + "\" x2=\"" + F(x2) + "\" y2=\"" + F(y) + "\" stroke=\"#008c95\" stroke-width=\"1.2\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            sb.AppendLine("<text x=\"" + F(labelX ?? (x1 + x2) / 2.0) + "\" y=\"" + F(labelY ?? y - 7) + "\" text-anchor=\"middle\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"20\" font-weight=\"500\" fill=\"#007f87\">" + label + "</text>");
        }

        private static void AppendDetailVerticalDimension(StringBuilder sb, double x, double y1, double y2, double extensionX1, double extensionX2, string label, double? labelX = null, double? labelY = null)
        {
            sb.AppendLine("<line x1=\"" + F(extensionX1) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x) + "\" y2=\"" + F(y1) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(extensionX2) + "\" y1=\"" + F(y2) + "\" x2=\"" + F(x) + "\" y2=\"" + F(y2) + "\" stroke=\"#008c95\" stroke-width=\"1\"/>");
            sb.AppendLine("<line x1=\"" + F(x) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x) + "\" y2=\"" + F(y2) + "\" stroke=\"#008c95\" stroke-width=\"1.2\" marker-start=\"url(#cadStart)\" marker-end=\"url(#cadEnd)\"/>");
            sb.AppendLine("<text transform=\"translate(" + F(labelX ?? x - 8) + " " + F(labelY ?? (y1 + y2) / 2.0) + ") rotate(-90)\" text-anchor=\"middle\" font-family=\"Aptos Narrow,Aptos,Arial,sans-serif\" font-size=\"20\" font-weight=\"500\" fill=\"#007f87\">" + label + "</text>");
        }

        private static string OrthographicStyle(PortalAssemblyPart part)
        {
            var name = (part.Name ?? "").ToLowerInvariant();
            if (name.Contains("kogelpotblad")) return "#f7f7f4";
            if (name.Contains("kogelpot")) return "url(#steelBall)";
            if (name.Contains("stabilisatieplaat")) return "#f2f1ec";
            if (name.Contains("afdekkap") || name.Contains("eindstop")) return "#1c1f23";
            if (name.Contains("hte2")) return "#737c83";
            if (name.Contains("hsr15")) return "#7b858e";
            if (name.Contains("adapterplaat") || string.Equals(part.Kind, "profile", StringComparison.OrdinalIgnoreCase)) return "#b7bec4";
            if (string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase)) return "#f2f1ec";
            return "#c4c9ce";
        }

        private static int OrthographicLayerPriority(PortalAssemblyPart part)
        {
            var name = (part == null ? "" : part.Name ?? "").ToLowerInvariant();

            // In voor- en zijaanzicht delen de hefkolom en het vaste zijprofiel
            // dezelfde projectiediepte. Teken de kolom eerst en het draagprofiel
            // daarna, zodat de kolom zichtbaar tegen de onderzijde van het profiel
            // eindigt in plaats van er optisch doorheen te lopen.
            if (name.Contains("hte2 kolom")) return 0;
            if (name.Contains("hte2 o1")) return 1;
            if (name.Contains("vast railframe")) return 3;
            if (name.Contains("bewegend buitenframe") || name.Contains("werkbladhouder")) return 4;
            if (name.Contains("kogelpotblad")) return 5;
            return 2;
        }

        private static bool IsRoundTopComponent(PortalAssemblyPart part)
        {
            var name = (part.Name ?? "").ToLowerInvariant();
            return name.Contains("kogelpot") && !name.Contains("kogelpotblad");
        }

        private static double DetermineMovementRange(IEnumerable<PortalAssemblyPart> parts)
        {
            var positions = (parts ?? Enumerable.Empty<PortalAssemblyPart>())
                .Where(item => item != null && (item.Name ?? "").IndexOf("borgpositie", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.Xmm)
                .ToArray();
            if (positions.Length >= 2) return Math.Max(0, positions.Max() - positions.Min());
            throw new InvalidOperationException("De horizontale bewegingsgrenzen ontbreken in de LEX-assemblydata.");
        }

        private static BallTransferPatternInfo BuildBallTransferPattern(IEnumerable<PortalAssemblyPart> parts)
        {
            var list = (parts ?? Enumerable.Empty<PortalAssemblyPart>()).Where(item => item != null).ToList();
            var worktop = list.FirstOrDefault(item => (item.Name ?? "").IndexOf("kogelpotblad", StringComparison.OrdinalIgnoreCase) >= 0);
            var balls = list.Where(IsRoundTopComponent).ToList();
            if (worktop == null || balls.Count == 0) return null;

            var rows = balls
                .GroupBy(item => Math.Round(item.Zmm, 3))
                .OrderBy(group => group.Key)
                .ToList();
            var fullestRow = rows.OrderByDescending(group => group.Count()).First();
            var pitchX = SmallestPositiveGap(fullestRow.Select(item => item.Xmm));
            var pitchZ = SmallestPositiveGap(rows.Select(group => group.Key));
            var rowStarts = rows.Select(group => group.Min(item => item.Xmm)).OrderBy(value => value).ToArray();
            var stagger = SmallestPositiveGap(rowStarts);
            if (stagger <= 0 && pitchX > 0) stagger = pitchX / 2.0;
            var first = balls[0];
            return new BallTransferPatternInfo
            {
                Count = balls.Count,
                PitchXmm = pitchX,
                PitchZmm = pitchZ,
                StaggerMm = stagger,
                EdgeXmm = Math.Max(0, worktop.SizeXmm / 2.0 - balls.Max(item => Math.Abs(item.Xmm - worktop.Xmm))),
                EdgeZmm = Math.Max(0, worktop.SizeZmm / 2.0 - balls.Max(item => Math.Abs(item.Zmm - worktop.Zmm))),
                BodyDiameterMm = first.BodyDiameterMm,
                FlangeDiameterMm = first.FlangeDiameterMm,
                BallDiameterMm = first.BallDiameterMm,
                InsertionLengthMm = first.InsertionLengthMm,
                WorkingHeightMm = first.WorkingHeightMm
            };
        }

        private static double SmallestPositiveGap(IEnumerable<double> values)
        {
            var ordered = (values ?? Enumerable.Empty<double>()).Distinct().OrderBy(value => value).ToArray();
            var minimum = double.MaxValue;
            for (var index = 1; index < ordered.Length; index++)
            {
                var gap = ordered[index] - ordered[index - 1];
                if (gap > 0.001) minimum = Math.Min(minimum, gap);
            }
            return minimum == double.MaxValue ? 0 : minimum;
        }

        private sealed class BallTransferPatternInfo
        {
            public int Count { get; set; }
            public double PitchXmm { get; set; }
            public double PitchZmm { get; set; }
            public double StaggerMm { get; set; }
            public double EdgeXmm { get; set; }
            public double EdgeZmm { get; set; }
            public double BodyDiameterMm { get; set; }
            public double FlangeDiameterMm { get; set; }
            public double BallDiameterMm { get; set; }
            public double InsertionLengthMm { get; set; }
            public double WorkingHeightMm { get; set; }
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public static string CustomerPowerPointPath(string glbPath)
        {
            return CustomerPowerPointPath(glbPath, null);
        }

        public static string CustomerPowerPointPath(string glbPath, PortalQuoteRequest request)
        {
            var folder = Path.GetDirectoryName(glbPath) ?? "";
            return Path.Combine(folder, CustomerDocumentStem(request) + "_KLANTPRESENTATIE.pptx");
        }

        public static string CustomerPdfPath(string glbPath)
        {
            return CustomerPdfPath(glbPath, null);
        }

        public static string CustomerPdfPath(string glbPath, PortalQuoteRequest request)
        {
            var folder = Path.GetDirectoryName(glbPath) ?? "";
            return Path.Combine(folder, CustomerDocumentStem(request) + "_KLANTBIJLAGE.pdf");
        }

        private static void BuildModelPages(dynamic presentation, string glbPath, string temporaryFolder, SolidWorksCustomerPresentationProfile profile)
        {
            dynamic orbitSlide = presentation.Slides.Item(2);
            dynamic orbitPlaceholder = FindShape(orbitSlide, "MODEL_ORBIT");
            var left = (float)orbitPlaceholder.Left;
            var top = (float)orbitPlaceholder.Top;
            var width = (float)orbitPlaceholder.Width;
            var height = (float)orbitPlaceholder.Height;
            DeleteShape(orbitSlide, "MODEL_ORBIT_LABEL");
            orbitPlaceholder.Delete();

            dynamic modelShape = orbitSlide.Shapes.Add3DModel(
                glbPath,
                MsoFalse,
                MsoTrue,
                left,
                top,
                width,
                height);
            modelShape.Name = "MODEL_ORBIT";
            modelShape.AlternativeText = profile.CoverImageAltText;

            // Gebruik bewust twee verschillende klantcamera's. Het hogere, rustige
            // 3/4-aanzicht werkt als hoofdrender; de tweede hoek geeft de
            // voordelenpagina een eigen beeld en voorkomt visuele herhaling.
            var coverPerspectivePath = RenderModelView(modelShape, temporaryFolder, "iso-cover", profile.CoverRotationX, profile.CoverRotationY, profile.CoverRotationZ);
            var benefitsPerspectivePath = RenderModelView(modelShape, temporaryFolder, "iso-benefits", profile.BenefitsRotationX, profile.BenefitsRotationY, profile.BenefitsRotationZ);
            PlacePreview(presentation.Slides.Item(1), "MODEL_COVER", coverPerspectivePath, profile.CoverImageAltText);
            BuildBenefitsPage(presentation.Slides.Item(2), benefitsPerspectivePath, profile);

            modelShape.Delete();
        }

        private static string RenderModelView(dynamic modelShape, string folder, string name, double rotationX, double rotationY, double rotationZ)
        {
            SetRotation(modelShape, rotationX, rotationY, rotationZ);
            Thread.Sleep(180);
            var rawPath = Path.Combine(folder, name + "-raw.png");
            var path = Path.Combine(folder, name + ".png");
            modelShape.Export(rawPath, PpShapeFormatPng);
            if (!File.Exists(rawPath))
                throw new InvalidOperationException("PowerPoint kon het " + name + "-aanzicht niet als afbeelding vastleggen.");
            PolishCustomerRender(rawPath, path);
            return path;
        }

        private static void PolishCustomerRender(string sourcePath, string destinationPath)
        {
            // De neutrale PowerPoint-belichting maakt licht HPL en aluminium snel
            // vrijwel wit. Een beperkte contrastcorrectie houdt wit wit, maar
            // maakt profielvlakken, rails en schaduwen beter leesbaar. De subtiele
            // warme balans voorkomt een klinisch blauwe aluminiumweergave.
            const float contrast = 1.09f;
            var midpoint = 0.5f * (1f - contrast);
            var colorMatrix = new ColorMatrix(new[]
            {
                new[] { contrast, 0f, 0f, 0f, 0f },
                new[] { 0f, contrast, 0f, 0f, 0f },
                new[] { 0f, 0f, contrast, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { midpoint + 0.010f, midpoint, midpoint - 0.008f, 0f, 1f }
            });

            using (var source = new Bitmap(sourcePath))
            using (var adjusted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(adjusted))
            using (var attributes = new ImageAttributes())
            {
                adjusted.SetResolution(source.HorizontalResolution, source.VerticalResolution);
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                // De render staat naadloos op dezelfde achtergrond als de pagina.
                // Een dunne alpha-contour houdt het witte HPL herkenbaar; de lage,
                // diffuse slagschaduw geeft diepte zonder opnieuw een tegel te maken.
                DrawRenderSilhouette(graphics, source, -2, 0, 0.045f);
                DrawRenderSilhouette(graphics, source, 2, 0, 0.045f);
                DrawRenderSilhouette(graphics, source, 0, -2, 0.045f);
                DrawRenderSilhouette(graphics, source, 0, 2, 0.045f);
                DrawRenderSilhouette(graphics, source, -1, -1, 0.035f);
                DrawRenderSilhouette(graphics, source, 1, -1, 0.035f);
                DrawRenderSilhouette(graphics, source, -1, 1, 0.035f);
                DrawRenderSilhouette(graphics, source, 1, 1, 0.035f);
                DrawRenderSilhouette(graphics, source, -5, 8, 0.018f);
                DrawRenderSilhouette(graphics, source, 0, 10, 0.026f);
                DrawRenderSilhouette(graphics, source, 5, 8, 0.018f);
                DrawRenderSilhouette(graphics, source, 0, 14, 0.012f);

                attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                graphics.DrawImage(
                    source,
                    new Rectangle(0, 0, adjusted.Width, adjusted.Height),
                    0,
                    0,
                    source.Width,
                    source.Height,
                    GraphicsUnit.Pixel,
                    attributes);
                adjusted.Save(destinationPath, ImageFormat.Png);
            }
        }

        private static void DrawRenderSilhouette(Graphics graphics, Image source, int offsetX, int offsetY, float opacity)
        {
            var shadowMatrix = new ColorMatrix(new[]
            {
                new[] { 0f, 0f, 0f, 0f, 0f },
                new[] { 0f, 0f, 0f, 0f, 0f },
                new[] { 0f, 0f, 0f, 0f, 0f },
                new[] { 0f, 0f, 0f, opacity, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            });
            using (var shadowAttributes = new ImageAttributes())
            {
                shadowAttributes.SetColorMatrix(shadowMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                graphics.DrawImage(
                    source,
                    new Rectangle(offsetX, offsetY, source.Width, source.Height),
                    0,
                    0,
                    source.Width,
                    source.Height,
                    GraphicsUnit.Pixel,
                    shadowAttributes);
            }
        }

        private static void SetRotation(dynamic modelShape, double rotationX, double rotationY, double rotationZ)
        {
            dynamic format = modelShape.Model3D;
            format.ResetModel(false);
            format.AutoFit = MsoTrue;
            format.RotationX = rotationX;
            format.RotationY = rotationY;
            format.RotationZ = rotationZ;
        }

        private static void BuildBenefitsPage(dynamic slide, string perspectivePath, SolidWorksCustomerPresentationProfile profile)
        {
            SetShapeText(slide, "EYEBROW_2", profile.BenefitsEyebrow);
            SetShapeText(slide, "ORBIT_TITLE", profile.BenefitsTitle);
            SetShapeText(slide, "ORBIT_SUBTITLE", profile.BenefitsIntroduction);
            DeleteShape(slide, "MODEL_ORBIT_BACKING");
            DeleteShape(slide, "MODEL_ORBIT_LABEL");
            DeleteShape(slide, "ORBIT_HINT_BAR");
            DeleteShape(slide, "ORBIT_HINT");

            var benefits = profile.Benefits ?? new CustomerPresentationBenefit[0];
            for (var index = 0; index < Math.Min(4, benefits.Count); index++)
            {
                var benefit = benefits[index];
                AddBenefitRow(slide, 42, 154 + (index * 71), 456, benefit.Number, benefit.Title, benefit.Body);
            }
            DeleteShape(slide, "MODEL_BENEFITS_BACKDROP");
            AddPictureContained(slide, perspectivePath, 535, 142, 360, 328, "MODEL_BENEFITS", profile.BenefitsImageAltText);
        }

        private static void AddBenefitRow(dynamic slide, float left, float top, float width, string number, string title, string body)
        {
            dynamic numberBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top + 1, 34, 20);
            numberBox.TextFrame.TextRange.Text = number;
            FormatText(numberBox, 10, true, OfficeRgb(0, 113, 227));

            dynamic titleBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left + 44, top, width - 44, 23);
            titleBox.TextFrame.TextRange.Text = title;
            FormatText(titleBox, 13, true, OfficeRgb(29, 29, 31));

            dynamic bodyBox = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left + 44, top + 25, width - 44, 32);
            bodyBox.TextFrame.TextRange.Text = body;
            FormatText(bodyBox, 10.5f, false, OfficeRgb(82, 82, 88));

            dynamic line = slide.Shapes.AddLine(left + 44, top + 62, left + width, top + 62);
            line.Line.ForeColor.RGB = OfficeRgb(224, 228, 232);
            line.Line.Weight = 0.8f;
        }

        private static void PrepareStaticSlides(dynamic presentation, PortalQuoteRequest request, WorkbenchModel model, SolidWorksCustomerPresentationProfile profile)
        {
            dynamic cover = presentation.Slides.Item(1);
            SetShapeText(cover, "EYEBROW_1", "PROJECTVOORSTEL VOOR " + CustomerName(request.CustomerName).ToUpperInvariant());
            SetShapeText(cover, "COVER_TITLE", profile.DisplayName);
            SetShapeText(cover, "COVER_SUBTITLE", profile.CoverSubtitle);
            SetShapeText(cover, "COVER_MATERIAL", profile.CoverPromise);
            SetShapeText(cover, "COVER_CUSTOMER", "Concept bij offerte · " + DateTime.Today.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("nl-NL")));
            DeleteShapesWithPrefixes(cover, "COVER_WIDTH_", "COVER_DEPTH_", "COVER_HEIGHT_");
            MoveShape(cover, "COVER_MATERIAL", 42, 319, 360, 54);
            MoveShape(cover, "COVER_CUSTOMER", 42, 411, 315, 21);

            dynamic specification = presentation.Slides.Item(4);
            BuildSpecificationPage(specification, profile);

            dynamic drawing = presentation.Slides.Item(5);
            SetShapeText(drawing, "EYEBROW_5", profile.DrawingEyebrow);
            SetShapeText(drawing, "DRAWING_TITLE_5", profile.DrawingTitle);
            SetShapeText(drawing, "DRAWING_INTRO_5", profile.DrawingIntroduction);
            SetShapeText(drawing, "DRAWING_NOTE_5", profile.DrawingNote);
            if (profile.ShowBallTransferDetails && presentation.Slides.Count >= 6)
            {
                dynamic detail = presentation.Slides.Item(6);
                SetShapeText(detail, "EYEBROW_5", profile.DetailEyebrow);
                SetShapeText(detail, "DRAWING_TITLE_5", profile.DetailTitle);
                SetShapeText(detail, "DRAWING_INTRO_5", profile.DetailIntroduction);
                SetShapeText(detail, "DRAWING_NOTE_5", profile.DetailNote);
            }
            presentation.Slides.Item(3).Delete();
            RenumberSlides(presentation);
        }

        private static void RenumberSlides(dynamic presentation)
        {
            for (var index = 1; index <= presentation.Slides.Count; index++)
            {
                dynamic slide = presentation.Slides.Item(index);
                for (var shapeIndex = 1; shapeIndex <= slide.Shapes.Count; shapeIndex++)
                {
                    dynamic shape = slide.Shapes.Item(shapeIndex);
                    if (!((string)shape.Name).StartsWith("PAGE_", StringComparison.Ordinal)) continue;
                    shape.TextFrame.TextRange.Text = index.ToString("00", CultureInfo.InvariantCulture);
                    break;
                }
            }
        }

        private static void BuildSpecificationPage(dynamic slide, SolidWorksCustomerPresentationProfile profile)
        {
            DeleteShapesWithPrefixes(slide, "SPEC_", "DIMENSION_", "APPROVAL_");
            SetShapeText(slide, "EYEBROW_4", profile.SpecificationEyebrow);
            AddText(slide, "SPEC_NEW_TITLE", 42, 46, 832, 54, profile.SpecificationTitle, 27, true, OfficeRgb(27, 37, 42));
            AddText(slide, "SPEC_NEW_INTRO", 42, 91, 840, 36, profile.SpecificationIntroduction, 12, false, OfficeRgb(101, 111, 118));

            var specifications = profile.Specifications ?? new CustomerPresentationSpecification[0];
            for (var index = 0; index < Math.Min(4, specifications.Count); index++)
            {
                var specification = specifications[index];
                AddSpecificationRow(slide, 42, 151 + (index * 75), 505, specification.Label, specification.Body);
            }

            dynamic divider = slide.Shapes.AddLine(582, 145, 582, 463);
            divider.Line.ForeColor.RGB = OfficeRgb(220, 224, 229);
            divider.Line.Weight = 1f;
            AddText(slide, "SCOPE_TITLE", 615, 151, 270, 28, profile.ScopeTitle, 17, true, OfficeRgb(27, 37, 42));
            AddText(slide, "SCOPE_BODY", 615, 197, 268, 150,
                profile.ScopeBody,
                12, false, OfficeRgb(82, 82, 88));
            AddText(slide, "SCOPE_APPROVAL", 615, 365, 268, 76,
                profile.ScopeApprovalText,
                12, true, OfficeRgb(0, 91, 184));
        }

        private static void AddSpecificationRow(dynamic slide, float left, float top, float width, string title, string body)
        {
            AddText(slide, "SPEC_ROW_TITLE_" + title, left, top, 155, 20, title, 9.5f, true, OfficeRgb(0, 113, 227));
            AddText(slide, "SPEC_ROW_BODY_" + title, left + 166, top - 2, width - 166, 48, body, 11.5f, false, OfficeRgb(53, 60, 64));
            dynamic line = slide.Shapes.AddLine(left + 166, top + 57, left + width, top + 57);
            line.Line.ForeColor.RGB = OfficeRgb(224, 228, 232);
            line.Line.Weight = 0.8f;
        }

        private static dynamic AddText(dynamic slide, string name, float left, float top, float width, float height, string text, float size, bool bold, int color)
        {
            dynamic shape = slide.Shapes.AddTextbox(MsoTextOrientationHorizontal, left, top, width, height);
            shape.Name = name;
            shape.TextFrame.TextRange.Text = text;
            FormatText(shape, size, bold, color);
            return shape;
        }

        private static void FormatText(dynamic shape, float fontSize, bool bold, int color)
        {
            shape.TextFrame.MarginLeft = 0;
            shape.TextFrame.MarginRight = 0;
            shape.TextFrame.MarginTop = 0;
            shape.TextFrame.MarginBottom = 0;
            shape.TextFrame.WordWrap = MsoTrue;
            shape.TextFrame.TextRange.Font.Name = "Aptos";
            shape.TextFrame.TextRange.Font.Size = fontSize;
            shape.TextFrame.TextRange.Font.Bold = bold ? MsoTrue : MsoFalse;
            shape.TextFrame.TextRange.Font.Color.RGB = color;
        }

        private static int OfficeRgb(int red, int green, int blue)
        {
            return red + (green << 8) + (blue << 16);
        }

        private static void PlacePreview(dynamic slide, string placeholderName, string imagePath, string altText)
        {
            dynamic placeholder = FindShape(slide, placeholderName);
            var left = (float)placeholder.Left;
            var top = (float)placeholder.Top;
            var width = (float)placeholder.Width;
            var height = (float)placeholder.Height;
            DeleteShape(slide, placeholderName + "_LABEL");
            DeleteShape(slide, placeholderName + "_BACKING");
            DeleteShape(slide, placeholderName + "_BACKDROP");
            placeholder.Delete();
            dynamic image = slide.Shapes.AddPicture(imagePath, MsoFalse, MsoTrue, left, top, width, height);
            image.Name = placeholderName;
            image.AlternativeText = altText;
        }

        private static void PlacePreviewContained(dynamic slide, string placeholderName, string imagePath, string altText)
        {
            dynamic placeholder = FindShape(slide, placeholderName);
            var left = (float)placeholder.Left;
            var top = (float)placeholder.Top;
            var width = (float)placeholder.Width;
            var height = (float)placeholder.Height;
            DeleteShape(slide, placeholderName + "_LABEL");
            placeholder.Delete();
            AddPictureContained(slide, imagePath, left, top, width, height, placeholderName, altText);
        }

        private static void AddPictureContained(dynamic slide, string imagePath, float left, float top, float width, float height, string name, string altText)
        {
            float imageWidth;
            float imageHeight;
            using (var source = Image.FromFile(imagePath))
            {
                var scale = Math.Min(width / source.Width, height / source.Height);
                imageWidth = (float)(source.Width * scale);
                imageHeight = (float)(source.Height * scale);
            }

            var imageLeft = left + (width - imageWidth) / 2f;
            var imageTop = top + (height - imageHeight) / 2f;
            dynamic image = slide.Shapes.AddPicture(imagePath, MsoFalse, MsoTrue, imageLeft, imageTop, imageWidth, imageHeight);
            image.Name = name;
            image.AlternativeText = altText;
        }

        private static void SetShapeText(dynamic slide, string shapeName, string text)
        {
            try
            {
                dynamic shape = FindShape(slide, shapeName);
                shape.TextFrame.TextRange.Text = text ?? "";
            }
            catch (InvalidOperationException)
            {
                // Een optioneel tekstveld kan in een oudere template ontbreken.
            }
        }

        private static dynamic FindShape(dynamic slide, string name)
        {
            for (var index = 1; index <= slide.Shapes.Count; index++)
            {
                dynamic shape = slide.Shapes.Item(index);
                if (string.Equals((string)shape.Name, name, StringComparison.Ordinal)) return shape;
            }
            throw new InvalidOperationException("PowerPoint-template bevat de vereiste vorm niet: " + name);
        }

        private static void DeleteShape(dynamic slide, string name)
        {
            for (var index = slide.Shapes.Count; index >= 1; index--)
            {
                dynamic shape = slide.Shapes.Item(index);
                if (!string.Equals((string)shape.Name, name, StringComparison.Ordinal)) continue;
                shape.Delete();
                return;
            }
        }

        private static void DeleteShapesWithPrefixes(dynamic slide, params string[] prefixes)
        {
            for (var index = slide.Shapes.Count; index >= 1; index--)
            {
                dynamic shape = slide.Shapes.Item(index);
                var name = (string)shape.Name;
                if (prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))) shape.Delete();
            }
        }

        private static void MoveShape(dynamic slide, string name, float left, float top, float width, float height)
        {
            try
            {
                dynamic shape = FindShape(slide, name);
                shape.Left = left;
                shape.Top = top;
                shape.Width = width;
                shape.Height = height;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void ReplaceTokens(dynamic presentation, IDictionary<string, string> tokens)
        {
            for (var slideIndex = 1; slideIndex <= presentation.Slides.Count; slideIndex++)
            {
                dynamic slide = presentation.Slides.Item(slideIndex);
                for (var shapeIndex = 1; shapeIndex <= slide.Shapes.Count; shapeIndex++)
                {
                    dynamic shape = slide.Shapes.Item(shapeIndex);
                    ReplaceShapeTokens(shape, tokens);
                }
            }
        }

        private static void ReplaceShapeTokens(dynamic shape, IDictionary<string, string> tokens)
        {
            try
            {
                if (shape.Type == 6)
                {
                    for (var index = 1; index <= shape.GroupItems.Count; index++)
                        ReplaceShapeTokens(shape.GroupItems.Item(index), tokens);
                    return;
                }

                if (shape.HasTextFrame != MsoTrue || shape.TextFrame.HasText != MsoTrue) return;
                var text = (string)shape.TextFrame.TextRange.Text;
                foreach (var token in tokens) text = text.Replace("{{" + token.Key + "}}", token.Value ?? "");
                shape.TextFrame.TextRange.Text = text;
            }
            catch
            {
                // Media- en modelschapes hebben niet altijd alle tekstproperties.
            }
        }

        private static IDictionary<string, string> BuildTokens(PortalQuoteRequest request, WorkbenchModel model, SolidWorksCustomerPresentationProfile profile)
        {
            var sheetMaterials = model.Sheets
                .Where(item => item != null && item.Material != null)
                .Select(item => item.Material.Name)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var profileMaterials = model.Profiles
                .Where(item => item != null && item.Material != null)
                .Select(item => item.Material.Name)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sheetMaterial = sheetMaterials.Length > 0
                ? string.Join(" · ", sheetMaterials)
                : FriendlyMaterial(request.SheetMaterialId, "Plaatmateriaal volgens configuratie");
            var profileMaterial = profileMaterials.Length > 0
                ? string.Join(" · ", profileMaterials)
                : FriendlyMaterial(request.ProfileMaterialId, "Profielmateriaal volgens configuratie");
            var configurationSummary = IsLex(request.Product)
                ? "Lineaire geleiding · elektrische hefkolommen · kogelpotten · complete profielconstructie"
                : "Complete uitvoering volgens de gekozen configuratie";

            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "PRODUCT_NAME", profile.DisplayName },
                { "WIDTH_MM", Millimetres(request.WidthMm) },
                { "DEPTH_MM", Millimetres(request.DepthMm) },
                { "HEIGHT_MM", Millimetres(request.HeightMm) },
                { "QUANTITY", Math.Max(1, request.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "CUSTOMER_NAME", CustomerName(request.CustomerName) },
                { "SHEET_MATERIAL", sheetMaterial },
                { "PROFILE_MATERIAL", profileMaterial },
                { "MATERIAL_SUMMARY", IsLex(request.Product) ? "Elektrisch verstelbaar · verschuifbaar kogelpotblad · stabiele profielconstructie" : sheetMaterial + " · " + profileMaterial },
                { "CONFIGURATION_SUMMARY", configurationSummary }
            };
        }

        private static string FriendlyProductName(string product, string projectName)
        {
            if (string.Equals(product, "werktafel", StringComparison.OrdinalIgnoreCase)) return "Werktafel";
            if (string.Equals(product, "werkbankkast", StringComparison.OrdinalIgnoreCase)) return "Werkbankkast";
            if (string.Equals(product, "vakkenkast", StringComparison.OrdinalIgnoreCase)) return "Vakkenkast";
            if (IsLex(product)) return "Workstation";
            if (!string.IsNullOrWhiteSpace(projectName)) return projectName.Trim();
            return string.IsNullOrWhiteSpace(product) ? "Klantuitvoering" : product.Trim();
        }

        private static string FriendlyMaterial(string materialId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(materialId)) return fallback;
            return materialId.Replace('_', ' ');
        }

        private static bool IsLex(string product)
        {
            return string.Equals(product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product, "lex-werktafel", StringComparison.OrdinalIgnoreCase);
        }

        private static string CustomerName(string value)
        {
            value = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, "Testklant", StringComparison.OrdinalIgnoreCase)
                ? "beoordeling"
                : value;
        }

        private static string ProjectTitle(PortalQuoteRequest request, WorkbenchModel model)
        {
            var requested = request == null ? "" : (request.ProjectName ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(requested)) return requested;
            if (request != null && IsLex(request.Product)) return "Workstation";
            return FriendlyProductName(request == null ? null : request.Product, model == null ? null : model.ProjectName);
        }

        private static string CustomerDocumentStem(PortalQuoteRequest request)
        {
            var customer = request == null ? "KLANT" : CustomerName(request.CustomerName);
            if (string.Equals(customer, "beoordeling", StringComparison.OrdinalIgnoreCase)) customer = "KLANT";
            var project = request == null || string.IsNullOrWhiteSpace(request.ProjectName) ? "WORKSTATION" : request.ProjectName.Trim();
            var value = (customer + "_" + project).ToUpperInvariant();
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            value = value.Replace(' ', '_');
            return value;
        }

        private static string Millimetres(double value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string MillimetresExact(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string ResolveTemplatePath(SolidWorksCustomerPresentationProfile profile)
        {
            var templateFileName = profile == null || string.IsNullOrWhiteSpace(profile.TemplateFileName)
                ? "Klantpresentatie-template.pptx"
                : profile.TemplateFileName;
            var assemblyFolder = Path.GetDirectoryName(typeof(SolidWorksCustomerPowerPointExporter).Assembly.Location);
            var candidates = new[]
            {
                assemblyFolder,
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.CurrentDirectory,
                Path.Combine(Environment.CurrentDirectory, "bin")
            };
            foreach (var folder in candidates.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                var candidate = Path.Combine(folder, "SolidWorksAssets", templateFileName);
                if (File.Exists(candidate)) return candidate;
            }
            return Path.Combine(assemblyFolder ?? AppDomain.CurrentDomain.BaseDirectory, "SolidWorksAssets", templateFileName);
        }

        private static void TryRemoveTemporaryFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch
            {
                // Tijdelijke previews mogen een geslaagde presentatie-export niet blokkeren.
            }
        }

        private static void ReleaseCom(object value)
        {
            if (value == null || !Marshal.IsComObject(value)) return;
            try { Marshal.FinalReleaseComObject(value); }
            catch { }
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
    }
}
