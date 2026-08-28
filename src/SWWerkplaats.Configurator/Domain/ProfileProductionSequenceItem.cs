using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileProductionSequenceItem
    {
        public int ProductionOrder { get; set; }
        public string TraceId { get; set; }
        public string ProfileId { get; set; }
        public string PartName { get; set; }
        public Material Material { get; set; }
        public double ProfileLengthMm { get; set; }
        public ProfileStickerPlacement Sticker { get; set; }
        public ProfileMachiningFrame MachiningFrame { get; set; }
        public string ClampInstruction { get; set; }
        public string StickerInstruction { get; set; }
        public List<ProfileOperation> Operations { get; private set; }

        public ProfileProductionSequenceItem()
        {
            Operations = new List<ProfileOperation>();
        }
    }
}
