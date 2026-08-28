using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>Single application-wide resolver for component/plate/rail bolts into profile slots.</summary>
    public sealed class ProfileFastenerCalculationService
    {
        public void Assign(WorkbenchModel model)
        {
            if (model == null) return;
            foreach (var calculation in model.ProfileFastenerCalculations)
            {
                calculation.SelectedLengthMm = null;
                calculation.SelectedThreadEngagementMm = null;
                calculation.RemainingBottomClearanceMm = null;
                calculation.OpenData.Clear();
                if (calculation.BoltFamily == null)
                {
                    Block(calculation, "Boutfamilie en verkrijgbare lengtes ontbreken.");
                    SynchronizeHardware(model, calculation);
                    continue;
                }
                if (!calculation.AvailableThreadZoneMm.HasValue || calculation.AvailableThreadZoneMm.Value <= 0)
                {
                    Block(calculation, "Exacte beschikbare draadzone van de gekozen profielmoer/ontvanger ontbreekt.");
                    SynchronizeHardware(model, calculation);
                    continue;
                }
                if (calculation.AvailableThreadZoneMm.Value + 0.001 < calculation.MinimumThreadEngagementMm)
                {
                    Block(calculation, "Bruikbare draadzone is kleiner dan de vereiste minimale draadaangrijping.");
                    SynchronizeHardware(model, calculation);
                    continue;
                }
                try
                {
                    calculation.SelectedLengthMm = FastenerSelectionService.SelectTSlotBoltLength(
                        calculation.BoltFamily, calculation.PassingStackMm, calculation.ThreadInletOffsetMm,
                        calculation.MinimumThreadEngagementMm, calculation.MaximumInsertionDepthMm,
                        calculation.BottomClearanceMm);
                    calculation.SelectedThreadEngagementMm = Math.Min(calculation.AvailableThreadZoneMm.Value,
                        calculation.SelectedLengthMm.Value - calculation.PassingStackMm - calculation.ThreadInletOffsetMm);
                    calculation.RemainingBottomClearanceMm = calculation.PassingStackMm
                        + calculation.MaximumInsertionDepthMm - calculation.SelectedLengthMm.Value;
                    calculation.Status = AssemblyDataStatus.Confirmed;
                }
                catch (InvalidOperationException ex)
                {
                    Block(calculation, ex.Message);
                }
                SynchronizeHardware(model, calculation);
            }
        }

        private static void Block(ProfileFastenerCalculation calculation, string message)
        {
            calculation.Status = AssemblyDataStatus.Unresolved;
            calculation.OpenData.Add(message);
        }

        private static void SynchronizeHardware(WorkbenchModel model, ProfileFastenerCalculation calculation)
        {
            var hardware = model.Hardware.FirstOrDefault(item => string.Equals(item.ArticleNumber, calculation.HardwareArticleNumber, StringComparison.OrdinalIgnoreCase));
            if (hardware == null) return;
            if (calculation.SelectedLengthMm.HasValue)
            {
                hardware.Note = (hardware.Note ?? string.Empty) + " Centrale boutberekening: " + calculation.SelectedLengthMm.Value.ToString("0.###")
                    + " mm boutlengte = doorvoer " + calculation.PassingStackMm.ToString("0.###")
                    + " mm + draadinlaat " + calculation.ThreadInletOffsetMm.ToString("0.###")
                    + " mm + " + calculation.SelectedThreadEngagementMm.Value.ToString("0.###")
                    + " mm effectieve draadaangrijping; resterende afstand tot sleufbodem "
                    + calculation.RemainingBottomClearanceMm.Value.ToString("0.###") + " mm. Bruikbare moerdraad "
                    + calculation.AvailableThreadZoneMm.Value.ToString("0.###") + " mm"
                    + (string.IsNullOrWhiteSpace(calculation.ReceivingThreadComponentId)
                        ? "."
                        : " van masterdata-component " + calculation.ReceivingThreadComponentId
                            + (calculation.ReceivingThreadThroughHole ? " (doorlopend draadgat)." : " (blind draadgat)."));
            }
            else
            {
                hardware.BomStatus = "OPEN - boutlengte geblokkeerd";
                hardware.Note = (hardware.Note ?? string.Empty) + " OPEN DATA: " + string.Join(" ", calculation.OpenData);
            }
        }
    }
}
