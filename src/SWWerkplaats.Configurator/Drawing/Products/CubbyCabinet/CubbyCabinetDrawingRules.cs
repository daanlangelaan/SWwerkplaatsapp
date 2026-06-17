using System;

namespace SWWerkplaats.Configurator.Drawing.Products.CubbyCabinet
{
    public static class CubbyCabinetDrawingRules
    {
        public static double OuterWidth(int columns, double cellWidth, double materialThickness)
        {
            return Math.Max(1, columns) * cellWidth + (Math.Max(1, columns) + 1) * materialThickness;
        }

        public static double OuterHeight(int rows, double cellHeight, double materialThickness)
        {
            return Math.Max(1, rows) * cellHeight + (Math.Max(1, rows) + 1) * materialThickness;
        }

        public static double CombSlotDepth(double materialThickness)
        {
            return Math.Max(0, materialThickness / 2.0);
        }

        public static double CombSlotWidth(double materialThickness, double clearance)
        {
            return Math.Max(0, materialThickness + Math.Max(0, clearance));
        }
    }
}
