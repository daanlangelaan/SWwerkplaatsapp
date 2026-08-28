using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileProjectConfiguration
    {
        public string SchemaVersion { get; set; }
        public string ProductId { get; set; }
        public string ProjectName { get; set; }
        public bool ProductionReleased { get; set; }
        public ProfileProjectMachineSettings MachineSettings { get; set; }
        public List<string> ProductionBlockers { get; set; }
        public List<ProfileProjectPiece> Profiles { get; set; }
        public List<ProfileProjectConnection> Connections { get; set; }

        public ProfileProjectConfiguration()
        {
            ProductionBlockers = new List<string>();
            Profiles = new List<ProfileProjectPiece>();
            Connections = new List<ProfileProjectConnection>();
        }
    }

    public sealed class ProfileProjectPiece
    {
        public int ProductionOrder { get; set; }
        public string TraceId { get; set; }
        public string ProfileId { get; set; }
        public string PartName { get; set; }
        public string MaterialId { get; set; }
        public string MaterialName { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }
        public string MemberId { get; set; }
        public ProfileProjectGeometry Geometry { get; set; }
        public double AssemblyXmm { get; set; }
        public double AssemblyYmm { get; set; }
        public double AssemblyZmm { get; set; }
        public double RotationXDeg { get; set; }
        public double RotationYDeg { get; set; }
        public double RotationZDeg { get; set; }
        public string AssemblyOrientation { get; set; }
        public ProfileProjectSticker Sticker { get; set; }
        public ProfileProjectMachiningFrame MachiningFrame { get; set; }
        public string ClampInstruction { get; set; }
        public string StickerInstruction { get; set; }
        public List<ProfileProjectOperation> Operations { get; set; }

        public ProfileProjectPiece()
        {
            Operations = new List<ProfileProjectOperation>();
        }
    }

    public sealed class ProfileProjectGeometry
    {
        public string ProfileSeries { get; set; }
        public double SlotWidthMm { get; set; }
        public double EdgeOffsetMm { get; set; }
        public double PitchMm { get; set; }
        public int PerimeterSlotCount { get; set; }
        public int CoreHoleCountPerEnd { get; set; }
        public string EndTapThread { get; set; }
        public string Status { get; set; }
        public List<double> WidthFaceAxisOffsetsMm { get; set; }
        public List<double> HeightFaceAxisOffsetsMm { get; set; }

        public ProfileProjectGeometry()
        {
            WidthFaceAxisOffsetsMm = new List<double>();
            HeightFaceAxisOffsetsMm = new List<double>();
        }
    }

    public sealed class ProfileProjectMachineSettings
    {
        public string ContractId { get; set; }
        public double SpindleRpm { get; set; }
        public double SpindleSpinUpSeconds { get; set; }
        public double SafeParkZMm { get; set; }
        public double SafeParkYMm { get; set; }
        public double ClearanceAboveProfileMm { get; set; }
        public double SurfaceBreakthroughMm { get; set; }
        public double ThroughOvertravelMm { get; set; }
        public double SurfaceFeedMmMin { get; set; }
        public double DrillFeedMmMin { get; set; }
        public string X0AnchorRule { get; set; }
        public string RollDirectionRule { get; set; }
        public string MasterdataSource { get; set; }
        public List<string> ValidatedProfileTypes { get; set; }

        public ProfileProjectMachineSettings()
        {
            ValidatedProfileTypes = new List<string>();
        }
    }

    public sealed class ProfileProjectSticker
    {
        public string FaceId { get; set; }
        public string LocalFace { get; set; }
        public string Rule { get; set; }
        public string AnchorEnd { get; set; }
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
    }

    public sealed class ProfileProjectMachiningFrame
    {
        public string X0AnchorEnd { get; set; }
        public string StickerFaceId { get; set; }
        public string RollDirection { get; set; }
        public string RollViewDirection { get; set; }
        public List<ProfileProjectMachiningFace> Faces { get; set; }

        public ProfileProjectMachiningFrame()
        {
            Faces = new List<ProfileProjectMachiningFace>();
        }
    }

    public sealed class ProfileProjectMachiningFace
    {
        public string FaceId { get; set; }
        public int QuarterTurnsFromD0 { get; set; }
        public int LocalNormalAxis { get; set; }
        public int LocalNormalSign { get; set; }
        public string LocalFace { get; set; }
        public string CrossSectionFace { get; set; }
        public double FaceSpanMm { get; set; }
        public double ProfileHeightWhenUpMm { get; set; }
        public List<double> SlotAxisOffsetsMm { get; set; }

        public ProfileProjectMachiningFace()
        {
            SlotAxisOffsetsMm = new List<double>();
        }
    }

    public sealed class ProfileProjectOperation
    {
        public int Sequence { get; set; }
        public string Kind { get; set; }
        public string FaceId { get; set; }
        public int CoreHoleIndex { get; set; }
        public int SlotIndex { get; set; }
        public double SlotAxisOffsetMm { get; set; }
        public string Side { get; set; }
        public double PositionFromEndAMm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public bool ThroughHole { get; set; }
        public double SawAngleDeg { get; set; }
        public string WorkOrigin { get; set; }
        public string MachineHint { get; set; }
        public string ExecutionParty { get; set; }
        public string Note { get; set; }
    }

    public sealed class ProfileProjectConnection
    {
        public string ConnectionId { get; set; }
        public string WorkflowId { get; set; }
        public string JointType { get; set; }
        public string InstructionGroup { get; set; }
        public string TappedMemberId { get; set; }
        public string TappedPartName { get; set; }
        public string TappedEnd { get; set; }
        public int CoreHoleIndex { get; set; }
        public string SlotMemberId { get; set; }
        public string SlotPartName { get; set; }
        public string SlotFace { get; set; }
        public string SlotLane { get; set; }
        public string ConnectorId { get; set; }
        public string FastenerId { get; set; }
        public string FastenerStandardId { get; set; }
        public double FastenerThreadMm { get; set; }
        public double HexKeyAcrossFlatsMm { get; set; }
        public double ToolPassageClearanceMm { get; set; }
        public double DrillIncrementMm { get; set; }
        public double AccessHoleDiameterMm { get; set; }
        public string AccessHoleCalculation { get; set; }
        public double AccessHoleOffsetMm { get; set; }
        public string AccessHoleReference { get; set; }
        public string AccessFace { get; set; }
        public string AccessFaceId { get; set; }
        public int AccessSlotIndex { get; set; }
        public double AccessSlotAxisOffsetMm { get; set; }
        public double AccessXmm { get; set; }
        public double AccessYmm { get; set; }
        public double AccessZmm { get; set; }
        public string FastenerAxisOrder { get; set; }
        public string Tool { get; set; }
        public double? FinalTorqueNm { get; set; }
        public string Status { get; set; }
        public bool AccessHoleProductionReady { get; set; }
        public List<string> OpenData { get; set; }
        public List<ProfileProjectConnectionInstance> Instances { get; set; }

        public ProfileProjectConnection()
        {
            OpenData = new List<string>();
            Instances = new List<ProfileProjectConnectionInstance>();
        }
    }

    public sealed class ProfileProjectConnectionInstance
    {
        public int UnitNumber { get; set; }
        public string TappedTraceId { get; set; }
        public string SlotTraceId { get; set; }
    }
}
