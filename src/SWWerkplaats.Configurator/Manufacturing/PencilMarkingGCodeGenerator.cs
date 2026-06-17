using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class PencilMarkingOptions
    {
        public int ToolNumber { get; set; }
        public string ToolName { get; set; }
        public double WriteDepthMm { get; set; }
        public double FeedRateMmMin { get; set; }
        public double PlungeRateMmMin { get; set; }
        public double TextHeightMm { get; set; }
        public double PartMarginMm { get; set; }

        public static PencilMarkingOptions Default()
        {
            return new PencilMarkingOptions
            {
                ToolNumber = 2,
                ToolName = "Geveerd potlood",
                WriteDepthMm = -3.0,
                FeedRateMmMin = 2200,
                PlungeRateMmMin = 500,
                TextHeightMm = 12,
                PartMarginMm = 12
            };
        }
    }

    public sealed class PencilMarkingGCodeGenerator
    {
        private static readonly Dictionary<char, string[]> Font = BuildFont();

        public bool HasMarks(NestedStockSheet stock)
        {
            return stock != null && stock.Placements != null && stock.Placements.Count > 0;
        }

        public string ExportPlan(NestingPlan plan, PencilMarkingOptions options)
        {
            if (options == null) options = PencilMarkingOptions.Default();
            var sb = new StringBuilder();
            sb.AppendLine("Nestplaat;Onderdeel;Instantie;Geroteerd;Tekstregel_1;Tekstregel_2;Lokale_X_mm;Lokale_Y_mm;Teksthoogte_mm;Schrijf_Z_mm;Toolnummer;Toolnaam");
            if (plan == null) return sb.ToString();

            foreach (var stock in plan.StockSheets)
            {
                foreach (var placement in stock.Placements)
                {
                    var margin = Math.Min(options.PartMarginMm, Math.Min(placement.Part.LengthMm, placement.Part.WidthMm) / 5.0);
                    var lines = MarkLines(placement);
                    var textHeight = Math.Min(options.TextHeightMm, Math.Max(6.0, (placement.Part.WidthMm - 2.0 * margin) / Math.Max(1, lines.Count * 1.45)));
                    var maxLineWidth = MaxTextWidth(lines, textHeight);
                    if (maxLineWidth > placement.Part.LengthMm - 2.0 * margin)
                    {
                        textHeight *= (placement.Part.LengthMm - 2.0 * margin) / maxLineWidth;
                        textHeight = Math.Max(5.0, textHeight);
                    }

                    sb.Append(E(stock.Name)).Append(';');
                    sb.Append(E(placement.Part.Name)).Append(';');
                    sb.Append(placement.InstanceNumber).Append(';');
                    sb.Append(placement.Rotated ? "ja" : "nee").Append(';');
                    sb.Append(E(lines.Count > 0 ? lines[0] : "")).Append(';');
                    sb.Append(E(lines.Count > 1 ? lines[1] : "")).Append(';');
                    sb.Append(F(margin)).Append(';');
                    sb.Append(F(margin)).Append(';');
                    sb.Append(F(textHeight)).Append(';');
                    sb.Append(F(options.WriteDepthMm)).Append(';');
                    sb.Append(options.ToolNumber).Append(';');
                    sb.AppendLine(E(options.ToolName));
                }
            }

            return sb.ToString();
        }

        public void Append(StringBuilder sb, NestedStockSheet stock, MachineProfile machine, PencilMarkingOptions options)
        {
            if (!HasMarks(stock)) return;
            if (sb == null) throw new ArgumentNullException("sb");
            if (machine == null) throw new ArgumentNullException("machine");
            if (options == null) options = PencilMarkingOptions.Default();

            sb.AppendLine();
            sb.AppendLine("(--- BEWERKING 0: onderdelen markeren met geveerd potlood ---)");
            sb.AppendLine("(Plaats " + options.ToolName + " in de freeshouder. Zet Z0 op de potloodpunt.)");
            sb.AppendLine("(Schrijfdiepte " + F(options.WriteDepthMm) + " mm vanaf potlood-Z0; hou rekening met veerweg.)");
            sb.AppendLine("M5");
            sb.AppendLine("(TOOLCHANGE: machine gaat eerst naar home/wisselpositie)");
            sb.AppendLine("(1/2 Z-as naar machine-home)");
            sb.AppendLine("G28 G91 Z0.");
            sb.AppendLine("G90");
            sb.AppendLine("(2/2 X/Y naar machine-home voor toolwissel)");
            sb.AppendLine("G28 G91 X0. Y0.");
            sb.AppendLine("G90");
            sb.AppendLine("(STOP: plaats T" + options.ToolNumber + " - " + options.ToolName + ")");
            sb.AppendLine("(Druk pas op Cycle Start als potlood, houder en Z0 gecontroleerd zijn.)");
            sb.AppendLine("M0");
            sb.AppendLine("T" + options.ToolNumber + " M6");
            sb.AppendLine("G0 Z" + F(machine.SafeZmm));

            foreach (var placement in stock.Placements)
            {
                AppendPlacementMark(sb, placement, machine, options);
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static void AppendPlacementMark(StringBuilder sb, NestedSheetPlacement placement, MachineProfile machine, PencilMarkingOptions options)
        {
            if (placement == null || placement.Part == null) return;

            var margin = Math.Min(options.PartMarginMm, Math.Min(placement.Part.LengthMm, placement.Part.WidthMm) / 5.0);
            if (placement.Part.LengthMm < margin * 2 + 20 || placement.Part.WidthMm < margin * 2 + options.TextHeightMm)
            {
                return;
            }

            var lines = MarkLines(placement);
            var textHeight = Math.Min(options.TextHeightMm, Math.Max(6.0, (placement.Part.WidthMm - 2.0 * margin) / Math.Max(1, lines.Count * 1.45)));
            var maxLineWidth = MaxTextWidth(lines, textHeight);
            if (maxLineWidth > placement.Part.LengthMm - 2.0 * margin)
            {
                textHeight *= (placement.Part.LengthMm - 2.0 * margin) / maxLineWidth;
                textHeight = Math.Max(5.0, textHeight);
            }

            var linePitch = textHeight * 1.35;
            var y = margin;
            sb.AppendLine();
            sb.AppendLine("(" + placement.Label + " potloodmarkering)");
            foreach (var line in lines)
            {
                AppendTextLine(sb, placement, line, margin, y, textHeight, machine, options);
                y += linePitch;
                if (y + textHeight > placement.Part.WidthMm - margin) break;
            }
        }

        private static List<string> MarkLines(NestedSheetPlacement placement)
        {
            var lines = new List<string>();
            var part = placement.Part;
            lines.Add(CleanText("#" + placement.InstanceNumber.ToString(CultureInfo.InvariantCulture) + " " + part.Name));
            var material = part.Material == null ? "" : " " + F(part.Material.ThicknessMm) + "MM";
            lines.Add(CleanText(F(part.LengthMm) + "X" + F(part.WidthMm) + material));
            return lines;
        }

        private static void AppendTextLine(StringBuilder sb, NestedSheetPlacement placement, string text, double x, double y, double height, MachineProfile machine, PencilMarkingOptions options)
        {
            var scale = height / 7.0;
            var advance = scale * 5.8;
            var cx = x;
            foreach (var ch in text)
            {
                if (ch == ' ')
                {
                    cx += advance;
                    continue;
                }

                string[] strokes;
                if (!Font.TryGetValue(ch, out strokes))
                {
                    cx += advance;
                    continue;
                }

                foreach (var stroke in strokes)
                {
                    var values = stroke.Split(',');
                    if (values.Length != 4) continue;
                    var x0 = cx + ToDouble(values[0]) * scale;
                    var y0 = y + ToDouble(values[1]) * scale;
                    var x1 = cx + ToDouble(values[2]) * scale;
                    var y1 = y + ToDouble(values[3]) * scale;
                    var p0 = Transform(placement, x0, y0);
                    var p1 = Transform(placement, x1, y1);
                    sb.AppendLine("G0 Z" + F(machine.SafeZmm));
                    sb.AppendLine("G0 X" + F(p0.X) + " Y" + F(p0.Y));
                    sb.AppendLine("G1 Z" + F(options.WriteDepthMm) + " F" + F(options.PlungeRateMmMin));
                    sb.AppendLine("G1 X" + F(p1.X) + " Y" + F(p1.Y) + " F" + F(options.FeedRateMmMin));
                }

                cx += advance;
            }

            sb.AppendLine("G0 Z" + F(machine.SafeZmm));
        }

        private static double MaxTextWidth(List<string> lines, double height)
        {
            var scale = height / 7.0;
            var advance = scale * 5.8;
            var max = 0.0;
            foreach (var line in lines)
            {
                var width = Math.Max(0, line.Length - 1) * advance + 4 * scale;
                if (width > max) max = width;
            }

            return max;
        }

        private static string CleanText(string value)
        {
            value = (value ?? "").ToUpperInvariant();
            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '#' || ch == '-' || ch == 'X' || ch == ' ')
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append(' ');
                }
            }

            while (sb.ToString().Contains("  "))
            {
                sb.Replace("  ", " ");
            }

            return sb.ToString().Trim();
        }

        private static Point2 Transform(NestedSheetPlacement placement, double x, double y)
        {
            if (!placement.Rotated)
            {
                return new Point2(placement.Xmm + x, placement.Ymm + y);
            }

            return new Point2(placement.Xmm + y, placement.Ymm + placement.Part.LengthMm - x);
        }

        private static double ToDouble(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string E(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static Dictionary<char, string[]> BuildFont()
        {
            var f = new Dictionary<char, string[]>();
            f['A'] = S("0,0,0,5", "0,5,2,7", "2,7,4,5", "4,5,4,0", "0,3,4,3");
            f['B'] = S("0,0,0,7", "0,7,3,7", "3,7,4,6", "4,6,4,4", "4,4,3,3", "0,3,3,3", "3,3,4,2", "4,2,4,1", "4,1,3,0", "3,0,0,0");
            f['C'] = S("4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0", "1,0,3,0", "3,0,4,1");
            f['D'] = S("0,0,0,7", "0,7,3,7", "3,7,4,6", "4,6,4,1", "4,1,3,0", "3,0,0,0");
            f['E'] = S("4,7,0,7", "0,7,0,0", "0,0,4,0", "0,3.5,3.4,3.5");
            f['F'] = S("0,0,0,7", "0,7,4,7", "0,3.5,3.3,3.5");
            f['G'] = S("4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0", "1,0,4,0", "4,0,4,3", "4,3,2.4,3");
            f['H'] = S("0,0,0,7", "4,0,4,7", "0,3.5,4,3.5");
            f['I'] = S("0,7,4,7", "2,7,2,0", "0,0,4,0");
            f['J'] = S("4,7,4,1", "4,1,3,0", "3,0,1,0", "1,0,0,1");
            f['K'] = S("0,0,0,7", "4,7,0,3.5", "0,3.5,4,0");
            f['L'] = S("0,7,0,0", "0,0,4,0");
            f['M'] = S("0,0,0,7", "0,7,2,4", "2,4,4,7", "4,7,4,0");
            f['N'] = S("0,0,0,7", "0,7,4,0", "4,0,4,7");
            f['O'] = S("1,0,3,0", "3,0,4,1", "4,1,4,6", "4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0");
            f['P'] = S("0,0,0,7", "0,7,3,7", "3,7,4,6", "4,6,4,4", "4,4,3,3", "3,3,0,3");
            f['Q'] = S("1,0,3,0", "3,0,4,1", "4,1,4,6", "4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0", "2.6,1.2,4.4,-.5");
            f['R'] = S("0,0,0,7", "0,7,3,7", "3,7,4,6", "4,6,4,4", "4,4,3,3", "3,3,0,3", "2.2,3,4,0");
            f['S'] = S("4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,4.5", "0,4.5,1,3.5", "1,3.5,3,3.5", "3,3.5,4,2.5", "4,2.5,4,1", "4,1,3,0", "3,0,1,0", "1,0,0,1");
            f['T'] = S("0,7,4,7", "2,7,2,0");
            f['U'] = S("0,7,0,1", "0,1,1,0", "1,0,3,0", "3,0,4,1", "4,1,4,7");
            f['V'] = S("0,7,2,0", "2,0,4,7");
            f['W'] = S("0,7,0.8,0", "0.8,0,2,3", "2,3,3.2,0", "3.2,0,4,7");
            f['X'] = S("0,7,4,0", "4,7,0,0");
            f['Y'] = S("0,7,2,3.5", "4,7,2,3.5", "2,3.5,2,0");
            f['Z'] = S("0,7,4,7", "4,7,0,0", "0,0,4,0");
            f['0'] = S("1,0,3,0", "3,0,4,1", "4,1,4,6", "4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0", "0.8,0.8,3.2,6.2");
            f['1'] = S("1,5.8,2,7", "2,7,2,0", "0.8,0,3.2,0");
            f['2'] = S("0,6,1,7", "1,7,3,7", "3,7,4,6", "4,6,4,5", "4,5,0,0", "0,0,4,0");
            f['3'] = S("0,7,4,7", "4,7,2.2,3.6", "2.2,3.6,4,3", "4,3,4,1", "4,1,3,0", "3,0,1,0", "1,0,0,1");
            f['4'] = S("4,0,4,7", "4,3,0,3", "0,3,3,7");
            f['5'] = S("4,7,0,7", "0,7,0,4", "0,4,3,4", "3,4,4,3", "4,3,4,1", "4,1,3,0", "3,0,1,0", "1,0,0,1");
            f['6'] = S("4,6,3,7", "3,7,1,7", "1,7,0,6", "0,6,0,1", "0,1,1,0", "1,0,3,0", "3,0,4,1", "4,1,4,3", "4,3,3,4", "3,4,0,4");
            f['7'] = S("0,7,4,7", "4,7,1,0");
            f['8'] = S("1,0,3,0", "3,0,4,1", "4,1,4,2.5", "4,2.5,3,3.5", "3,3.5,1,3.5", "1,3.5,0,2.5", "0,2.5,0,1", "0,1,1,0", "1,3.5,0,4.5", "0,4.5,0,6", "0,6,1,7", "1,7,3,7", "3,7,4,6", "4,6,4,4.5", "4,4.5,3,3.5");
            f['9'] = S("4,3,1,3", "1,3,0,4", "0,4,0,6", "0,6,1,7", "1,7,3,7", "3,7,4,6", "4,6,4,1", "4,1,3,0", "3,0,1,0", "1,0,0,1");
            f['#'] = S("1,0,1,7", "3,0,3,7", "0,2,4,2", "0,5,4,5");
            f['-'] = S("0,3.5,4,3.5");
            return f;
        }

        private static string[] S(params string[] strokes)
        {
            return strokes;
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
