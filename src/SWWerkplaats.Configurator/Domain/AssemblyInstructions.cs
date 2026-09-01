using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public enum AssemblyDataStatus { Confirmed, Provisional, Unresolved }
    public enum ProfileEnd { A, B }
    public enum AssemblyJointType { StandardConnector, HingeSlidingNut, SheetHinge }

    public sealed class AssemblyConnection
    {
        public string ConnectionId { get; set; }
        public string WorkflowId { get; set; }
        public AssemblyJointType JointType { get; set; }
        public string InstructionGroup { get; set; }
        public string TappedMemberId { get; set; }
        public string TappedPartName { get; set; }
        public ProfileEnd TappedEnd { get; set; }
        // Een standaardverbinder is gekoppeld aan precies één kopse kernboring K1..Kn.
        // Een 40x80-kop heeft daardoor twee afzonderlijke verbindingen: K1 en K2.
        public int CoreHoleIndex { get; set; }
        public string SlotMemberId { get; set; }
        public string SlotPartName { get; set; }
        public string SlotFace { get; set; }
        public string SlotLane { get; set; }
        public string ConnectorId { get; set; }
        public string FastenerStandardId { get; set; }
        public string FastenerId { get; set; }
        public double FastenerThreadMm { get; set; }
        public double HexKeyAcrossFlatsMm { get; set; }
        public double ToolPassageClearanceMm { get; set; }
        public double DrillIncrementMm { get; set; }
        public double AccessHoleDiameterMm { get; set; }
        public string AccessHoleCalculation { get; set; }
        public string FastenerAxisOrder { get; set; }
        public double AccessHoleOffsetMm { get; set; }
        public string AccessHoleReference { get; set; }
        public string AccessFace { get; set; }
        // Typed machining/render coordinates. Human-readable AccessFace/Reference are
        // deliberately not parsed by downstream consumers.
        public string AccessFaceId { get; set; }
        public int AccessSlotIndex { get; set; }
        public double AccessSlotAxisOffsetMm { get; set; }
        public int AccessLocalNormalAxis { get; set; }
        public int AccessLocalNormalSign { get; set; }
        public double AccessXmm { get; set; }
        public double AccessYmm { get; set; }
        public double AccessZmm { get; set; }
        public string Tool { get; set; }
        public double? FinalTorqueNm { get; set; }
        public AssemblyDataStatus Status { get; set; }
        public List<string> OpenData { get; private set; }

        public AssemblyConnection() { OpenData = new List<string>(); }
    }

    public enum AssemblyInstructionPhase { Prepare, Preassemble, Insert, Position, Tighten, Inspect }
    public enum AssemblyInstructionGrouping { Individual, EquivalentProfiles }

    public sealed class AssemblyInstructionMaterialItem
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class AssemblyInstructionConnectionPoint
    {
        public string ConnectionId { get; set; }
        public string TappedTraceId { get; set; }
        public string SlotTraceId { get; set; }
        public ProfileEnd TappedEnd { get; set; }
        public int CoreHoleIndex { get; set; }
        public double CoreWidthOffsetMm { get; set; }
        public double CoreHeightOffsetMm { get; set; }
        public double TappedLocalXmm { get; set; }
        public double TappedLocalYmm { get; set; }
        public double TappedLocalZmm { get; set; }
        public double AccessXmm { get; set; }
        public double AccessYmm { get; set; }
        public double AccessZmm { get; set; }
        public string AccessPlane { get; set; }
        public int AccessFaceSign { get; set; }
        public double AccessHoleDiameterMm { get; set; }
        public AssemblyInstructionHardwareRenderGeometry HardwareRender { get; set; }
    }

    /// <summary>
    /// Directly renderable envelope for one standard-connector set. This contract is
    /// presentation-only: its status and open-data fields prevent provisional supplier
    /// dimensions from being reused for CAM or purchasing.
    /// </summary>
    public sealed class AssemblyInstructionHardwareRenderGeometry
    {
        public int ContractVersion { get; set; }
        public string ConnectorId { get; set; }
        public string FastenerId { get; set; }
        public string FastenerHeadStyle { get; set; }
        public double? ConnectorPlateThicknessMm { get; set; }
        public double? ConnectorPlateWidthMm { get; set; }
        public double? ConnectorPlateHeightMm { get; set; }
        public double? ConnectorJawLengthMm { get; set; }
        public double? ConnectorJawWidthMm { get; set; }
        public double? ConnectorJawHeightMm { get; set; }
        public double? ConnectorJawSpacingMm { get; set; }
        public double? BoltShankDiameterMm { get; set; }
        public double? BoltShankLengthMm { get; set; }
        public double? BoltHeadDiameterMm { get; set; }
        public double? BoltHeadHeightMm { get; set; }
        public double? SocketAcrossFlatsMm { get; set; }
        // All offsets use the tapped-profile end plane as zero. Connector/head
        // centers are positive outwards; the shank center is positive inwards.
        public double? ConnectorCenterFromProfileEndMm { get; set; }
        public double? ShankCenterFromProfileEndMm { get; set; }
        public double? HeadCenterFromProfileEndMm { get; set; }
        // Axial jaw projection into the tapped profile from its end plane.
        public double? InsertionTravelMm { get; set; }
        public string Status { get; set; }
        public List<string> OpenData { get; private set; }

        public AssemblyInstructionHardwareRenderGeometry()
        {
            OpenData = new List<string>();
        }
    }

    public sealed class AssemblyInstructionStep
    {
        public int Number { get; set; }
        public AssemblyInstructionPhase Phase { get; set; }
        public string GroupId { get; set; }
        public string GroupLabel { get; set; }
        public string Title { get; set; }
        public string VisualKind { get; set; }
        public bool FocusAssemblyView { get; set; }
        public string PrimaryPart { get; set; }
        public string SecondaryPart { get; set; }
        public string Tool { get; set; }
        public int RepeatCount { get; set; }
        public string Measure { get; set; }
        public bool ShowAssemblyDetail { get; set; }
        public bool MoveAsRigidGroup { get; set; }
        public AssemblyDataStatus Status { get; set; }
        public List<string> ConnectionIds { get; private set; }
        public List<string> PrimaryTraceIds { get; private set; }
        public List<string> SecondaryTraceIds { get; private set; }
        public List<string> MarkerTraceIds { get; private set; }
        public List<AssemblyInstructionMaterialItem> MaterialItems { get; private set; }
        public List<AssemblyInstructionConnectionPoint> ConnectionPoints { get; private set; }
        public List<string> Warnings { get; private set; }

        public AssemblyInstructionStep()
        {
            ConnectionIds = new List<string>();
            PrimaryTraceIds = new List<string>();
            SecondaryTraceIds = new List<string>();
            MarkerTraceIds = new List<string>();
            MaterialItems = new List<AssemblyInstructionMaterialItem>();
            ConnectionPoints = new List<AssemblyInstructionConnectionPoint>();
            Warnings = new List<string>();
        }
    }

    public sealed class AssemblyInstructionPlan
    {
        public bool Available { get; set; }
        public bool SequenceConfirmed { get; set; }
        public string WorkflowId { get; set; }
        public bool CanReleaseForProduction { get; set; }
        public string StatusLabel { get; set; }
        public string ScopeLabel { get; set; }
        public AssemblyInstructionGrouping Grouping { get; set; }
        public bool CanShowIndividualSteps { get; set; }
        public List<AssemblyInstructionStep> Steps { get; private set; }
        public List<string> MissingData { get; private set; }

        public AssemblyInstructionPlan()
        {
            Steps = new List<AssemblyInstructionStep>();
            MissingData = new List<string>();
        }
    }
}
