using System;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public static class ContourDepthStrategy
    {
        public static double RoughDepthMm(double materialThicknessMm, double onionSkinMm)
        {
            ValidateThickness(materialThicknessMm);
            return Math.Max(0.1, materialThicknessMm - Math.Max(0.1, onionSkinMm));
        }

        public static double FinalDepthMm(double materialThicknessMm, double throughCutOvertravelMm)
        {
            ValidateThickness(materialThicknessMm);
            return materialThicknessMm + Math.Max(0, throughCutOvertravelMm);
        }

        private static void ValidateThickness(double materialThicknessMm)
        {
            if (materialThicknessMm <= 0)
                throw new InvalidOperationException("CAM-export geblokkeerd: plaatdikte moet groter zijn dan 0 mm.");
        }
    }
}
