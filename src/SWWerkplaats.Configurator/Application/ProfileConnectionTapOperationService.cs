using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileConnectionTapOperationService
    {
        public void Assign(WorkbenchModel model, int orderQuantity)
        {
            if (model == null || model.AssemblyConnections.Count == 0) return;

            var units = Math.Max(1, orderQuantity);
            var placements = model.AssemblyPlacements
                .Where(placement => placement.Kind == AssemblyComponentKind.Profile
                    && !string.IsNullOrWhiteSpace(placement.MemberId)
                    && !string.IsNullOrWhiteSpace(placement.TraceId))
                .GroupBy(placement => placement.MemberId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var existing = ExistingTapKeys(model.ProfileOperations);

            foreach (var connection in model.AssemblyConnections
                .Where(connection => connection.JointType == AssemblyJointType.StandardConnector)
                .OrderBy(connection => connection.ConnectionId, StringComparer.Ordinal))
            {
                AssemblyPlacement placement;
                if (string.IsNullOrWhiteSpace(connection.TappedMemberId)
                    || !placements.TryGetValue(connection.TappedMemberId, out placement))
                    throw new InvalidOperationException("Tapbewerking geblokkeerd: standaardverbinding "
                        + connection.ConnectionId + " heeft geen gekoppeld fysiek profiel.");

                var profile = model.Profiles.SingleOrDefault(item => item.PieceTraceIds.Contains(placement.TraceId));
                if (profile == null)
                    throw new InvalidOperationException("Tapbewerking geblokkeerd: profieltrace ontbreekt voor standaardverbinding "
                        + connection.ConnectionId + ".");
                if (profile.Quantity % units != 0)
                    throw new InvalidOperationException("Tapbewerking geblokkeerd: profielaantal kan niet over orderunits worden verdeeld.");

                var piecesPerUnit = profile.Quantity / units;
                var firstUnitPieceIndex = profile.PieceTraceIds.IndexOf(placement.TraceId);
                if (firstUnitPieceIndex < 0 || firstUnitPieceIndex >= piecesPerUnit)
                    throw new InvalidOperationException("Tapbewerking geblokkeerd: ongeldige profielindex voor " + placement.TraceId + ".");

                if (connection.CoreHoleIndex <= 0)
                    throw new InvalidOperationException("Tapbewerking geblokkeerd: standaardverbinding " + connection.ConnectionId
                        + " mist een expliciete kernboring K1..Kn.");

                for (var unit = 0; unit < units; unit++)
                {
                    var traceId = profile.PieceTraceIds[unit * piecesPerUnit + firstUnitPieceIndex];
                    var end = EndLabel(connection.TappedEnd);
                    if (!existing.Add(traceId + "|" + end + "|K" + connection.CoreHoleIndex)) continue;

                    var operation = new ProfileOperation
                    {
                        ProfileId = (profile.Material == null ? "profile" : profile.Material.Id) + "_" + profile.Name.Replace(' ', '_'),
                        PartName = profile.Name,
                        Quantity = 1,
                        Material = profile.Material,
                        ProfileLengthMm = profile.LengthMm,
                        Sequence = 2,
                        Kind = ProfileOperationKind.Tap,
                        CoreHoleIndex = connection.CoreHoleIndex,
                        Side = end,
                        PositionFromEndAMm = connection.TappedEnd == ProfileEnd.A ? 0 : profile.LengthMm,
                        DiameterMm = connection.FastenerThreadMm > 0 ? connection.FastenerThreadMm : 8,
                        ThroughHole = false,
                        WorkOrigin = end,
                        MachineHint = "TAP_M8",
                        ExecutionParty = "WERKPLAATS/LEVERANCIER",
                        Note = "M8 draad in kernboring voor standaardverbinder " + connection.ConnectionId
                    };
                    operation.PieceTraceIds.Add(traceId);
                    model.ProfileOperations.Add(operation);
                }
            }
        }

        private static HashSet<string> ExistingTapKeys(IEnumerable<ProfileOperation> operations)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var operation in operations.Where(operation => operation.Kind == ProfileOperationKind.Tap))
            foreach (var traceId in operation.PieceTraceIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (operation.CoreHoleIndex <= 0) continue;
                if (AppliesToEnd(operation.Side, "Kop A")) keys.Add(traceId + "|Kop A|K" + operation.CoreHoleIndex);
                if (AppliesToEnd(operation.Side, "Kop B")) keys.Add(traceId + "|Kop B|K" + operation.CoreHoleIndex);
            }
            return keys;
        }

        private static bool AppliesToEnd(string side, string end)
        {
            var text = side ?? string.Empty;
            return text.IndexOf("A/B", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf(end, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string EndLabel(ProfileEnd end)
        {
            return end == ProfileEnd.A ? "Kop A" : "Kop B";
        }
    }
}
