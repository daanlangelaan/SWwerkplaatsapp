using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class LexWorkbenchConfig
    {
        public string ProductVariant { get; set; }
        public string ProjectName { get; set; }
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double HeightMm { get; set; }
        public double ColumnCenterDistanceMm { get; set; }
        public double ColumnBaseHeightMm { get; set; }
        public double StabilizationPlateHeightMm { get; set; }
        public double StabilizationPlateWidthMm { get; set; }
        public double RailCenterDistanceMm { get; set; }
        public double WorktopCenterSupportOffsetMm { get; set; }
        public double FixedRailFrameWidthMm { get; set; }
        public double FixedRailFrameDepthMm { get; set; }
        public double CarriageCenterDistanceMm { get; set; }
        public List<double> HorizontalLockPositionsMm { get; private set; }
        public double CarriageAdapterLengthMm { get; set; }
        public double CarriageAdapterWidthMm { get; set; }
        public double CarriageAdapterThicknessMm { get; set; }
        public double CarriageAdapterClearanceHoleDiameterMm { get; set; }
        public double CarriageAdapterSlotLengthMm { get; set; }
        public double CarriageAdapterSlotWidthMm { get; set; }
        public double CarriageAdapterSlotPitchMm { get; set; }
        public double CarriageAdapterProfileGrooveOffsetMm { get; set; }
        public double BallTransferHoleDiameterMm { get; set; }
        public double BallTransferBodyDiameterMm { get; set; }
        public double BallTransferFlangeDiameterMm { get; set; }
        public double BallTransferFlangeThicknessMm { get; set; }
        public double BallTransferFlangeRecessDiameterMm { get; set; }
        public double BallTransferFlangeRecessDepthMm { get; set; }
        public double BallTransferInsertionLengthMm { get; set; }
        public double BallTransferBallDiameterMm { get; set; }
        public double BallTransferWorkingHeightMm { get; set; }
        public ProfileSawingMode SawingMode { get; set; }
        public Material Profile80x80 { get; set; }
        public Material Profile40x40 { get; set; }
        public Material TopSheet { get; set; }
        public Material StabilizationSheet { get; set; }
        public Material CarriageAdapterSheet { get; set; }
        public LinearGuideTemplate LinearGuide { get; set; }
        public LiftColumnTemplate LiftColumn { get; set; }
        public LevelingFootCornerAdapterTemplate LevelingFootCornerAdapter { get; set; }
        public LevelingFootTemplate LevelingFoot { get; set; }
        public SwingLatchTemplate SwingLatch { get; set; }
        public double MovingFrameSlotAxisEdgeOffsetMm { get; set; }
        public List<string> DesignNotes { get; private set; }

        public LexWorkbenchConfig()
        {
            DesignNotes = new List<string>();
            HorizontalLockPositionsMm = new List<double>();
        }
    }
}
