using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileCoreHolePosition
    {
        public int CoreHoleIndex { get; set; }
        public double WidthOffsetMm { get; set; }
        public double HeightOffsetMm { get; set; }
        public double LocalXmm { get; set; }
        public double LocalYmm { get; set; }
        public double LocalZmm { get; set; }
    }

    public sealed class ProfileSlotGeometry
    {
        public string MaterialId { get; set; }
        public string ProfileSeries { get; set; }
        public double SlotWidthMm { get; set; }
        public double EdgeOffsetMm { get; set; }
        public double PitchMm { get; set; }
        public int ExpectedPerimeterSlotCount { get; set; }
        public int ExpectedCoreHoleCountPerEnd { get; set; }
        public string EndTapThread { get; set; }
        public string Status { get; set; }
        public double? SlotMouthWidthMm { get; set; }
        public double? SlotMouthDepthMm { get; set; }
        public double? SlotCavityWidthMm { get; set; }
        public double? SlotCavityDepthMm { get; set; }
        public double? OutsideCornerRadiusMm { get; set; }
        public double? CoreHoleDiameterMm { get; set; }
        public string CoreHoleContour { get; set; }
        public string GeometrySource { get; set; }
        public string OpenGeometryData { get; set; }
        public IList<double> WidthFaceAxisOffsetsMm { get; set; }
        public IList<double> HeightFaceAxisOffsetsMm { get; set; }

        public int CalculatedPerimeterSlotCount
        {
            get
            {
                return 2 * (WidthFaceAxisOffsetsMm == null ? 0 : WidthFaceAxisOffsetsMm.Count)
                    + 2 * (HeightFaceAxisOffsetsMm == null ? 0 : HeightFaceAxisOffsetsMm.Count);
            }
        }

        public int CalculatedCoreHoleCountPerEnd
        {
            get
            {
                return (WidthFaceAxisOffsetsMm == null ? 0 : WidthFaceAxisOffsetsMm.Count)
                    * (HeightFaceAxisOffsetsMm == null ? 0 : HeightFaceAxisOffsetsMm.Count);
            }
        }
    }
}
