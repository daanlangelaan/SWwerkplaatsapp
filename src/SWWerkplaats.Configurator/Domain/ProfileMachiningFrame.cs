using System.Collections.Generic;
using System.Linq;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileMachiningFace
    {
        public string FaceId { get; set; }
        public int QuarterTurnsFromD0 { get; set; }
        public int LocalNormalAxis { get; set; }
        public int LocalNormalSign { get; set; }
        public string LocalFace { get; set; }
        public string CrossSectionFace { get; set; }
        public double FaceSpanMm { get; set; }
        public double ProfileHeightWhenUpMm { get; set; }
        public IList<double> SlotAxisOffsetsMm { get; set; }
    }

    public sealed class ProfileMachiningFrame
    {
        public string TraceId { get; set; }
        public ProfileEnd X0AnchorEnd { get; set; }
        public string StickerFaceId { get; set; }
        public string RollDirection { get; set; }
        public string RollViewDirection { get; set; }
        public IList<ProfileMachiningFace> Faces { get; private set; }

        public ProfileMachiningFrame()
        {
            StickerFaceId = "D0";
            Faces = new List<ProfileMachiningFace>();
        }

        public ProfileMachiningFace Face(string faceId)
        {
            return Faces.SingleOrDefault(face => string.Equals(face.FaceId, faceId, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
