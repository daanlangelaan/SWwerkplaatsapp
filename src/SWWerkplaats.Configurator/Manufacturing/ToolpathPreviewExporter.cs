using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ToolpathPreviewExporter
    {
        public string ExportSvg(NestedStockSheet stock, ToolDefinition tool)
        {
            if (stock == null) throw new ArgumentNullException("stock");
            if (tool == null) throw new ArgumentNullException("tool");

            var canvasWidth = 1200.0;
            var canvasHeight = 820.0;
            var margin = 54.0;
            var scale = Math.Min((canvasWidth - 2 * margin) / Math.Max(1, stock.StockLengthMm), (canvasHeight - 2 * margin) / Math.Max(1, stock.StockWidthMm));
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) scale = 1;

            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(canvasWidth) + "\" height=\"" + F(canvasHeight) + "\" viewBox=\"0 0 " + F(canvasWidth) + " " + F(canvasHeight) + "\">");
            sb.AppendLine("<style>");
            sb.AppendLine("text{font-family:Arial,sans-serif}.title{font-size:22px;font-weight:700;fill:#111827}.sub{font-size:12px;fill:#667085}.stock{fill:#f8fafc;stroke:#111827;stroke-width:1.4}.part{fill:rgba(226,232,240,.46);stroke:#94a3b8;stroke-width:1}.pocket{fill:none;stroke:#d97706;stroke-width:1.3;stroke-dasharray:5 4}.cutout{fill:#fff;stroke:#0f172a;stroke-width:1.2}.hole{fill:none;stroke:#2563eb;stroke-width:1.2}.drill{fill:#2563eb}.contour{fill:none;stroke:#dc2626;stroke-width:1.4}.tabbed{stroke:#b91c1c;stroke-width:2}.label{font-size:10px;fill:#344054}.legend{font-size:12px;fill:#344054}");
            sb.AppendLine("</style>");
            sb.AppendLine("<rect x=\"0\" y=\"0\" width=\"" + F(canvasWidth) + "\" height=\"" + F(canvasHeight) + "\" fill=\"#ffffff\"/>");
            sb.AppendLine("<text class=\"title\" x=\"28\" y=\"34\">" + Xml(stock.Name) + " toolpath preview</text>");
            sb.AppendLine("<text class=\"sub\" x=\"28\" y=\"54\">Pockets oranje, door-uitsparingen wit, gaten blauw, buitencontour rood. Tool " + Xml(tool.Name) + " diameter " + F(tool.DiameterMm) + " mm.</text>");
            sb.AppendLine("<rect class=\"stock\" x=\"" + F(margin) + "\" y=\"" + F(margin) + "\" width=\"" + F(stock.StockLengthMm * scale) + "\" height=\"" + F(stock.StockWidthMm * scale) + "\"/>");

            foreach (var placement in stock.Placements)
            {
                DrawPart(sb, stock, placement, margin, scale);
            }

            foreach (var placement in stock.Placements)
            {
                DrawPockets(sb, stock, placement, tool, margin, scale);
                DrawHoles(sb, stock, placement, tool, margin, scale);
                DrawContour(sb, stock, placement, tool, margin, scale);
            }

            sb.AppendLine("<text class=\"legend\" x=\"28\" y=\"" + F(canvasHeight - 24) + "\">Let op: preview toont freesbaanhartlijnen, niet het weggehaalde materiaalvolume.</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static void DrawPart(StringBuilder sb, NestedStockSheet stock, NestedSheetPlacement placement, double margin, double scale)
        {
            var x = margin + placement.Xmm * scale;
            var y = margin + (stock.StockWidthMm - placement.Ymm - placement.WidthMm) * scale;
            var w = placement.LengthMm * scale;
            var h = placement.WidthMm * scale;
            sb.AppendLine("<rect class=\"part\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\"/>");
            if (w > 40 && h > 16)
            {
                sb.AppendLine("<text class=\"label\" x=\"" + F(x + 5) + "\" y=\"" + F(y + 13) + "\">" + Xml(placement.Part.Name + " #" + placement.InstanceNumber + (placement.Rotated ? " R" : "")) + "</text>");
            }
        }

        private static void DrawPockets(StringBuilder sb, NestedStockSheet stock, NestedSheetPlacement placement, ToolDefinition tool, double margin, double scale)
        {
            foreach (var pocket in placement.Part.Pockets)
            {
                if (pocket.DepthMode == OperationDepthMode.Through)
                {
                    var cutoutPoints = new List<Point2>
                    {
                        Transform(placement, pocket.Xmm, pocket.Ymm),
                        Transform(placement, pocket.Xmm + pocket.LengthMm, pocket.Ymm),
                        Transform(placement, pocket.Xmm + pocket.LengthMm, pocket.Ymm + pocket.WidthMm),
                        Transform(placement, pocket.Xmm, pocket.Ymm + pocket.WidthMm),
                        Transform(placement, pocket.Xmm, pocket.Ymm)
                    };
                    sb.AppendLine("<path class=\"cutout\" d=\"" + Path(cutoutPoints, stock, margin, scale) + " Z\"><title>" + Xml(placement.Part.Name + " - " + pocket.Name + " door-en-door") + "</title></path>");
                    continue;
                }

                var inset = Math.Max(tool.RadiusMm, 0.1);
                var x0 = pocket.Xmm + inset;
                var y0 = pocket.Ymm + inset;
                var x1 = pocket.Xmm + pocket.LengthMm - inset;
                var y1 = pocket.Ymm + pocket.WidthMm - inset;
                if (x1 <= x0 || y1 <= y0)
                {
                    x0 = pocket.Xmm + pocket.LengthMm / 2.0;
                    y0 = pocket.Ymm + pocket.WidthMm / 2.0;
                    x1 = x0;
                    y1 = y0;
                }

                var points = new List<Point2>
                {
                    Transform(placement, x0, y0),
                    Transform(placement, x1, y0),
                    Transform(placement, x1, y1),
                    Transform(placement, x0, y1),
                    Transform(placement, x0, y0)
                };
                sb.AppendLine("<path class=\"pocket\" d=\"" + Path(points, stock, margin, scale) + "\"><title>" + Xml(placement.Part.Name + " - " + pocket.Name) + "</title></path>");
            }
        }

        private static void DrawHoles(StringBuilder sb, NestedStockSheet stock, NestedSheetPlacement placement, ToolDefinition tool, double margin, double scale)
        {
            foreach (var hole in placement.Part.Holes)
            {
                var p = Transform(placement, hole.Xmm, hole.Ymm);
                var x = margin + p.X * scale;
                var y = margin + (stock.StockWidthMm - p.Y) * scale;
                var radius = Math.Max(1.5, (hole.DiameterMm <= tool.DiameterMm + 0.05 ? tool.RadiusMm : (hole.DiameterMm - tool.DiameterMm) / 2.0) * scale);
                if (hole.DiameterMm <= tool.DiameterMm + 0.05)
                {
                    sb.AppendLine("<circle class=\"drill\" cx=\"" + F(x) + "\" cy=\"" + F(y) + "\" r=\"" + F(radius) + "\"><title>" + Xml(hole.Name) + "</title></circle>");
                }
                else
                {
                    sb.AppendLine("<circle class=\"hole\" cx=\"" + F(x) + "\" cy=\"" + F(y) + "\" r=\"" + F(radius) + "\"><title>" + Xml(hole.Name + " diameter " + F(hole.DiameterMm)) + "</title></circle>");
                }
            }
        }

        private static void DrawContour(StringBuilder sb, NestedStockSheet stock, NestedSheetPlacement placement, ToolDefinition tool, double margin, double scale)
        {
            var local = ContourPoints(placement.Part, tool.RadiusMm);
            var points = new List<Point2>();
            foreach (var point in local)
            {
                points.Add(Transform(placement, point.X, point.Y));
            }

            sb.AppendLine("<path class=\"contour" + (placement.Part.UseTabs ? " tabbed" : "") + "\" d=\"" + Path(points, stock, margin, scale) + "\"><title>" + Xml(placement.Label + (placement.Part.UseTabs ? " met tabs" : "")) + "</title></path>");
        }

        private static List<Point2> ContourPoints(SheetPart part, double radius)
        {
            var points = new List<Point2>();
            var x0 = -radius;
            var y0 = -radius;
            var x1 = part.LengthMm + radius;
            var y1 = part.WidthMm + radius;
            if (part.HasToeKickNotch)
            {
                var notchX = Math.Min(part.ToeKickDepthMm + radius, x1);
                var notchY = Math.Min(part.ToeKickHeightMm + radius, y1);
                points.Add(new Point2(notchX, y0));
                points.Add(new Point2(x1, y0));
                points.Add(new Point2(x1, y1));
                points.Add(new Point2(x0, y1));
                points.Add(new Point2(x0, notchY));
                points.Add(new Point2(notchX, notchY));
                points.Add(new Point2(notchX, y0));
                return points;
            }

            if (part.HasCornerNotches)
            {
                var n = part.CornerNotchSizeMm;
                var nx0 = n + radius;
                var ny0 = n + radius;
                var nx1 = part.LengthMm - n - radius;
                var ny1 = part.WidthMm - n - radius;
                points.Add(new Point2(nx0, y0));
                points.Add(new Point2(nx1, y0));
                points.Add(new Point2(nx1, ny0));
                points.Add(new Point2(x1, ny0));
                points.Add(new Point2(x1, ny1));
                points.Add(new Point2(nx1, ny1));
                points.Add(new Point2(nx1, y1));
                points.Add(new Point2(nx0, y1));
                points.Add(new Point2(nx0, ny1));
                points.Add(new Point2(x0, ny1));
                points.Add(new Point2(x0, ny0));
                points.Add(new Point2(nx0, ny0));
                points.Add(new Point2(nx0, y0));
                return points;
            }

            points.Add(new Point2(x0, y0));
            points.Add(new Point2(x1, y0));
            points.Add(new Point2(x1, y1));
            points.Add(new Point2(x0, y1));
            points.Add(new Point2(x0, y0));
            return points;
        }

        private static string Path(List<Point2> points, NestedStockSheet stock, double margin, double scale)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < points.Count; i++)
            {
                sb.Append(i == 0 ? "M " : " L ");
                sb.Append(F(margin + points[i].X * scale)).Append(' ');
                sb.Append(F(margin + (stock.StockWidthMm - points[i].Y) * scale));
            }

            return sb.ToString();
        }

        private static Point2 Transform(NestedSheetPlacement placement, double x, double y)
        {
            if (placement.Part != null && placement.Part.MirrorInNestingX)
            {
                x = placement.Part.LengthMm - x;
            }

            if (!placement.Rotated)
            {
                return new Point2(placement.Xmm + x, placement.Ymm + y);
            }

            return new Point2(placement.Xmm + y, placement.Ymm + placement.Part.LengthMm - x);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private struct Point2
        {
            public readonly double X;
            public readonly double Y;

            public Point2(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
