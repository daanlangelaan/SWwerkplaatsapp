using System;

namespace SWWerkplaats.Configurator.Domain
{
    public static class ProductDrawingStrategy
    {
        public const double DefaultAlignmentGrooveDepthMm = 3.0;
        public const double DefaultAlignmentGrooveClearanceMm = 1.0;
        public const double DefaultDrawerGrooveDepthMm = 3.0;
        public const double DefaultDrawerGrooveClearanceMm = 0.8;

        public static double GrooveDepthForMaterial(Material material)
        {
            if (material == null) return DefaultAlignmentGrooveDepthMm;
            return Math.Min(DefaultAlignmentGrooveDepthMm, Math.Max(0.1, material.ThicknessMm - 0.1));
        }

        public static double PlateSizeWithOppositeGrooveInsertion(double clearSizeMm, double grooveDepthMm)
        {
            return clearSizeMm + 2.0 * Math.Max(0, grooveDepthMm);
        }

        public static double PlateSizeWithSingleGrooveInsertion(double clearSizeMm, double grooveDepthMm)
        {
            return clearSizeMm + Math.Max(0, grooveDepthMm);
        }

        public static double CenterOffsetForSingleGrooveInsertion(double grooveDepthMm)
        {
            return Math.Max(0, grooveDepthMm) / 2.0;
        }
    }
}
