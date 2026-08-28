using System;
using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class ProfileCncMachineSettings
    {
        public const string StickerEndAtMachineX0Rule = "sticker_end_against_machine_x0";
        public const string ClockwiseFromX0Rule = "clockwise_viewed_from_x0_to_positive_x";

        public string ContractId { get; set; }
        public double SpindleRpm { get; set; }
        public double SpindleSpinUpSeconds { get; set; }
        public double SafeParkZMm { get; set; }
        public double SafeParkYMm { get; set; }
        public double ClearanceAboveProfileMm { get; set; }
        public double SurfaceBreakthroughMm { get; set; }
        public double ThroughOvertravelMm { get; set; }
        public double SurfaceFeedMmMin { get; set; }
        public double DrillFeedMmMin { get; set; }
        public string X0AnchorRule { get; set; }
        public string RollDirectionRule { get; set; }
        public string SourcePath { get; set; }
        public IList<string> ValidatedProfileTypes { get; private set; }

        public ProfileCncMachineSettings() { ValidatedProfileTypes = new List<string>(); }

        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(ContractId)) throw new InvalidOperationException("Profiel-CNC-contract-ID ontbreekt.");
            Positive(SpindleRpm, "spindeltoerental");
            Positive(SpindleSpinUpSeconds, "wachttijd tot spindeltoerental");
            Positive(SafeParkZMm, "veilige parkeerhoogte Z");
            Positive(SafeParkYMm, "veilige parkeerpositie Y");
            Positive(ClearanceAboveProfileMm, "aanloophoogte");
            Positive(SurfaceBreakthroughMm, "rustige doorbraak");
            Positive(ThroughOvertravelMm, "doorsteek");
            Positive(SurfaceFeedMmMin, "voeding eerste boorfase");
            Positive(DrillFeedMmMin, "voeding vervolgboring");
            if (!string.Equals(X0AnchorRule, StickerEndAtMachineX0Rule, StringComparison.Ordinal))
                throw new InvalidOperationException("Profiel-CNC X0-contract is niet vrijgegeven: " + (X0AnchorRule ?? "<ontbreekt>") + ".");
            if (!string.Equals(RollDirectionRule, ClockwiseFromX0Rule, StringComparison.Ordinal))
                throw new InvalidOperationException("Profiel-CNC rolcontract is niet vrijgegeven: " + (RollDirectionRule ?? "<ontbreekt>") + ".");
            if (ValidatedProfileTypes.Count == 0)
                throw new InvalidOperationException("Profiel-CNC heeft geen fysiek vrijgegeven profielmaten.");
        }

        public bool IsValidated(Material material)
        {
            if (material == null) return false;
            var direct = Dimension(material.WidthMm) + "X" + Dimension(material.HeightMm);
            var reverse = Dimension(material.HeightMm) + "X" + Dimension(material.WidthMm);
            foreach (var profileType in ValidatedProfileTypes)
                if (string.Equals(profileType, direct, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(profileType, reverse, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Dimension(double value)
        {
            return Math.Round(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void Positive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                throw new InvalidOperationException("Profiel-CNC " + label + " moet positief zijn.");
        }
    }
}
