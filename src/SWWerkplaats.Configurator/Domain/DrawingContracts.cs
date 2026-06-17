namespace SWWerkplaats.Configurator.Domain
{
    public enum ModelAxis
    {
        X,
        Y,
        Z
    }

    public enum OperationFace
    {
        CenterPlane,
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY,
        PositiveZ,
        NegativeZ
    }

    public enum OperationDepthMode
    {
        Through,
        BlindFromFace,
        PocketFromFace
    }

    public enum SolidWorksBasePlane
    {
        Default,
        Top,
        Front,
        Right
    }

    public sealed class SheetDrawingContract
    {
        public AssemblyOrientation Orientation { get; set; }
        public SolidWorksBasePlane BasePlane { get; set; }
        public ModelAxis LengthAxis { get; set; }
        public ModelAxis WidthAxis { get; set; }
        public ModelAxis ThicknessAxis { get; set; }
        public OperationFace DefaultOperationFace { get; set; }
        public int SheetXSign { get; set; }
        public int SheetYSign { get; set; }
        public string Notes { get; set; }
    }

    public static class DrawingContracts
    {
        public static SheetDrawingContract ForOrientation(AssemblyOrientation orientation)
        {
            switch (orientation)
            {
                case AssemblyOrientation.SheetHorizontal:
                    return new SheetDrawingContract
                    {
                        Orientation = orientation,
                        BasePlane = SolidWorksBasePlane.Top,
                        LengthAxis = ModelAxis.X,
                        WidthAxis = ModelAxis.Z,
                        ThicknessAxis = ModelAxis.Y,
                        DefaultOperationFace = OperationFace.CenterPlane,
                        SheetXSign = 1,
                        SheetYSign = 1,
                        Notes = "Horizontal sheets: sheet X maps to world X, sheet Y maps to world Z."
                    };

                case AssemblyOrientation.SheetVerticalX:
                    return new SheetDrawingContract
                    {
                        Orientation = orientation,
                        BasePlane = SolidWorksBasePlane.Front,
                        LengthAxis = ModelAxis.X,
                        WidthAxis = ModelAxis.Y,
                        ThicknessAxis = ModelAxis.Z,
                        DefaultOperationFace = OperationFace.CenterPlane,
                        SheetXSign = 1,
                        SheetYSign = 1,
                        Notes = "Front/back sheets: sheet X maps to world X, sheet Y maps to world Y."
                    };

                case AssemblyOrientation.SheetVerticalZ:
                    return new SheetDrawingContract
                    {
                        Orientation = orientation,
                        BasePlane = SolidWorksBasePlane.Right,
                        LengthAxis = ModelAxis.Z,
                        WidthAxis = ModelAxis.Y,
                        ThicknessAxis = ModelAxis.X,
                        DefaultOperationFace = OperationFace.CenterPlane,
                        SheetXSign = -1,
                        SheetYSign = 1,
                        Notes = "Side sheets: sheet X is depth from front; SolidWorks Right Plane sketch mirrors this axis."
                    };

                default:
                    return new SheetDrawingContract
                    {
                        Orientation = orientation,
                        BasePlane = SolidWorksBasePlane.Default,
                        LengthAxis = ModelAxis.X,
                        WidthAxis = ModelAxis.Y,
                        ThicknessAxis = ModelAxis.Z,
                        DefaultOperationFace = OperationFace.CenterPlane,
                        SheetXSign = 1,
                        SheetYSign = 1,
                        Notes = "Default orientation has no sheet-specific drawing contract."
                    };
            }
        }
    }
}
