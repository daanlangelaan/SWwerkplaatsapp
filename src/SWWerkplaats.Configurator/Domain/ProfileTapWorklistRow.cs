namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileTapWorklistRow
    {
        public int ProductionOrder { get; set; }
        public string TraceId { get; set; }
        public string PartName { get; set; }
        public string MaterialId { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }
        public string End { get; set; }
        public bool StickerEnd { get; set; }
        public string StickerFace { get; set; }
        public double StickerOffsetFromX0Mm { get; set; }
        public string CoreHole { get; set; }
        public double CoreXmm { get; set; }
        public double CoreYmm { get; set; }
        public bool UnderStickerFace { get; set; }
        public string Thread { get; set; }
        public bool TapRequired { get; set; }
        public string Instruction { get; set; }
    }
}
