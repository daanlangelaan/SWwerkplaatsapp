namespace SWWerkplaats.Configurator.Domain
{
    using System.Collections.Generic;

    public enum AssemblyComponentKind
    {
        Profile,
        Sheet,
        Purchased
    }

    public enum AssemblyOrientation
    {
        Default,
        SheetHorizontal,
        SheetVerticalX,
        SheetVerticalZ
    }

    public sealed class AssemblyPlacement
    {
        public string MemberId { get; set; }
        public string TraceId { get; set; }
        public ProfileStickerPlacement Sticker { get; set; }
        public AssemblyComponentKind Kind { get; set; }
        public string PartName { get; set; }
        public string ComponentId { get; set; }
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public AssemblyOrientation Orientation { get; set; }
        public string VisualKind { get; set; }
        public string Shape { get; set; }
        public double RotationXDeg { get; set; }
        public double RotationYDeg { get; set; }
        public double RotationZDeg { get; set; }

        public List<AssemblyComponentPartPose> ComponentPartPoses { get; private set; }
        public List<AssemblyComponentPartAttachment> ComponentPartAttachments { get; private set; }

        public AssemblyPlacement()
        {
            ComponentPartPoses = new List<AssemblyComponentPartPose>();
            ComponentPartAttachments = new List<AssemblyComponentPartAttachment>();
        }
    }

    /// <summary>
    /// Generic articulation of one physical part inside a purchased component.
    /// The part ID comes from the component primitive contract in masterdata.
    /// </summary>
    public sealed class AssemblyComponentPartPose
    {
        public string PartId { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double RotationXDeg { get; set; }
        public double RotationYDeg { get; set; }
        public double RotationZDeg { get; set; }
    }

    /// <summary>
    /// Declares which assembly member rigidly drives a physical component part.
    /// It is used by the render-motion contract and does not create CAM geometry.
    /// </summary>
    public sealed class AssemblyComponentPartAttachment
    {
        public string PartId { get; set; }
        public string PartName { get; set; }
    }
}
