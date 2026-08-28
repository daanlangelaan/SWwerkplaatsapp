using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileProjectConfigurationService
    {
        public const string CurrentSchemaVersion = "sww-profile-configuration-v1";

        public ProfileProjectConfiguration Build(WorkbenchModel model)
        {
            if (model == null) throw new ArgumentNullException("model");
            var sequence = new ProfileProductionSequenceService().Build(model);
            var placements = model.AssemblyPlacements
                .Where(value => value.Kind == AssemblyComponentKind.Profile)
                .SelectMany(value => TraceIds(value).Select(traceId => new { traceId, value }))
                .GroupBy(value => value.traceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(value => value.Key, value => value.First().value, StringComparer.OrdinalIgnoreCase);
            var placementsByMember = model.AssemblyPlacements
                .Where(value => value.Kind == AssemblyComponentKind.Profile && !string.IsNullOrWhiteSpace(value.MemberId))
                .GroupBy(value => value.MemberId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.OrdinalIgnoreCase);
            var result = new ProfileProjectConfiguration
            {
                SchemaVersion = CurrentSchemaVersion,
                ProductId = model.ProductId,
                ProjectName = model.ProjectName,
                MachineSettings = ProjectMachineSettings(ProfileCncMasterSettings.LoadRequired())
            };

            var geometryCatalog = new ProfileSlotGeometryCatalog();
            foreach (var item in sequence)
            {
                AssemblyPlacement placement;
                placements.TryGetValue(item.TraceId, out placement);
                ProfileSlotGeometry geometry = null;
                try { geometry = geometryCatalog.FindRequired(item.Material.Id); }
                catch (InvalidOperationException) { }
                result.Profiles.Add(ToProjectPiece(item, placement, geometry));
            }
            foreach (var connection in model.AssemblyConnections.OrderBy(value => value.ConnectionId, StringComparer.OrdinalIgnoreCase))
                result.Connections.Add(ToProjectConnection(connection, placementsByMember));

            Validate(result);
            result.ProductionReleased = result.ProductionBlockers.Count == 0;
            return result;
        }

        public string Serialize(ProfileProjectConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            return PrettyJson(new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(configuration));
        }

        public ProfileProjectConfiguration Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Profielconfiguratie is leeg.");
            var value = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<ProfileProjectConfiguration>(json);
            if (value == null || !string.Equals(value.SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Profielconfiguratie heeft een onbekende of ontbrekende schemaversie.");
            Validate(value);
            value.ProductionReleased = value.ProductionBlockers.Count == 0;
            return value;
        }

        public IReadOnlyList<ProfileProductionSequenceItem> ToProductionSequence(ProfileProjectConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            return configuration.Profiles.OrderBy(value => value.ProductionOrder).Select(ToSequenceItem).ToArray();
        }

        public IReadOnlyList<ProfilePart> ToProfileParts(ProfileProjectConfiguration configuration)
        {
            return configuration.Profiles
                .GroupBy(value => value.ProfileId + "|" + value.PartName + "|" + value.MaterialId + "|" + value.LengthMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first = group.First();
                    var part = new ProfilePart
                    {
                        Name = first.PartName,
                        Material = Material(first),
                        LengthMm = first.LengthMm,
                        Quantity = group.Count(),
                        OrientationNote = first.Sticker == null ? "" : first.Sticker.OrientationInstruction
                    };
                    part.PieceTraceIds.AddRange(group.OrderBy(value => value.ProductionOrder).Select(value => value.TraceId));
                    return part;
                }).ToArray();
        }

        public IReadOnlyList<ProfileOperation> ToOperations(ProfileProjectConfiguration configuration)
        {
            return ToProductionSequence(configuration).SelectMany(value => value.Operations).ToArray();
        }

        public IReadOnlyList<AssemblyPlacement> ToPlacements(ProfileProjectConfiguration configuration)
        {
            return configuration.Profiles.Select(piece =>
            {
                var placement = new AssemblyPlacement
                {
                    MemberId = piece.MemberId,
                    TraceId = piece.TraceId,
                    Kind = AssemblyComponentKind.Profile,
                    PartName = piece.PartName,
                    LengthMm = piece.LengthMm,
                    WidthMm = piece.WidthMm,
                    HeightMm = piece.HeightMm,
                    Xmm = piece.AssemblyXmm,
                    Ymm = piece.AssemblyYmm,
                    Zmm = piece.AssemblyZmm,
                    RotationXDeg = piece.RotationXDeg,
                    RotationYDeg = piece.RotationYDeg,
                    RotationZDeg = piece.RotationZDeg,
                    Sticker = Sticker(piece.Sticker)
                };
                AssemblyOrientation orientation;
                if (Enum.TryParse(piece.AssemblyOrientation, out orientation)) placement.Orientation = orientation;
                return placement;
            }).ToArray();
        }

        public ProfileCncMachineSettings ToCncMachineSettings(ProfileProjectConfiguration configuration)
        {
            if (configuration == null || configuration.MachineSettings == null)
                throw new InvalidOperationException("Profielconfiguratie mist CNC-machine-instellingen.");
            var source = configuration.MachineSettings;
            var result = new ProfileCncMachineSettings
            {
                ContractId = source.ContractId, SpindleRpm = source.SpindleRpm,
                SpindleSpinUpSeconds = source.SpindleSpinUpSeconds, SafeParkZMm = source.SafeParkZMm,
                SafeParkYMm = source.SafeParkYMm, ClearanceAboveProfileMm = source.ClearanceAboveProfileMm,
                SurfaceBreakthroughMm = source.SurfaceBreakthroughMm, ThroughOvertravelMm = source.ThroughOvertravelMm,
                SurfaceFeedMmMin = source.SurfaceFeedMmMin, DrillFeedMmMin = source.DrillFeedMmMin,
                X0AnchorRule = source.X0AnchorRule, RollDirectionRule = source.RollDirectionRule,
                SourcePath = "Profielconfiguratie.json (masterdata: " + source.MasterdataSource + ")"
            };
            foreach (var value in source.ValidatedProfileTypes) result.ValidatedProfileTypes.Add(value);
            result.EnsureValid();
            return result;
        }

        private static ProfileProjectPiece ToProjectPiece(ProfileProductionSequenceItem item, AssemblyPlacement placement, ProfileSlotGeometry geometry)
        {
            var piece = new ProfileProjectPiece
            {
                ProductionOrder = item.ProductionOrder,
                TraceId = item.TraceId,
                ProfileId = item.ProfileId,
                PartName = item.PartName,
                MaterialId = item.Material == null ? "" : item.Material.Id,
                MaterialName = item.Material == null ? "" : item.Material.Name,
                WidthMm = item.Material == null ? 0 : item.Material.WidthMm,
                HeightMm = item.Material == null ? 0 : item.Material.HeightMm,
                LengthMm = item.ProfileLengthMm,
                MemberId = placement == null ? "" : placement.MemberId,
                Geometry = ProjectGeometry(geometry),
                AssemblyXmm = placement == null ? 0 : placement.Xmm,
                AssemblyYmm = placement == null ? 0 : placement.Ymm,
                AssemblyZmm = placement == null ? 0 : placement.Zmm,
                RotationXDeg = placement == null ? 0 : placement.RotationXDeg,
                RotationYDeg = placement == null ? 0 : placement.RotationYDeg,
                RotationZDeg = placement == null ? 0 : placement.RotationZDeg,
                AssemblyOrientation = placement == null ? "" : placement.Orientation.ToString(),
                Sticker = ProjectSticker(item.Sticker),
                MachiningFrame = ProjectFrame(item.MachiningFrame),
                ClampInstruction = item.ClampInstruction,
                StickerInstruction = item.StickerInstruction
            };
            foreach (var operation in item.Operations) piece.Operations.Add(ProjectOperation(operation));
            return piece;
        }

        private static ProfileProjectConnection ToProjectConnection(AssemblyConnection source, IDictionary<string, AssemblyPlacement> placementsByMember)
        {
            var exactFace = !string.IsNullOrWhiteSpace(source.AccessFaceId);
            var exactLane = source.AccessSlotIndex > 0;
            var result = new ProfileProjectConnection
            {
                ConnectionId = source.ConnectionId,
                WorkflowId = source.WorkflowId,
                JointType = source.JointType.ToString(),
                InstructionGroup = source.InstructionGroup,
                TappedMemberId = source.TappedMemberId,
                TappedPartName = source.TappedPartName,
                TappedEnd = source.TappedEnd.ToString(),
                CoreHoleIndex = source.CoreHoleIndex,
                SlotMemberId = source.SlotMemberId,
                SlotPartName = source.SlotPartName,
                SlotFace = source.SlotFace,
                SlotLane = source.SlotLane,
                ConnectorId = source.ConnectorId,
                FastenerId = source.FastenerId,
                FastenerStandardId = source.FastenerStandardId,
                FastenerThreadMm = source.FastenerThreadMm,
                HexKeyAcrossFlatsMm = source.HexKeyAcrossFlatsMm,
                ToolPassageClearanceMm = source.ToolPassageClearanceMm,
                DrillIncrementMm = source.DrillIncrementMm,
                AccessHoleDiameterMm = source.AccessHoleDiameterMm,
                AccessHoleCalculation = source.AccessHoleCalculation,
                AccessHoleOffsetMm = source.AccessHoleOffsetMm,
                AccessHoleReference = source.AccessHoleReference,
                AccessFace = source.AccessFace,
                AccessFaceId = source.AccessFaceId,
                AccessSlotIndex = source.AccessSlotIndex,
                AccessSlotAxisOffsetMm = source.AccessSlotAxisOffsetMm,
                AccessXmm = source.AccessXmm,
                AccessYmm = source.AccessYmm,
                AccessZmm = source.AccessZmm,
                FastenerAxisOrder = source.FastenerAxisOrder,
                Tool = source.Tool,
                FinalTorqueNm = source.FinalTorqueNm,
                Status = source.Status.ToString(),
                AccessHoleProductionReady = source.JointType != AssemblyJointType.StandardConnector || (exactFace && exactLane),
                OpenData = source.OpenData.ToList()
            };
            AssemblyPlacement tapped;
            AssemblyPlacement slot;
            var tappedTraceIds = placementsByMember.TryGetValue(source.TappedMemberId ?? "", out tapped) ? TraceIds(tapped).ToArray() : new string[0];
            var slotTraceIds = placementsByMember.TryGetValue(source.SlotMemberId ?? "", out slot) ? TraceIds(slot).ToArray() : new string[0];
            var count = Math.Max(tappedTraceIds.Length, slotTraceIds.Length);
            for (var index = 0; index < count; index++) result.Instances.Add(new ProfileProjectConnectionInstance
            {
                UnitNumber = index + 1,
                TappedTraceId = index < tappedTraceIds.Length ? tappedTraceIds[index] : "",
                SlotTraceId = index < slotTraceIds.Length ? slotTraceIds[index] : ""
            });
            return result;
        }

        private static void Validate(ProfileProjectConfiguration configuration)
        {
            if (configuration.ProductionBlockers == null) configuration.ProductionBlockers = new List<string>();
            else configuration.ProductionBlockers.Clear();
            if (configuration.Profiles == null) configuration.Profiles = new List<ProfileProjectPiece>();
            if (configuration.Connections == null) configuration.Connections = new List<ProfileProjectConnection>();
            if (configuration.MachineSettings == null) configuration.ProductionBlockers.Add("CNC-machine-instellingen ontbreken.");
            foreach (var duplicate in configuration.Profiles.Where(value => !string.IsNullOrWhiteSpace(value.TraceId)).GroupBy(value => value.TraceId, StringComparer.OrdinalIgnoreCase).Where(value => value.Count() > 1))
                configuration.ProductionBlockers.Add("Dubbel profielstuk-ID: " + duplicate.Key + ".");
            foreach (var piece in configuration.Profiles)
            {
                if (string.IsNullOrWhiteSpace(piece.TraceId)) configuration.ProductionBlockers.Add("Profielstuk zonder trace-ID.");
                if (piece.Sticker == null) configuration.ProductionBlockers.Add((piece.TraceId ?? "profiel") + ": stickerplaatsing ontbreekt.");
                if (piece.Geometry == null) configuration.ProductionBlockers.Add((piece.TraceId ?? "profiel") + ": vrijgegeven profielgeometrie ontbreekt.");
                if (piece.MachiningFrame == null || piece.MachiningFrame.Faces == null || piece.MachiningFrame.Faces.Count != 4)
                    configuration.ProductionBlockers.Add((piece.TraceId ?? "profiel") + ": volledig machineframe D0-D3 ontbreekt.");
                if (piece.Operations == null) piece.Operations = new List<ProfileProjectOperation>();
                foreach (var operation in piece.Operations.Where(value => string.Equals(value.Kind, ProfileOperationKind.Drill.ToString(), StringComparison.OrdinalIgnoreCase)))
                    if (string.IsNullOrWhiteSpace(operation.FaceId) || operation.SlotIndex <= 0)
                        configuration.ProductionBlockers.Add(piece.TraceId + ": boring mist exact vlak D0-D3 of sleuf S1..Sn.");
                if (piece.Operations.Any(value => string.Equals(value.Kind, ProfileOperationKind.Drill.ToString(), StringComparison.OrdinalIgnoreCase))
                    && !IsValidatedProfileType(configuration.MachineSettings, piece.WidthMm, piece.HeightMm))
                    configuration.ProductionBlockers.Add(piece.TraceId + ": boorparameters voor profielmaat "
                        + Math.Round(piece.WidthMm) + "x" + Math.Round(piece.HeightMm) + " zijn nog niet fysiek gevalideerd.");
            }
            foreach (var connection in configuration.Connections.Where(value => string.Equals(value.JointType, AssemblyJointType.StandardConnector.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                if (connection.CoreHoleIndex <= 0) configuration.ProductionBlockers.Add(connection.ConnectionId + ": standaardverbinder mist kernboring K1..Kn.");
                if (!connection.AccessHoleProductionReady) configuration.ProductionBlockers.Add(connection.ConnectionId + ": sleuteltoegangsgat mist exact profielvlak en sleufnummer.");
                if (connection.Instances == null || connection.Instances.Count == 0 || connection.Instances.Any(value => string.IsNullOrWhiteSpace(value.TappedTraceId) || string.IsNullOrWhiteSpace(value.SlotTraceId)))
                    configuration.ProductionBlockers.Add(connection.ConnectionId + ": fysieke profielstuk-koppeling is onvolledig.");
            }
            var unique = configuration.ProductionBlockers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            configuration.ProductionBlockers.Clear();
            configuration.ProductionBlockers.AddRange(unique);
        }

        private static bool IsValidatedProfileType(ProfileProjectMachineSettings settings, double widthMm, double heightMm)
        {
            if (settings == null || settings.ValidatedProfileTypes == null) return false;
            var direct = Math.Round(widthMm) + "X" + Math.Round(heightMm);
            var reverse = Math.Round(heightMm) + "X" + Math.Round(widthMm);
            return settings.ValidatedProfileTypes.Any(value => string.Equals(value, direct, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, reverse, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> TraceIds(AssemblyPlacement placement)
        {
            if (placement.Sticker != null && placement.Sticker.TraceIds.Count > 0) return placement.Sticker.TraceIds;
            return string.IsNullOrWhiteSpace(placement.TraceId) ? Enumerable.Empty<string>() : new[] { placement.TraceId };
        }

        private static ProfileProjectSticker ProjectSticker(ProfileStickerPlacement value)
        {
            if (value == null) return null;
            return new ProfileProjectSticker
            {
                FaceId = value.FaceId, LocalFace = value.LocalFace, Rule = value.Rule.ToString(), AnchorEnd = value.AnchorEnd.ToString(),
                OffsetFromAnchorEndMm = value.OffsetFromAnchorEndMm, LongitudinalAxis = value.LongitudinalAxis, FaceAxis = value.FaceAxis,
                FaceSign = value.FaceSign, LocalXmm = value.LocalXmm, LocalYmm = value.LocalYmm, LocalZmm = value.LocalZmm,
                LocalNormalX = value.LocalNormalX, LocalNormalY = value.LocalNormalY, LocalNormalZ = value.LocalNormalZ,
                WorldNormalX = value.WorldNormalX, WorldNormalY = value.WorldNormalY, WorldNormalZ = value.WorldNormalZ,
                LongitudinalSizeMm = value.LongitudinalSizeMm, TransverseSizeMm = value.TransverseSizeMm,
                ObstructionFree = value.ObstructionFree, VisibilityScore = value.VisibilityScore, OrientationInstruction = value.OrientationInstruction
            };
        }

        private static ProfileProjectGeometry ProjectGeometry(ProfileSlotGeometry value)
        {
            if (value == null) return null;
            return new ProfileProjectGeometry
            {
                ProfileSeries = value.ProfileSeries, SlotWidthMm = value.SlotWidthMm, EdgeOffsetMm = value.EdgeOffsetMm,
                PitchMm = value.PitchMm, PerimeterSlotCount = value.CalculatedPerimeterSlotCount,
                CoreHoleCountPerEnd = value.CalculatedCoreHoleCountPerEnd, EndTapThread = value.EndTapThread,
                Status = value.Status,
                WidthFaceAxisOffsetsMm = value.WidthFaceAxisOffsetsMm == null ? new List<double>() : value.WidthFaceAxisOffsetsMm.ToList(),
                HeightFaceAxisOffsetsMm = value.HeightFaceAxisOffsetsMm == null ? new List<double>() : value.HeightFaceAxisOffsetsMm.ToList()
            };
        }

        private static ProfileProjectMachineSettings ProjectMachineSettings(ProfileCncMachineSettings value)
        {
            return new ProfileProjectMachineSettings
            {
                ContractId = value.ContractId, SpindleRpm = value.SpindleRpm, SpindleSpinUpSeconds = value.SpindleSpinUpSeconds,
                SafeParkZMm = value.SafeParkZMm, SafeParkYMm = value.SafeParkYMm,
                ClearanceAboveProfileMm = value.ClearanceAboveProfileMm, SurfaceBreakthroughMm = value.SurfaceBreakthroughMm,
                ThroughOvertravelMm = value.ThroughOvertravelMm, SurfaceFeedMmMin = value.SurfaceFeedMmMin,
                DrillFeedMmMin = value.DrillFeedMmMin, X0AnchorRule = value.X0AnchorRule,
                RollDirectionRule = value.RollDirectionRule, MasterdataSource = value.SourcePath,
                ValidatedProfileTypes = value.ValidatedProfileTypes.ToList()
            };
        }

        private static ProfileProjectMachiningFrame ProjectFrame(ProfileMachiningFrame value)
        {
            if (value == null) return null;
            var result = new ProfileProjectMachiningFrame
            {
                X0AnchorEnd = value.X0AnchorEnd.ToString(), StickerFaceId = value.StickerFaceId,
                RollDirection = value.RollDirection, RollViewDirection = value.RollViewDirection
            };
            foreach (var face in value.Faces) result.Faces.Add(new ProfileProjectMachiningFace
            {
                FaceId = face.FaceId, QuarterTurnsFromD0 = face.QuarterTurnsFromD0, LocalNormalAxis = face.LocalNormalAxis,
                LocalNormalSign = face.LocalNormalSign, LocalFace = face.LocalFace, CrossSectionFace = face.CrossSectionFace,
                FaceSpanMm = face.FaceSpanMm, ProfileHeightWhenUpMm = face.ProfileHeightWhenUpMm,
                SlotAxisOffsetsMm = face.SlotAxisOffsetsMm == null ? new List<double>() : face.SlotAxisOffsetsMm.ToList()
            });
            return result;
        }

        private static ProfileProjectOperation ProjectOperation(ProfileOperation value)
        {
            return new ProfileProjectOperation
            {
                Sequence = value.Sequence, Kind = value.Kind.ToString(), FaceId = value.FaceId, CoreHoleIndex = value.CoreHoleIndex,
                SlotIndex = value.SlotIndex, SlotAxisOffsetMm = value.SlotAxisOffsetMm, Side = value.Side,
                PositionFromEndAMm = value.PositionFromEndAMm, DiameterMm = value.DiameterMm, DepthMm = value.DepthMm,
                ThroughHole = value.ThroughHole, SawAngleDeg = value.SawAngleDeg, WorkOrigin = value.WorkOrigin,
                MachineHint = value.MachineHint, ExecutionParty = value.ExecutionParty, Note = value.Note
            };
        }

        private static ProfileProductionSequenceItem ToSequenceItem(ProfileProjectPiece piece)
        {
            var item = new ProfileProductionSequenceItem
            {
                ProductionOrder = piece.ProductionOrder, TraceId = piece.TraceId, ProfileId = piece.ProfileId,
                PartName = piece.PartName, Material = Material(piece), ProfileLengthMm = piece.LengthMm,
                Sticker = Sticker(piece.Sticker), MachiningFrame = Frame(piece.TraceId, piece.MachiningFrame),
                ClampInstruction = piece.ClampInstruction, StickerInstruction = piece.StickerInstruction
            };
            foreach (var source in piece.Operations)
            {
                ProfileOperationKind kind;
                if (!Enum.TryParse(source.Kind, out kind)) throw new InvalidOperationException(piece.TraceId + ": onbekende profielbewerking " + source.Kind + ".");
                var operation = new ProfileOperation
                {
                    ProfileId = piece.ProfileId, PartName = piece.PartName, Quantity = 1, Material = item.Material,
                    ProfileLengthMm = piece.LengthMm, Sequence = source.Sequence, Kind = kind, FaceId = source.FaceId,
                    CoreHoleIndex = source.CoreHoleIndex, SlotIndex = source.SlotIndex, SlotAxisOffsetMm = source.SlotAxisOffsetMm,
                    Side = source.Side, PositionFromEndAMm = source.PositionFromEndAMm, DiameterMm = source.DiameterMm,
                    DepthMm = source.DepthMm, ThroughHole = source.ThroughHole, SawAngleDeg = source.SawAngleDeg,
                    WorkOrigin = source.WorkOrigin, MachineHint = source.MachineHint, ExecutionParty = source.ExecutionParty, Note = source.Note
                };
                operation.PieceTraceIds.Add(piece.TraceId);
                item.Operations.Add(operation);
            }
            return item;
        }

        private static Material Material(ProfileProjectPiece piece)
        {
            return new Material { Id = piece.MaterialId, Name = piece.MaterialName, Kind = MaterialKind.Profile, WidthMm = piece.WidthMm, HeightMm = piece.HeightMm };
        }

        private static ProfileStickerPlacement Sticker(ProfileProjectSticker value)
        {
            if (value == null) return null;
            ProfileStickerPlacementRule rule;
            ProfileEnd end;
            if (!Enum.TryParse(value.Rule, out rule)) throw new InvalidOperationException("Onbekende stickerplaatsingsregel " + value.Rule + ".");
            if (!Enum.TryParse(value.AnchorEnd, out end)) throw new InvalidOperationException("Onbekende stickerankerkop " + value.AnchorEnd + ".");
            return new ProfileStickerPlacement
            {
                FaceId = value.FaceId, LocalFace = value.LocalFace, Rule = rule, AnchorEnd = end,
                OffsetFromAnchorEndMm = value.OffsetFromAnchorEndMm, LongitudinalAxis = value.LongitudinalAxis,
                FaceAxis = value.FaceAxis, FaceSign = value.FaceSign, LocalXmm = value.LocalXmm, LocalYmm = value.LocalYmm,
                LocalZmm = value.LocalZmm, LocalNormalX = value.LocalNormalX, LocalNormalY = value.LocalNormalY,
                LocalNormalZ = value.LocalNormalZ, WorldNormalX = value.WorldNormalX, WorldNormalY = value.WorldNormalY,
                WorldNormalZ = value.WorldNormalZ, LongitudinalSizeMm = value.LongitudinalSizeMm,
                TransverseSizeMm = value.TransverseSizeMm, ObstructionFree = value.ObstructionFree,
                VisibilityScore = value.VisibilityScore, OrientationInstruction = value.OrientationInstruction
            };
        }

        private static ProfileMachiningFrame Frame(string traceId, ProfileProjectMachiningFrame value)
        {
            if (value == null) return null;
            ProfileEnd end;
            if (!Enum.TryParse(value.X0AnchorEnd, out end)) throw new InvalidOperationException(traceId + ": onbekende X=0-kop.");
            var frame = new ProfileMachiningFrame
            {
                TraceId = traceId, X0AnchorEnd = end, StickerFaceId = value.StickerFaceId,
                RollDirection = value.RollDirection, RollViewDirection = value.RollViewDirection
            };
            foreach (var face in value.Faces) frame.Faces.Add(new ProfileMachiningFace
            {
                FaceId = face.FaceId, QuarterTurnsFromD0 = face.QuarterTurnsFromD0,
                LocalNormalAxis = face.LocalNormalAxis, LocalNormalSign = face.LocalNormalSign,
                LocalFace = face.LocalFace, CrossSectionFace = face.CrossSectionFace,
                FaceSpanMm = face.FaceSpanMm, ProfileHeightWhenUpMm = face.ProfileHeightWhenUpMm,
                SlotAxisOffsetsMm = face.SlotAxisOffsetsMm == null ? new List<double>() : face.SlotAxisOffsetsMm.ToList()
            });
            return frame;
        }

        private static string PrettyJson(string json)
        {
            var sb = new StringBuilder(json.Length + json.Length / 4);
            var indent = 0;
            var inString = false;
            var escaped = false;
            for (var index = 0; index < json.Length; index++)
            {
                var ch = json[index];
                if (inString)
                {
                    sb.Append(ch);
                    if (escaped) escaped = false;
                    else if (ch == '\\') escaped = true;
                    else if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '"') { inString = true; sb.Append(ch); continue; }
                if (ch == '{' || ch == '[') { sb.Append(ch).AppendLine(); indent++; AppendIndent(sb, indent); continue; }
                if (ch == '}' || ch == ']') { sb.AppendLine(); indent--; AppendIndent(sb, indent); sb.Append(ch); continue; }
                if (ch == ',') { sb.Append(ch).AppendLine(); AppendIndent(sb, indent); continue; }
                if (ch == ':') { sb.Append(": "); continue; }
                sb.Append(ch);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendIndent(StringBuilder sb, int indent)
        {
            for (var index = 0; index < indent; index++) sb.Append("  ");
        }
    }
}
