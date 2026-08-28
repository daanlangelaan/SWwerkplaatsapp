using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Derives standard end-to-slot connections from physical profile contacts.
    /// One connection is emitted for every canonical core hole in the tapped end.
    /// </summary>
    public sealed class ProfileAssemblyConnectionDerivationService
    {
        private const double ContactToleranceMm = 0.05;

        public void Assign(WorkbenchModel model)
        {
            if (model == null || !UsesStandardConnectors(model)) return;
            // Een bestaand expliciet verbindingscontract is leidend (bijvoorbeeld
            // machinebasis). Automatische afleiding vult uitsluitend producten aan
            // die nog geen product-specifieke duplicaatlogica hebben.
            if (model.AssemblyConnections.Any(c => c.JointType == AssemblyJointType.StandardConnector)) return;
            var profiles = model.AssemblyPlacements
                .Where(p => p.Kind == AssemblyComponentKind.Profile && !string.IsNullOrWhiteSpace(p.MemberId))
                .ToArray();
            var cores = new ProfileCoreHolePositionService();
            var hardware = new AssemblyHardwareRenderContractService();
            var existing = new HashSet<string>(model.AssemblyConnections
                .Where(c => c.JointType == AssemblyJointType.StandardConnector)
                .Select(Key), StringComparer.OrdinalIgnoreCase);

            foreach (var tapped in profiles)
            {
                var tappedMaterial = cores.MaterialForPlacement(model, tapped);
                if (tappedMaterial == null) continue;
                var tappedDimensions = Dimensions(tapped);
                var tappedAxis = LongitudinalAxis(tapped, tappedDimensions);
                foreach (var end in new[] { ProfileEnd.A, ProfileEnd.B })
                {
                    var sign = end == ProfileEnd.A ? -1.0 : 1.0;
                    var endPoint = new[] { tapped.Xmm, tapped.Ymm, tapped.Zmm };
                    endPoint[tappedAxis] += sign * tappedDimensions[tappedAxis] / 2.0;
                    var receiver = profiles
                        .Where(p => !ReferenceEquals(p, tapped))
                        .Select(p => new { Placement = p, Dimensions = Dimensions(p) })
                        .Where(x => LongitudinalAxis(x.Placement, x.Dimensions) != tappedAxis)
                        .Where(x => IsEndToSideContact(endPoint, tapped, tappedDimensions, tappedAxis, x.Placement, x.Dimensions))
                        .OrderBy(x => DistanceSquared(endPoint, x.Placement))
                        .Select(x => x.Placement)
                        .FirstOrDefault();
                    if (receiver == null) continue;

                    foreach (var core in cores.Build(tapped, tappedMaterial))
                    {
                        var candidate = NewConnection(tapped, receiver, end, core.CoreHoleIndex, hardware);
                        if (existing.Add(Key(candidate))) model.AssemblyConnections.Add(candidate);
                    }
                }
            }
        }

        private static bool UsesStandardConnectors(WorkbenchModel model)
        {
            return model.AssemblyConnections.Any(c => c.JointType == AssemblyJointType.StandardConnector)
                || model.Hardware.Any(h => (h.Name ?? string.Empty).IndexOf("standaardverbinder", StringComparison.OrdinalIgnoreCase) >= 0
                    || (h.ArticleNumber ?? string.Empty).IndexOf("100342", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsEndToSideContact(double[] endPoint, AssemblyPlacement tapped, double[] tappedDimensions,
            int tappedAxis, AssemblyPlacement receiver, double[] receiverDimensions)
        {
            var receiverCenter = new[] { receiver.Xmm, receiver.Ymm, receiver.Zmm };
            var faceDistance = Math.Abs(Math.Abs(endPoint[tappedAxis] - receiverCenter[tappedAxis]) - receiverDimensions[tappedAxis] / 2.0);
            if (faceDistance > ContactToleranceMm) return false;
            var receiverAxis = LongitudinalAxis(receiver, receiverDimensions);
            if (Math.Abs(endPoint[receiverAxis] - receiverCenter[receiverAxis]) > receiverDimensions[receiverAxis] / 2.0 + ContactToleranceMm) return false;
            var thirdAxis = Enumerable.Range(0, 3).Single(axis => axis != tappedAxis && axis != receiverAxis);
            return Math.Abs(endPoint[thirdAxis] - receiverCenter[thirdAxis]) <=
                (tappedDimensions[thirdAxis] + receiverDimensions[thirdAxis]) / 2.0 + ContactToleranceMm;
        }

        private static AssemblyConnection NewConnection(AssemblyPlacement tapped, AssemblyPlacement receiver,
            ProfileEnd end, int coreHoleIndex, AssemblyHardwareRenderContractService hardware)
        {
            const string connectorId = "techxxl_standard_connector_8_40";
            const string fastenerId = "techxxl_button_head_iso7380_m8x25";
            var connection = new AssemblyConnection
            {
                ConnectionId = "auto-" + tapped.MemberId + "-" + end.ToString().ToLowerInvariant() + "-k" + coreHoleIndex.ToString(CultureInfo.InvariantCulture),
                WorkflowId = "standard-connector-v1",
                JointType = AssemblyJointType.StandardConnector,
                InstructionGroup = tapped.PartName,
                TappedMemberId = tapped.MemberId,
                TappedPartName = tapped.PartName,
                TappedEnd = end,
                CoreHoleIndex = coreHoleIndex,
                SlotMemberId = receiver.MemberId,
                SlotPartName = receiver.PartName,
                ConnectorId = connectorId,
                FastenerStandardId = "standard-profile-connector-groove8-m8",
                FastenerId = fastenerId,
                FastenerThreadMm = hardware.RequiredFastenerThreadDiameterMm(fastenerId),
                HexKeyAcrossFlatsMm = hardware.RequiredFastenerSocketAcrossFlatsMm(fastenerId),
                AccessHoleDiameterMm = hardware.RequiredConnectorAccessHoleDiameterMm(connectorId),
                FastenerAxisOrder = "inbuskop → standaardverbinder → M8-bout → getapte profielkern",
                Tool = "inbussleutel SW5",
                Status = AssemblyDataStatus.Provisional
            };
            connection.OpenData.Add("D0-D3-vlak, sleufbaan en toegangsgat worden uit de fysieke profielcontacten afgeleid.");
            return connection;
        }

        private static string Key(AssemblyConnection c)
        {
            return (c.TappedMemberId ?? "") + "|" + c.TappedEnd + "|" + c.CoreHoleIndex + "|" + (c.SlotMemberId ?? "");
        }

        private static double[] Dimensions(AssemblyPlacement p) { return new[] { Math.Max(2, p.LengthMm), Math.Max(2, p.HeightMm), Math.Max(2, p.WidthMm) }; }
        private static int LongitudinalAxis(AssemblyPlacement p, double[] d) { return p.Sticker != null ? p.Sticker.LongitudinalAxis : Array.IndexOf(d, d.Max()); }
        private static double DistanceSquared(double[] point, AssemblyPlacement p) { var x = point[0]-p.Xmm; var y=point[1]-p.Ymm; var z=point[2]-p.Zmm; return x*x+y*y+z*z; }
    }
}
