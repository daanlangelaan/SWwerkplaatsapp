using System;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalVisualizationService
    {
        public string BuildProductSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            if (request != null &&
                (string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)))
            {
                return BuildLexWorkbenchSvg(model, request);
            }

            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase))
            {
                return BuildWorkbenchSvg(model, request);
            }

            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                return BuildWorkbenchCabinetSvg(model, request);
            }

            if (request != null && string.Equals(request.Product, "machinebasis", StringComparison.OrdinalIgnoreCase))
            {
                return BuildMachineBaseSvg(model, request);
            }

            if (request != null && string.Equals(request.Product, "robotcel", StringComparison.OrdinalIgnoreCase))
            {
                return BuildRobotCellSvg(model, request);
            }

            return BuildCabinetSvg(model, request);
        }

        private static string BuildRobotCellSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, ProductDefaults.RobotCellWidthMm);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, ProductDefaults.RobotCellWorktopHeightMm);
            const double x = 112;
            const double y = 116;
            const double w = 570;
            const double h = 278;
            var sb = BeginSvg(model, "Robot cel");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">80x80 staanders · staand onderframe met 1 dwarsligger · staand bladframe · T-slotbanen gelijk</text>");
            sb.AppendLine("<rect class=\"floor\" x=\"70\" y=\"448\" width=\"690\" height=\"12\" rx=\"6\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x) + "\" y=\"" + F(y + 82) + "\" width=\"38\" height=\"" + F(h - 52) + "\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x + w - 38) + "\" y=\"" + F(y + 82) + "\" width=\"38\" height=\"" + F(h - 52) + "\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"drawer\" x=\"" + F(x + 38) + "\" y=\"" + F(y + 64) + "\" width=\"" + F(w - 76) + "\" height=\"32\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(x - 8) + "\" y=\"" + F(y + 50) + "\" width=\"" + F(w + 16) + "\" height=\"14\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x + 38) + "\" y=\"" + F(y + 34) + "\" width=\"" + F(w - 76) + "\" height=\"16\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"cap\" x=\"" + F(x + 34) + "\" y=\"" + F(y + 34) + "\" width=\"6\" height=\"16\" rx=\"2\"/><rect class=\"cap\" x=\"" + F(x + w - 40) + "\" y=\"" + F(y + 34) + "\" width=\"6\" height=\"16\" rx=\"2\"/>");
            sb.AppendLine("<rect class=\"drawer\" x=\"" + F(x + 38) + "\" y=\"" + F(y + h - 36) + "\" width=\"" + F(w - 76) + "\" height=\"32\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"profile\" x=\"" + F(x + w / 2.0 - 19) + "\" y=\"" + F(y + h - 36) + "\" width=\"38\" height=\"32\" rx=\"2\"/>");
            sb.AppendLine("<line class=\"divider\" x1=\"" + F(x + 19) + "\" y1=\"" + F(y + h + 28) + "\" x2=\"" + F(x + 19) + "\" y2=\"421\"/><line class=\"divider\" x1=\"" + F(x + w - 19) + "\" y1=\"" + F(y + h + 28) + "\" x2=\"" + F(x + w - 19) + "\" y2=\"421\"/>");
            sb.AppendLine("<circle class=\"knob\" cx=\"" + F(x + 19) + "\" cy=\"429\" r=\"12\"/><circle class=\"knob\" cx=\"" + F(x + w - 19) + "\" cy=\"429\" r=\"12\"/>");
            sb.AppendLine("<text class=\"dim\" x=\"" + F(x + w / 2 - 105) + "\" y=\"488\">" + F(widthMm) + " mm breed · blad " + F(heightMm) + " mm</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string BuildMachineBaseSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, ProductDefaults.MachineBaseWidthMm);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, ProductDefaults.MachineBaseHeightMm);
            var worktopHeightMm = ValueOr(request == null ? 0 : request.MachineBaseWorktopHeightMm, ProductDefaults.MachineBaseWorktopHeightMm);
            const double x = 92;
            const double y = 92;
            const double w = 620;
            const double h = 330;
            var worktopY = y + h - h * worktopHeightMm / heightMm;
            var sb = BeginSvg(model, "Parametrische machinebasis");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">Frame fase 1 · " + F(widthMm) + " breed · bladhoogte " + F(worktopHeightMm) + "</text>");
            sb.AppendLine("<rect class=\"floor\" x=\"62\" y=\"448\" width=\"710\" height=\"12\" rx=\"6\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"18\" height=\"" + F(h) + "\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x + w - 18) + "\" y=\"" + F(y) + "\" width=\"18\" height=\"" + F(h) + "\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(x + 18) + "\" y=\"" + F(y) + "\" width=\"" + F(w - 36) + "\" height=\"14\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"plinth\" x=\"" + F(x + 18) + "\" y=\"" + F(y + h - 16) + "\" width=\"" + F(w - 36) + "\" height=\"16\" rx=\"3\"/>");
            sb.AppendLine("<rect class=\"drawer\" x=\"" + F(x + 18) + "\" y=\"" + F(worktopY - 16) + "\" width=\"" + F(w - 36) + "\" height=\"16\" rx=\"3\"/>");
            sb.AppendLine("<circle class=\"knob\" cx=\"" + F(x + 9) + "\" cy=\"438\" r=\"12\"/><circle class=\"knob\" cx=\"" + F(x + w - 9) + "\" cy=\"438\" r=\"12\"/>");
            sb.AppendLine("<text class=\"dim\" x=\"" + F(x + w / 2 - 75) + "\" y=\"488\">Buitenbreedte " + F(widthMm) + " mm</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string BuildCabinetSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, 2400);
            var depthMm = ValueOr(request == null ? 0 : request.DepthMm, 600);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, 900);
            var units = Math.Max(1, Math.Min(12, request == null ? 4 : request.UnitCount));
            var drawers = Math.Max(0, Math.Min(6, request == null ? 1 : request.DefaultDrawerCount));
            var shelves = Math.Max(0, Math.Min(5, request == null ? 1 : request.DefaultShelfCount));
            var topDrawer = request != null && request.IncludeTopDrawer;
            var sliding = request != null && string.Equals(request.DoorMode, "sliding", StringComparison.OrdinalIgnoreCase);
            var hinged = request != null && (string.Equals(request.DoorMode, "links", StringComparison.OrdinalIgnoreCase) || string.Equals(request.DoorMode, "rechts", StringComparison.OrdinalIgnoreCase));

            var sb = BeginSvg(model, "Cabinet preview");
            sb.AppendLine("<rect class=\"floor\" x=\"70\" y=\"438\" width=\"690\" height=\"14\" rx=\"7\"/>");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">" + model.Sheets.Count + " plaatdelen - " + model.Hardware.Count + " beslagregels - richtvisualisatie</text>");

            var frontX = 82.0;
            var frontY = 120.0;
            var frontW = 520.0;
            var frontH = 270.0;
            var topH = 18.0;
            var plinthH = 34.0;
            var unitW = frontW / units;

            sb.AppendLine("<rect class=\"shadow\" x=\"96\" y=\"405\" width=\"492\" height=\"18\" rx=\"9\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(frontX - 12) + "\" y=\"" + F(frontY - topH) + "\" width=\"" + F(frontW + 24) + "\" height=\"" + F(topH) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(frontX) + "\" y=\"" + F(frontY) + "\" width=\"" + F(frontW) + "\" height=\"" + F(frontH) + "\" rx=\"8\"/>");
            sb.AppendLine("<rect class=\"plinth\" x=\"" + F(frontX + 18) + "\" y=\"" + F(frontY + frontH - plinthH) + "\" width=\"" + F(frontW - 36) + "\" height=\"" + F(plinthH) + "\" rx=\"5\"/>");

            for (var i = 1; i < units; i++)
            {
                var x = frontX + i * unitW;
                sb.AppendLine("<line class=\"divider\" x1=\"" + F(x) + "\" y1=\"" + F(frontY + 8) + "\" x2=\"" + F(x) + "\" y2=\"" + F(frontY + frontH - 8) + "\"/>");
            }

            for (var i = 0; i < units; i++)
            {
                var x = frontX + i * unitW + 10;
                var w = unitW - 20;
                var zoneTop = frontY + 18;
                var zoneBottom = frontY + frontH - plinthH - 12;
                if (topDrawer)
                {
                    sb.AppendLine("<rect class=\"drawer\" x=\"" + F(x) + "\" y=\"" + F(zoneTop) + "\" width=\"" + F(w) + "\" height=\"38\" rx=\"5\"/>");
                    sb.AppendLine("<circle class=\"knob\" cx=\"" + F(x + w / 2) + "\" cy=\"" + F(zoneTop + 19) + "\" r=\"3\"/>");
                    zoneTop += 50;
                }

                if (drawers > 0)
                {
                    var gap = 8.0;
                    var drawerH = Math.Max(22, Math.Min(42, (zoneBottom - zoneTop - gap * (drawers - 1)) / drawers));
                    for (var d = 0; d < drawers; d++)
                    {
                        var y = zoneTop + d * (drawerH + gap);
                        if (y + drawerH > zoneBottom) break;
                        sb.AppendLine("<rect class=\"drawer\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(drawerH) + "\" rx=\"5\"/>");
                        sb.AppendLine("<line class=\"handle\" x1=\"" + F(x + w * 0.32) + "\" y1=\"" + F(y + drawerH / 2) + "\" x2=\"" + F(x + w * 0.68) + "\" y2=\"" + F(y + drawerH / 2) + "\"/>");
                    }
                }
                else if (sliding)
                {
                    sb.AppendLine("<rect class=\"door\" x=\"" + F(x) + "\" y=\"" + F(zoneTop) + "\" width=\"" + F(w * 0.58) + "\" height=\"" + F(zoneBottom - zoneTop) + "\" rx=\"6\"/>");
                    sb.AppendLine("<rect class=\"door alt\" x=\"" + F(x + w * 0.42) + "\" y=\"" + F(zoneTop + 10) + "\" width=\"" + F(w * 0.58) + "\" height=\"" + F(zoneBottom - zoneTop - 10) + "\" rx=\"6\"/>");
                    sb.AppendLine("<line class=\"rail\" x1=\"" + F(x) + "\" y1=\"" + F(zoneTop + 10) + "\" x2=\"" + F(x + w) + "\" y2=\"" + F(zoneTop + 10) + "\"/>");
                    sb.AppendLine("<line class=\"rail\" x1=\"" + F(x) + "\" y1=\"" + F(zoneBottom - 8) + "\" x2=\"" + F(x + w) + "\" y2=\"" + F(zoneBottom - 8) + "\"/>");
                }
                else if (hinged)
                {
                    sb.AppendLine("<rect class=\"door\" x=\"" + F(x) + "\" y=\"" + F(zoneTop) + "\" width=\"" + F(w) + "\" height=\"" + F(zoneBottom - zoneTop) + "\" rx=\"6\"/>");
                    sb.AppendLine("<circle class=\"knob\" cx=\"" + F(x + w - 16) + "\" cy=\"" + F(zoneTop + (zoneBottom - zoneTop) / 2) + "\" r=\"3.5\"/>");
                }
                else
                {
                    var shelfPitch = Math.Min(52.0, (zoneBottom - zoneTop) / (shelves + 1));
                    var startTop = IsShelfStartTop(request == null ? null : request.ShelfStartMode);
                    for (var s = 1; s <= shelves; s++)
                    {
                        var y = startTop
                            ? zoneTop + shelfPitch * s
                            : zoneBottom - shelfPitch * (shelves - s + 1);
                        sb.AppendLine("<line class=\"shelf\" x1=\"" + F(x) + "\" y1=\"" + F(y) + "\" x2=\"" + F(x + w) + "\" y2=\"" + F(y) + "\"/>");
                    }
                }
            }

            DrawDimension(sb, frontX, 414, frontX + frontW, 414, F0(widthMm) + " mm breed");
            DrawVerticalDimension(sb, 46, frontY - topH, 46, frontY + frontH, F0(heightMm) + " mm hoog");

            var sideX = 654.0;
            var sideY = 153.0;
            var sideW = 150.0;
            var sideH = 226.0;
            sb.AppendLine("<text class=\"smallTitle\" x=\"" + F(sideX) + "\" y=\"122\">Zijaanzicht</text>");
            sb.AppendLine("<rect class=\"side\" x=\"" + F(sideX) + "\" y=\"" + F(sideY) + "\" width=\"" + F(sideW) + "\" height=\"" + F(sideH) + "\" rx=\"7\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(sideX - 8) + "\" y=\"" + F(sideY - 17) + "\" width=\"" + F(sideW + 16) + "\" height=\"17\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"plinth\" x=\"" + F(sideX + 14) + "\" y=\"" + F(sideY + sideH - 30) + "\" width=\"" + F(sideW - 28) + "\" height=\"30\" rx=\"5\"/>");
            DrawDimension(sb, sideX, 414, sideX + sideW, 414, F0(depthMm) + " mm diep");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string BuildWorkbenchCabinetSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, 2400);
            var depthMm = ValueOr(request == null ? 0 : request.DepthMm, 600);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, 900);
            var units = Math.Max(1, Math.Min(12, request == null ? 4 : request.UnitCount));
            var plinthMm = ValueOr(request == null ? 0 : request.WorkbenchCabinetPlinthHeightMm, ProductDefaults.WorkbenchCabinetPlinthHeightMm);
            var hasSidePlinth = request != null && (request.WorkbenchCabinetIncludeLeftSidePlinth || request.WorkbenchCabinetIncludeRightSidePlinth);

            var sb = BeginSvg(model, "Werkbank met kastonderbouw preview");
            sb.AppendLine("<rect class=\"floor\" x=\"66\" y=\"438\" width=\"720\" height=\"14\" rx=\"7\"/>");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">Doorlopende bodem - deurparen met T-stijl - losse voorzetplint</text>");

            var x = 82.0;
            var y = 120.0;
            var w = 560.0;
            var h = 270.0;
            var topH = 18.0;
            var plinthH = Math.Max(25.0, Math.Min(52.0, h * plinthMm / heightMm));
            var bodyBottom = y + h - plinthH - 8.0;
            var unitW = w / units;
            var frontRadiusMm = request == null || !request.WorkbenchCabinetFrontPanelCornerRadiusMm.HasValue
                ? ProductDefaults.WorkbenchCabinetFrontPanelCornerRadiusMm
                : request.WorkbenchCabinetFrontPanelCornerRadiusMm.Value;
            var frontRadiusPx = Math.Max(0, frontRadiusMm * w / widthMm);

            sb.AppendLine("<rect class=\"shadow\" x=\"94\" y=\"405\" width=\"536\" height=\"18\" rx=\"9\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(x - 10) + "\" y=\"" + F(y - topH) + "\" width=\"" + F(w + 20) + "\" height=\"" + F(topH) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"case\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h - plinthH) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"plinth\" x=\"" + F(x + 14) + "\" y=\"" + F(y + h - plinthH + 8) + "\" width=\"" + F(w - 28) + "\" height=\"" + F(plinthH - 8) + "\" rx=\"4\"/>");
            sb.AppendLine("<line class=\"divider\" x1=\"" + F(x) + "\" y1=\"" + F(bodyBottom) + "\" x2=\"" + F(x + w) + "\" y2=\"" + F(bodyBottom) + "\"/>");

            for (var i = 0; i < units; i++)
            {
                var doorX = x + i * unitW + 5;
                var doorW = unitW - 10;
                var doorY = y + 8;
                var doorH = bodyBottom - doorY - 6;
                sb.AppendLine("<rect class=\"door\" x=\"" + F(doorX) + "\" y=\"" + F(doorY) + "\" width=\"" + F(doorW) + "\" height=\"" + F(doorH) + "\" rx=\"" + F(frontRadiusPx) + "\"/>");
                var handleX = i % 2 == 0 ? doorX + doorW - 13 : doorX + 13;
                sb.AppendLine("<circle class=\"knob\" cx=\"" + F(handleX) + "\" cy=\"" + F(doorY + doorH / 2.0) + "\" r=\"3.5\"/>");
            }

            for (var boundary = 1; boundary < units; boundary++)
            {
                var bx = x + boundary * unitW;
                if (boundary % 2 == 0)
                {
                    sb.AppendLine("<line class=\"divider\" x1=\"" + F(bx) + "\" y1=\"" + F(y + 3) + "\" x2=\"" + F(bx) + "\" y2=\"" + F(bodyBottom) + "\"/>");
                }
                else
                {
                    sb.AppendLine("<rect class=\"plinth\" x=\"" + F(bx - 4) + "\" y=\"" + F(y + 4) + "\" width=\"8\" height=\"" + F(bodyBottom - y - 4) + "\" rx=\"2\"/>");
                }
            }

            DrawDimension(sb, x, 414, x + w, 414, F0(widthMm) + " mm breed");
            DrawVerticalDimension(sb, 48, y - topH, 48, y + h, F0(heightMm) + " mm hoog");

            var sideX = 692.0;
            var sideY = 150.0;
            var sideW = 130.0;
            var sideH = 226.0;
            sb.AppendLine("<text class=\"smallTitle\" x=\"" + F(sideX) + "\" y=\"122\">Zijaanzicht</text>");
            sb.AppendLine("<rect class=\"side\" x=\"" + F(sideX) + "\" y=\"" + F(sideY) + "\" width=\"" + F(sideW) + "\" height=\"" + F(sideH - 28) + "\" rx=\"6\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(sideX - 7) + "\" y=\"" + F(sideY - 17) + "\" width=\"" + F(sideW + 14) + "\" height=\"17\" rx=\"4\"/>");
            if (hasSidePlinth)
                sb.AppendLine("<rect class=\"plinth\" x=\"" + F(sideX + 10) + "\" y=\"" + F(sideY + sideH - 45) + "\" width=\"" + F(sideW - 20) + "\" height=\"35\" rx=\"4\"/>");
            DrawDimension(sb, sideX, 414, sideX + sideW, 414, F0(depthMm) + " mm diep");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static bool IsShelfStartTop(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            return value == "top" || value == "boven";
        }

        private static string BuildWorkbenchSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, 1500);
            var depthMm = ValueOr(request == null ? 0 : request.DepthMm, 750);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, 900);

            var sb = BeginSvg(model, "Werktafel preview");
            sb.AppendLine("<rect class=\"floor\" x=\"86\" y=\"438\" width=\"700\" height=\"14\" rx=\"7\"/>");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">" + model.Sheets.Count + " plaatdelen - " + model.Profiles.Count + " profieldelen - richtvisualisatie</text>");

            var x = 116.0;
            var y = 150.0;
            var w = 560.0;
            var h = 238.0;
            var legW = 24.0;
            sb.AppendLine("<rect class=\"shadow\" x=\"122\" y=\"404\" width=\"548\" height=\"18\" rx=\"9\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"" + F(x - 18) + "\" y=\"" + F(y - 26) + "\" width=\"" + F(w + 36) + "\" height=\"28\" rx=\"8\"/>");
            sb.AppendLine("<rect class=\"profile\" x=\"" + F(x) + "\" y=\"" + F(y + 16) + "\" width=\"" + F(w) + "\" height=\"18\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"profile\" x=\"" + F(x) + "\" y=\"" + F(y + h - 42) + "\" width=\"" + F(w) + "\" height=\"16\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"leg\" x=\"" + F(x + 22) + "\" y=\"" + F(y + 28) + "\" width=\"" + F(legW) + "\" height=\"" + F(h - 42) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"leg\" x=\"" + F(x + w - 46) + "\" y=\"" + F(y + 28) + "\" width=\"" + F(legW) + "\" height=\"" + F(h - 42) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"leg rear\" x=\"" + F(x + 72) + "\" y=\"" + F(y + 44) + "\" width=\"" + F(legW) + "\" height=\"" + F(h - 58) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"leg rear\" x=\"" + F(x + w - 96) + "\" y=\"" + F(y + 44) + "\" width=\"" + F(legW) + "\" height=\"" + F(h - 58) + "\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"side\" x=\"710\" y=\"170\" width=\"110\" height=\"174\" rx=\"8\"/>");
            sb.AppendLine("<rect class=\"top\" x=\"700\" y=\"142\" width=\"130\" height=\"28\" rx=\"8\"/>");
            DrawDimension(sb, x - 18, 414, x + w + 18, 414, F0(widthMm) + " mm breed");
            DrawVerticalDimension(sb, 62, y - 26, 62, y + h, F0(heightMm) + " mm hoog");
            DrawDimension(sb, 700, 382, 830, 382, F0(depthMm) + " mm diep");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string BuildLexWorkbenchSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            var widthMm = ValueOr(request == null ? 0 : request.WidthMm, ProductDefaults.LexWorkbenchWidthMm);
            var depthMm = ValueOr(request == null ? 0 : request.DepthMm, ProductDefaults.LexWorkbenchDepthMm);
            var heightMm = ValueOr(request == null ? 0 : request.HeightMm, ProductDefaults.LexWorkbenchHeightMm);
            var revolution = request != null && string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase);
            var sb = BeginSvg(model, revolution ? "Werktafel LEX Revolution" : "Werktafel LEX basisontwerp");
            sb.AppendLine("<rect class=\"floor\" x=\"70\" y=\"438\" width=\"720\" height=\"14\" rx=\"7\"/>");
            sb.AppendLine("<text class=\"title\" x=\"54\" y=\"46\">" + Xml(model.ProjectName) + "</text>");
            sb.AppendLine("<text class=\"sub\" x=\"54\" y=\"70\">" + (revolution ? "Revolution ontwikkelvariant - startbasis gelijk aan vrijgegeven LEX" : "Basis v0.1 - HTE2 - HSR15 - 53 kogelpotten - rollenbaan vervallen") + "</text>");
            sb.AppendLine("<rect class=\"shadow\" x=\"126\" y=\"416\" width=\"540\" height=\"12\" rx=\"6\"/>");
            sb.AppendLine("<rect class=\"profile\" x=\"145\" y=\"397\" width=\"155\" height=\"16\" rx=\"4\"/><rect class=\"profile\" x=\"505\" y=\"397\" width=\"155\" height=\"16\" rx=\"4\"/>");
            sb.AppendLine("<rect class=\"leg\" x=\"205\" y=\"188\" width=\"58\" height=\"210\" rx=\"5\"/><rect class=\"leg\" x=\"548\" y=\"188\" width=\"58\" height=\"210\" rx=\"5\"/>");
            sb.AppendLine("<rect class=\"side\" x=\"263\" y=\"248\" width=\"285\" height=\"86\" rx=\"4\"/>");
            sb.AppendLine("<rect class=\"profile\" x=\"120\" y=\"160\" width=\"570\" height=\"28\" rx=\"5\"/>");
            sb.AppendLine("<line class=\"rail\" x1=\"130\" y1=\"153\" x2=\"680\" y2=\"153\"/><rect class=\"top\" x=\"90\" y=\"109\" width=\"650\" height=\"35\" rx=\"7\"/>");
            for (var i = 0; i < 11; i++) sb.AppendLine("<circle class=\"knob\" cx=\"" + F(120 + i * 59) + "\" cy=\"126\" r=\"3\"/>");
            DrawDimension(sb, 90, 418, 740, 418, F0(widthMm) + " mm breed");
            DrawVerticalDimension(sb, 58, 109, 58, 413, F0(heightMm) + "-" + F0(heightMm + 400) + " mm hoog");
            DrawDimension(sb, 750, 382, 835, 382, F0(depthMm) + " mm diep");
            sb.AppendLine("<text class=\"smallTitle\" x=\"282\" y=\"294\">HPL stabilisatieplaat 955 x 240 x 6 - eigen maakdeel</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static StringBuilder BeginSvg(WorkbenchModel model, string label)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"900\" height=\"480\" viewBox=\"0 0 900 480\" role=\"img\" aria-label=\"" + Xml(label) + "\">");
            sb.AppendLine("<style>");
            sb.AppendLine("text{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif}");
            sb.AppendLine(".title{font-size:22px;font-weight:750;fill:#14171a}.sub{font-size:13px;fill:#667085}.smallTitle{font-size:13px;font-weight:700;fill:#475467}");
            sb.AppendLine(".case,.side{fill:#f6f2e9;stroke:#9c8f7a;stroke-width:1.5}.top{fill:#d9b98f;stroke:#8b6f48;stroke-width:1.5}.plinth{fill:#b8a994;stroke:#7a6f60;stroke-width:1.2}");
            sb.AppendLine(".drawer{fill:#fbfaf7;stroke:#9c8f7a;stroke-width:1.1}.door{fill:#ede7dc;stroke:#9c8f7a;stroke-width:1.1}.door.alt{fill:#e1d8ca}.divider,.shelf{stroke:#9c8f7a;stroke-width:1.5}.handle,.rail{stroke:#475467;stroke-width:2;stroke-linecap:round}.knob{fill:#475467}");
            sb.AppendLine(".profile,.leg{fill:#d8dde5;stroke:#697586;stroke-width:1.5}.rear{fill:#c8d0da}.cap{fill:#090b0d;stroke:#090b0d;stroke-width:1}.shadow,.floor{fill:#e9edf2}.dim{stroke:#667085;stroke-width:1;marker-start:url(#a);marker-end:url(#a)}.dimText{font-size:12px;fill:#475467;font-weight:650}");
            sb.AppendLine("</style><defs><marker id=\"a\" markerWidth=\"6\" markerHeight=\"6\" refX=\"3\" refY=\"3\" orient=\"auto\"><path d=\"M0,3 L6,0 L6,6 Z\" fill=\"#667085\"/></marker></defs>");
            return sb;
        }

        private static void DrawDimension(StringBuilder sb, double x1, double y1, double x2, double y2, string label)
        {
            sb.AppendLine("<line class=\"dim\" x1=\"" + F(x1) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x2) + "\" y2=\"" + F(y2) + "\"/>");
            sb.AppendLine("<text class=\"dimText\" text-anchor=\"middle\" x=\"" + F((x1 + x2) / 2.0) + "\" y=\"" + F(y1 + 20) + "\">" + Xml(label) + "</text>");
        }

        private static void DrawVerticalDimension(StringBuilder sb, double x1, double y1, double x2, double y2, string label)
        {
            sb.AppendLine("<line class=\"dim\" x1=\"" + F(x1) + "\" y1=\"" + F(y1) + "\" x2=\"" + F(x2) + "\" y2=\"" + F(y2) + "\"/>");
            sb.AppendLine("<text class=\"dimText\" transform=\"translate(" + F(x1 - 16) + " " + F((y1 + y2) / 2.0) + ") rotate(-90)\" text-anchor=\"middle\">" + Xml(label) + "</text>");
        }

        private static double ValueOr(double value, double fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string F0(double value)
        {
            return value.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
