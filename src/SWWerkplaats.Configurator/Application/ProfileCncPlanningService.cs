using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileCncPlanningService
    {
        private readonly ProfileCncMachineSettings settings;

        public ProfileCncPlanningService(ProfileCncMachineSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException("settings");
            this.settings.EnsureValid();
        }

        public ProfileCncPlan Build(ProfileProductionSequenceItem item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (item.Sticker == null || item.MachiningFrame == null)
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": sticker- of machineframe ontbreekt.");
            if (item.MachiningFrame.X0AnchorEnd != item.Sticker.AnchorEnd)
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": X=0-kop wijkt af van de stickerankerkop.");

            var drills = item.Operations.Where(operation => operation.Kind == ProfileOperationKind.Drill).ToArray();
            var plan = new ProfileCncPlan { Item = item };
            if (drills.Length == 0) return plan;
            if (!settings.IsValidated(item.Material))
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": boorparameters voor profielmaat "
                    + ProfileType(item.Material) + " zijn nog niet fysiek gevalideerd. Maak eerst een proefstuk en geef de profielmaat vrij.");

            var planned = drills.Select(operation => PlanHole(item, operation)).ToArray();
            var currentQuarterTurn = 0;
            foreach (var face in item.MachiningFrame.Faces.OrderBy(value => value.QuarterTurnsFromD0))
            {
                var holes = planned.Where(hole => string.Equals(hole.FaceId, face.FaceId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(hole => hole.MachineXmm).ThenBy(hole => hole.MachineYmm).ToArray();
                if (holes.Length == 0) continue;
                var setup = new ProfileCncSetup
                {
                    FaceId = face.FaceId,
                    Face = face,
                    QuarterTurnsFromPrevious = (face.QuarterTurnsFromD0 - currentQuarterTurn + 4) % 4
                };
                foreach (var hole in holes) setup.Holes.Add(hole);
                plan.Setups.Add(setup);
                currentQuarterTurn = face.QuarterTurnsFromD0;
            }
            if (plan.Setups.Sum(setup => setup.Holes.Count) != drills.Length)
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": niet alle boringen konden aan D0-D3 worden gekoppeld.");
            return plan;
        }

        private ProfileCncPlannedHole PlanHole(ProfileProductionSequenceItem item, ProfileOperation operation)
        {
            if (string.IsNullOrWhiteSpace(operation.FaceId))
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": boring mist fysiek vlak D0-D3; vrije tekst '"
                    + (operation.Side ?? "") + "' wordt niet als geometrie gebruikt.");
            var face = item.MachiningFrame.Face(operation.FaceId.Trim());
            if (face == null) throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": onbekend vlak " + operation.FaceId + ".");
            if (operation.DiameterMm <= 0) throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": ongeldige boordiameter.");
            if (operation.PositionFromEndAMm < -0.001 || operation.PositionFromEndAMm > item.ProfileLengthMm + 0.001)
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": boor-X ligt buiten het profiel.");

            var slot = ResolveSlot(item, operation, face);
            var machineX = item.MachiningFrame.X0AnchorEnd == ProfileEnd.A
                ? operation.PositionFromEndAMm
                : item.ProfileLengthMm - operation.PositionFromEndAMm;
            var finalZ = operation.ThroughHole ? -settings.ThroughOvertravelMm : face.ProfileHeightWhenUpMm - operation.DepthMm;
            if (!operation.ThroughHole && (operation.DepthMm <= 0 || finalZ < -0.001))
                throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": blinde boring mist een geldige diepte.");
            return new ProfileCncPlannedHole
            {
                Source = operation,
                FaceId = face.FaceId,
                SlotIndex = slot.Item1,
                MachineXmm = machineX,
                MachineYmm = slot.Item2,
                SurfaceZmm = face.ProfileHeightWhenUpMm,
                FinalZmm = finalZ
            };
        }

        private static Tuple<int, double> ResolveSlot(ProfileProductionSequenceItem item, ProfileOperation operation, ProfileMachiningFace face)
        {
            if (operation.SlotIndex > 0)
            {
                if (face.SlotAxisOffsetsMm == null || operation.SlotIndex > face.SlotAxisOffsetsMm.Count)
                    throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": sleuf S" + operation.SlotIndex
                        + " bestaat niet op " + face.FaceId + " (" + face.FaceSpanMm + " mm-vlak).");
                var offset = face.SlotAxisOffsetsMm[operation.SlotIndex - 1];
                if (operation.SlotAxisOffsetMm > 0 && Math.Abs(operation.SlotAxisOffsetMm - offset) > 0.01)
                    throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": sleufnummer en Y-as spreken elkaar tegen.");
                return Tuple.Create(operation.SlotIndex, offset);
            }
            if (operation.SlotAxisOffsetMm > 0 && face.SlotAxisOffsetsMm != null)
            {
                for (var index = 0; index < face.SlotAxisOffsetsMm.Count; index++)
                    if (Math.Abs(face.SlotAxisOffsetsMm[index] - operation.SlotAxisOffsetMm) <= 0.01)
                        return Tuple.Create(index + 1, face.SlotAxisOffsetsMm[index]);
            }
            throw new InvalidOperationException("CNC-productie geblokkeerd voor " + item.TraceId + ": boring op " + face.FaceId
                + " mist een gevalideerd sleufnummer S1..Sn of een bestaande sleufas-Y.");
        }

        private static string ProfileType(Material material)
        {
            if (material == null) return "onbekend";
            return Math.Round(material.WidthMm).ToString(System.Globalization.CultureInfo.InvariantCulture) + "x"
                + Math.Round(material.HeightMm).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
