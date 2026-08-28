using System;

namespace SWWerkplaats.Configurator.Application
{
    public static class FastenerToolAccessCalculator
    {
        public static double RoundHoleDiameterMm(double hexKeyAcrossFlatsMm, double passageClearanceMm, double drillIncrementMm)
        {
            if (hexKeyAcrossFlatsMm <= 0) throw new ArgumentOutOfRangeException("hexKeyAcrossFlatsMm");
            if (passageClearanceMm < 0) throw new ArgumentOutOfRangeException("passageClearanceMm");
            if (drillIncrementMm <= 0) throw new ArgumentOutOfRangeException("drillIncrementMm");

            var keyAcrossCornersMm = hexKeyAcrossFlatsMm / Math.Cos(Math.PI / 6.0);
            var requiredMm = keyAcrossCornersMm + passageClearanceMm;
            return Math.Round(Math.Ceiling(requiredMm / drillIncrementMm) * drillIncrementMm, 3);
        }

        public const string CalculationLabel = "ceil((SW / cos(30°)) + gereedschapspeling, boorstap)";
    }
}
