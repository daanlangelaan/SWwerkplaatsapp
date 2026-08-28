using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class AssemblyInstructionPlanningService
    {
        public AssemblyInstructionPlan Build(WorkbenchModel model, bool groupEquivalentProfiles = true)
        {
            var plan = new AssemblyInstructionPlan();
            plan.Grouping = groupEquivalentProfiles ? AssemblyInstructionGrouping.EquivalentProfiles : AssemblyInstructionGrouping.Individual;
            if (model == null || model.AssemblyConnections.Count == 0)
            {
                plan.Available = false;
                plan.CanReleaseForProduction = false;
                plan.StatusLabel = "Nog geen profielverbindingen vastgelegd";
                return plan;
            }

            plan.Available = true;
            if (groupEquivalentProfiles && string.Equals(model.ProductId, "machinebasis", StringComparison.Ordinal))
                return BuildMachineBasePlan(model, plan);

            plan.WorkflowId = "standard-connector-v1";
            plan.ScopeLabel = "Assemblage van machinaal voorbereid bouwpakket";
            plan.SequenceConfirmed = model.AssemblyConnections.All(IsKnownWorkflow);
            var stepNumber = 1;
            foreach (var instructionGroup in model.AssemblyConnections.GroupBy(c => c.InstructionGroup ?? c.TappedPartName ?? "verbinding"))
            {
                var partitions = groupEquivalentProfiles
                    ? instructionGroup.GroupBy(c => EquivalentGroupingKey(model, c), StringComparer.Ordinal).Select(group => group.ToArray()).ToArray()
                    : instructionGroup.Select(connection => new[] { connection }).ToArray();
                for (var partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
                {
                var connections = partitions[partitionIndex].OrderBy(c => c.ConnectionId, StringComparer.Ordinal).ToArray();
                var first = connections[0];
                var groupLabel = partitions.Length == 1
                    ? instructionGroup.Key
                    : instructionGroup.Key + " · " + EquivalentGroupingLabel(model, first);
                var groupId = instructionGroup.Key + "::" + (partitionIndex + 1).ToString(CultureInfo.InvariantCulture);
                var status = connections.Any(c => c.Status == AssemblyDataStatus.Unresolved)
                    ? AssemblyDataStatus.Unresolved
                    : connections.Any(c => c.Status == AssemblyDataStatus.Provisional)
                        ? AssemblyDataStatus.Provisional
                        : AssemblyDataStatus.Confirmed;

                if (first.JointType == AssemblyJointType.HingeSlidingNut)
                {
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                        "Schuif schuifmoeren in de T-sleuf", "hinge-sliding-nut", first.TappedPartName, first.SlotPartName, null, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                        "Plaats het scharnier op de deur", "hinge-sliding-nut", first.TappedPartName, first.SlotPartName, null, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Position, groupId, groupLabel,
                        "Hang deur in en lijn uit", "hinge-sliding-nut", first.TappedPartName, first.SlotPartName, null, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                        "Draai de scharnieren vast", "hinge-sliding-nut", first.TappedPartName, first.SlotPartName, first.Tool, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Inspect, groupId, groupLabel,
                        "Controleer vrije deurslag", "square-and-flush", first.TappedPartName, first.SlotPartName, null, null, status);
                }
                else
                {
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                        "Monteer bout + verbinder los voor", "preassemble-connector", first.TappedPartName, null, first.Tool, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                        "Schuif verbinder in T-sleuf", "slide-into-slot", first.TappedPartName, first.SlotPartName, null, null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Position, groupId, groupLabel,
                        "Schuif tot de inbuskop uitlijnt", "align-access-hole", first.TappedPartName, first.SlotPartName, null,
                        first.AccessHoleOffsetMm.ToString("0.#") + " mm", status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                        first.FinalTorqueNm.HasValue ? "Draai definitief vast" : "Zet handvast", "allen-through-hole",
                        first.TappedPartName, first.SlotPartName, first.Tool ?? "Inbussleutel",
                        first.FinalTorqueNm.HasValue ? first.FinalTorqueNm.Value.ToString("0.#") + " Nm" : null, status);
                    Add(plan, model, connections, stepNumber++, AssemblyInstructionPhase.Inspect, groupId, groupLabel,
                        "Controleer haaks en vlak", "square-and-flush", first.TappedPartName, first.SlotPartName, "Winkelhaak", null, status);
                }

                foreach (var open in connections.SelectMany(c => c.OpenData).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal))
                    if (!plan.MissingData.Contains(open)) plan.MissingData.Add(open);
                }
            }

            plan.CanShowIndividualSteps = groupEquivalentProfiles && plan.Steps.Any(step => step.RepeatCount > 1);
            plan.CanReleaseForProduction = model.AssemblyConnections.All(c => c.Status == AssemblyDataStatus.Confirmed)
                && plan.MissingData.Count == 0;
            plan.StatusLabel = plan.CanReleaseForProduction
                ? "Productiegegevens compleet"
                : "Voorbeeld gereed · productiegegevens nog aanvullen";
            return plan;
        }

        private static AssemblyInstructionPlan BuildMachineBasePlan(WorkbenchModel model, AssemblyInstructionPlan plan)
        {
            plan.WorkflowId = "machinebase-subassemblies-v1";
            plan.ScopeLabel = "Assemblage van machinaal voorbereid bouwpakket";
            plan.SequenceConfirmed = model.AssemblyConnections.All(IsKnownWorkflow);
            var stepNumber = 1;
            AddLayerSubassembly(plan, model, ref stepNumber, "machinebase-lower", "Onderlaag subassembly", "Onderligger", "Onderframe tussenligger");
            AddLayerSubassembly(plan, model, ref stepNumber, "machinebase-worktop", "Werkbladlaag subassembly", "Bladligger", "Bladframe tussenligger");
            AddLayerSubassembly(plan, model, ref stepNumber, "machinebase-top", "Toplaag subassembly", "Bovenligger", "Topframe tussenligger");

            var doorPrefixes = model.AssemblyConnections
                .Where(connection => connection.JointType == AssemblyJointType.StandardConnector
                    && (connection.InstructionGroup ?? "").EndsWith(" frame", StringComparison.Ordinal)
                    && (connection.InstructionGroup ?? "").StartsWith("Veiligheidsdeur ", StringComparison.Ordinal))
                .Select(connection => connection.InstructionGroup.Substring(0, connection.InstructionGroup.Length - " frame".Length))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            for (var doorIndex = 0; doorIndex < doorPrefixes.Length; doorIndex++)
                AddDoorSubassembly(plan, model, ref stepNumber, doorIndex + 1, doorPrefixes[doorIndex]);

            foreach (var open in model.AssemblyConnections.SelectMany(connection => connection.OpenData)
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
                plan.MissingData.Add(open);
            plan.CanShowIndividualSteps = true;
            plan.CanReleaseForProduction = model.AssemblyConnections.All(connection => connection.Status == AssemblyDataStatus.Confirmed)
                && plan.MissingData.Count == 0;
            plan.StatusLabel = plan.CanReleaseForProduction
                ? "Productiegegevens compleet"
                : "Voorbeeld gereed · productiegegevens nog aanvullen";
            foreach (var step in plan.Steps) step.FocusAssemblyView = true;
            return plan;
        }

        private static void AddLayerSubassembly(AssemblyInstructionPlan plan, WorkbenchModel model, ref int stepNumber,
            string groupId, string groupLabel, string layerName, string intermediateName)
        {
            var intermediate = Connections(model, connection => connection.JointType == AssemblyJointType.StandardConnector
                && (connection.TappedPartName ?? "").StartsWith(intermediateName + " ", StringComparison.Ordinal));
            var coreRails = Connections(model, connection => connection.JointType == AssemblyJointType.StandardConnector
                && string.Equals(connection.InstructionGroup, layerName, StringComparison.Ordinal)
                && (string.Equals(connection.TappedPartName, layerName + " voor", StringComparison.Ordinal)
                    || string.Equals(connection.TappedPartName, layerName + " achter", StringComparison.Ordinal)));
            var outerRails = Connections(model, connection => connection.JointType == AssemblyJointType.StandardConnector
                && string.Equals(connection.InstructionGroup, layerName, StringComparison.Ordinal)
                && (string.Equals(connection.TappedPartName, layerName + " links", StringComparison.Ordinal)
                    || string.Equals(connection.TappedPartName, layerName + " rechts", StringComparison.Ordinal)));
            if (intermediate.Length == 0 || coreRails.Length == 0 || outerRails.Length == 0) return;

            var intermediateCount = DistinctPrimaryCount(intermediate);
            var coreConnections = intermediate.Concat(coreRails)
                .GroupBy(connection => connection.ConnectionId, StringComparer.Ordinal).Select(group => group.First()).ToArray();
            var coreMemberIds = coreConnections.Select(connection => connection.TappedMemberId)
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
            AddGatherStep(plan, model, coreConnections, stepNumber++, groupId, groupLabel,
                "Verzamel de profielen en het verbindingsmateriaal", coreMemberIds);
            AddMachineStep(plan, model, intermediate, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                "Voorzie tussenliggers aan beide koppen van verbinders", "preassemble-connector", intermediate[0].TappedPartName, null,
                intermediate[0].Tool, null, intermediateCount);
            AddMachineStep(plan, model, intermediate, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                "Schuif tussenliggers vanaf de zijkant in de langsliggers", "slide-into-slot", intermediate[0].TappedPartName,
                intermediate[0].SlotPartName, null, null, intermediateCount);
            AddMachineStep(plan, model, intermediate, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                "Positioneer en draai tussenliggers vast", "allen-through-hole", intermediate[0].TappedPartName,
                intermediate[0].SlotPartName, intermediate[0].Tool, null, intermediateCount);
            AddMachineStep(plan, model, coreRails, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                "Voorzie laagframe van verbinders voor de staanders", "preassemble-connector", coreRails[0].TappedPartName, null,
                coreRails[0].Tool, null, DistinctPrimaryCount(coreRails), coreMemberIds);
            AddMachineStep(plan, model, coreRails, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                "Schuif laagframe in de vier staanders", "slide-into-slot", coreRails[0].TappedPartName,
                coreRails[0].SlotPartName, null, null, 1, coreMemberIds, true, true);
            AddMachineStep(plan, model, coreRails, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                "Positioneer en draai laagframe vast", "allen-through-hole", coreRails[0].TappedPartName,
                coreRails[0].SlotPartName, coreRails[0].Tool, null, 1, coreMemberIds, true);
            AddMachineStep(plan, model, outerRails, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                "Voorzie twee buitenliggers aan beide koppen van verbinders", "preassemble-connector", outerRails[0].TappedPartName, null,
                outerRails[0].Tool, null, DistinctPrimaryCount(outerRails));
            AddMachineStep(plan, model, outerRails, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                "Plaats buitenliggers tussen de staanders", "slide-into-slot", outerRails[0].TappedPartName,
                outerRails[0].SlotPartName, null, null, DistinctPrimaryCount(outerRails), null, true);
            AddMachineStep(plan, model, outerRails, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                "Positioneer en draai buitenliggers vast", "allen-through-hole", outerRails[0].TappedPartName,
                outerRails[0].SlotPartName, outerRails[0].Tool, null, DistinctPrimaryCount(outerRails), null, true);
        }

        private static void AddDoorSubassembly(AssemblyInstructionPlan plan, WorkbenchModel model, ref int stepNumber,
            int doorIndex, string doorPrefix)
        {
            var frame = Connections(model, connection => connection.JointType == AssemblyJointType.StandardConnector
                && string.Equals(connection.InstructionGroup, doorPrefix + " frame", StringComparison.Ordinal));
            var hinges = Connections(model, connection => connection.JointType == AssemblyJointType.HingeSlidingNut
                && string.Equals(connection.InstructionGroup, doorPrefix + " scharnieren", StringComparison.Ordinal));
            if (frame.Length == 0 || hinges.Length == 0) return;
            var groupId = "machinebase-door-" + doorIndex.ToString(CultureInfo.InvariantCulture);
            var groupLabel = doorPrefix + " subassembly";
            var doorMemberIds = model.AssemblyPlacements.Where(placement => placement.Kind == AssemblyComponentKind.Profile
                && (placement.PartName ?? "").StartsWith(doorPrefix + " ", StringComparison.Ordinal))
                .Select(placement => placement.MemberId).ToArray();
            var frameCount = DistinctPrimaryCount(frame);
            var groupConnections = frame.Concat(hinges)
                .GroupBy(connection => connection.ConnectionId, StringComparer.Ordinal).Select(group => group.First()).ToArray();
            AddGatherStep(plan, model, groupConnections, stepNumber++, groupId, groupLabel,
                "Verzamel de deurprofielen en het verbindingsmateriaal", doorMemberIds);
            AddMachineStep(plan, model, frame, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                "Voorzie boven- en onderregel aan beide koppen van verbinders", "preassemble-connector", frame[0].TappedPartName, null,
                frame[0].Tool, null, frameCount, doorMemberIds);
            AddMachineStep(plan, model, frame, stepNumber++, AssemblyInstructionPhase.Insert, groupId, groupLabel,
                "Plaats de regels tussen de deurstijlen", "slide-into-slot", frame[0].TappedPartName,
                frame[0].SlotPartName, null, null, frameCount, doorMemberIds);
            AddMachineStep(plan, model, frame, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                "Positioneer en draai het deurframe vast", "allen-through-hole", frame[0].TappedPartName,
                frame[0].SlotPartName, frame[0].Tool, null, 1, doorMemberIds);
            AddMachineStep(plan, model, hinges, stepNumber++, AssemblyInstructionPhase.Preassemble, groupId, groupLabel,
                "Plaats scharnieren met schuifmoeren", "hinge-sliding-nut", hinges[0].TappedPartName,
                hinges[0].SlotPartName, null, null, hinges.Length, doorMemberIds);
            AddMachineStep(plan, model, hinges, stepNumber++, AssemblyInstructionPhase.Tighten, groupId, groupLabel,
                "Hang deur in, positioneer en draai vast", "hinge-sliding-nut", hinges[0].TappedPartName,
                hinges[0].SlotPartName, hinges[0].Tool, null, 1, doorMemberIds);
        }

        private static void AddMachineStep(AssemblyInstructionPlan plan, WorkbenchModel model, AssemblyConnection[] connections,
            int number, AssemblyInstructionPhase phase, string groupId, string groupLabel, string title, string visualKind,
            string primaryPart, string secondaryPart, string tool, string measure, int repeatCount,
            IEnumerable<string> additionalPrimaryMemberIds = null, bool showAssemblyDetail = false, bool moveAsRigidGroup = false)
        {
            Add(plan, model, connections, number, phase, groupId, groupLabel, title, visualKind, primaryPart, secondaryPart,
                tool, measure, StepStatus(connections), repeatCount, additionalPrimaryMemberIds, showAssemblyDetail, moveAsRigidGroup);
        }

        private static void AddGatherStep(AssemblyInstructionPlan plan, WorkbenchModel model, AssemblyConnection[] connections,
            int number, string groupId, string groupLabel, string title, IEnumerable<string> memberIds)
        {
            var ids = (memberIds ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).ToArray();
            var firstPlacement = ids.Select(memberId => Placement(model, memberId)).FirstOrDefault(placement => placement != null);
            var step = new AssemblyInstructionStep
            {
                Number = number,
                Phase = AssemblyInstructionPhase.Prepare,
                GroupId = groupId,
                GroupLabel = groupLabel,
                Title = title,
                VisualKind = "gather-subassembly",
                PrimaryPart = firstPlacement == null ? groupLabel : firstPlacement.PartName,
                RepeatCount = 1,
                Status = StepStatus(connections)
            };
            foreach (var connection in connections) step.ConnectionIds.Add(connection.ConnectionId);
            foreach (var traceId in ids.Select(memberId => TraceId(model, memberId))
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
            {
                step.PrimaryTraceIds.Add(traceId);
                step.MarkerTraceIds.Add(traceId);
            }
            AddGatherMaterials(step, connections);
            if (step.Status != AssemblyDataStatus.Confirmed)
                step.Warnings.Add("Controleer de open profiel- en hardwaredata vóór productievrijgave.");
            plan.Steps.Add(step);
        }

        private static void AddGatherMaterials(AssemblyInstructionStep step, IEnumerable<AssemblyConnection> connections)
        {
            var connectionArray = connections.ToArray();
            foreach (var group in connectionArray.Where(connection => !string.IsNullOrWhiteSpace(connection.ConnectorId))
                .GroupBy(connection => connection.ConnectorId, StringComparer.Ordinal))
                step.MaterialItems.Add(new AssemblyInstructionMaterialItem
                {
                    ItemId = group.Key,
                    Label = ConnectorLabel(group.First()),
                    Category = "Verbinder",
                    Quantity = group.Count()
                });
            foreach (var group in connectionArray.Where(connection => !string.IsNullOrWhiteSpace(connection.FastenerId))
                .GroupBy(connection => connection.FastenerId, StringComparer.Ordinal))
                step.MaterialItems.Add(new AssemblyInstructionMaterialItem
                {
                    ItemId = group.Key,
                    Label = FastenerLabel(group.First()),
                    Category = "Bevestiging",
                    Quantity = group.First().JointType == AssemblyJointType.HingeSlidingNut ? group.Count() * 4 : group.Count()
                });
            var hinges = connectionArray.Where(connection => connection.JointType == AssemblyJointType.HingeSlidingNut).ToArray();
            if (hinges.Length > 0)
                step.MaterialItems.Add(new AssemblyInstructionMaterialItem
                {
                    ItemId = "techxxl_t_slot_nut_8_bridge_m6",
                    Label = "TechXXL T-moer 8 met brug M6 · TIN 100242",
                    Category = "Bevestiging",
                    Quantity = hinges.Length * 4
                });
        }

        private static string ConnectorLabel(AssemblyConnection connection)
        {
            if (connection.JointType == AssemblyJointType.HingeSlidingNut) return "TechXXL scharnier 8 40x40 licht · TIN 102930";
            if (string.Equals(connection.ConnectorId, "techxxl_standard_connector_8_40", StringComparison.Ordinal))
                return "TechXXL standaardverbinder 8 40 · TIN 100342 · M8";
            return connection.ConnectorId;
        }

        private static string FastenerLabel(AssemblyConnection connection)
        {
            if (connection.JointType == AssemblyJointType.HingeSlidingNut)
                return "TechXXL verzonken inbusbout M6×12 · TIN 100691";
            if (string.Equals(connection.FastenerId, "techxxl_button_head_iso7380_m8x25", StringComparison.Ordinal))
                return "TechXXL ISO 7380 M8×25 · TIN 100673";
            return connection.FastenerId;
        }

        private static AssemblyConnection[] Connections(WorkbenchModel model, Func<AssemblyConnection, bool> predicate)
        {
            return model.AssemblyConnections.Where(predicate).OrderBy(connection => connection.ConnectionId, StringComparer.Ordinal).ToArray();
        }

        private static int DistinctPrimaryCount(IEnumerable<AssemblyConnection> connections)
        {
            return connections.Select(connection => connection.TappedMemberId).Distinct(StringComparer.Ordinal).Count();
        }

        private static AssemblyDataStatus StepStatus(IEnumerable<AssemblyConnection> connections)
        {
            return connections.Any(connection => connection.Status == AssemblyDataStatus.Unresolved)
                ? AssemblyDataStatus.Unresolved
                : connections.Any(connection => connection.Status == AssemblyDataStatus.Provisional)
                    ? AssemblyDataStatus.Provisional
                    : AssemblyDataStatus.Confirmed;
        }

        private static bool IsKnownWorkflow(AssemblyConnection connection)
        {
            return string.Equals(connection.WorkflowId, "standard-connector-v1", StringComparison.Ordinal)
                || string.Equals(connection.WorkflowId, "hinge-sliding-nut-v1", StringComparison.Ordinal);
        }

        private static string EquivalentGroupingKey(WorkbenchModel model, AssemblyConnection connection)
        {
            var tapped = Placement(model, connection.TappedMemberId);
            var receiver = Placement(model, connection.SlotMemberId);
            if (tapped == null || receiver == null) return "unresolved|" + connection.ConnectionId;
            return string.Join("|", new[]
            {
                PlacementGeometryKey(tapped), PlacementGeometryKey(receiver), connection.WorkflowId,
                connection.TappedEnd.ToString(), connection.SlotFace, connection.SlotLane,
                connection.ConnectorId, connection.FastenerStandardId, connection.FastenerId,
                connection.AccessFace, connection.FastenerAxisOrder, connection.Tool,
                Number(connection.AccessHoleOffsetMm), Number(connection.AccessHoleDiameterMm),
                connection.FinalTorqueNm.HasValue ? Number(connection.FinalTorqueNm.Value) : "handvast",
                InstallationDirection(tapped, receiver)
            });
        }

        private static string EquivalentGroupingLabel(WorkbenchModel model, AssemblyConnection connection)
        {
            var placement = Placement(model, connection.TappedMemberId);
            if (placement == null) return connection.ConnectionId;
            return LongitudinalAxis(placement) + " · " + Number(LongitudinalLength(placement)) + " mm · kop " + connection.TappedEnd;
        }

        private static AssemblyPlacement Placement(WorkbenchModel model, string memberId)
        {
            return model.AssemblyPlacements.FirstOrDefault(item => string.Equals(item.MemberId, memberId, StringComparison.Ordinal));
        }

        private static string PlacementGeometryKey(AssemblyPlacement placement)
        {
            return string.Join(",", new[]
            {
                Number(placement.LengthMm), Number(placement.WidthMm), Number(placement.HeightMm),
                placement.Orientation.ToString(), Number(placement.RotationXDeg), Number(placement.RotationYDeg), Number(placement.RotationZDeg)
            });
        }

        private static string InstallationDirection(AssemblyPlacement tapped, AssemblyPlacement receiver)
        {
            var values = new[] { receiver.Xmm - tapped.Xmm, receiver.Ymm - tapped.Ymm, receiver.Zmm - tapped.Zmm };
            var axis = Array.IndexOf(values.Select(Math.Abs).ToArray(), values.Select(Math.Abs).Max());
            return new[] { "X", "Y", "Z" }[axis] + (values[axis] < 0 ? "-" : "+");
        }

        private static string LongitudinalAxis(AssemblyPlacement placement)
        {
            var sizes = new[] { placement.LengthMm, placement.WidthMm, placement.HeightMm };
            return new[] { "X", "Y", "Z" }[Array.IndexOf(sizes, sizes.Max())];
        }

        private static double LongitudinalLength(AssemblyPlacement placement)
        {
            return Math.Max(placement.LengthMm, Math.Max(placement.WidthMm, placement.HeightMm));
        }

        private static string Number(double value)
        {
            return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void Add(AssemblyInstructionPlan plan, WorkbenchModel model, AssemblyConnection[] connections, int number,
            AssemblyInstructionPhase phase, string groupId, string groupLabel, string title, string visualKind, string primaryPart,
            string secondaryPart, string tool, string measure, AssemblyDataStatus status, int? repeatCount = null,
            IEnumerable<string> additionalPrimaryMemberIds = null, bool showAssemblyDetail = false, bool moveAsRigidGroup = false)
        {
            var step = new AssemblyInstructionStep
            {
                Number = number,
                Phase = phase,
                GroupId = groupId,
                GroupLabel = groupLabel,
                Title = title,
                VisualKind = visualKind,
                PrimaryPart = primaryPart,
                SecondaryPart = secondaryPart,
                Tool = tool,
                RepeatCount = repeatCount ?? connections.Length,
                Measure = measure,
                ShowAssemblyDetail = showAssemblyDetail,
                MoveAsRigidGroup = moveAsRigidGroup,
                Status = status
            };
            foreach (var connection in connections) step.ConnectionIds.Add(connection.ConnectionId);
            foreach (var connection in connections)
            {
                var point = ConnectionPoint(model, connection);
                if (point != null) step.ConnectionPoints.Add(point);
            }
            foreach (var traceId in connections.Select(connection => TraceId(model, connection.TappedMemberId)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
            {
                step.PrimaryTraceIds.Add(traceId);
                step.MarkerTraceIds.Add(traceId);
            }
            if (additionalPrimaryMemberIds != null)
                foreach (var traceId in additionalPrimaryMemberIds.Select(memberId => TraceId(model, memberId))
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
                    if (!step.PrimaryTraceIds.Contains(traceId)) step.PrimaryTraceIds.Add(traceId);
            foreach (var traceId in connections.Select(connection => TraceId(model, connection.SlotMemberId)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
            {
                step.SecondaryTraceIds.Add(traceId);
                if (!step.MarkerTraceIds.Contains(traceId)) step.MarkerTraceIds.Add(traceId);
            }
            if (phase == AssemblyInstructionPhase.Preassemble) AddGatherMaterials(step, connections);
            if (status != AssemblyDataStatus.Confirmed)
                step.Warnings.Add("Controleer de open profiel- en hardwaredata vóór productievrijgave.");
            plan.Steps.Add(step);
        }

        private static AssemblyInstructionConnectionPoint ConnectionPoint(WorkbenchModel model, AssemblyConnection connection)
        {
            if (connection == null || connection.JointType != AssemblyJointType.StandardConnector || connection.CoreHoleIndex <= 0) return null;
            var tapped = Placement(model, connection.TappedMemberId);
            var receiver = Placement(model, connection.SlotMemberId);
            if (tapped == null || receiver == null) return null;

            var coreService = new ProfileCoreHolePositionService();
            var core = coreService.Build(model, tapped).FirstOrDefault(item => item.CoreHoleIndex == connection.CoreHoleIndex);
            if (core == null)
                throw new InvalidOperationException("Assemblage-instructie geblokkeerd: kernboring K" + connection.CoreHoleIndex
                    + " ontbreekt in de profielmasterdata voor " + tapped.TraceId + ".");

            var tappedDimensions = PlacementDimensions(tapped);
            var tappedAxis = LongitudinalAxisIndex(tapped, tappedDimensions);
            var tappedLocal = new[] { core.LocalXmm, core.LocalYmm, core.LocalZmm };
            tappedLocal[tappedAxis] = (connection.TappedEnd == ProfileEnd.A ? -1 : 1) * tappedDimensions[tappedAxis] / 2.0;
            var tappedWorld = WorldPoint(tapped, tappedLocal);

            var receiverDimensions = PlacementDimensions(receiver);
            var receiverAxis = LongitudinalAxisIndex(receiver, receiverDimensions);
            var fastenerDirection = WorldDirection(tapped, tappedAxis);
            var accessAxis = Array.IndexOf(fastenerDirection.Select(Math.Abs).ToArray(), fastenerDirection.Select(Math.Abs).Max());
            if (accessAxis == receiverAxis)
                throw new InvalidOperationException("Assemblage-instructie geblokkeerd: bevestigingsas loopt evenwijdig aan het ontvangende profiel voor "
                    + connection.ConnectionId + ".");
            var tappedCenter = new[] { tapped.Xmm, tapped.Ymm, tapped.Zmm };
            var receiverCenter = new[] { receiver.Xmm, receiver.Ymm, receiver.Zmm };
            var towardTapped = tappedCenter[accessAxis] - receiverCenter[accessAxis];
            if (Math.Abs(towardTapped) < 0.001) towardTapped = tappedWorld[accessAxis] - receiverCenter[accessAxis];
            var accessFaceSign = towardTapped >= 0 ? -1 : 1;
            var access = (double[])tappedWorld.Clone();
            access[accessAxis] = receiverCenter[accessAxis] + accessFaceSign * receiverDimensions[accessAxis] / 2.0;
            access[receiverAxis] = Math.Max(receiverCenter[receiverAxis] - receiverDimensions[receiverAxis] / 2.0 + 4,
                Math.Min(receiverCenter[receiverAxis] + receiverDimensions[receiverAxis] / 2.0 - 4, tappedWorld[receiverAxis]));

            var hardwareRender = new AssemblyHardwareRenderContractService().Build(connection);
            return new AssemblyInstructionConnectionPoint
            {
                ConnectionId = connection.ConnectionId,
                TappedTraceId = tapped.TraceId,
                SlotTraceId = receiver.TraceId,
                TappedEnd = connection.TappedEnd,
                CoreHoleIndex = connection.CoreHoleIndex,
                CoreWidthOffsetMm = core.WidthOffsetMm,
                CoreHeightOffsetMm = core.HeightOffsetMm,
                TappedLocalXmm = tappedLocal[0],
                TappedLocalYmm = tappedLocal[1],
                TappedLocalZmm = tappedLocal[2],
                AccessXmm = access[0],
                AccessYmm = access[1],
                AccessZmm = access[2],
                AccessPlane = new[] { "x", "y", "z" }[accessAxis],
                AccessFaceSign = accessFaceSign,
                AccessHoleDiameterMm = connection.AccessHoleDiameterMm,
                HardwareRender = hardwareRender
            };
        }

        private static double[] PlacementDimensions(AssemblyPlacement placement)
        {
            return new[] { Math.Max(2, placement.LengthMm), Math.Max(2, placement.HeightMm), Math.Max(2, placement.WidthMm) };
        }

        private static int LongitudinalAxisIndex(AssemblyPlacement placement, double[] dimensions)
        {
            return placement.Sticker != null && placement.Sticker.LongitudinalAxis >= 0 && placement.Sticker.LongitudinalAxis <= 2
                ? placement.Sticker.LongitudinalAxis
                : Array.IndexOf(dimensions, dimensions.Max());
        }

        private static double[] WorldPoint(AssemblyPlacement placement, double[] local)
        {
            var rotated = Rotate(placement, local);
            return new[] { placement.Xmm + rotated[0], placement.Ymm + rotated[1], placement.Zmm + rotated[2] };
        }

        private static double[] WorldDirection(AssemblyPlacement placement, int axis)
        {
            var local = new double[3];
            local[axis] = 1;
            return Rotate(placement, local);
        }

        private static double[] Rotate(AssemblyPlacement placement, double[] value)
        {
            var x = value[0];
            var y = value[1];
            var z = value[2];
            var rx = placement.RotationXDeg * Math.PI / 180.0;
            var ry = placement.RotationYDeg * Math.PI / 180.0;
            var rz = placement.RotationZDeg * Math.PI / 180.0;
            var nextY = y * Math.Cos(rx) - z * Math.Sin(rx);
            z = y * Math.Sin(rx) + z * Math.Cos(rx);
            y = nextY;
            var nextX = x * Math.Cos(ry) + z * Math.Sin(ry);
            z = -x * Math.Sin(ry) + z * Math.Cos(ry);
            x = nextX;
            nextX = x * Math.Cos(rz) - y * Math.Sin(rz);
            y = x * Math.Sin(rz) + y * Math.Cos(rz);
            x = nextX;
            return new[] { x, y, z };
        }

        private static string TraceId(WorkbenchModel model, string memberId)
        {
            var placement = model.AssemblyPlacements.FirstOrDefault(item => string.Equals(item.MemberId, memberId, StringComparison.Ordinal));
            return placement == null ? null : placement.TraceId;
        }

    }
}
