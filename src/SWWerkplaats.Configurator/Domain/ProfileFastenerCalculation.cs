using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileFastenerCalculation
    {
        public string CalculationId { get; set; }
        public string HardwareArticleNumber { get; set; }
        public string AttachmentKind { get; set; }
        public FastenerDefinition BoltFamily { get; set; }
        public double PassingStackMm { get; set; }
        public double MinimumThreadEngagementMm { get; set; }
        public string ReceivingThreadComponentId { get; set; }
        public string ReceivingThreadSource { get; set; }
        public double? AvailableThreadZoneMm { get; set; }
        public double ThreadInletOffsetMm { get; set; }
        public double MaximumInsertionDepthMm { get; set; }
        public bool ReceivingThreadThroughHole { get; set; }
        public double BottomClearanceMm { get; set; }
        public double? SelectedLengthMm { get; set; }
        public double? SelectedThreadEngagementMm { get; set; }
        public double? RemainingBottomClearanceMm { get; set; }
        public AssemblyDataStatus Status { get; set; }
        public List<string> OpenData { get; private set; }

        public ProfileFastenerCalculation() { OpenData = new List<string>(); }
    }
}
