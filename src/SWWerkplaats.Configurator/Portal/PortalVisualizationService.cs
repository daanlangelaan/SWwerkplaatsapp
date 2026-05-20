using System;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalVisualizationService
    {
        public string BuildProductSvg(WorkbenchModel model, PortalQuoteRequest request)
        {
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase))
            {
                return BuildWorkbenchSvg(model, request);
            }

            return BuildCabinetSvg(model, request);
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
                    for (var s = 1; s <= shelves; s++)
                    {
                        var y = zoneTop + (zoneBottom - zoneTop) * s / (shelves + 1);
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

        private static StringBuilder BeginSvg(WorkbenchModel model, string label)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"900\" height=\"480\" viewBox=\"0 0 900 480\" role=\"img\" aria-label=\"" + Xml(label) + "\">");
            sb.AppendLine("<style>");
            sb.AppendLine("text{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif}");
            sb.AppendLine(".title{font-size:22px;font-weight:750;fill:#14171a}.sub{font-size:13px;fill:#667085}.smallTitle{font-size:13px;font-weight:700;fill:#475467}");
            sb.AppendLine(".case,.side{fill:#f6f2e9;stroke:#9c8f7a;stroke-width:1.5}.top{fill:#d9b98f;stroke:#8b6f48;stroke-width:1.5}.plinth{fill:#b8a994;stroke:#7a6f60;stroke-width:1.2}");
            sb.AppendLine(".drawer{fill:#fbfaf7;stroke:#9c8f7a;stroke-width:1.1}.door{fill:#ede7dc;stroke:#9c8f7a;stroke-width:1.1}.door.alt{fill:#e1d8ca}.divider,.shelf{stroke:#9c8f7a;stroke-width:1.5}.handle,.rail{stroke:#475467;stroke-width:2;stroke-linecap:round}.knob{fill:#475467}");
            sb.AppendLine(".profile,.leg{fill:#d8dde5;stroke:#697586;stroke-width:1.5}.rear{fill:#c8d0da}.shadow,.floor{fill:#e9edf2}.dim{stroke:#667085;stroke-width:1;marker-start:url(#a);marker-end:url(#a)}.dimText{font-size:12px;fill:#475467;font-weight:650}");
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
