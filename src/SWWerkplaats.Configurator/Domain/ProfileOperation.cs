using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public enum ProfileOperationKind
    {
        SawCut,
        Drill,
        Tap
    }

    public sealed class ProfileOperation
    {
        public string ProfileId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public Material Material { get; set; }
        public double ProfileLengthMm { get; set; }
        public int Sequence { get; set; }
        public ProfileOperationKind Kind { get; set; }
        // CNC-bewerkingen worden uitsluitend op een fysiek, van D0 afgeleid vlak gepland.
        // Side blijft alleen bestaan voor leesbare/legacy uitvoer en is geen geometrische bron.
        public string FaceId { get; set; }
        // Voor een kopse tapbewerking: de expliciet gekozen kernboring K1..Kn.
        // Aanwezigheid van meerdere kernboringen betekent nooit dat ze allemaal getapt worden.
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
        public List<string> PieceTraceIds { get; private set; }

        public ProfileOperation()
        {
            PieceTraceIds = new List<string>();
        }
    }
}
