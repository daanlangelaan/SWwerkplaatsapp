using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileCncPlannedHole
    {
        public ProfileOperation Source { get; set; }
        public string FaceId { get; set; }
        public int SlotIndex { get; set; }
        public double MachineXmm { get; set; }
        public double MachineYmm { get; set; }
        public double SurfaceZmm { get; set; }
        public double FinalZmm { get; set; }
    }

    public sealed class ProfileCncSetup
    {
        public string FaceId { get; set; }
        public int QuarterTurnsFromPrevious { get; set; }
        public ProfileMachiningFace Face { get; set; }
        public IList<ProfileCncPlannedHole> Holes { get; private set; }
        public ProfileCncSetup() { Holes = new List<ProfileCncPlannedHole>(); }
    }

    public sealed class ProfileCncPlan
    {
        public ProfileProductionSequenceItem Item { get; set; }
        public IList<ProfileCncSetup> Setups { get; private set; }
        public ProfileCncPlan() { Setups = new List<ProfileCncSetup>(); }
    }
}
