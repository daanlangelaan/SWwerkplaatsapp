using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

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
                    if (IsDrawerPullPocket(pocket))
                    {
                        var handlePoints = DrawerPullCapsulePoints(placement.Part, pocket);
                        var transformedHandlePoints = new List<Point2>();
                        foreach (var handlePoint in handlePoints)
                        {
                            transformedHandlePoints.Add(Transform(placement, handlePoint.X, handlePoint.Y));
                        }

                        sb.AppendLine("<path class=\"cutout\" d=\"" + Path(transformedHandlePoints, stock, margin, scale) + " Z\"><title>" + Xml(placement.Part.Name + " - uitgefreesde handgreep door-en-door") + "</title></path>");
                        continue;
                    }

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
                if (IsDrawerPullRoundEnd(hole)) continue;
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

        private static List<Point2> DrawerPullCapsulePoints(SheetPart part, SheetPocket middle)
        {
            var x0 = middle.Xmm;
            var y0 = middle.Ymm;
            var x1 = middle.Xmm + middle.LengthMm;
            var y1 = middle.Ymm + middle.WidthMm;

            foreach (var hole in part.Holes)
            {
                if (!IsDrawerPullRoundEnd(hole)) continue;
                var radius = hole.DiameterMm / 2.0;
                x0 = Math.Min(x0, hole.Xmm - radius);
                y0 = Math.Min(y0, hole.Ymm - radius);
                x1 = Math.Max(x1, hole.Xmm + radius);
                y1 = Math.Max(y1, hole.Ymm + radius);
            }

            var radiusMm = Math.Max(0.1, Math.Min(x1 - x0, y1 - y0) / 2.0);
            var centerY = (y0 + y1) / 2.0;
            var leftCenterX = x0 + radiusMm;
            var rightCenterX = x1 - radiusMm;
            var points = new List<Point2>();
            const int segments = 16;
            points.Add(new Point2(leftCenterX, y0));
            points.Add(new Point2(rightCenterX, y0));
            for (var i = 1; i <= segments; i++)
            {
                var angle = -Math.PI / 2.0 + Math.PI * i / segments;
                points.Add(new Point2(rightCenterX + Math.Cos(angle) * radiusMm, centerY + Math.Sin(angle) * radiusMm));
            }
            for (var i = 1; i <= segments; i++)
            {
                var angle = Math.PI / 2.0 + Math.PI * i / segments;
                points.Add(new Point2(leftCenterX + Math.Cos(angle) * radiusMm, centerY + Math.Sin(angle) * radiusMm));
            }
            points.Add(points[0]);
            return points;
        }

        private static bool IsDrawerPullPocket(SheetPocket pocket)
        {
            return pocket != null
                && pocket.Name != null
                && pocket.Name.IndexOf("Uitgefreesde handgreep", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDrawerPullRoundEnd(SheetHole hole)
        {
            return hole != null
                && hole.Name != null
                && hole.Name.StartsWith("Uitgefreesde handgreep ", StringComparison.OrdinalIgnoreCase)
                && hole.Name.IndexOf("ronding", StringComparison.OrdinalIgnoreCase) >= 0;
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
            var custom = SheetContourGeometry.ToolCenterContour(part, radius);
            if (custom.Count > 0)
            {
                foreach (var point in custom) points.Add(new Point2(point.Xmm, point.Ymm));
                return points;
            }
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
                var nl = part.CornerNotchLengthMm > 0 ? part.CornerNotchLengthMm : part.CornerNotchSizeMm;
                var nw = part.CornerNotchWidthMm > 0 ? part.CornerNotchWidthMm : part.CornerNotchSizeMm;
                var nx0 = nl + radius;
                var ny0 = nw + radius;
                var nx1 = part.LengthMm - nl - radius;
                var ny1 = part.WidthMm - nw - radius;
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

            if (part.CornerRadiusMm > 0.001)
            {
                AddRoundedRectangleContour(points, part.LengthMm, part.WidthMm, part.CornerRadiusMm, radius);
                return points;
            }

            points.Add(new Point2(x0, y0));
            points.Add(new Point2(x1, y0));
            points.Add(new Point2(x1, y1));
            points.Add(new Point2(x0, y1));
            points.Add(new Point2(x0, y0));
            return points;
        }

        private static void AddRoundedRectangleContour(List<Point2> points, double length, double width, double cornerRadius, double toolRadius)
        {
            var r = Math.Max(0, Math.Min(cornerRadius, Math.Min(length, width) / 2.0));
            var pathRadius = r + toolRadius;
            points.Add(new Point2(r, -toolRadius));
            points.Add(new Point2(length - r, -toolRadius));
            AddContourArc(points, length - r, r, pathRadius, -Math.PI / 2.0, 0);
            points.Add(new Point2(length + toolRadius, width - r));
            AddContourArc(points, length - r, width - r, pathRadius, 0, Math.PI / 2.0);
            points.Add(new Point2(r, width + toolRadius));
            AddContourArc(points, r, width - r, pathRadius, Math.PI / 2.0, Math.PI);
            points.Add(new Point2(-toolRadius, r));
            AddContourArc(points, r, r, pathRadius, Math.PI, Math.PI * 1.5);
        }

        private static void AddContourArc(List<Point2> points, double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            const int segments = 8;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + (endAngle - startAngle) * i / segments;
                points.Add(new Point2(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
            }
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
