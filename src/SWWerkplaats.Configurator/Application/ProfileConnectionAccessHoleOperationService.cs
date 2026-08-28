using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>Turns confirmed standard connections into receiver-profile drill operations.</summary>
    public sealed class ProfileConnectionAccessHoleOperationService
    {
        public void Assign(WorkbenchModel model, int orderQuantity)
        {
            if (model == null) return;
            var units = Math.Max(1, orderQuantity);
            var placements = model.AssemblyPlacements
                .Where(p => p.Kind == AssemblyComponentKind.Profile && !string.IsNullOrWhiteSpace(p.MemberId) && !string.IsNullOrWhiteSpace(p.TraceId))
                .ToDictionary(p => p.MemberId, StringComparer.OrdinalIgnoreCase);
            var existing = new HashSet<string>(model.ProfileOperations
                .Where(o => o.Kind == ProfileOperationKind.Drill)
                .SelectMany(o => o.PieceTraceIds.Select(t => t + "|" + o.FaceId + "|" + o.SlotIndex + "|" + Math.Round(o.PositionFromEndAMm, 3))),
                StringComparer.OrdinalIgnoreCase);

            foreach (var connection in model.AssemblyConnections.Where(c => c.JointType == AssemblyJointType.StandardConnector))
            {
                AssemblyPlacement placement;
                if (!placements.TryGetValue(connection.SlotMemberId ?? string.Empty, out placement))
                    throw new InvalidOperationException("Toegangsgat geblokkeerd: ontvangend profiel ontbreekt voor " + connection.ConnectionId + ".");
                if (connection.Status != AssemblyDataStatus.Confirmed || string.IsNullOrWhiteSpace(connection.AccessFaceId) || connection.AccessSlotIndex <= 0)
                    throw new InvalidOperationException("Toegangsgat geblokkeerd: bevestigde D0-D3/S-baan ontbreekt voor " + connection.ConnectionId + ".");
                var profile = model.Profiles.Single(p => p.PieceTraceIds.Contains(placement.TraceId));
                var piecesPerUnit = profile.Quantity / units;
                var firstIndex = profile.PieceTraceIds.IndexOf(placement.TraceId);
                for (var unit = 0; unit < units; unit++)
                {
                    var traceId = profile.PieceTraceIds[unit * piecesPerUnit + firstIndex];
                    var key = traceId + "|" + connection.AccessFaceId + "|" + connection.AccessSlotIndex + "|" + Math.Round(connection.AccessHoleOffsetMm, 3);
                    if (!existing.Add(key)) continue;
                    var operation = new ProfileOperation
                    {
                        ProfileId = (profile.Material == null ? "profile" : profile.Material.Id) + "_" + profile.Name.Replace(' ', '_'),
                        PartName = profile.Name,
                        Quantity = 1,
                        Material = profile.Material,
                        ProfileLengthMm = profile.LengthMm,
                        Sequence = 3,
                        Kind = ProfileOperationKind.Drill,
                        FaceId = connection.AccessFaceId,
                        SlotIndex = connection.AccessSlotIndex,
                        SlotAxisOffsetMm = connection.AccessSlotAxisOffsetMm,
                        Side = connection.AccessFace,
                        PositionFromEndAMm = connection.AccessHoleOffsetMm,
                        DiameterMm = connection.AccessHoleDiameterMm,
                        ThroughHole = true,
                        WorkOrigin = "Kop A / " + connection.AccessFaceId,
                        MachineHint = "DRILL_CONNECTOR_ACCESS",
                        ExecutionParty = "WERKPLAATS/LEVERANCIER",
                        Note = "Toegangsgat standaardverbinder " + connection.ConnectionId
                    };
                    operation.PieceTraceIds.Add(traceId);
                    model.ProfileOperations.Add(operation);
                }
            }
        }
    }
}
