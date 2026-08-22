using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class NestingExporter
    {
        public string ExportCsv(NestingPlan plan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nestplaat;Materiaal;Voorraadmaat_mm;Onderdeel;Instantie;X_links_mm;Y_onder_mm;Lengte_mm;Breedte_mm;Geroteerd;Nesting_hand;Label");
            foreach (var stock in plan.StockSheets)
            {
                foreach (var placement in stock.Placements)
                {
                    sb.Append(E(stock.Name)).Append(';');
                    sb.Append(E(stock.Material.Name)).Append(';');
                    sb.Append(E(F(stock.StockLengthMm) + " x " + F(stock.StockWidthMm))).Append(';');
                    sb.Append(E(placement.Part.Name)).Append(';');
                    sb.Append(placement.InstanceNumber).Append(';');
                    sb.Append(F(placement.Xmm)).Append(';');
                    sb.Append(F(placement.Ymm)).Append(';');
                    sb.Append(F(placement.LengthMm)).Append(';');
                    sb.Append(F(placement.WidthMm)).Append(';');
                    sb.Append(placement.Rotated ? "ja" : "nee").Append(';');
                    sb.Append(placement.Part.MirrorInNestingX ? "gespiegeld-x" : "").Append(';');
                    sb.AppendLine(E(placement.Label));
                }
            }

            return sb.ToString();
        }

        public string ExportSvg(NestingPlan plan)
        {
            return ExportSvg(plan, null);
        }

        public string ExportSvg(NestingPlan plan, ToolDefinition tool)
        {
            var maxWidth = 0.0;
            var totalHeight = 40.0;
            foreach (var stock in plan.StockSheets)
            {
                if (stock.StockLengthMm > maxWidth) maxWidth = stock.StockLengthMm;
                totalHeight += stock.StockWidthMm;
            }

            var scale = 0.32;
            var canvasWidth = maxWidth * scale + 80;
            var canvasHeight = totalHeight * scale + plan.StockSheets.Count * 90 + 80;
            var yOffset = 40.0;

            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(canvasWidth) + "\" height=\"" + F(canvasHeight) + "\" viewBox=\"0 0 " + F(canvasWidth) + " " + F(canvasHeight) + "\">");
            sb.AppendLine("<style>");
            sb.AppendLine("text{font-family:Arial, sans-serif;fill:#111}");
            sb.AppendLine(".stock{fill:#f8fafc;stroke:#111;stroke-width:1.5}");
            sb.AppendLine(".part{fill:#dbeafe;stroke:#1d4ed8;stroke-width:1.2}");
            sb.AppendLine(".pocket{fill:rgba(251,191,36,.28);stroke:#b45309;stroke-width:1;stroke-dasharray:5 4}");
            sb.AppendLine(".cutout{fill:#fff;stroke:#0f172a;stroke-width:1.1}");
            sb.AppendLine(".hole{fill:#fff;stroke:#0f172a;stroke-width:1.2}");
            sb.AppendLine(".tab{stroke:#f59e0b;stroke-width:4;stroke-linecap:round}");
            sb.AppendLine(".label{font-size:14px;font-weight:700}");
            sb.AppendLine(".dim{font-size:12px}");
            sb.AppendLine(".small{font-size:11px;fill:#334155}");
            sb.AppendLine("</style>");

            foreach (var stock in plan.StockSheets)
            {
                var sx = 40.0;
                var sy = yOffset;
                sb.AppendLine("<text class=\"label\" x=\"" + F(sx) + "\" y=\"" + F(sy - 12) + "\">" + Xml(stock.Name + " - " + stock.Material.Name + " - " + F(stock.StockLengthMm) + " x " + F(stock.StockWidthMm) + " mm") + "</text>");
                sb.AppendLine("<rect class=\"stock\" x=\"" + F(sx) + "\" y=\"" + F(sy) + "\" width=\"" + F(stock.StockLengthMm * scale) + "\" height=\"" + F(stock.StockWidthMm * scale) + "\"/>");

                foreach (var placement in stock.Placements)
                {
                    var x = sx + placement.Xmm * scale;
                    var y = sy + (stock.StockWidthMm - placement.Ymm - placement.WidthMm) * scale;
                    var w = placement.LengthMm * scale;
                    var h = placement.WidthMm * scale;
                    if (placement.Part.CustomContour != null && placement.Part.CustomContour.Count >= 3)
                    {
                        sb.AppendLine("<path class=\"part\" d=\"" + CustomContourPath(placement, x, y, scale) + "\"/>");
                    }
                    else if (placement.Part.HasCornerNotches)
                    {
                        sb.AppendLine("<path class=\"part\" d=\"" + NotchedPath(placement, x, y, scale) + "\"/>");
                    }
                    else if (placement.Part.HasToeKickNotch)
                    {
                        sb.AppendLine("<path class=\"part\" d=\"" + ToeKickPath(placement, x, y, scale) + "\"/>");
                    }
                    else
                    {
                        var cornerRadius = System.Math.Max(0, placement.Part.CornerRadiusMm * scale);
                        sb.AppendLine("<rect class=\"part\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\" rx=\"" + F(cornerRadius) + "\" ry=\"" + F(cornerRadius) + "\"/>");
                    }
                    if (placement.Part.UseTabs) sb.AppendLine(TabMarkersSvg(placement, x, y, scale));
                    var drawerPullSvg = DrawerPullSvg(placement, x, y, scale);
                    if (!string.IsNullOrEmpty(drawerPullSvg)) sb.AppendLine(drawerPullSvg);
                    foreach (var pocket in placement.Part.Pockets)
                    {
                        if (IsDrawerPullPocket(pocket)) continue;
                        sb.AppendLine(PocketSvg(placement, pocket, tool, x, y, scale));
                    }
                    foreach (var hole in placement.Part.Holes)
                    {
                        if (IsDrawerPullRoundEnd(hole)) continue;
                        sb.AppendLine(HoleSvg(placement, hole, x, y, scale));
                    }
                    sb.AppendLine("<text class=\"label\" x=\"" + F(x + 8) + "\" y=\"" + F(y + 22) + "\">" + Xml(placement.Part.Name + " #" + placement.InstanceNumber) + "</text>");
                    sb.AppendLine("<text class=\"dim\" x=\"" + F(x + 8) + "\" y=\"" + F(y + 42) + "\">" + Xml(F(placement.Part.LengthMm) + " x " + F(placement.Part.WidthMm) + " x " + F(placement.Part.Material.ThicknessMm) + " mm") + "</text>");
                    sb.AppendLine("<text class=\"small\" x=\"" + F(x + 8) + "\" y=\"" + F(y + h - 10) + "\">" + Xml("X" + F(placement.Xmm) + " Y" + F(placement.Ymm) + (placement.Rotated ? " geroteerd" : "") + (placement.Part.MirrorInNestingX ? " gespiegeld" : "")) + "</text>");
                }

                yOffset += stock.StockWidthMm * scale + 90.0;
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string NotchedPath(NestedSheetPlacement placement, double x, double y, double scale)
        {
            var l = placement.Part.LengthMm;
            var w = placement.Part.WidthMm;
            var nl = placement.Part.CornerNotchLengthMm > 0 ? placement.Part.CornerNotchLengthMm : placement.Part.CornerNotchSizeMm;
            var nw = placement.Part.CornerNotchWidthMm > 0 ? placement.Part.CornerNotchWidthMm : placement.Part.CornerNotchSizeMm;
            nl = System.Math.Max(0, System.Math.Min(nl, l / 2.0));
            nw = System.Math.Max(0, System.Math.Min(nw, w / 2.0));
            var p = new[]
            {
                new Point2(nl, 0),
                new Point2(l - nl, 0),
                new Point2(l - nl, nw),
                new Point2(l, nw),
                new Point2(l, w - nw),
                new Point2(l - nl, w - nw),
                new Point2(l - nl, w),
                new Point2(nl, w),
                new Point2(nl, w - nw),
                new Point2(0, w - nw),
                new Point2(0, nw),
                new Point2(nl, nw)
            };

            return LocalPath(placement, x, y, scale, p);
        }

        private static string CustomContourPath(NestedSheetPlacement placement, double x, double y, double scale)
        {
            var contour = SheetContourGeometry.ToolCenterContour(placement.Part, 0);
            var points = new Point2[contour.Count];
            for (var i = 0; i < contour.Count; i++) points[i] = new Point2(contour[i].Xmm, contour[i].Ymm);
            return LocalPath(placement, x, y, scale, points);
        }

        private static string ToeKickPath(NestedSheetPlacement placement, double x, double y, double scale)
        {
            var l = placement.Part.LengthMm;
            var w = placement.Part.WidthMm;
            var d = System.Math.Max(0, System.Math.Min(placement.Part.ToeKickDepthMm, l - 1));
            var h = System.Math.Max(0, System.Math.Min(placement.Part.ToeKickHeightMm, w - 1));
            var p = new[]
            {
                new Point2(d, 0),
                new Point2(l, 0),
                new Point2(l, w),
                new Point2(0, w),
                new Point2(0, h),
                new Point2(d, h)
            };

            return LocalPath(placement, x, y, scale, p);
        }

        private static string PocketSvg(NestedSheetPlacement placement, SheetPocket pocket, ToolDefinition tool, double x, double y, double scale)
        {
            if (IsCapsulePocket(pocket))
            {
                var capsule = CapsulePoints(pocket.Xmm, pocket.Ymm, pocket.Xmm + pocket.LengthMm, pocket.Ymm + pocket.WidthMm);
                var capsulePath = LocalPath(placement, x, y, scale, capsule);
                var capsuleClass = pocket.DepthMode == OperationDepthMode.Through ? "cutout" : "pocket";
                return "<path class=\"" + capsuleClass + "\" d=\"" + capsulePath + "\"><title>" + Xml(pocket.Name + " - capsulesleuf") + "</title></path>";
            }
            var x0 = pocket.Xmm;
            var y0 = pocket.Ymm;
            var x1 = pocket.Xmm + pocket.LengthMm;
            var y1 = pocket.Ymm + pocket.WidthMm;
            var overcut = ApplyShortEndOvercut(pocket, placement.Part, tool, ref x0, ref y0, ref x1, ref y1);

            var p0 = LocalToPlaced(placement, x0, y0);
            var p1 = LocalToPlaced(placement, x1, y1);
            var minX = System.Math.Min(p0.X, p1.X);
            var maxX = System.Math.Max(p0.X, p1.X);
            var minY = System.Math.Min(p0.Y, p1.Y);
            var maxY = System.Math.Max(p0.Y, p1.Y);

            var cssClass = pocket.DepthMode == OperationDepthMode.Through ? "cutout" : "pocket";
            var title = pocket.Name + (overcut > 0 ? " incl. doorfrees-overmaat " + F(overcut) + " mm" : "");
            return "<rect class=\"" + cssClass + "\" x=\"" + F(x + minX * scale) + "\" y=\"" + F(y + (placement.WidthMm - maxY) * scale) + "\" width=\"" + F((maxX - minX) * scale) + "\" height=\"" + F((maxY - minY) * scale) + "\"><title>" + Xml(title) + "</title></rect>";
        }

        private static bool IsCapsulePocket(SheetPocket pocket)
        {
            return pocket != null && string.Equals(pocket.Shape, "capsule", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string TabMarkersSvg(NestedSheetPlacement placement, double x, double y, double scale)
        {
            var l = placement.Part.LengthMm;
            var w = placement.Part.WidthMm;
            var half = System.Math.Min(10.0, System.Math.Min(l, w) / 8.0);
            var pairs = new[]
            {
                new[] { new Point2(l / 2.0 - half, 0), new Point2(l / 2.0 + half, 0) },
                new[] { new Point2(l / 2.0 - half, w), new Point2(l / 2.0 + half, w) },
                new[] { new Point2(0, w / 2.0 - half), new Point2(0, w / 2.0 + half) },
                new[] { new Point2(l, w / 2.0 - half), new Point2(l, w / 2.0 + half) }
            };
            var sb = new StringBuilder();
            foreach (var pair in pairs)
            {
                var a = LocalToPlaced(placement, pair[0].X, pair[0].Y);
                var b = LocalToPlaced(placement, pair[1].X, pair[1].Y);
                sb.Append("<line class=\"tab\" x1=\"").Append(F(x + a.X * scale)).Append("\" y1=\"").Append(F(y + (placement.WidthMm - a.Y) * scale))
                    .Append("\" x2=\"").Append(F(x + b.X * scale)).Append("\" y2=\"").Append(F(y + (placement.WidthMm - b.Y) * scale)).Append("\"><title>Contourtab</title></line>");
            }
            return sb.ToString();
        }

        private static double ApplyShortEndOvercut(SheetPocket pocket, SheetPart part, ToolDefinition tool, ref double x0, ref double y0, ref double x1, ref double y1)
        {
            if (pocket == null || part == null || tool == null) return 0;

            var overcut = System.Math.Max(0, tool.RadiusMm);
            if (overcut <= 0) return 0;

            var applied = false;
            var horizontalSlot = pocket.LengthMm >= pocket.WidthMm;
            if (horizontalSlot)
            {
                if (pocket.Xmm <= 0.001)
                {
                    x0 -= overcut;
                    applied = true;
                }

                if (pocket.Xmm + pocket.LengthMm >= part.LengthMm - 0.001)
                {
                    x1 += overcut;
                    applied = true;
                }

                return applied ? overcut : 0;
            }

            if (pocket.Ymm <= 0.001)
            {
                y0 -= overcut;
                applied = true;
            }

            if (pocket.Ymm + pocket.WidthMm >= part.WidthMm - 0.001)
            {
                y1 += overcut;
                applied = true;
            }

            return applied ? overcut : 0;
        }

        private static string HoleSvg(NestedSheetPlacement placement, SheetHole hole, double x, double y, double scale)
        {
            var point = LocalToPlaced(placement, hole.Xmm, hole.Ymm);

            var radius = System.Math.Max(1.8, hole.DiameterMm * scale / 2.0);
            return "<circle class=\"hole\" cx=\"" + F(x + point.X * scale) + "\" cy=\"" + F(y + (placement.WidthMm - point.Y) * scale) + "\" r=\"" + F(radius) + "\"><title>" + Xml(hole.Name) + "</title></circle>";
        }

        private static string DrawerPullSvg(NestedSheetPlacement placement, double x, double y, double scale)
        {
            if (placement == null || placement.Part == null) return "";

            SheetPocket middle = null;
            foreach (var pocket in placement.Part.Pockets)
            {
                if (!IsDrawerPullPocket(pocket)) continue;
                middle = pocket;
                break;
            }

            if (middle == null) return "";

            var x0 = middle.Xmm;
            var y0 = middle.Ymm;
            var x1 = middle.Xmm + middle.LengthMm;
            var y1 = middle.Ymm + middle.WidthMm;
            var roundEndCount = 0;

            foreach (var hole in placement.Part.Holes)
            {
                if (!IsDrawerPullRoundEnd(hole)) continue;
                roundEndCount++;
                var radius = hole.DiameterMm / 2.0;
                x0 = System.Math.Min(x0, hole.Xmm - radius);
                y0 = System.Math.Min(y0, hole.Ymm - radius);
                x1 = System.Math.Max(x1, hole.Xmm + radius);
                y1 = System.Math.Max(y1, hole.Ymm + radius);
            }

            if (roundEndCount < 2) return "";

            var points = CapsulePoints(x0, y0, x1, y1);
            var path = LocalPath(placement, x, y, scale, points);
            return "<path class=\"cutout\" d=\"" + path + "\"><title>Uitgefreesde handgreep</title></path>";
        }

        private static Point2[] CapsulePoints(double x0, double y0, double x1, double y1)
        {
            var width = x1 - x0;
            var height = y1 - y0;
            var horizontal = width >= height;
            var radius = System.Math.Max(0.1, System.Math.Min(width, height) / 2.0);
            var segments = 10;
            var count = 2 + segments * 2;
            var points = new Point2[count];
            var i = 0;

            if (horizontal)
            {
                var cy = (y0 + y1) / 2.0;
                var cxLeft = x0 + radius;
                var cxRight = x1 - radius;
                points[i++] = new Point2(cxLeft, y0);
                points[i++] = new Point2(cxRight, y0);
                for (var s = 1; s <= segments; s++)
                {
                    var angle = -System.Math.PI / 2.0 + System.Math.PI * s / segments;
                    points[i++] = new Point2(cxRight + System.Math.Cos(angle) * radius, cy + System.Math.Sin(angle) * radius);
                }

                for (var s = 1; s <= segments; s++)
                {
                    var angle = System.Math.PI / 2.0 + System.Math.PI * s / segments;
                    points[i++] = new Point2(cxLeft + System.Math.Cos(angle) * radius, cy + System.Math.Sin(angle) * radius);
                }

                return points;
            }

            var cx = (x0 + x1) / 2.0;
            var cyTop = y1 - radius;
            var cyBottom = y0 + radius;
            points[i++] = new Point2(x1, cyBottom);
            points[i++] = new Point2(x1, cyTop);
            for (var s = 1; s <= segments; s++)
            {
                var angle = 0 + System.Math.PI * s / segments;
                points[i++] = new Point2(cx + System.Math.Cos(angle) * radius, cyTop + System.Math.Sin(angle) * radius);
            }

            for (var s = 1; s <= segments; s++)
            {
                var angle = System.Math.PI + System.Math.PI * s / segments;
                points[i++] = new Point2(cx + System.Math.Cos(angle) * radius, cyBottom + System.Math.Sin(angle) * radius);
            }

            return points;
        }

        private static bool IsDrawerPullPocket(SheetPocket pocket)
        {
            return pocket != null
                && !IsCapsulePocket(pocket)
                && pocket.Name != null
                && pocket.Name.IndexOf("Uitgefreesde handgreep", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDrawerPullRoundEnd(SheetHole hole)
        {
            return hole != null
                && hole.Name != null
                && hole.Name.StartsWith("Uitgefreesde handgreep ", System.StringComparison.OrdinalIgnoreCase)
                && hole.Name.IndexOf("ronding", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string LocalPath(NestedSheetPlacement placement, double x, double y, double scale, Point2[] points)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < points.Length; i++)
            {
                var point = LocalToPlaced(placement, points[i].X, points[i].Y);
                sb.Append(i == 0 ? "M " : " L ");
                sb.Append(F(x + point.X * scale)).Append(' ').Append(F(y + (placement.WidthMm - point.Y) * scale));
            }

            sb.Append(" Z");
            return sb.ToString();
        }

        private static Point2 LocalToPlaced(NestedSheetPlacement placement, double x, double y)
        {
            if (placement.Part != null && placement.Part.MirrorInNestingX)
            {
                x = placement.Part.LengthMm - x;
            }

            if (!placement.Rotated)
            {
                return new Point2(x, y);
            }

            return new Point2(y, placement.Part.LengthMm - x);
        }

        private static string E(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
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
