using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileTapWorklistService
    {
        public IReadOnlyList<ProfileTapWorklistRow> Build(IEnumerable<ProfileProductionSequenceItem> sequence)
        {
            var catalog = new ProfileSlotGeometryCatalog();
            var rows = new List<ProfileTapWorklistRow>();
            foreach (var item in sequence ?? Enumerable.Empty<ProfileProductionSequenceItem>())
            {
                if (item.Material == null) continue;
                ProfileSlotGeometry geometry;
                try { geometry = catalog.FindRequired(item.Material.Id); }
                catch (InvalidOperationException) { continue; }

                foreach (var end in new[] { "Kop A", "Kop B" })
                {
                    var taps = item.Operations.Where(operation => operation.Kind == ProfileOperationKind.Tap && AppliesToEnd(operation.Side, end)).ToArray();
                    if (taps.Length == 0) continue;
                    var selectedHoles = taps.Select(operation => operation.CoreHoleIndex).Distinct().ToArray();
                    if (selectedHoles.Any(index => index <= 0))
                        throw new InvalidOperationException("Taplijst geblokkeerd voor " + item.TraceId + " " + end
                            + ": iedere tapbewerking moet een expliciete kernboring K1..Kn aanwijzen.");
                    var holeNumber = 0;
                    foreach (var y in geometry.HeightFaceAxisOffsetsMm)
                    foreach (var x in geometry.WidthFaceAxisOffsetsMm)
                    {
                        holeNumber++;
                        if (!selectedHoles.Contains(holeNumber)) continue;
                        var d0 = item.MachiningFrame == null ? null : item.MachiningFrame.Face("D0");
                        var underSticker = UnderStickerFace(d0, x, y, item.Material.WidthMm, item.Material.HeightMm);
                        rows.Add(new ProfileTapWorklistRow
                        {
                            ProductionOrder = item.ProductionOrder,
                            TraceId = item.TraceId,
                            PartName = item.PartName,
                            MaterialId = item.Material.Id,
                            WidthMm = item.Material.WidthMm,
                            HeightMm = item.Material.HeightMm,
                            LengthMm = item.ProfileLengthMm,
                            End = end,
                            StickerEnd = item.Sticker != null && ((item.Sticker.AnchorEnd == ProfileEnd.A) == (end == "Kop A")),
                            StickerFace = d0 == null ? "ONTBREEKT" : d0.FaceId + " / "
                                + d0.CrossSectionFace + " / " + F(d0.FaceSpanMm) + " mm-vlak",
                            StickerOffsetFromX0Mm = item.Sticker == null ? 0 : item.Sticker.OffsetFromAnchorEndMm,
                            CoreHole = "K" + holeNumber.ToString(CultureInfo.InvariantCulture),
                            CoreXmm = x,
                            CoreYmm = y,
                            UnderStickerFace = underSticker,
                            Thread = geometry.EndTapThread,
                            TapRequired = true,
                            Instruction = "Tap " + geometry.EndTapThread + " in kernboring K" + holeNumber + " aan "
                                + ((item.Sticker != null && ((item.Sticker.AnchorEnd == ProfileEnd.A) == (end == "Kop A")))
                                    ? "X=0 · stickerzijde." : "X=L · eindzijde.")
                        });
                    }
                }
            }
            return rows;
        }

        private static bool AppliesToEnd(string side, string end)
        {
            var text = side ?? string.Empty;
            if (text.IndexOf("A/B", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return text.IndexOf(end, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool UnderStickerFace(ProfileMachiningFace stickerFace, double x, double y, double width, double height)
        {
            if (stickerFace == null) return false;
            var face = stickerFace.CrossSectionFace ?? string.Empty;
            if (string.Equals(face, "+W", StringComparison.OrdinalIgnoreCase)) return Math.Abs(x - (width - 20)) < 0.001;
            if (string.Equals(face, "-W", StringComparison.OrdinalIgnoreCase)) return Math.Abs(x - 20) < 0.001;
            if (string.Equals(face, "+H", StringComparison.OrdinalIgnoreCase)) return Math.Abs(y - (height - 20)) < 0.001;
            if (string.Equals(face, "-H", StringComparison.OrdinalIgnoreCase)) return Math.Abs(y - 20) < 0.001;
            return false;
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
