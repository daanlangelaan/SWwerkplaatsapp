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

        public static double OuterDepth(double cellDepth, double frontInset, double materialThickness)
        {
            return Math.Max(1, cellDepth) + Math.Max(0, frontInset) + Math.Max(1, materialThickness);
        }

        public static double CellWidthFromOuter(int columns, double outerWidth, double materialThickness)
        {
            var count = Math.Max(1, columns);
            return Math.Max(40, (outerWidth - (count + 1) * materialThickness) / count);
        }

        public static double CellHeightFromOuter(int rows, double outerHeight, double materialThickness)
        {
            var count = Math.Max(1, rows);
            return Math.Max(40, (outerHeight - (count + 1) * materialThickness) / count);
        }

        public static double CellDepthFromOuter(double outerDepth, double frontInset, double materialThickness)
        {
            return Math.Max(40, outerDepth - Math.Max(0, frontInset) - materialThickness);
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
