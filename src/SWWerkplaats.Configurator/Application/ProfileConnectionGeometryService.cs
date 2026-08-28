using System;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Resolves every standard connector against the physical profile placements.
    /// The supplier diameter remains authoritative; assembly geometry only determines
    /// the exact D0-D3 face, S1..Sn lane and longitudinal access position.
    /// </summary>
    public sealed class ProfileConnectionGeometryService
    {
        public void Assign(WorkbenchModel model)
        {
            if (model == null || model.AssemblyConnections.Count == 0) return;
            var placements = model.AssemblyPlacements
                .Where(value => value.Kind == AssemblyComponentKind.Profile && !string.IsNullOrWhiteSpace(value.MemberId))
                .GroupBy(value => value.MemberId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var coreService = new ProfileCoreHolePositionService();

            foreach (var connection in model.AssemblyConnections
                .Where(value => value.JointType == AssemblyJointType.StandardConnector))
            {
                AssemblyPlacement tapped;
                AssemblyPlacement receiver;
                if (!placements.TryGetValue(connection.TappedMemberId ?? string.Empty, out tapped)
                    || !placements.TryGetValue(connection.SlotMemberId ?? string.Empty, out receiver))
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " mist een fysiek profielstuk.");

                var tappedMaterial = coreService.MaterialForPlacement(model, tapped);
                var receiverMaterial = coreService.MaterialForPlacement(model, receiver);
                if (tappedMaterial == null || receiverMaterial == null)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " mist profielmateriaal.");
                var core = coreService.Build(tapped, tappedMaterial)
                    .SingleOrDefault(value => value.CoreHoleIndex == connection.CoreHoleIndex);
                if (core == null)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " verwijst naar een ontbrekende kernboring.");

                var tappedDimensions = PlacementDimensions(tapped);
                var tappedAxis = LongitudinalAxis(tapped, tappedDimensions);
                var tappedLocal = new[] { core.LocalXmm, core.LocalYmm, core.LocalZmm };
                tappedLocal[tappedAxis] = (connection.TappedEnd == ProfileEnd.A ? -1 : 1) * tappedDimensions[tappedAxis] / 2.0;
                var tappedWorld = WorldPoint(tapped, tappedLocal);

                var receiverDimensions = PlacementDimensions(receiver);
                var receiverAxis = LongitudinalAxis(receiver, receiverDimensions);
                var fastenerLocal = InverseRotate(receiver, WorldDirection(tapped, tappedAxis));
                var accessAxis = DominantAxis(fastenerLocal);
                if (accessAxis == receiverAxis)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " loopt evenwijdig aan het ontvangende profiel.");

                var tappedCenterLocal = InverseRotate(receiver, new[]
                {
                    tapped.Xmm - receiver.Xmm,
                    tapped.Ymm - receiver.Ymm,
                    tapped.Zmm - receiver.Zmm
                });
                var accessFaceSign = tappedCenterLocal[accessAxis] >= 0 ? -1 : 1;
                var accessLocal = InverseRotate(receiver, new[]
                {
                    tappedWorld[0] - receiver.Xmm,
                    tappedWorld[1] - receiver.Ymm,
                    tappedWorld[2] - receiver.Zmm
                });
                accessLocal[accessAxis] = accessFaceSign * receiverDimensions[accessAxis] / 2.0;
                accessLocal[receiverAxis] = Math.Max(-receiverDimensions[receiverAxis] / 2.0 + 4,
                    Math.Min(receiverDimensions[receiverAxis] / 2.0 - 4, accessLocal[receiverAxis]));

                var frame = new ProfileMachiningFrameService().Build(receiver.TraceId, receiver, receiverMaterial);
                var accessFace = frame.Faces.SingleOrDefault(face => face.LocalNormalAxis == accessAxis && face.LocalNormalSign == accessFaceSign);
                var receivingFace = frame.Faces.SingleOrDefault(face => face.LocalNormalAxis == accessAxis && face.LocalNormalSign == -accessFaceSign);
                if (accessFace == null || receivingFace == null)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " kon niet aan D0-D3 worden gekoppeld.");

                var laneAxis = Enumerable.Range(0, 3).Single(axis => axis != receiverAxis && axis != accessAxis);
                var laneLocalOffset = accessLocal[laneAxis];
                var laneCandidates = accessFace.SlotAxisOffsetsMm
                    .Select((offset, index) => new
                    {
                        Index = index + 1,
                        Offset = offset,
                        LocalOffset = offset - accessFace.FaceSpanMm / 2.0
                    }).ToArray();
                var lane = laneCandidates.OrderBy(value => Math.Abs(value.LocalOffset - laneLocalOffset)).FirstOrDefault();
                if (lane == null || Math.Abs(lane.LocalOffset - laneLocalOffset) > 0.05)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId
                        + " valt niet op een bestaande sleufas: lokaal " + Number(laneLocalOffset)
                        + " mm; kandidaten " + string.Join(", ", laneCandidates.Select(value => Number(value.LocalOffset) + " mm"))
                        + "; getapt=" + tapped.PartName + " afm=" + string.Join("/", tappedDimensions.Select(Number))
                        + " as=" + tappedAxis + " kern=" + string.Join("/", tappedLocal.Select(Number))
                        + "; ontvanger=" + receiver.PartName + " afm=" + string.Join("/", receiverDimensions.Select(Number))
                        + " as=" + receiverAxis + " toegang=" + accessAxis + " baan=" + laneAxis + ".");

                var effectiveClearance = connection.AccessHoleDiameterMm
                    - connection.HexKeyAcrossFlatsMm / Math.Cos(Math.PI / 6.0);
                if (connection.AccessHoleDiameterMm <= 0 || effectiveClearance <= 0)
                    throw new InvalidOperationException("Standaardverbinding " + connection.ConnectionId + " heeft geen bruikbaar leveranciers-toegangsgat.");

                connection.SlotFace = receivingFace.FaceId + " (" + receivingFace.LocalFace + ") naar getapte profielkop";
                connection.SlotLane = accessFace.FaceId + "/S" + lane.Index.ToString(CultureInfo.InvariantCulture)
                    + "; sleufas " + Number(lane.Offset) + " mm over het " + Number(accessFace.FaceSpanMm) + "-mm vlak";
                connection.AccessFace = accessFace.FaceId + " (" + accessFace.LocalFace + ") vrije gereedschapszijde";
                connection.AccessFaceId = accessFace.FaceId;
                connection.AccessSlotIndex = lane.Index;
                connection.AccessSlotAxisOffsetMm = lane.Offset;
                connection.AccessLocalNormalAxis = accessFace.LocalNormalAxis;
                connection.AccessLocalNormalSign = accessFace.LocalNormalSign;
                connection.AccessHoleOffsetMm = Math.Round(accessLocal[receiverAxis] + receiverDimensions[receiverAxis] / 2.0, 3);
                connection.AccessHoleReference = "vanaf kop A van " + receiver.TraceId + "; as op "
                    + accessFace.FaceId + "/S" + lane.Index.ToString(CultureInfo.InvariantCulture);
                connection.ToolPassageClearanceMm = Math.Round(effectiveClearance, 3);
                connection.DrillIncrementMm = 0;
                connection.AccessHoleCalculation = "TechXXL TIN 100342 vereist Ø" + Number(connection.AccessHoleDiameterMm)
                    + " mm; effectieve diametrale ruimte boven SW" + Number(connection.HexKeyAcrossFlatsMm)
                    + " over de hoeken is " + Number(connection.ToolPassageClearanceMm) + " mm";
                var accessWorld = WorldPoint(receiver, accessLocal);
                connection.AccessXmm = Math.Round(accessWorld[0], 3);
                connection.AccessYmm = Math.Round(accessWorld[1], 3);
                connection.AccessZmm = Math.Round(accessWorld[2], 3);
                connection.Status = AssemblyDataStatus.Confirmed;
                connection.OpenData.Clear();
            }
        }

        private static double[] PlacementDimensions(AssemblyPlacement placement)
        {
            return new[] { Math.Max(2, placement.LengthMm), Math.Max(2, placement.HeightMm), Math.Max(2, placement.WidthMm) };
        }

        private static int LongitudinalAxis(AssemblyPlacement placement, double[] dimensions)
        {
            return placement.Sticker != null && placement.Sticker.LongitudinalAxis >= 0 && placement.Sticker.LongitudinalAxis <= 2
                ? placement.Sticker.LongitudinalAxis
                : Array.IndexOf(dimensions, dimensions.Max());
        }

        private static int DominantAxis(double[] direction)
        {
            var absolute = direction.Select(Math.Abs).ToArray();
            var axis = Array.IndexOf(absolute, absolute.Max());
            if (absolute[axis] < 0.999)
                throw new InvalidOperationException("Standaardverbinderas is niet orthogonaal aan het ontvangende profiel.");
            return axis;
        }

        private static double[] WorldPoint(AssemblyPlacement placement, double[] local)
        {
            var rotated = Rotate(placement, local, false);
            return new[] { placement.Xmm + rotated[0], placement.Ymm + rotated[1], placement.Zmm + rotated[2] };
        }

        private static double[] WorldDirection(AssemblyPlacement placement, int axis)
        {
            var local = new double[3];
            local[axis] = 1;
            return Rotate(placement, local, false);
        }

        private static double[] InverseRotate(AssemblyPlacement placement, double[] value)
        {
            return Rotate(placement, value, true);
        }

        private static double[] Rotate(AssemblyPlacement placement, double[] value, bool inverse)
        {
            var x = value[0]; var y = value[1]; var z = value[2];
            var rx = placement.RotationXDeg * Math.PI / 180.0;
            var ry = placement.RotationYDeg * Math.PI / 180.0;
            var rz = placement.RotationZDeg * Math.PI / 180.0;
            if (inverse)
            {
                RotateZ(ref x, ref y, -rz);
                RotateY(ref x, ref z, -ry);
                RotateX(ref y, ref z, -rx);
            }
            else
            {
                RotateX(ref y, ref z, rx);
                RotateY(ref x, ref z, ry);
                RotateZ(ref x, ref y, rz);
            }
            return new[] { x, y, z };
        }

        private static void RotateX(ref double y, ref double z, double radians)
        {
            var nextY = y * Math.Cos(radians) - z * Math.Sin(radians);
            z = y * Math.Sin(radians) + z * Math.Cos(radians);
            y = nextY;
        }

        private static void RotateY(ref double x, ref double z, double radians)
        {
            var nextX = x * Math.Cos(radians) + z * Math.Sin(radians);
            z = -x * Math.Sin(radians) + z * Math.Cos(radians);
            x = nextX;
        }

        private static void RotateZ(ref double x, ref double y, double radians)
        {
            var nextX = x * Math.Cos(radians) - y * Math.Sin(radians);
            y = x * Math.Sin(radians) + y * Math.Cos(radians);
            x = nextX;
        }

        private static string Number(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
