using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ProfileMachiningVisualSvgExporter
    {
        public string Generate(IEnumerable<ProfileProductionSequenceItem> source, IEnumerable<ProfileTapWorklistRow> tapSource)
        {
            var tapRows = (tapSource ?? Enumerable.Empty<ProfileTapWorklistRow>()).Where(row => row.TapRequired).ToArray();
            var traceIds = new HashSet<string>(tapRows.Select(row => row.TraceId), StringComparer.OrdinalIgnoreCase);
            var items = (source ?? Enumerable.Empty<ProfileProductionSequenceItem>()).Where(item => traceIds.Contains(item.TraceId)).OrderBy(item => item.ProductionOrder).ToArray();
            const int width = 1240;
            const int cardHeight = 236;
            var height = 118 + Math.Max(1, items.Length) * cardHeight + 25;
            var tappedEnds = tapRows.Select(row => row.TraceId + "|" + row.End).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var twoSided = tapRows.GroupBy(row => row.TraceId, StringComparer.OrdinalIgnoreCase).Count(group => group.Select(row => row.End).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);
            var geometryCatalog = new ProfileSlotGeometryCatalog();
            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ").Append(width).Append(' ').Append(height).Append("' role='img' aria-label='Visuele controle tapbewerkingen'>");
            sb.Append("<style>text{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;fill:#182230}.title{font-size:27px;font-weight:800}.metric{font-size:20px;font-weight:800}.metricLabel{font-size:11px;fill:#667085}.card{fill:#fff;stroke:#d0d5dd;stroke-width:1.4}.head{fill:#eef4fa}.profile{fill:#dce3e8;stroke:#667085;stroke-width:1.4}.slot{fill:#aab7c2}.axis{stroke:#98a2b3;stroke-width:1;stroke-dasharray:4 4}.tap{fill:#f7906c;stroke:#b42318;stroke-width:2.5}.name{font-size:18px;font-weight:800}.small{font-size:11px;fill:#667085}.sticker{fill:#d95d45}.stickerText{fill:#fff;font-size:10px;font-weight:800}.tapText{fill:#b42318;font-size:11px;font-weight:800}.dimension{stroke:#667085;stroke-width:1.5}</style>");
            sb.Append("<rect width='100%' height='100%' fill='#f7f8fa'/><text class='title' x='34' y='42'>Tapbewerkingen · visuele controle</text>");
            Metric(sb, 34, 56, items.Length.ToString(CultureInfo.InvariantCulture), "tapprofielen");
            Metric(sb, 185, 56, tappedEnds.ToString(CultureInfo.InvariantCulture), "te tappen koppen");
            Metric(sb, 360, 56, tapRows.Length.ToString(CultureInfo.InvariantCulture), "M8-tapgaten");
            Metric(sb, 515, 56, twoSided.ToString(CultureInfo.InvariantCulture), "tweezijdig getapt");
            if (items.Length == 0) sb.Append("<text class='name' x='34' y='205'>Geen vrijgegeven tapbewerkingen in deze configuratie.</text>");
            for (var index = 0; index < items.Length; index++) Card(sb, items[index], tapRows, geometryCatalog, 34, 118 + index * cardHeight, 1172, cardHeight - 14);
            return sb.Append("</svg>").ToString();
        }

        public void Export(string path, IEnumerable<ProfileProductionSequenceItem> source, IEnumerable<ProfileTapWorklistRow> tapSource)
        {
            File.WriteAllText(path, Generate(source, tapSource), new UTF8Encoding(false));
        }

        private static void Card(StringBuilder sb, ProfileProductionSequenceItem item, ProfileTapWorklistRow[] allRows, ProfileSlotGeometryCatalog geometryCatalog, int x, int y, int width, int height)
        {
            var rows = allRows.Where(row => string.Equals(row.TraceId, item.TraceId, StringComparison.OrdinalIgnoreCase)).ToArray();
            sb.Append("<g transform='translate(").Append(x).Append(' ').Append(y).Append(")'><rect class='card' rx='16' width='").Append(width).Append("' height='").Append(height).Append("'/><path class='head' d='M16 0h1140a16 16 0 0 1 16 16v38H0V16A16 16 0 0 1 16 0z'/>");
            sb.Append("<text class='name' x='20' y='29'>").Append(Xml(item.ProductionOrder.ToString("00", CultureInfo.InvariantCulture) + " · " + item.TraceId + " · " + item.PartName)).Append("</text>");
            ProfileVisual(sb, item, geometryCatalog.FindRequired(item.Material.Id), 24, 78, 430);
            var ends = rows.GroupBy(row => row.End, StringComparer.OrdinalIgnoreCase).OrderByDescending(group => group.Any(row => row.StickerEnd)).ToArray();
            var faceX = ends.Length == 1 ? 760 : 535;
            for (var index = 0; index < ends.Length; index++) EndFace(sb, ends[index].ToArray(), faceX + index * 315, 66, 280);
            sb.Append("</g>");
        }

        private static void ProfileVisual(StringBuilder sb, ProfileProductionSequenceItem item, ProfileSlotGeometry geometry, int x, int y, int profileWidth)
        {
            var inset = item.Sticker == null ? 0 : item.Sticker.OffsetFromAnchorEndMm;
            var fraction = item.ProfileLengthMm <= 0 ? 0 : Math.Max(0, Math.Min(1, inset / item.ProfileLengthMm));
            var sx = x + fraction * profileWidth;
            const int profileHeight = 52;
            var stickerFace = item.Sticker == null ? string.Empty : item.Sticker.LocalFace ?? string.Empty;
            var d0 = item.MachiningFrame == null ? null : item.MachiningFrame.Face("D0");
            var sectionFace = d0 == null ? string.Empty : d0.CrossSectionFace ?? string.Empty;
            var faceDimension = d0 == null ? 0 : d0.FaceSpanMm;
            var showWidthSpan = Math.Abs(faceDimension - item.Material.WidthMm) <= Math.Abs(faceDimension - item.Material.HeightMm);
            var slotOffsets = showWidthSpan ? geometry.WidthFaceAxisOffsetsMm : geometry.HeightFaceAxisOffsetsMm;
            sb.Append("<g data-trace-id='").Append(Xml(item.TraceId)).Append("' data-material='").Append(Xml(item.Material.Id))
                .Append("' data-sticker-face='").Append(Xml(stickerFace)).Append("' data-section-face='").Append(Xml(sectionFace))
                .Append("' data-face-span='").Append(F(faceDimension)).Append("' data-slot-count='").Append(slotOffsets.Count).Append("'>");
            sb.Append("<rect class='profile' rx='3' x='").Append(x).Append("' y='").Append(y).Append("' width='").Append(profileWidth).Append("' height='").Append(profileHeight).Append("'/>");
            foreach (var offset in slotOffsets)
            {
                var slotY = y + (offset / faceDimension) * profileHeight - 3;
                sb.Append("<rect class='slot' x='").Append(x).Append("' y='").Append(F(slotY)).Append("' width='").Append(profileWidth).Append("' height='6'/>");
            }
            sb.Append("<rect class='sticker' rx='3' x='").Append(F(sx - 10)).Append("' y='").Append(y + (profileHeight - 32) / 2).Append("' width='20' height='32'/></g>");
            sb.Append("<line class='dimension' x1='").Append(x).Append("' y1='").Append(y + 75).Append("' x2='").Append(x + profileWidth).Append("' y2='").Append(y + 75).Append("'/><line class='dimension' x1='").Append(x).Append("' y1='").Append(y + 68).Append("' x2='").Append(x).Append("' y2='").Append(y + 82).Append("'/><line class='dimension' x1='").Append(x + profileWidth).Append("' y1='").Append(y + 68).Append("' x2='").Append(x + profileWidth).Append("' y2='").Append(y + 82).Append("'/>");
            sb.Append("<text class='small' text-anchor='middle' x='").Append(x + profileWidth / 2).Append("' y='").Append(y + 94).Append("'>").Append(F(item.ProfileLengthMm)).Append(" mm</text>");
        }

        private static void EndFace(StringBuilder sb, ProfileTapWorklistRow[] rows, int x, int y, int boxWidth)
        {
            rows = rows.OrderBy(row => row.CoreYmm).ThenBy(row => row.CoreXmm).ToArray();
            var stickerEnd = rows.Any(row => row.StickerEnd);
            var maxX = Math.Max(40, rows.Max(row => row.CoreXmm) + 20);
            var maxY = Math.Max(40, rows.Max(row => row.CoreYmm) + 20);
            var scale = Math.Min(205.0 / maxX, 120.0 / maxY);
            var faceW = maxX * scale;
            var faceH = maxY * scale;
            var faceX = x + (boxWidth - faceW) / 2;
            var faceY = y + 31 + (122 - faceH) / 2;
            if (stickerEnd) sb.Append("<rect class='sticker' rx='4' x='").Append(x + 91).Append("' y='").Append(y).Append("' width='98' height='23'/><text class='stickerText' text-anchor='middle' x='").Append(x + 140).Append("' y='").Append(y + 16).Append("'>STICKER</text>");
            sb.Append("<rect class='profile' x='").Append(F(faceX)).Append("' y='").Append(F(faceY)).Append("' width='").Append(F(faceW)).Append("' height='").Append(F(faceH)).Append("'/>");
            foreach (var row in rows)
            {
                var cx = faceX + row.CoreXmm * scale;
                var cy = faceY + faceH - row.CoreYmm * scale;
                sb.Append("<line class='axis' x1='").Append(F(cx)).Append("' y1='").Append(F(faceY)).Append("' x2='").Append(F(cx)).Append("' y2='").Append(F(faceY + faceH)).Append("'/><line class='axis' x1='").Append(F(faceX)).Append("' y1='").Append(F(cy)).Append("' x2='").Append(F(faceX + faceW)).Append("' y2='").Append(F(cy)).Append("'/>");
                sb.Append("<circle class='tap' cx='").Append(F(cx)).Append("' cy='").Append(F(cy)).Append("' r='7'/><text class='tapText' x='").Append(F(cx + 10)).Append("' y='").Append(F(cy + 4)).Append("'>").Append(Xml(row.CoreHole)).Append("</text>");
            }
        }

        private static void Metric(StringBuilder sb, int x, int y, string value, string label)
        {
            sb.Append("<text class='metric' x='").Append(x).Append("' y='").Append(y + 18).Append("'>").Append(Xml(value)).Append("</text><text class='metricLabel' x='").Append(x).Append("' y='").Append(y + 36).Append("'>").Append(Xml(label)).Append("</text>");
        }

        private static string F(double value) { return value.ToString("0.##", CultureInfo.InvariantCulture); }
        private static string Xml(string value) { return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;"); }
    }
}
