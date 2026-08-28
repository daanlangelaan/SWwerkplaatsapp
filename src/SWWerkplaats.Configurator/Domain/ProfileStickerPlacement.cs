using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public enum ProfileStickerPlacementRule
    {
        UpperFace,
        AssemblyViewSide,
        InclinedVisibleFace
    }

    public sealed class ProfileStickerPlacement
    {
        public string FaceId { get; set; }
        public string LocalFace { get; set; }
        public ProfileStickerPlacementRule Rule { get; set; }
        public ProfileEnd AnchorEnd { get; set; }
        public double OffsetFromAnchorEndMm { get; set; }
        public int LongitudinalAxis { get; set; }
        public int FaceAxis { get; set; }
        public int FaceSign { get; set; }
        public double LocalXmm { get; set; }
        public double LocalYmm { get; set; }
        public double LocalZmm { get; set; }
        public double LocalNormalX { get; set; }
        public double LocalNormalY { get; set; }
        public double LocalNormalZ { get; set; }
        public double WorldNormalX { get; set; }
        public double WorldNormalY { get; set; }
        public double WorldNormalZ { get; set; }
        public double LongitudinalSizeMm { get; set; }
        public double TransverseSizeMm { get; set; }
        public bool ObstructionFree { get; set; }
        public double VisibilityScore { get; set; }
        public string OrientationInstruction { get; set; }
        public List<string> TraceIds { get; private set; }

        public ProfileStickerPlacement()
        {
            FaceId = "D0";
            TraceIds = new List<string>();
        }
    }
}
