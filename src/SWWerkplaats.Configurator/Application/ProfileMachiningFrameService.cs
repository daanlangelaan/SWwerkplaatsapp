using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Derives the complete CNC face frame from the existing assembly sticker placement.
    /// It deliberately stores no second sticker-face truth.
    /// </summary>
    public sealed class ProfileMachiningFrameService
    {
        private readonly ProfileSlotGeometryCatalog slotGeometry;

        public ProfileMachiningFrameService()
            : this(new ProfileSlotGeometryCatalog())
        {
        }

        internal ProfileMachiningFrameService(ProfileSlotGeometryCatalog slotGeometry)
        {
            this.slotGeometry = slotGeometry ?? throw new ArgumentNullException("slotGeometry");
        }

        public ProfileMachiningFrame Build(string traceId, AssemblyPlacement placement, Material material)
        {
            if (placement == null) throw new InvalidOperationException("CNC-frame ontbreekt: assemblyplaatsing ontbreekt voor " + traceId + ".");
            if (placement.Sticker == null) throw new InvalidOperationException("CNC-frame ontbreekt: stickerplaatsing ontbreekt voor " + traceId + ".");
            if (material == null) throw new InvalidOperationException("CNC-frame ontbreekt: profielmateriaal ontbreekt voor " + traceId + ".");

            var sticker = placement.Sticker;
            ValidateSticker(sticker, traceId);
            var dimensions = PlacementDimensions(placement);
            var lengthDirectionSign = sticker.AnchorEnd == ProfileEnd.A ? 1 : -1;
            var longitudinal = Unit(sticker.LongitudinalAxis, lengthDirectionSign);
            var d0 = Unit(sticker.FaceAxis, sticker.FaceSign);
            // D1 is the face that becomes uppermost after one clockwise quarter-turn,
            // viewed from the X=0 stop towards machine +X.
            var d1 = Cross(longitudinal, d0);
            if (IsZero(d1)) throw new InvalidOperationException("CNC-frame ongeldig: D0 staat evenwijdig aan de lengteas voor " + traceId + ".");

            var geometry = slotGeometry.FindRequired(material.Id);
            var frame = new ProfileMachiningFrame
            {
                TraceId = traceId,
                X0AnchorEnd = sticker.AnchorEnd,
                StickerFaceId = "D0",
                RollDirection = "rechtsom",
                RollViewDirection = "vanaf machine-X=0 in de richting van +X"
            };
            frame.Faces.Add(BuildFace("D0", 0, d0, sticker.LongitudinalAxis, dimensions, material, geometry));
            frame.Faces.Add(BuildFace("D1", 1, d1, sticker.LongitudinalAxis, dimensions, material, geometry));
            frame.Faces.Add(BuildFace("D2", 2, Negate(d0), sticker.LongitudinalAxis, dimensions, material, geometry));
            frame.Faces.Add(BuildFace("D3", 3, Negate(d1), sticker.LongitudinalAxis, dimensions, material, geometry));
            return frame;
        }

        private static ProfileMachiningFace BuildFace(string faceId, int quarterTurns, int[] normal, int longitudinalAxis,
            double[] dimensions, Material material, ProfileSlotGeometry geometry)
        {
            var normalAxis = Axis(normal);
            var normalSign = normal[normalAxis];
            var tangentAxis = Enumerable.Range(0, 3).Single(axis => axis != longitudinalAxis && axis != normalAxis);
            var normalSize = dimensions[normalAxis];
            var span = dimensions[tangentAxis];
            var crossSectionDimension = Math.Abs(normalSize - material.WidthMm) <= Math.Abs(normalSize - material.HeightMm) ? "W" : "H";
            var offsets = Math.Abs(span - material.WidthMm) <= Math.Abs(span - material.HeightMm)
                ? geometry.WidthFaceAxisOffsetsMm
                : geometry.HeightFaceAxisOffsetsMm;
            return new ProfileMachiningFace
            {
                FaceId = faceId,
                QuarterTurnsFromD0 = quarterTurns,
                LocalNormalAxis = normalAxis,
                LocalNormalSign = normalSign,
                LocalFace = (normalSign < 0 ? "-" : "+") + AxisName(normalAxis),
                CrossSectionFace = (normalSign < 0 ? "-" : "+") + crossSectionDimension,
                FaceSpanMm = span,
                ProfileHeightWhenUpMm = normalSize,
                SlotAxisOffsetsMm = offsets == null ? new List<double>() : offsets.ToList()
            };
        }

        private static void ValidateSticker(ProfileStickerPlacement sticker, string traceId)
        {
            if (!string.Equals(sticker.FaceId, "D0", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CNC-frame ongeldig: het bestaande stickervlak moet D0 zijn voor " + traceId + ".");
            if (sticker.LongitudinalAxis < 0 || sticker.LongitudinalAxis > 2 || sticker.FaceAxis < 0 || sticker.FaceAxis > 2
                || sticker.LongitudinalAxis == sticker.FaceAxis || (sticker.FaceSign != -1 && sticker.FaceSign != 1))
                throw new InvalidOperationException("CNC-frame ongeldig: lokale stickerassen ontbreken of zijn ongeldig voor " + traceId + ".");
        }

        private static double[] PlacementDimensions(AssemblyPlacement placement)
        {
            return new[]
            {
                Math.Max(2, placement.LengthMm),
                Math.Max(2, placement.HeightMm),
                Math.Max(2, placement.WidthMm)
            };
        }

        private static int[] Unit(int axis, int sign) { var result = new int[3]; result[axis] = sign; return result; }
        private static int[] Negate(int[] value) { return new[] { -value[0], -value[1], -value[2] }; }
        private static int[] Cross(int[] left, int[] right)
        {
            return new[]
            {
                left[1] * right[2] - left[2] * right[1],
                left[2] * right[0] - left[0] * right[2],
                left[0] * right[1] - left[1] * right[0]
            };
        }
        private static bool IsZero(int[] value) { return value[0] == 0 && value[1] == 0 && value[2] == 0; }
        private static int Axis(int[] value) { return value[0] != 0 ? 0 : (value[1] != 0 ? 1 : 2); }
        private static string AxisName(int axis) { return axis == 0 ? "X" : (axis == 1 ? "Y" : "Z"); }
    }
}
