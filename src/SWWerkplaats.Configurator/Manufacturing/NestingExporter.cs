using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

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
            sb.AppendLine(".hole{fill:#fff;stroke:#0f172a;stroke-width:1.2}");
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
                    if (placement.Part.HasCornerNotches)
                    {
                        sb.AppendLine("<path class=\"part\" d=\"" + NotchedPath(placement, x, y, scale) + "\"/>");
                    }
                    else if (placement.Part.HasToeKickNotch)
                    {
                        sb.AppendLine("<path class=\"part\" d=\"" + ToeKickPath(placement, x, y, scale) + "\"/>");
                    }
                    else
                    {
                        sb.AppendLine("<rect class=\"part\" x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\"/>");
                    }
                    foreach (var pocket in placement.Part.Pockets)
                    {
                        sb.AppendLine(PocketSvg(placement, pocket, x, y, scale));
                    }
                    foreach (var hole in placement.Part.Holes)
                    {
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
            var n = placement.Part.CornerNotchSizeMm;

            n = System.Math.Max(0, System.Math.Min(n, System.Math.Min(l, w) / 2.0));
            var p = new[]
            {
                new Point2(n, 0),
                new Point2(l - n, 0),
                new Point2(l - n, n),
                new Point2(l, n),
                new Point2(l, w - n),
                new Point2(l - n, w - n),
                new Point2(l - n, w),
                new Point2(n, w),
                new Point2(n, w - n),
                new Point2(0, w - n),
                new Point2(0, n),
                new Point2(n, n)
            };

            return LocalPath(placement, x, y, scale, p);
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

        private static string PocketSvg(NestedSheetPlacement placement, SheetPocket pocket, double x, double y, double scale)
        {
            var p0 = LocalToPlaced(placement, pocket.Xmm, pocket.Ymm);
            var p1 = LocalToPlaced(placement, pocket.Xmm + pocket.LengthMm, pocket.Ymm + pocket.WidthMm);
            var minX = System.Math.Min(p0.X, p1.X);
            var maxX = System.Math.Max(p0.X, p1.X);
            var minY = System.Math.Min(p0.Y, p1.Y);
            var maxY = System.Math.Max(p0.Y, p1.Y);

            return "<rect class=\"pocket\" x=\"" + F(x + minX * scale) + "\" y=\"" + F(y + (placement.WidthMm - maxY) * scale) + "\" width=\"" + F((maxX - minX) * scale) + "\" height=\"" + F((maxY - minY) * scale) + "\"/>";
        }

        private static string HoleSvg(NestedSheetPlacement placement, SheetHole hole, double x, double y, double scale)
        {
            var point = LocalToPlaced(placement, hole.Xmm, hole.Ymm);

            var radius = System.Math.Max(1.8, hole.DiameterMm * scale / 2.0);
            return "<circle class=\"hole\" cx=\"" + F(x + point.X * scale) + "\" cy=\"" + F(y + (placement.WidthMm - point.Y) * scale) + "\" r=\"" + F(radius) + "\"><title>" + Xml(hole.Name) + "</title></circle>";
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
