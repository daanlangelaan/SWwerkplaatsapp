using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class ProfileCncOperatorProgramGenerator
    {
        private readonly ProfileCncMachineSettings settings;

        public ProfileCncOperatorProgramGenerator()
            : this(ProfileCncMasterSettings.LoadRequired()) { }

        public ProfileCncOperatorProgramGenerator(ProfileCncMachineSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException("settings");
            this.settings.EnsureValid();
        }

        public string Generate(IEnumerable<ProfileProductionSequenceItem> sequence)
        {
            var items = (sequence ?? Enumerable.Empty<ProfileProductionSequenceItem>()).ToArray();
            var plans = items.Select(item => new ProfileCncPlanningService(settings).Build(item)).ToArray();
            var sb = new StringBuilder();
            sb.AppendLine("%");
            sb.AppendLine("(SWW PROFIEL CNC BOORPROGRAMMA)");
            sb.AppendLine("(CONTRACT " + Safe(settings.ContractId) + ")");
            sb.AppendLine("(WERKWIJZE: DE AFGEZAAGDE KOP WAAR DE STICKER KOMT BLIJFT TEGEN DE VASTE AANSLAG)");
            sb.AppendLine("(KEER HET PROFIEL NOOIT IN DE LENGTERICHTING OM)");
            sb.AppendLine("(BIJ DRAAIEN: KIJK VANAF DE VASTE AANSLAG LANGS HET PROFIEL EN DRAAI MET DE KLOK MEE)");
            sb.AppendLine("G90 G94 G91.1 G40 G49 G17");
            sb.AppendLine("G21");
            sb.AppendLine("G54");
            Park(sb);

            foreach (var plan in plans)
            {
                var item = plan.Item;
                sb.AppendLine();
                sb.AppendLine("(SWW_PROFILE;ORDER=" + item.ProductionOrder.ToString(CultureInfo.InvariantCulture)
                    + ";TRACE_ID=" + Safe(item.TraceId) + ";TYPE=" + ProfileType(item.Material)
                    + ";LENGTH_MM=" + F(item.ProfileLengthMm) + ")");
                var d0 = item.MachiningFrame.Face("D0");
                sb.AppendLine("(PROFIEL " + Safe(item.TraceId) + ": " + ProfileCncOperatorText.CompactFaceName(item.Material, d0)
                    + " BOVEN; STICKER OP " + ProfileCncOperatorText.StickerCentimeters(item) + " CM)");
                sb.AppendLine("(SWW_CLAMP;ANCHOR_END=" + EndText(item.MachiningFrame.X0AnchorEnd) + ";UP_FACE=D0;BED_FACE=D2)");
                AppendOperatorStop(sb, Safe(item.TraceId) + ": " + ProfileCncOperatorText.CompactFaceName(item.Material, d0)
                    + " BOVEN. KOP TEGEN AANSLAG. KLEM.");
                AppendOperatorStop(sb, "STICKER " + Safe(item.TraceId) + " OP " + ProfileCncOperatorText.StickerCentimeters(item)
                    + " CM. MIDDEN BOVEN. START.");

                if (plan.Setups.Count == 0)
                {
                    sb.AppendLine("(GEEN CNC-BOORBEWERKING VOOR DIT PROFIEL; ZAAG- EN TAPWERK STAAN IN APARTE LIJSTEN)");
                    continue;
                }

                var firstSetup = true;
                foreach (var setup in plan.Setups)
                {
                    if (!firstSetup && setup.QuarterTurnsFromPrevious > 0)
                    {
                        sb.AppendLine("M5");
                        Park(sb);
                        EmitRollStops(sb, setup.QuarterTurnsFromPrevious, setup.Face, item.Material);
                    }
                    else if (firstSetup && setup.Face.QuarterTurnsFromD0 > 0)
                    {
                        sb.AppendLine("M5");
                        Park(sb);
                        EmitRollStops(sb, setup.Face.QuarterTurnsFromD0, setup.Face, item.Material);
                    }
                    sb.AppendLine("(BOVEN: " + ProfileCncOperatorText.CompactFaceName(item.Material, setup.Face)
                        + "; HOOGTE " + F(setup.Face.ProfileHeightWhenUpMm) + " MM; " + Safe(VisibleSlots(setup.Face)) + ")");
                    sb.AppendLine("(SWW_SETUP;FACE=" + setup.FaceId + ";HEIGHT_MM=" + F(setup.Face.ProfileHeightWhenUpMm)
                        + ";LOCAL_FACE=" + Safe(setup.Face.LocalFace) + ";SLOTS=" + Safe(SlotList(setup.Face)) + ")");
                    StartSpindle(sb);
                    foreach (var hole in setup.Holes) EmitHole(sb, hole);
                    firstSetup = false;
                }
                sb.AppendLine("M5");
                Park(sb);
            }
            sb.AppendLine();
            sb.AppendLine("M5");
            Park(sb);
            sb.AppendLine("M30");
            sb.AppendLine("%");
            return sb.ToString();
        }

        public string Generate(ProfileProjectConfiguration configuration, IEnumerable<ProfileProductionSequenceItem> sequence)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (configuration.ProductionReleased) return Generate(sequence);

            var sb = new StringBuilder();
            sb.AppendLine("%");
            sb.AppendLine("(SWW PROFIEL CNC NIET VRIJGEGEVEN)");
            sb.AppendLine("(BRON PROFIELCONFIGURATIE.JSON)");
            sb.AppendLine("(CONTROLEER PROFIELCONFIGURATIE-VALIDATIE.TXT)");
            foreach (var blocker in configuration.ProductionBlockers.Take(20))
                sb.AppendLine("(BLOKKADE: " + Safe(blocker) + ")");
            if (configuration.ProductionBlockers.Count > 20)
                sb.AppendLine("(BLOKKADE: NOG " + (configuration.ProductionBlockers.Count - 20).ToString(CultureInfo.InvariantCulture) + " REGELS IN VALIDATIEBESTAND)");
            sb.AppendLine("M5");
            sb.AppendLine("M30");
            sb.AppendLine("%");
            return sb.ToString();
        }

        private void EmitHole(StringBuilder sb, ProfileCncPlannedHole hole)
        {
            var clearance = hole.SurfaceZmm + settings.ClearanceAboveProfileMm;
            var surfaceBreak = Math.Max(hole.FinalZmm, hole.SurfaceZmm - settings.SurfaceBreakthroughMm);
            sb.AppendLine("(BOOR: SLEUF " + hole.SlotIndex.ToString(CultureInfo.InvariantCulture) + " VAN LINKS; "
                + F(hole.MachineXmm) + " MM VAN AANSLAG; DIA " + F(hole.Source.DiameterMm)
                + "; " + (hole.Source.ThroughHole ? "DOOR" : "DIEPTE " + F(hole.Source.DepthMm) + " MM") + ")");
            sb.AppendLine("(SWW_HOLE;FACE=" + hole.FaceId + ";SLOT=" + hole.SlotIndex.ToString(CultureInfo.InvariantCulture)
                + ";X_MM=" + F(hole.MachineXmm) + ";Y_MM=" + F(hole.MachineYmm) + ";DIA_MM=" + F(hole.Source.DiameterMm) + ")");
            sb.AppendLine("G0 Z" + F(clearance));
            sb.AppendLine("G0 X" + F(hole.MachineXmm) + " Y" + F(hole.MachineYmm));
            sb.AppendLine("G0 Z" + F(hole.SurfaceZmm));
            sb.AppendLine("G1 Z" + F(surfaceBreak) + " F" + F(settings.SurfaceFeedMmMin));
            if (hole.FinalZmm < surfaceBreak - 0.001)
                sb.AppendLine("G1 Z" + F(hole.FinalZmm) + " F" + F(settings.DrillFeedMmMin));
            sb.AppendLine("G0 Z" + F(clearance));
        }

        private void StartSpindle(StringBuilder sb)
        {
            sb.AppendLine("S" + F(settings.SpindleRpm) + " M3");
            sb.AppendLine("(WACHT " + F(settings.SpindleSpinUpSeconds) + " SEC OP TOERENTAL)");
            sb.AppendLine("G4 P" + F(settings.SpindleSpinUpSeconds));
        }

        private void Park(StringBuilder sb)
        {
            sb.AppendLine("G0 Z" + F(settings.SafeParkZMm));
            sb.AppendLine("G0 X0");
            sb.AppendLine("G0 Y" + F(settings.SafeParkYMm));
        }

        private static string SlotList(ProfileMachiningFace face)
        {
            if (face.SlotAxisOffsetsMm == null || face.SlotAxisOffsetsMm.Count == 0) return "GEEN";
            return string.Join(", ", face.SlotAxisOffsetsMm.Select((offset, index) => "S" + (index + 1).ToString(CultureInfo.InvariantCulture) + "=Y" + F(offset)));
        }
        private static string VisibleSlots(ProfileMachiningFace face)
        {
            var count = face.SlotAxisOffsetsMm == null ? 0 : face.SlotAxisOffsetsMm.Count;
            if (count == 0) return "GEEN SLEUVEN ZICHTBAAR";
            if (count == 1) return "1 SLEUF ZICHTBAAR";
            return count.ToString(CultureInfo.InvariantCulture) + " SLEUVEN ZICHTBAAR";
        }
        private static void EmitRollStops(StringBuilder sb, int quarterTurns, ProfileMachiningFace targetFace, Material material)
        {
            AppendOperatorStop(sb, "KIJK VANAF AANSLAG. DRAAI " + ProfileCncOperatorText.CompactTurn(quarterTurns) + " RECHTSOM.");
            AppendOperatorStop(sb, ProfileCncOperatorText.CompactFaceName(material, targetFace)
                + " BOVEN. KOP TEGEN AANSLAG. KLEM. START.");
        }
        private static void AppendOperatorStop(StringBuilder sb, string message)
        {
            var line = "M0 (" + message + ")";
            if (line.Length > 96)
                throw new InvalidOperationException("CNC-operatorstop is langer dan 96 tekens: " + line);
            sb.AppendLine(line);
        }
        private static string EndText(ProfileEnd end) { return end == ProfileEnd.A ? "KOP A" : "KOP B"; }
        private static string ProfileType(Material material) { return material == null ? "ONBEKEND" : F(material.WidthMm) + "X" + F(material.HeightMm); }
        private static string Safe(string value) { return (value ?? string.Empty).Replace("(", "[").Replace(")", "]").Replace(";", ","); }
        private static string F(double value) { return value.ToString("0.##", CultureInfo.InvariantCulture); }
    }
}
