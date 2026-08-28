using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalAssembly3DService
    {
        public List<PortalAssemblyPart> Build(WorkbenchModel model, PortalQuoteRequest request)
        {
            LexWorkbenchConfig lexConfig = null;
            if (request != null &&
                (string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)))
            {
                var factory = new PortalConfigurationFactory();
                lexConfig = string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)
                    ? factory.BuildLexRevolutionWorkbench(request)
                    : factory.BuildLexWorkbench(request);
            }
            var parts = BuildFromPlacements(model, lexConfig);
            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                var config = new PortalConfigurationFactory().BuildWorkbenchCabinet(request);
                AddWorkbenchCabinetFeet(parts, config);
                AddWorkbenchCabinetPlinthAdapters(parts, config);
            }
            else if (request != null &&
                (string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)))
            {
                AddLexBallTransferUnits(parts, lexConfig);
            }

            AddProfileConnectionAccessHoles(parts, model);
            ApplyPresentationMetadata(parts);
            if (lexConfig != null || (request != null && string.Equals(request.Product, "hoogteverstelbare_werktafel", StringComparison.OrdinalIgnoreCase)))
                PortalMotionContractService.ApplyPartMotionMetadata(parts);
            return parts;
        }

        private static void ApplyPresentationMetadata(IEnumerable<PortalAssemblyPart> parts)
        {
            foreach (var part in parts)
            {
                if (part == null) continue;
                var name = (part.Name ?? string.Empty).ToLowerInvariant();
                part.SuppressSideHoleMarkers = name.StartsWith("zijwand");
                if (name.StartsWith("draaideur ") || name.StartsWith("schuifdeur u")) part.VisibilityGroup = "door";
                else if (name.Contains("kogelpotblad")) part.VisibilityGroup = "optional-top";

                if (!string.IsNullOrWhiteSpace(part.AppearanceRole)) { }
                else if (name.StartsWith("custom ")) part.AppearanceRole = "custom-black";
                else if (string.Equals(part.Shape, "pa40-hinge", StringComparison.OrdinalIgnoreCase)
                    || (part.Shape ?? string.Empty).StartsWith("black-hole-", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("zwarte eindkap") || name.Contains("zwarte spleet") || name.Contains("liangyue ly103-12 clip")) part.AppearanceRole = "black-hardware";
                else if ((part.Kind ?? string.Empty).StartsWith("hardware-cabinet", StringComparison.OrdinalIgnoreCase)) part.AppearanceRole = "cabinet-hardware";
                else if (string.Equals(part.Shape, "acrylic-panel", StringComparison.OrdinalIgnoreCase)) part.AppearanceRole = "transparent-panel";
                else if (name.Contains("kogelpotblad")) part.AppearanceRole = "primary-surface";
                else if (name.Contains("schwenkriegel") && name.Contains("rode kap")) part.AppearanceRole = "swing-latch-cap";
                else if (name.Contains("schwenkriegel")) part.AppearanceRole = "swing-latch-body";
                else if (name.Contains("afdekkap")) part.AppearanceRole = "end-cap";
                else if (name.Contains("stabilisatieplaat")) part.AppearanceRole = "stabilizer";
                else if (name.Contains("hoekadapter") || name.Contains("adapterplaat") || name.Contains("stellfußsockel")) part.AppearanceRole = "adapter";
                else if (name.Contains("hte2")) part.AppearanceRole = "lifting-column";
                else if (name.Contains("hsr15r wagen")) part.AppearanceRole = "guide-carriage";
                else if (name.Contains("hsr15")) part.AppearanceRole = "linear-guide";
                else if (name.Contains("eindstop") || name.Contains("stelvoet") || name.Contains("stelpoot") || name.Contains("plunjer")) part.AppearanceRole = "dark-hardware";
                else if (name.Contains("vast railframe")) part.AppearanceRole = "fixed-frame";
                else if (name.Contains("schuifframe") || name.Contains("meebewegend") || name.Contains("tafelbladframe")) part.AppearanceRole = "moving-frame";
                else if (name.Contains("voetprofiel")) part.AppearanceRole = "foot-profile";
                else if (name.Contains("kogelpot")) part.AppearanceRole = "ball-transfer";
                else if (string.Equals(part.Kind, "sheet", StringComparison.OrdinalIgnoreCase)) part.AppearanceRole = "sheet";
                else if (string.Equals(part.Kind, "profile", StringComparison.OrdinalIgnoreCase)) part.AppearanceRole = "profile";
                else if ((part.Kind ?? string.Empty).StartsWith("hardware", StringComparison.OrdinalIgnoreCase)) part.AppearanceRole = "hardware";
                else part.AppearanceRole = "generic";

                foreach (var hole in part.Holes)
                {
                    if (!string.IsNullOrWhiteSpace(hole.VisualRole)) continue;
                    var holeName = (hole.Name ?? string.Empty).ToLowerInvariant();
                    hole.VisualRole = holeName.StartsWith("kogelpot ") ? "ball-seat"
                        : (holeName.StartsWith("toegangsgat standaardverbinder") ? "connector-access" : "standard");
                }
                foreach (var pocket in part.Pockets)
                {
                    var pocketName = (pocket.Name ?? string.Empty).ToLowerInvariant();
                    pocket.VisualRole = pocketName.Contains("handgreep") ? "handle-cutout" : "standard";
                }
            }
        }

        private static void AddLexBallTransferUnits(List<PortalAssemblyPart> parts, LexWorkbenchConfig config)
        {
            if (parts == null || config == null) return;
            var top = parts.Find(part => part != null && string.Equals(part.Name, "Kogelpotblad HPL", StringComparison.OrdinalIgnoreCase));
            if (top == null) return;

            var surfaceY = top.Ymm + top.SizeYmm / 2.0;
            var totalHeight = config.BallTransferInsertionLengthMm + config.BallTransferFlangeRecessDepthMm + config.BallTransferWorkingHeightMm;
            var centerY = surfaceY + (config.BallTransferWorkingHeightMm - config.BallTransferInsertionLengthMm - config.BallTransferFlangeRecessDepthMm) / 2.0;
            foreach (var hole in top.Holes)
            {
                if (hole == null || string.IsNullOrWhiteSpace(hole.Name) || !hole.Name.StartsWith("Kogelpot ", StringComparison.OrdinalIgnoreCase)) continue;
                parts.Add(new PortalAssemblyPart
                {
                    Name = hole.Name + " QB310-equivalent",
                    Kind = "hardware-ball-transfer",
                    Shape = "ball-transfer",
                    Xmm = hole.Xmm,
                    Ymm = centerY,
                    Zmm = hole.Zmm,
                    SizeXmm = config.BallTransferFlangeDiameterMm,
                    SizeYmm = totalHeight,
                    SizeZmm = config.BallTransferFlangeDiameterMm,
                    BodyDiameterMm = config.BallTransferBodyDiameterMm,
                    FlangeDiameterMm = config.BallTransferFlangeDiameterMm,
                    FlangeThicknessMm = config.BallTransferFlangeThicknessMm,
                    FlangeRecessDepthMm = config.BallTransferFlangeRecessDepthMm,
                    InsertionLengthMm = config.BallTransferInsertionLengthMm,
                    BallDiameterMm = config.BallTransferBallDiameterMm,
                    WorkingHeightMm = config.BallTransferWorkingHeightMm
                });
            }
        }

        private static List<PortalAssemblyPart> BuildFromPlacements(WorkbenchModel model, LexWorkbenchConfig lexConfig)
        {
            var parts = new List<PortalAssemblyPart>();
            var coreHolePositions = new ProfileCoreHolePositionService();
            var profileRenderContracts = new ProfileRenderContractService();
            var componentRenderContracts = new ComponentPrimitiveRenderContractService();
            foreach (var placement in model.AssemblyPlacements)
            {
                // Legacy decoratieve markers zijn vervangen door gaten uit het
                // bevestigde AssemblyConnection-contract.
                if ((placement.Shape ?? string.Empty).StartsWith("black-hole-", StringComparison.OrdinalIgnoreCase)) continue;
                var sheet = FindSheet(model, placement.PartName);
                if (placement.Kind == AssemblyComponentKind.Sheet && sheet == null)
                    throw new InvalidOperationException("Rendercontract mist plaatrecord voor " + placement.PartName + ".");
                if (sheet != null && (sheet.Material == null || sheet.Material.ThicknessMm <= 0))
                    throw new InvalidOperationException("Rendercontract mist plaatdikte voor " + placement.PartName + ".");
                var thickness = sheet == null ? 0 : sheet.Material.ThicknessMm;
                var sx = placement.LengthMm;
                var sy = thickness;
                var sz = placement.WidthMm;
                if ((placement.Kind == AssemblyComponentKind.Profile || placement.Kind == AssemblyComponentKind.Purchased) && placement.HeightMm > 0)
                {
                    sy = placement.HeightMm;
                }

                if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    sx = placement.LengthMm;
                    sy = placement.WidthMm;
                    sz = thickness;
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    sx = thickness;
                    sy = placement.WidthMm;
                    sz = placement.LengthMm;
                }

                if (string.Equals(placement.Shape, "leveling-foot-adapter", StringComparison.OrdinalIgnoreCase))
                {
                    AddLexLevelingFootCornerAdapter(parts, placement);
                    continue;
                }

                if (placement.Kind == AssemblyComponentKind.Purchased && !string.IsNullOrWhiteSpace(placement.ComponentId))
                {
                    AddComponentPrimitives(parts, placement, componentRenderContracts.BuildRequired(placement.ComponentId));
                    continue;
                }

                if (sheet != null && placement.Orientation == AssemblyOrientation.SheetVerticalZ && HasSlidingDoorFrontEdgeCutout(sheet))
                {
                    AddSlidingDoorPassThroughVerticalZPanel(parts, placement, sheet, thickness);
                    continue;
                }
                if (sheet != null && sheet.HasToeKickNotch && placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    AddNotchedVerticalZPanel(parts, placement, sheet, thickness);
                    continue;
                }
                if (sheet != null && sheet.HasCornerNotches && placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    AddCornerNotchedHorizontalSheet(parts, placement, sheet, thickness);
                    continue;
                }

                var part = new PortalAssemblyPart
                {
                    Name = placement.PartName,
                    MemberId = placement.MemberId,
                    TraceId = placement.TraceId,
                    Sticker = placement.Sticker,
                    Kind = placement.Kind == AssemblyComponentKind.Profile
                        ? "profile"
                        : (placement.Kind == AssemblyComponentKind.Purchased ? (string.IsNullOrWhiteSpace(placement.VisualKind) ? "hardware" : placement.VisualKind) : "sheet"),
                    Shape = string.IsNullOrWhiteSpace(placement.Shape) ? "box" : placement.Shape,
                    Xmm = placement.Xmm,
                    Ymm = placement.Ymm,
                    Zmm = placement.Zmm,
                    SizeXmm = sx,
                    SizeYmm = sy,
                    SizeZmm = sz,
                    RotationXDeg = placement.RotationXDeg,
                    RotationYDeg = placement.RotationYDeg,
                    RotationZDeg = placement.RotationZDeg,
                    MaterialAppearance = sheet == null || sheet.Material == null ? null : sheet.Material.RenderAppearance,
                    MaterialThicknessAxis = sheet == null ? null : SheetThicknessAxis(placement.Orientation)
                };
                if (placement.Kind == AssemblyComponentKind.Profile)
                {
                    try
                    {
                        foreach (var coreHole in coreHolePositions.Build(model, placement)) part.CoreHoles.Add(coreHole);
                        part.ProfileRender = profileRenderContracts.Build(model, placement);
                    }
                    catch (InvalidOperationException)
                    {
                        // Niet-vrijgegeven profielgeometrie blijft leeg; de instructieplanner blokkeert
                        // de bijbehorende verbinding afzonderlijk voor productievrijgave.
                    }
                }
                if (string.Equals(placement.Shape, "swing-latch", StringComparison.OrdinalIgnoreCase))
                {
                    if (lexConfig == null || lexConfig.SwingLatch == null)
                        throw new InvalidOperationException("Rendercontract mist draaibare aanslagdata voor " + placement.PartName + ".");
                    AddLexSwingLatch(parts, placement, lexConfig.SwingLatch);
                    continue;
                }
                if (sheet != null) part.CornerRadiusMm = sheet.CornerRadiusMm;
                AddOutline(part, sheet);
                AddHoles(part, placement, sheet, thickness);
                AddPockets(part, placement, sheet, thickness);
                AddLexHsr15RailHoles(part, placement);
                AddLexHte2EndPlateSlots(part, placement);
                parts.Add(part);
            }

            if (parts.Count == 0)
            {
                foreach (var sheet in model.Sheets)
                {
                    var placement = new AssemblyPlacement
                    {
                        Kind = AssemblyComponentKind.Sheet,
                        PartName = sheet.Name,
                        LengthMm = sheet.LengthMm,
                        WidthMm = sheet.WidthMm,
                        Xmm = 0,
                        Ymm = sheet.CenterHeightMm,
                        Zmm = 0,
                        Orientation = AssemblyOrientation.SheetHorizontal
                    };
                    if (sheet.Material == null || sheet.Material.ThicknessMm <= 0)
                        throw new InvalidOperationException("Rendercontract mist plaatdikte voor " + sheet.Name + ".");
                    var thickness = sheet.Material.ThicknessMm;
                    if (sheet.HasCornerNotches)
                    {
                        AddCornerNotchedHorizontalSheet(parts, placement, sheet, thickness);
                    }
                    else
                    {
                        var part = ApplySheetAppearance(Part(sheet.Name, "sheet", 0, sheet.CenterHeightMm, 0, sheet.LengthMm, thickness, sheet.WidthMm), sheet, "y");
                        part.CornerRadiusMm = sheet.CornerRadiusMm;
                        AddOutline(part, sheet);
                        AddHoles(part, placement, sheet, thickness);
                        AddPockets(part, placement, sheet, thickness);
                        parts.Add(part);
                    }
                }
            }

            return parts;
        }

        private static void AddProfileConnectionAccessHoles(List<PortalAssemblyPart> parts, WorkbenchModel model)
        {
            if (parts == null || model == null) return;
            var placements = model.AssemblyPlacements
                .Where(p => p.Kind == AssemblyComponentKind.Profile && !string.IsNullOrWhiteSpace(p.MemberId))
                .GroupBy(p => p.MemberId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var connection in model.AssemblyConnections.Where(c => c.JointType == AssemblyJointType.StandardConnector
                && c.Status == AssemblyDataStatus.Confirmed && c.AccessHoleDiameterMm > 0))
            {
                AssemblyPlacement placement;
                if (!placements.TryGetValue(connection.SlotMemberId ?? string.Empty, out placement)) continue;
                var part = parts.FirstOrDefault(p => string.Equals(p.MemberId, connection.SlotMemberId, StringComparison.OrdinalIgnoreCase));
                if (part == null) continue;
                var localNormal = new[] { 0.0, 0.0, 0.0 };
                if (connection.AccessLocalNormalAxis >= 0 && connection.AccessLocalNormalAxis <= 2)
                    localNormal[connection.AccessLocalNormalAxis] = connection.AccessLocalNormalSign;
                var normal = RotateLocal(localNormal[0], localNormal[1], localNormal[2],
                    placement.RotationXDeg, placement.RotationYDeg, placement.RotationZDeg);
                var absolute = new[] { Math.Abs(normal[0]), Math.Abs(normal[1]), Math.Abs(normal[2]) };
                var axis = Array.IndexOf(absolute, absolute.Max());
                part.Holes.Add(new PortalAssemblyHole
                {
                    Name = "Toegangsgat standaardverbinder " + connection.ConnectionId,
                    Xmm = connection.AccessXmm,
                    Ymm = connection.AccessYmm,
                    Zmm = connection.AccessZmm,
                    DiameterMm = connection.AccessHoleDiameterMm,
                    Plane = axis == 0 ? "x" : (axis == 1 ? "y" : "z"),
                    IsThroughCutout = true,
                    VisualRole = "connector-access"
                });
            }
        }

        private static void AddComponentPrimitives(List<PortalAssemblyPart> parts, AssemblyPlacement placement, ComponentPrimitiveRenderContract contract)
        {
            foreach (var primitive in contract.Primitives)
            {
                var center = RotateLocal(primitive.Xmm, primitive.Ymm, primitive.Zmm,
                    placement.RotationXDeg, placement.RotationYDeg, placement.RotationZDeg);
                var part = new PortalAssemblyPart
                {
                    Name = placement.PartName + " / " + primitive.Id,
                    MemberId = placement.MemberId + ":" + primitive.Id,
                    TraceId = placement.TraceId,
                    Kind = string.IsNullOrWhiteSpace(placement.VisualKind) ? "hardware" : placement.VisualKind,
                    Shape = primitive.Shape,
                    AppearanceRole = primitive.AppearanceRole,
                    ComponentId = contract.ComponentId,
                    ComponentRenderStatus = contract.Status,
                    ComponentRenderSource = contract.Source,
                    Xmm = placement.Xmm + center[0],
                    Ymm = placement.Ymm + center[1],
                    Zmm = placement.Zmm + center[2],
                    SizeXmm = primitive.InheritPlacementDimensions ? placement.LengthMm : primitive.SizeXmm,
                    SizeYmm = primitive.InheritPlacementDimensions ? placement.HeightMm : primitive.SizeYmm,
                    SizeZmm = primitive.InheritPlacementDimensions ? placement.WidthMm : primitive.SizeZmm,
                    RotationXDeg = placement.RotationXDeg + primitive.RotationXDeg,
                    RotationYDeg = placement.RotationYDeg + primitive.RotationYDeg,
                    RotationZDeg = placement.RotationZDeg + primitive.RotationZDeg,
                    RadiusTopMm = primitive.InheritPlacementDimensions && primitive.Shape == "cylinder" && primitive.RadiusTopMm <= 0
                        ? placement.LengthMm / 2.0 : primitive.RadiusTopMm,
                    RadiusBottomMm = primitive.InheritPlacementDimensions && primitive.Shape == "cylinder" && primitive.RadiusBottomMm <= 0
                        ? placement.WidthMm / 2.0 : primitive.RadiusBottomMm,
                    RadialSegments = primitive.RadialSegments
                };
                part.ComponentRenderOpenData.AddRange(contract.OpenData);
                foreach (var source in primitive.Holes)
                {
                    var hole = RotateLocal(source.Xmm, source.Ymm, source.Zmm,
                        placement.RotationXDeg, placement.RotationYDeg, placement.RotationZDeg);
                    part.Holes.Add(new PortalAssemblyHole
                    {
                        Name = source.Id,
                        Xmm = placement.Xmm + hole[0],
                        Ymm = placement.Ymm + hole[1],
                        Zmm = placement.Zmm + hole[2],
                        DiameterMm = source.DiameterMm,
                        DepthMm = source.DepthMm,
                        Plane = RotatePlane(source.Plane, placement.RotationXDeg, placement.RotationYDeg, placement.RotationZDeg),
                        Countersunk = source.CountersinkDiameterMm > source.DiameterMm && source.CountersinkDepthMm > 0,
                        CountersinkDiameterMm = source.CountersinkDiameterMm,
                        CountersinkDepthMm = source.CountersinkDepthMm,
                        VisualRole = "component-mounting-hole"
                    });
                }
                parts.Add(part);
            }
        }

        private static double[] RotateLocal(double x, double y, double z, double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var rx = rotationXDeg * Math.PI / 180.0;
            var ry = rotationYDeg * Math.PI / 180.0;
            var rz = rotationZDeg * Math.PI / 180.0;
            var cosX = Math.Cos(rx); var sinX = Math.Sin(rx);
            var y1 = y * cosX - z * sinX; var z1 = y * sinX + z * cosX;
            var cosY = Math.Cos(ry); var sinY = Math.Sin(ry);
            var x2 = x * cosY + z1 * sinY; var z2 = -x * sinY + z1 * cosY;
            var cosZ = Math.Cos(rz); var sinZ = Math.Sin(rz);
            return new[] { x2 * cosZ - y1 * sinZ, x2 * sinZ + y1 * cosZ, z2 };
        }

        private static string RotatePlane(string plane, double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var normal = string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase)
                ? RotateLocal(1, 0, 0, rotationXDeg, rotationYDeg, rotationZDeg)
                : (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase)
                    ? RotateLocal(0, 1, 0, rotationXDeg, rotationYDeg, rotationZDeg)
                    : RotateLocal(0, 0, 1, rotationXDeg, rotationYDeg, rotationZDeg));
            var absolute = new[] { Math.Abs(normal[0]), Math.Abs(normal[1]), Math.Abs(normal[2]) };
            var index = absolute[0] >= absolute[1] && absolute[0] >= absolute[2] ? 0 : (absolute[1] >= absolute[2] ? 1 : 2);
            return index == 0 ? "x" : (index == 1 ? "y" : "z");
        }

        private static void AddLexSwingLatch(List<PortalAssemblyPart> parts, AssemblyPlacement placement, SwingLatchTemplate latch)
        {
            var radians = placement.RotationYDeg * Math.PI / 180.0;
            var nx = Math.Sin(radians);
            var nz = Math.Cos(radians);
            var alongX = Math.Abs(nz) > 0.5;
            var upperProjection = latch.OverallProjectionMm - latch.BaseProjectionMm;
            var noseCenter = latch.NoseCenterDistanceMm;
            AddLatchCylinder(parts, placement.PartName + " montagevoet", placement, latch.MountingBaseDiameterMm,
                latch.BaseProjectionMm, placement.Ymm, latch.BaseProjectionMm / 2.0, nx, nz);
            AddLatchCylinder(parts, placement.PartName + " draaipunt", placement, latch.WidthMm,
                upperProjection, placement.Ymm, latch.BaseProjectionMm + upperProjection / 2.0, nx, nz);

            var bridge = Part(placement.PartName + " arm", "hardware-swing-latch",
                placement.Xmm + nx * (latch.BaseProjectionMm + upperProjection / 2.0),
                placement.Ymm + noseCenter / 2.0,
                placement.Zmm + nz * (latch.BaseProjectionMm + upperProjection / 2.0),
                alongX ? latch.WidthMm : upperProjection,
                noseCenter,
                alongX ? upperProjection : latch.WidthMm);
            bridge.Shape = "box";
            parts.Add(bridge);
            AddLatchCylinder(parts, placement.PartName + " aanslagnok", placement, latch.WidthMm,
                upperProjection, placement.Ymm + noseCenter, latch.BaseProjectionMm + upperProjection / 2.0, nx, nz);

            // De kap is een expliciete presentatiedetail binnen de als voorlopig gemarkeerde renderenvelop.
            var capThickness = upperProjection / 8.0;
            AddLatchCylinder(parts, placement.PartName + " rode kap", placement, latch.WidthMm * 0.62,
                capThickness, placement.Ymm, latch.OverallProjectionMm + capThickness / 2.0, nx, nz);
        }

        private static void AddLatchCylinder(List<PortalAssemblyPart> parts, string name, AssemblyPlacement placement,
            double diameter, double length, double centerY, double outwardCenter, double nx, double nz)
        {
            var alongZ = Math.Abs(nz) > 0.5;
            var part = Part(name, "hardware-swing-latch",
                placement.Xmm + nx * outwardCenter, centerY, placement.Zmm + nz * outwardCenter,
                alongZ ? diameter : length, diameter, alongZ ? length : diameter);
            part.Shape = alongZ ? "cylinder-z" : "cylinder-x";
            parts.Add(part);
        }

        private static void AddOutline(PortalAssemblyPart part, SheetPart sheet)
        {
            if (part == null || sheet == null || sheet.CustomContour == null || sheet.CustomContour.Count < 3) return;
            foreach (var point in SheetContourGeometry.ToolCenterContour(sheet, 0))
            {
                part.Outline.Add(new PortalAssemblyOutlinePoint
                {
                    Umm = point.Xmm - sheet.LengthMm / 2.0,
                    Vmm = point.Ymm - sheet.WidthMm / 2.0
                });
            }
        }

        private static void AddLexLevelingFootCornerAdapter(List<PortalAssemblyPart> parts, AssemblyPlacement placement)
        {
            var adapter = ProductDefaults.LexLevelingFootCornerAdapter();
            var direction = placement.Zmm < 0 ? -1.0 : 1.0;
            var mountingFaceZ = placement.Zmm - direction * adapter.ReachMm / 2.0;
            var footAxisZ = mountingFaceZ + direction * adapter.FootAxisFromMountingFaceMm;
            var armCenterY = adapter.MountingPlateHeightMm - adapter.SupportArmThicknessMm / 2.0;
            var prefix = placement.PartName;

            var mountingPlate = Part(
                prefix + " - montageplaat",
                "hardware-adapter",
                placement.Xmm,
                adapter.MountingPlateHeightMm / 2.0,
                mountingFaceZ + direction * adapter.MountingPlateThicknessMm / 2.0,
                adapter.WidthMm,
                adapter.MountingPlateHeightMm,
                adapter.MountingPlateThicknessMm);
            foreach (var xOffset in new[] { -adapter.MountingHolePitchMm / 2.0, adapter.MountingHolePitchMm / 2.0 })
            foreach (var yOffset in new[] { -adapter.MountingHolePitchMm / 2.0, adapter.MountingHolePitchMm / 2.0 })
            {
                mountingPlate.Holes.Add(new PortalAssemblyHole
                {
                    Name = "ZI-1744 bevestigingsgat M8",
                    Xmm = placement.Xmm + xOffset,
                    Ymm = adapter.MountingPlateHeightMm / 2.0 + yOffset,
                    Zmm = mountingPlate.Zmm + direction * adapter.MountingPlateThicknessMm / 2.0,
                    DiameterMm = adapter.MountingHoleDiameterMm,
                    Plane = "z",
                    IsThroughCutout = true
                });
            }
            parts.Add(mountingPlate);

            var bridge = Part(
                prefix + " - draagarm",
                "hardware-adapter",
                placement.Xmm,
                armCenterY,
                mountingFaceZ + direction * adapter.FootAxisFromMountingFaceMm / 2.0,
                adapter.WidthMm,
                adapter.SupportArmThicknessMm,
                adapter.FootAxisFromMountingFaceMm);
            parts.Add(bridge);

            var roundedEnd = Part(
                prefix + " - afgeronde M16 opname",
                "hardware-adapter",
                placement.Xmm,
                armCenterY,
                footAxisZ,
                adapter.WidthMm,
                adapter.SupportArmThicknessMm,
                adapter.WidthMm);
            roundedEnd.Shape = "cylinder";
            roundedEnd.Holes.Add(new PortalAssemblyHole
            {
                Name = "ZI-1744 M16 doorvoer",
                Xmm = placement.Xmm,
                Ymm = roundedEnd.Ymm + roundedEnd.SizeYmm / 2.0,
                Zmm = footAxisZ,
                DiameterMm = adapter.ThreadDiameterMm,
                Plane = "y",
                IsThroughCutout = true
            });
            parts.Add(roundedEnd);

            foreach (var xOffset in new[] { -25.0, 25.0 })
            {
                parts.Add(Part(
                    prefix + " - verstevigingsrib",
                    "hardware-adapter",
                    placement.Xmm + xOffset,
                    49,
                    mountingFaceZ + direction * 25,
                    10,
                    36,
                    36));
            }
        }

        private static void AddLexHsr15RailHoles(PortalAssemblyPart part, AssemblyPlacement placement)
        {
            if (part == null || placement == null || placement.Kind != AssemblyComponentKind.Purchased) return;
            if (string.IsNullOrWhiteSpace(placement.PartName) || !placement.PartName.StartsWith("HSR15 rail", StringComparison.OrdinalIgnoreCase)) return;

            var guide = ProductDefaults.LexHsr15LinearGuide();
            var holeCount = (int)Math.Floor((part.SizeXmm - 2.0 * guide.RailEndDistanceMm) / guide.RailMountingPitchMm) + 1;
            var firstX = part.Xmm - part.SizeXmm / 2.0 + guide.RailEndDistanceMm;
            for (var index = 0; index < holeCount; index++)
            {
                part.Holes.Add(new PortalAssemblyHole
                {
                    Name = "HSR15 railgat " + (index + 1) + " - omgekeerde montage",
                    Xmm = firstX + index * guide.RailMountingPitchMm,
                    Ymm = part.Ymm - part.SizeYmm / 2.0,
                    Zmm = part.Zmm,
                    DiameterMm = guide.RailHoleThroughDiameterMm,
                    DepthMm = 0,
                    Plane = "y",
                    IsThroughCutout = true,
                    Countersunk = true,
                    CountersinkDiameterMm = guide.RailHoleCounterboreDiameterMm,
                    CountersinkDepthMm = guide.RailHoleCounterboreDepthMm
                });
            }
        }

        private static void AddLexHte2EndPlateSlots(PortalAssemblyPart part, AssemblyPlacement placement)
        {
            if (part == null || placement == null || placement.Kind != AssemblyComponentKind.Purchased) return;
            if (string.IsNullOrWhiteSpace(placement.PartName) ||
                (!placement.PartName.StartsWith("HTE2 O1 onderplaat", StringComparison.OrdinalIgnoreCase) &&
                 !placement.PartName.StartsWith("HTE2 O1 bovenplaat", StringComparison.OrdinalIgnoreCase))) return;

            var column = ProductDefaults.LexHte2LiftColumn();
            var halfPitch = column.SlotCenterPitchMm / 2.0;
            foreach (var direction in new[] { -1.0, 1.0 })
            {
                part.Pockets.Add(new PortalAssemblyPocket
                {
                    Name = "HTE2 O1 montagesleuf",
                    Shape = "capsule",
                    Xmm = part.Xmm,
                    Ymm = part.Ymm,
                    Zmm = part.Zmm + direction * halfPitch,
                    SizeXmm = column.SlotLengthMm,
                    SizeYmm = part.SizeYmm,
                    SizeZmm = column.SlotWidthMm,
                    Plane = "y",
                    IsThroughCutout = false,
                    MinorDiameterMm = column.SlotWidthMm
                });
            }
        }

        private static void AddCornerNotchedHorizontalSheet(List<PortalAssemblyPart> parts, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            var notchLength = Math.Max(0, Math.Min(sheet.CornerNotchLengthMm > 0 ? sheet.CornerNotchLengthMm : sheet.CornerNotchSizeMm, sheet.LengthMm / 2.0 - 1));
            var notchWidth = Math.Max(0, Math.Min(sheet.CornerNotchWidthMm > 0 ? sheet.CornerNotchWidthMm : sheet.CornerNotchSizeMm, sheet.WidthMm / 2.0 - 1));
            if (notchLength <= 0 || notchWidth <= 0)
            {
                var fallback = ApplySheetAppearance(Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, sheet.LengthMm, thickness, sheet.WidthMm), sheet, "y");
                AddHoles(fallback, placement, sheet, thickness);
                AddPockets(fallback, placement, sheet, thickness);
                parts.Add(fallback);
                return;
            }

            var centerLength = Math.Max(2, sheet.LengthMm - 2 * notchLength);
            var center = ApplySheetAppearance(Part(placement.PartName + " midden", "sheet", placement.Xmm, placement.Ymm, placement.Zmm, centerLength, thickness, sheet.WidthMm), sheet, "y");
            var left = ApplySheetAppearance(Part(placement.PartName + " links", "sheet", placement.Xmm - sheet.LengthMm / 2.0 + notchLength / 2.0, placement.Ymm, placement.Zmm, notchLength, thickness, Math.Max(2, sheet.WidthMm - 2 * notchWidth)), sheet, "y");
            var right = ApplySheetAppearance(Part(placement.PartName + " rechts", "sheet", placement.Xmm + sheet.LengthMm / 2.0 - notchLength / 2.0, placement.Ymm, placement.Zmm, notchLength, thickness, Math.Max(2, sheet.WidthMm - 2 * notchWidth)), sheet, "y");
            AddHoles(center, placement, sheet, thickness);
            AddHoles(left, placement, sheet, thickness);
            AddHoles(right, placement, sheet, thickness);
            AddPockets(center, placement, sheet, thickness);
            parts.Add(center);
            parts.Add(left);
            parts.Add(right);
        }

        private static void AddNotchedVerticalZPanel(List<PortalAssemblyPart> parts, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            var panelHeight = Math.Max(2, placement.WidthMm);
            var panelDepth = Math.Max(2, placement.LengthMm);
            var notchDepth = Math.Max(0, Math.Min(sheet.ToeKickDepthMm, panelDepth - 1));
            var notchHeight = Math.Max(0, Math.Min(sheet.ToeKickHeightMm, panelHeight - 1));
            var frontZ = placement.Zmm - panelDepth / 2.0;

            if (notchHeight <= 0 || notchDepth <= 0)
            {
                parts.Add(ApplySheetAppearance(Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, thickness, panelHeight, panelDepth), sheet, "x"));
                return;
            }

            var upper = ApplySheetAppearance(Part(placement.PartName + " boven uitsparing", "sheet", placement.Xmm, notchHeight + (panelHeight - notchHeight) / 2.0, placement.Zmm, thickness, panelHeight - notchHeight, panelDepth), sheet, "x");
            var lowerDepth = panelDepth - notchDepth;
            var lowerZ = frontZ + notchDepth + lowerDepth / 2.0;
            var lower = ApplySheetAppearance(Part(placement.PartName + " plintvoet", "sheet", placement.Xmm, notchHeight / 2.0, lowerZ, thickness, notchHeight, lowerDepth), sheet, "x");
            AddHoles(upper, placement, sheet, thickness);
            AddHoles(lower, placement, sheet, thickness);
            AddPockets(upper, placement, sheet, thickness);
            parts.Add(upper);
            parts.Add(lower);
        }

        private static bool HasSlidingDoorFrontEdgeCutout(SheetPart sheet)
        {
            if (sheet == null) return false;
            foreach (var pocket in sheet.Pockets)
            {
                if (pocket == null) continue;
                if (pocket.DepthMode != OperationDepthMode.Through) continue;
                if (pocket.Xmm > 0.5) continue;
                if (!StartsWith(pocket.Name, "Schuifdeur doorvoer")) continue;
                return true;
            }

            return false;
        }

        private static void AddSlidingDoorPassThroughVerticalZPanel(List<PortalAssemblyPart> parts, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            SheetPocket cutout = null;
            foreach (var pocket in sheet.Pockets)
            {
                if (pocket == null) continue;
                if (pocket.DepthMode == OperationDepthMode.Through && pocket.Xmm <= 0.5 && StartsWith(pocket.Name, "Schuifdeur doorvoer"))
                {
                    cutout = pocket;
                    break;
                }
            }

            if (cutout == null)
            {
                var fallback = ApplySheetAppearance(Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, thickness, placement.WidthMm, placement.LengthMm), sheet, "x");
                AddHoles(fallback, placement, sheet, thickness);
                AddPockets(fallback, placement, sheet, thickness);
                parts.Add(fallback);
                return;
            }

            var panelHeight = Math.Max(2, placement.WidthMm);
            var panelDepth = Math.Max(2, placement.LengthMm);
            var frontZ = placement.Zmm - panelDepth / 2.0;
            var cutDepth = Math.Max(2, Math.Min(panelDepth - 2.0, cutout.LengthMm));
            var cutBottom = Math.Max(0, Math.Min(panelHeight, cutout.Ymm));
            var cutTop = Math.Max(cutBottom, Math.Min(panelHeight, cutout.Ymm + cutout.WidthMm));
            var notchDepth = sheet.HasToeKickNotch ? Math.Max(0, Math.Min(sheet.ToeKickDepthMm, panelDepth - 1)) : 0;
            var notchHeight = sheet.HasToeKickNotch ? Math.Max(0, Math.Min(sheet.ToeKickHeightMm, panelHeight - 1)) : 0;
            var hasToeKick = notchDepth > 0 && notchHeight > 0;

            if (hasToeKick)
            {
                var lowerBackHeight = Math.Max(notchHeight, Math.Min(cutBottom, panelHeight - 1));
                var lowerBackDepth = Math.Max(2, panelDepth - notchDepth);
                var lowerBackZ = frontZ + notchDepth + lowerBackDepth / 2.0;
                var lowerBack = ApplySheetAppearance(Part(placement.PartName + " plintvoet", "sheet", placement.Xmm, placement.Ymm - panelHeight / 2.0 + lowerBackHeight / 2.0, lowerBackZ, thickness, lowerBackHeight, lowerBackDepth), sheet, "x");
                AddHoles(lowerBack, placement, sheet, thickness);
                AddPockets(lowerBack, placement, sheet, thickness, false);
                parts.Add(lowerBack);

                var lowerFrontHeight = lowerBackHeight - notchHeight;
                if (lowerFrontHeight > 2)
                {
                    var lowerFront = ApplySheetAppearance(Part(
                        placement.PartName + " bodemplaat-aansluiting",
                        "sheet",
                        placement.Xmm,
                        placement.Ymm - panelHeight / 2.0 + notchHeight + lowerFrontHeight / 2.0,
                        frontZ + notchDepth / 2.0,
                        thickness,
                        lowerFrontHeight,
                        notchDepth), sheet, "x");
                    AddHoles(lowerFront, placement, sheet, thickness);
                    AddPockets(lowerFront, placement, sheet, thickness, false);
                    parts.Add(lowerFront);
                }

                var upperBackHeight = panelHeight - lowerBackHeight;
                if (upperBackHeight > 2)
                {
                    var upperBackDepth = Math.Max(2, panelDepth - cutDepth);
                    var upperBackZ = frontZ + cutDepth + upperBackDepth / 2.0;
                    var upperBack = ApplySheetAppearance(Part(placement.PartName + " achter doorvoer", "sheet", placement.Xmm, placement.Ymm - panelHeight / 2.0 + lowerBackHeight + upperBackHeight / 2.0, upperBackZ, thickness, upperBackHeight, upperBackDepth), sheet, "x");
                    AddHoles(upperBack, placement, sheet, thickness);
                    AddPockets(upperBack, placement, sheet, thickness, false);
                    parts.Add(upperBack);
                }
            }
            else
            {
                var backDepth = Math.Max(2, panelDepth - cutDepth);
                var backZ = frontZ + cutDepth + backDepth / 2.0;
                var back = ApplySheetAppearance(Part(placement.PartName + " achter doorvoer", "sheet", placement.Xmm, placement.Ymm, backZ, thickness, panelHeight, backDepth), sheet, "x");
                AddHoles(back, placement, sheet, thickness);
                AddPockets(back, placement, sheet, thickness, false);
                parts.Add(back);
            }

            if (!hasToeKick && cutBottom > 2)
            {
                var lower = ApplySheetAppearance(Part(
                    placement.PartName + " onder doorvoer",
                    "sheet",
                    placement.Xmm,
                    placement.Ymm - panelHeight / 2.0 + cutBottom / 2.0,
                    frontZ + cutDepth / 2.0,
                    thickness,
                    cutBottom,
                    cutDepth), sheet, "x");
                AddHoles(lower, placement, sheet, thickness);
                AddPockets(lower, placement, sheet, thickness, false);
                parts.Add(lower);
            }

            var upperHeight = panelHeight - cutTop;
            if (upperHeight > 2)
            {
                var upper = ApplySheetAppearance(Part(
                    placement.PartName + " boven doorvoer",
                    "sheet",
                    placement.Xmm,
                    placement.Ymm - panelHeight / 2.0 + cutTop + upperHeight / 2.0,
                    frontZ + cutDepth / 2.0,
                    thickness,
                    upperHeight,
                    cutDepth), sheet, "x");
                AddHoles(upper, placement, sheet, thickness);
                AddPockets(upper, placement, sheet, thickness, false);
                parts.Add(upper);
            }
        }

        private static void AddHoles(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            if (sheet == null) return;
            foreach (var hole in sheet.Holes)
            {
                if (IsDrawerPullRoundEnd(hole)) continue;

                var localX = hole.Xmm - sheet.LengthMm / 2.0;
                var localY = hole.Ymm - sheet.WidthMm / 2.0;
                var assemblyHole = new PortalAssemblyHole
                {
                    Name = hole.Name,
                    DiameterMm = Math.Max(0.1, hole.DiameterMm),
                    DepthMm = hole.DepthMm,
                    IsThroughCutout = hole.DepthMode == OperationDepthMode.Through,
                    Countersunk = hole.Countersunk,
                    CountersinkDiameterMm = hole.CountersinkDiameterMm,
                    CountersinkDepthMm = hole.CountersinkDepthMm
                };

                if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + HorizontalHoleFaceOffset(hole.Face, thickness, 0.8);
                    assemblyHole.Zmm = placement.Zmm + localY;
                    assemblyHole.Plane = "y";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + localY;
                    var explicitZ = ExplicitHoleFaceOffset(hole.Face, OperationFace.PositiveZ, OperationFace.NegativeZ, thickness, 0.8);
                    assemblyHole.Zmm = placement.Zmm + (explicitZ.HasValue ? explicitZ.Value : VerticalXHoleFaceOffset(placement.PartName, thickness, 0.8));
                    assemblyHole.Plane = "z";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    var explicitX = ExplicitHoleFaceOffset(hole.Face, OperationFace.PositiveX, OperationFace.NegativeX, thickness, 0.8);
                    var side = explicitX.HasValue ? explicitX.Value : VerticalZFaceOffset(placement.PartName, hole.Name, thickness, 0.8);
                    assemblyHole.Xmm = placement.Xmm + side;
                    assemblyHole.Ymm = placement.Ymm + localY;
                    assemblyHole.Zmm = placement.Zmm + localX;
                    assemblyHole.Plane = "x";
                }
                else
                {
                    continue;
                }

                if (!IsInsidePartBounds(part, assemblyHole)) continue;
                part.Holes.Add(assemblyHole);
                AddOppositeThroughHoleFace(part, placement, hole, assemblyHole, thickness);
            }
        }

        private static void AddOppositeThroughHoleFace(PortalAssemblyPart part, AssemblyPlacement placement, SheetHole source, PortalAssemblyHole visibleHole, double thickness)
        {
            if (part == null || placement == null || source == null || visibleHole == null) return;
            if (source.DepthMode != OperationDepthMode.Through || source.Face != OperationFace.CenterPlane) return;

            if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
            {
                var oppositeZ = new PortalAssemblyHole
                {
                    Name = visibleHole.Name,
                    Xmm = visibleHole.Xmm,
                    Ymm = visibleHole.Ymm,
                    Zmm = placement.Zmm + (visibleHole.Zmm >= placement.Zmm ? -thickness / 2.0 - 0.8 : thickness / 2.0 + 0.8),
                    DiameterMm = visibleHole.DiameterMm,
                    DepthMm = visibleHole.DepthMm,
                    Plane = visibleHole.Plane,
                    IsThroughCutout = visibleHole.IsThroughCutout,
                    Countersunk = visibleHole.Countersunk,
                    CountersinkDiameterMm = visibleHole.CountersinkDiameterMm,
                    CountersinkDepthMm = visibleHole.CountersinkDepthMm
                };

                if (IsInsidePartBounds(part, oppositeZ)) part.Holes.Add(oppositeZ);
                return;
            }

            if (placement.Orientation != AssemblyOrientation.SheetVerticalZ) return;
            if (!StartsWith(placement.PartName, "Tussenschot ") && !StartsWith(placement.PartName, "Volledig tussenschot ")) return;

            var opposite = new PortalAssemblyHole
            {
                Name = visibleHole.Name,
                Xmm = placement.Xmm + (visibleHole.Xmm >= placement.Xmm ? -thickness / 2.0 - 0.8 : thickness / 2.0 + 0.8),
                Ymm = visibleHole.Ymm,
                Zmm = visibleHole.Zmm,
                DiameterMm = visibleHole.DiameterMm,
                DepthMm = visibleHole.DepthMm,
                Plane = visibleHole.Plane,
                IsThroughCutout = visibleHole.IsThroughCutout,
                Countersunk = visibleHole.Countersunk,
                CountersinkDiameterMm = visibleHole.CountersinkDiameterMm,
                CountersinkDepthMm = visibleHole.CountersinkDepthMm
            };

            if (IsInsidePartBounds(part, opposite)) part.Holes.Add(opposite);
        }

        private static bool IsInsidePartBounds(PortalAssemblyPart part, PortalAssemblyHole hole)
        {
            if (part == null || hole == null) return false;
            var halfX = part.SizeXmm / 2.0 + 1.0;
            var halfY = part.SizeYmm / 2.0 + 1.0;
            var halfZ = part.SizeZmm / 2.0 + 1.0;
            return hole.Xmm >= part.Xmm - halfX && hole.Xmm <= part.Xmm + halfX &&
                   hole.Ymm >= part.Ymm - halfY && hole.Ymm <= part.Ymm + halfY &&
                   hole.Zmm >= part.Zmm - halfZ && hole.Zmm <= part.Zmm + halfZ;
        }

        private static void AddPockets(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, double thickness, bool includeThroughCutouts = true)
        {
            if (sheet == null) return;
            foreach (var pocket in sheet.Pockets)
            {
                if (!includeThroughCutouts && pocket.DepthMode == OperationDepthMode.Through) continue;
                if (sheet.Name != null && sheet.Name.StartsWith("Clipkist ", StringComparison.OrdinalIgnoreCase) &&
                    pocket.Name != null && pocket.Name.StartsWith("Sponning ", StringComparison.OrdinalIgnoreCase)) continue;

                var localCenterX = pocket.Xmm + pocket.LengthMm / 2.0 - sheet.LengthMm / 2.0;
                var localCenterY = pocket.Ymm + pocket.WidthMm / 2.0 - sheet.WidthMm / 2.0;
                var assemblyPocket = new PortalAssemblyPocket
                {
                    Name = pocket.Name,
                    Shape = string.IsNullOrWhiteSpace(pocket.Shape) ? "rectangle" : pocket.Shape,
                    IsThroughCutout = pocket.DepthMode == OperationDepthMode.Through
                };

                if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    assemblyPocket.Xmm = placement.Xmm + localCenterX;
                    assemblyPocket.Ymm = assemblyPocket.IsThroughCutout ? placement.Ymm : placement.Ymm + HorizontalPocketFaceOffset(sheet, pocket, thickness, 1.2);
                    assemblyPocket.Zmm = placement.Zmm + localCenterY;
                    assemblyPocket.SizeXmm = pocket.LengthMm;
                    assemblyPocket.SizeYmm = assemblyPocket.IsThroughCutout ? thickness : Math.Max(0.4, pocket.DepthMm);
                    assemblyPocket.SizeZmm = pocket.WidthMm;
                    assemblyPocket.Plane = "y";
                    if (!assemblyPocket.IsThroughCutout) AddHorizontalPocketEdgeReveals(part, placement, sheet, pocket, localCenterX, thickness);
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    assemblyPocket.Xmm = placement.Xmm + localCenterX;
                    assemblyPocket.Ymm = placement.Ymm + localCenterY;
                    assemblyPocket.Zmm = assemblyPocket.IsThroughCutout ? placement.Zmm : placement.Zmm + VerticalXPocketFaceOffset(placement.PartName, pocket, thickness);
                    assemblyPocket.SizeXmm = pocket.LengthMm;
                    assemblyPocket.SizeYmm = pocket.WidthMm;
                    assemblyPocket.SizeZmm = assemblyPocket.IsThroughCutout ? thickness : Math.Max(0.4, pocket.DepthMm);
                    assemblyPocket.Plane = "z";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    assemblyPocket.Xmm = assemblyPocket.IsThroughCutout ? placement.Xmm : placement.Xmm + VerticalZPocketFaceOffset(placement.PartName, pocket, thickness);
                    assemblyPocket.Ymm = placement.Ymm + localCenterY;
                    assemblyPocket.Zmm = placement.Zmm + localCenterX;
                    assemblyPocket.SizeXmm = assemblyPocket.IsThroughCutout ? thickness : Math.Max(0.4, pocket.DepthMm);
                    assemblyPocket.SizeYmm = pocket.WidthMm;
                    assemblyPocket.SizeZmm = pocket.LengthMm;
                    assemblyPocket.Plane = "x";
                }
                else
                {
                    continue;
                }

                part.Pockets.Add(assemblyPocket);
            }

            AddDrawerPullRoundEndCutouts(part, placement, sheet, thickness);
        }

        private static void AddDrawerPullRoundEndCutouts(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            if (part == null || placement == null || sheet == null) return;

            foreach (var hole in sheet.Holes)
            {
                if (!IsDrawerPullRoundEnd(hole)) continue;

                var localX = hole.Xmm - sheet.LengthMm / 2.0;
                var localY = hole.Ymm - sheet.WidthMm / 2.0;
                var diameter = Math.Max(3.0, hole.DiameterMm);
                var assemblyPocket = new PortalAssemblyPocket
                {
                    Name = hole.Name,
                    Shape = "cylinder",
                    IsThroughCutout = true
                };

                if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    assemblyPocket.Xmm = placement.Xmm + localX;
                    assemblyPocket.Ymm = placement.Ymm;
                    assemblyPocket.Zmm = placement.Zmm + localY;
                    assemblyPocket.SizeXmm = diameter;
                    assemblyPocket.SizeYmm = thickness;
                    assemblyPocket.SizeZmm = diameter;
                    assemblyPocket.Plane = "y";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    assemblyPocket.Xmm = placement.Xmm + localX;
                    assemblyPocket.Ymm = placement.Ymm + localY;
                    assemblyPocket.Zmm = placement.Zmm;
                    assemblyPocket.SizeXmm = diameter;
                    assemblyPocket.SizeYmm = diameter;
                    assemblyPocket.SizeZmm = thickness;
                    assemblyPocket.Plane = "z";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    assemblyPocket.Xmm = placement.Xmm;
                    assemblyPocket.Ymm = placement.Ymm + localY;
                    assemblyPocket.Zmm = placement.Zmm + localX;
                    assemblyPocket.SizeXmm = thickness;
                    assemblyPocket.SizeYmm = diameter;
                    assemblyPocket.SizeZmm = diameter;
                    assemblyPocket.Plane = "x";
                }
                else
                {
                    continue;
                }

                part.Pockets.Add(assemblyPocket);
            }
        }

        private static bool IsDrawerPullRoundEnd(SheetHole hole)
        {
            return hole != null
                && hole.Name != null
                && hole.Name.StartsWith("Uitgefreesde handgreep ", StringComparison.OrdinalIgnoreCase)
                && hole.Name.IndexOf("ronding", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddHorizontalPocketEdgeReveals(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, SheetPocket pocket, double localCenterX, double thickness)
        {
            if (part == null || sheet == null || pocket == null) return;
            if (!StartsWith(sheet.Name, "Werkblad")) return;

            var revealDepth = Math.Max(1.2, Math.Min(4.0, pocket.DepthMm));
            if (pocket.Ymm <= 0.01)
            {
                part.Pockets.Add(new PortalAssemblyPocket
                {
                    Name = pocket.Name + " voorzijde zichtbaar",
                    Xmm = placement.Xmm + localCenterX,
                    Ymm = placement.Ymm - thickness / 2.0 + revealDepth / 2.0,
                    Zmm = placement.Zmm - sheet.WidthMm / 2.0 - 1.2,
                    SizeXmm = pocket.LengthMm,
                    SizeYmm = revealDepth,
                    SizeZmm = 1.2,
                    Plane = "z"
                });
            }

            if (pocket.Ymm + pocket.WidthMm >= sheet.WidthMm - 0.01)
            {
                part.Pockets.Add(new PortalAssemblyPocket
                {
                    Name = pocket.Name + " achterzijde zichtbaar",
                    Xmm = placement.Xmm + localCenterX,
                    Ymm = placement.Ymm - thickness / 2.0 + revealDepth / 2.0,
                    Zmm = placement.Zmm + sheet.WidthMm / 2.0 + 1.2,
                    SizeXmm = pocket.LengthMm,
                    SizeYmm = revealDepth,
                    SizeZmm = 1.2,
                    Plane = "z"
                });
            }
        }

        private static double MaxPocketDepth(SheetPart sheet)
        {
            var max = 0.0;
            if (sheet == null) return max;
            foreach (var pocket in sheet.Pockets)
            {
                if (pocket.DepthMm > max) max = pocket.DepthMm;
            }

            return max;
        }

        private static List<Range1> PocketXRanges(SheetPart sheet)
        {
            var ranges = new List<Range1>();
            if (sheet == null) return ranges;
            foreach (var pocket in sheet.Pockets)
            {
                if (pocket.DepthMm <= 0 || pocket.LengthMm <= 0) continue;
                ranges.Add(new Range1(
                    Math.Max(0, Math.Min(sheet.LengthMm, pocket.Xmm)),
                    Math.Max(0, Math.Min(sheet.LengthMm, pocket.Xmm + pocket.LengthMm))));
            }

            ranges.Sort(delegate(Range1 a, Range1 b) { return a.Start.CompareTo(b.Start); });
            var merged = new List<Range1>();
            foreach (var range in ranges)
            {
                if (range.End <= range.Start) continue;
                if (merged.Count == 0 || range.Start > merged[merged.Count - 1].End)
                {
                    merged.Add(range);
                }
                else if (range.End > merged[merged.Count - 1].End)
                {
                    merged[merged.Count - 1] = new Range1(merged[merged.Count - 1].Start, range.End);
                }
            }

            return merged;
        }

        private static double HorizontalPocketFaceOffset(SheetPart sheet, SheetPocket pocket, double thickness, double visualDepth)
        {
            if (pocket != null)
            {
                var explicitOffset = ExplicitPocketFaceOffset(pocket.Face, OperationFace.PositiveY, OperationFace.NegativeY, thickness, pocket.DepthMm);
                if (explicitOffset.HasValue) return explicitOffset.Value;
            }

            if (sheet != null && pocket != null && StartsWith(sheet.Name, "Werkblad"))
            {
                return -thickness / 2.0 + Math.Max(0.4, Math.Min(2.2, pocket.DepthMm)) / 2.0;
            }

            return thickness / 2.0 + visualDepth;
        }

        private static double VerticalXPocketFaceOffset(string partName, SheetPocket pocket, double thickness)
        {
            var depth = pocket == null ? 0.4 : pocket.DepthMm;
            if (pocket != null)
            {
                var explicitOffset = ExplicitPocketFaceOffset(pocket.Face, OperationFace.PositiveZ, OperationFace.NegativeZ, thickness, depth);
                if (explicitOffset.HasValue) return explicitOffset.Value;
            }

            var d = Math.Max(0.4, depth);
            if (StartsWith(partName, "Ladefront") || StartsWith(partName, "Bovenlade front"))
            {
                return thickness / 2.0 - d / 2.0;
            }

            if (StartsWith(partName, "Ladeachter") || StartsWith(partName, "Bovenlade achter"))
            {
                return -thickness / 2.0 + d / 2.0;
            }

            return -thickness / 2.0 - d / 2.0;
        }

        private static double VerticalZPocketFaceOffset(string partName, SheetPocket pocket, double thickness)
        {
            var depth = pocket == null ? 0.4 : pocket.DepthMm;
            if (pocket != null)
            {
                var explicitOffset = ExplicitPocketFaceOffset(pocket.Face, OperationFace.PositiveX, OperationFace.NegativeX, thickness, depth);
                if (explicitOffset.HasValue) return explicitOffset.Value;
            }

            var d = Math.Max(0.4, depth);
            if (StartsWith(partName, "Ladezijde links") || StartsWith(partName, "Bovenlade zijde links"))
            {
                return thickness / 2.0 - d / 2.0;
            }

            if (StartsWith(partName, "Ladezijde rechts") || StartsWith(partName, "Bovenlade zijde rechts"))
            {
                return -thickness / 2.0 + d / 2.0;
            }

            return -thickness / 2.0 - d / 2.0;
        }

        private static double? ExplicitPocketFaceOffset(OperationFace face, OperationFace positiveFace, OperationFace negativeFace, double thickness, double depth)
        {
            var d = Math.Max(0.4, depth);
            if (face == positiveFace) return thickness / 2.0 - d / 2.0;
            if (face == negativeFace) return -thickness / 2.0 + d / 2.0;
            return null;
        }

        private static double? ExplicitHoleFaceOffset(OperationFace face, OperationFace positiveFace, OperationFace negativeFace, double thickness, double lift)
        {
            if (face == positiveFace) return thickness / 2.0 + lift;
            if (face == negativeFace) return -thickness / 2.0 - lift;
            return null;
        }

        private static double HorizontalHoleFaceOffset(OperationFace face, double thickness, double lift)
        {
            if (face == OperationFace.NegativeY) return -thickness / 2.0 - lift;
            return thickness / 2.0 + lift;
        }

        private static double VerticalZFaceOffset(string partName, string holeName, double thickness, double lift)
        {
            if (IsBottomAssemblyHole(holeName))
            {
                if (StartsWith(partName, "Zijwand links"))
                {
                    return -thickness / 2.0 - lift;
                }

                if (StartsWith(partName, "Zijwand rechts"))
                {
                    return thickness / 2.0 + lift;
                }
            }

            if (StartsWith(partName, "Zijwand links") || StartsWith(partName, "Ladezijde links"))
            {
                return thickness / 2.0 + lift;
            }

            if (StartsWith(partName, "Zijwand rechts") || StartsWith(partName, "Ladezijde rechts"))
            {
                return -thickness / 2.0 - lift;
            }

            var dividerNumber = DividerNumber(partName);
            var unitNumber = UnitNumberFromHole(holeName);
            if (dividerNumber > 0 && unitNumber > 0)
            {
                return unitNumber <= dividerNumber ? -thickness / 2.0 - lift : thickness / 2.0 + lift;
            }

            return thickness / 2.0 + lift;
        }

        private static bool IsBottomAssemblyHole(string value)
        {
            return StartsWith(value, "Montagegat bodem");
        }

        private static double VerticalXHoleFaceOffset(string partName, double thickness, double lift)
        {
            if (StartsWith(partName, "Achterwand"))
            {
                return thickness / 2.0 + lift;
            }

            if (StartsWith(partName, "Ladefront") || StartsWith(partName, "Bovenlade front"))
            {
                return thickness / 2.0 + lift;
            }

            if (StartsWith(partName, "Ladeachter") || StartsWith(partName, "Bovenlade achter"))
            {
                return -thickness / 2.0 - lift;
            }

            return -thickness / 2.0 - lift;
        }

        private static int DividerNumber(string value)
        {
            return NumberAfter(value, "Tussenschot ");
        }

        private static int UnitNumberFromHole(string value)
        {
            return NumberAfter(value, "U");
        }

        private static int NumberAfter(string value, string marker)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(marker)) return 0;
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return 0;
            index += marker.Length;
            var number = 0;
            var found = false;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                found = true;
                number = number * 10 + (value[index] - '0');
                index++;
            }

            return found ? number : 0;
        }

        private static bool StartsWith(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddWorkbenchCabinetFeet(List<PortalAssemblyPart> parts, WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var width = config.WidthMm;
            var depth = config.DepthMm;
            var units = config.UnitCount;
            var unitWidth = width / units;
            var inset = config.AdjustableFootInsetMm;
            var footHeight = config.PlinthHeightMm;
            footHeight = Math.Max(foot.MinHeightMm, Math.Min(foot.MaxHeightMm, footHeight));
            var diameter = Math.Max(2, foot.FootDiameterMm);
            var minimumInset = Math.Max(diameter / 2.0 + 1.0, foot.MountingBlockWidthMm / 2.0 + 1.0);
            inset = Math.Max(minimumInset, Math.Min(Math.Min(width, depth) / 2.0 - 1.0, inset));
            var frontZ = -depth / 2.0 + ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
            var backZ = depth / 2.0 - inset;
            var sidePlinthFootInset = ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
            var leftInset = config.IncludeLeftSidePlinth ? sidePlinthFootInset : inset;
            var rightInset = config.IncludeRightSidePlinth ? sidePlinthFootInset : inset;

            for (var boundary = 0; boundary <= units; boundary++)
            {
                var localX = boundary * unitWidth;
                if (boundary == 0) localX = leftInset;
                else if (boundary == units) localX = width - rightInset;
                var worldX = -width / 2.0 + localX;
                var label = boundary.ToString(System.Globalization.CultureInfo.InvariantCulture);
                AddWorkbenchCabinetFoot(parts, foot, "voor grens " + label, worldX, frontZ, footHeight, false);
                AddWorkbenchCabinetFoot(parts, foot, "achter grens " + label, worldX, backZ, footHeight, true);
            }
        }

        private static void AddWorkbenchCabinetFoot(
            List<PortalAssemblyPart> parts,
            AdjustableFootTemplate foot,
            string label,
            double footCenterX,
            double footCenterZ,
            double footHeight,
            bool rotate180)
        {
            var direction = rotate180 ? -1.0 : 1.0;
            var blockThickness = Math.Max(2, foot.MountingBlockThicknessMm);
            var blockCenterFromShortEdge = foot.MountingBlockLengthMm / 2.0;
            var blockCenterOffset = blockCenterFromShortEdge - foot.DefaultFootCenterFromShortEdgeMm;
            var blockCenterX = footCenterX + direction * blockCenterOffset;

            var mountingBlock = Part(
                "SEKTION montagevoet " + label,
                "hardware",
                blockCenterX,
                footHeight - blockThickness / 2.0,
                footCenterZ,
                foot.MountingBlockLengthMm,
                blockThickness,
                foot.MountingBlockWidthMm);
            mountingBlock.Shape = "box";
            parts.Add(mountingBlock);

            var mountingHoles = foot.MountingHoles ?? new AdjustableFootMountingHole[0];
            for (var index = 0; index < mountingHoles.Length; index++)
            {
                var hole = mountingHoles[index];
                var pin = Part(
                    "SEKTION klikpen " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + label,
                    "hardware-pin",
                    footCenterX + direction * hole.XOffsetMm,
                    footHeight + foot.PinLengthMm / 2.0,
                    footCenterZ + direction * hole.YOffsetMm,
                    foot.PinDiameterMm,
                    foot.PinLengthMm,
                    foot.PinDiameterMm);
                pin.Shape = "cylinder";
                parts.Add(pin);
            }

            var floorPlateHeight = Math.Min(6.0, Math.Max(3.0, footHeight * 0.08));
            var bodyTopY = footHeight - blockThickness;
            var bodyHeight = Math.Max(2, bodyTopY - floorPlateHeight);
            var bodyDiameter = foot.ClipStemDiameterMm > 0
                ? foot.ClipStemDiameterMm
                : Math.Min(40.0, foot.FootDiameterMm * 0.75);

            var roundLeg = Part(
                "SEKTION ronde stelpoot " + label,
                "hardware",
                footCenterX,
                floorPlateHeight + bodyHeight / 2.0,
                footCenterZ,
                bodyDiameter,
                bodyHeight,
                bodyDiameter);
            roundLeg.Shape = "cylinder";
            parts.Add(roundLeg);

            var floorPlate = Part(
                "SEKTION ronde vloerplaat " + label,
                "hardware",
                footCenterX,
                floorPlateHeight / 2.0,
                footCenterZ,
                foot.FootDiameterMm,
                floorPlateHeight,
                foot.FootDiameterMm);
            floorPlate.Shape = "cylinder";
            parts.Add(floorPlate);
        }

        private static void AddWorkbenchCabinetPlinthAdapters(List<PortalAssemblyPart> parts, WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            var width = config.WidthMm;
            var depth = config.DepthMm;
            var units = Math.Max(1, config.UnitCount);
            var unitWidth = width / units;
            var t = config.CarcassMaterial == null ? 18.0 : Math.Max(2.0, config.CarcassMaterial.ThicknessMm);
            var inset = Math.Max(config.AdjustableFootInsetMm, foot.FootDiameterMm / 2.0 + 1.0);
            var frontFootZ = -depth / 2.0 + ProductDefaults.WorkbenchCabinetFrontFootCenterFromFrontMm(config);
            var backFootZ = depth / 2.0 - inset;
            var frontPlinthInnerZ = -depth / 2.0 + config.PlinthSetbackMm + t;
            var frontStandOff = ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config);
            var sideStandOff = ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config);
            var sideFootInset = ProductDefaults.WorkbenchCabinetSideFootCenterFromOuterEdgeMm(config);
            var leftFootInset = config.IncludeLeftSidePlinth ? sideFootInset : inset;
            var rightFootInset = config.IncludeRightSidePlinth ? sideFootInset : inset;
            var plinthHeight = Math.Max(40.0, config.PlinthHeightMm - config.PlinthFloorClearanceMm);
            var plinthCenterY = config.PlinthFloorClearanceMm + plinthHeight / 2.0;
            const double sharedCornerVerticalSeparationMm = 7.0;
            var frontClipY = plinthCenterY - sharedCornerVerticalSeparationMm / 2.0;
            var sideClipY = plinthCenterY + sharedCornerVerticalSeparationMm / 2.0;

            for (var boundary = 0; boundary <= units; boundary++)
            {
                var localX = boundary * unitWidth;
                if (boundary == 0) localX = leftFootInset;
                else if (boundary == units) localX = width - rightFootInset;
                var worldX = -width / 2.0 + localX;
                var wingSign = boundary == units ? -1.0 : 1.0;
                AddPlinthAdapter(
                    parts,
                    adapter,
                    "voor grens " + boundary.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "z",
                    worldX,
                    frontClipY,
                    frontPlinthInnerZ,
                    1.0,
                    frontStandOff,
                    wingSign);
            }

            if (config.IncludeLeftSidePlinth)
            {
                var innerX = -width / 2.0 + t;
                AddPlinthAdapter(parts, adapter, "zijde links voor", "x", frontFootZ, sideClipY, innerX, 1.0, sideStandOff, 1.0);
                AddPlinthAdapter(parts, adapter, "zijde links achter", "x", backFootZ, sideClipY, innerX, 1.0, sideStandOff, -1.0);
            }
            if (config.IncludeRightSidePlinth)
            {
                var innerX = width / 2.0 - t;
                AddPlinthAdapter(parts, adapter, "zijde rechts voor", "x", frontFootZ, sideClipY, innerX, -1.0, sideStandOff, 1.0);
                AddPlinthAdapter(parts, adapter, "zijde rechts achter", "x", backFootZ, sideClipY, innerX, -1.0, sideStandOff, -1.0);
            }
        }

        private static void AddPlinthAdapter(
            List<PortalAssemblyPart> parts,
            PlinthClipAdapterTemplate adapter,
            string label,
            string normalAxis,
            double centerAlongPlinth,
            double centerY,
            double plinthInnerFace,
            double normalSign,
            double standOff,
            double wingSign)
        {
            var slotWidth = adapter.SlotWidthMm;
            var slotHeight = adapter.SlotHeightMm;
            var slotDepth = adapter.SlotDepthMm;
            var lipOverlap = adapter.GuideLipOverlapMm;
            var lipThickness = adapter.GuideLipThicknessMm;
            var totalDepth = standOff + slotDepth + lipThickness;
            var wingExtension = Math.Max(0, adapter.MountingWingExtensionMm);
            var baseCenterAlongPlinth = centerAlongPlinth + wingSign * wingExtension / 2.0;
            var basePart = AddAdapterBox(parts, "Plintclip-adapter basis " + label, baseCenterAlongPlinth, centerY, plinthInnerFace, normalSign, normalAxis, adapter.BackPlateWidthMm + wingExtension, adapter.BackPlateHeightMm, totalDepth, "hardware-adapter");

            var screwOffset = adapter.MountingHoleSpacingMm / 2.0;
            var upperScrewAlongPlinth = centerAlongPlinth + wingSign * adapter.UpperMountingHoleHorizontalOffsetMm;
            AddAdapterScrewHole(basePart, normalAxis, centerAlongPlinth, centerY - screwOffset, plinthInnerFace, normalSign, totalDepth, adapter.MountingHoleDiameterMm);
            AddAdapterScrewHole(basePart, normalAxis, upperScrewAlongPlinth, centerY + screwOffset, plinthInnerFace, normalSign, totalDepth, adapter.MountingHoleDiameterMm);
            AddAdapterCountersink(basePart, normalAxis, centerAlongPlinth, centerY - screwOffset, plinthInnerFace, normalSign, totalDepth, adapter.MountingHoleDiameterMm, adapter.MountingCountersinkDiameterMm, adapter.MountingCountersinkDepthMm);
            AddAdapterCountersink(basePart, normalAxis, upperScrewAlongPlinth, centerY + screwOffset, plinthInnerFace, normalSign, totalDepth, adapter.MountingHoleDiameterMm, adapter.MountingCountersinkDiameterMm, adapter.MountingCountersinkDepthMm);

            var channelBottomY = centerY - slotHeight / 2.0;
            var channelTopY = centerY + adapter.BackPlateHeightMm / 2.0 + 0.1;
            var channelHeight = channelTopY - channelBottomY;
            var channelCenterY = (channelTopY + channelBottomY) / 2.0;
            AddAdapterPocket(basePart, "Inschuifkamer cliptong", normalAxis, centerAlongPlinth, channelCenterY, plinthInnerFace + normalSign * standOff, normalSign, slotWidth, channelHeight, slotDepth);
            AddAdapterPocket(basePart, "Borgopening cliptong", normalAxis, centerAlongPlinth, channelCenterY, plinthInnerFace + normalSign * (standOff + slotDepth), normalSign, slotWidth - 2.0 * lipOverlap, channelHeight, lipThickness);

            AddAdapterBox(parts, "SEKTION C-clip inschuiftong " + label, centerAlongPlinth, centerY, plinthInnerFace + normalSign * standOff, normalSign, normalAxis, adapter.TongueWidthMm, adapter.TongueHeightMm, adapter.TongueThicknessMm, "hardware-clip");
        }

        private static void AddAdapterPocket(
            PortalAssemblyPart part,
            string name,
            string normalAxis,
            double centerAlongPlinth,
            double centerY,
            double normalStart,
            double normalSign,
            double sizeAlongPlinth,
            double sizeY,
            double sizeNormal)
        {
            var pocket = new PortalAssemblyPocket { Name = name, Shape = "box", Plane = normalAxis, IsThroughCutout = false };
            var normalCenter = normalStart + normalSign * sizeNormal / 2.0;
            if (string.Equals(normalAxis, "x", StringComparison.OrdinalIgnoreCase))
            {
                pocket.Xmm = normalCenter;
                pocket.Ymm = centerY;
                pocket.Zmm = centerAlongPlinth;
                pocket.SizeXmm = sizeNormal;
                pocket.SizeYmm = sizeY;
                pocket.SizeZmm = sizeAlongPlinth;
            }
            else
            {
                pocket.Xmm = centerAlongPlinth;
                pocket.Ymm = centerY;
                pocket.Zmm = normalCenter;
                pocket.SizeXmm = sizeAlongPlinth;
                pocket.SizeYmm = sizeY;
                pocket.SizeZmm = sizeNormal;
            }
            part.Pockets.Add(pocket);
        }

        private static PortalAssemblyPart AddAdapterBox(
            List<PortalAssemblyPart> parts,
            string name,
            double centerAlongPlinth,
            double centerY,
            double normalStart,
            double normalSign,
            string normalAxis,
            double sizeAlongPlinth,
            double sizeY,
            double sizeNormal,
            string kind)
        {
            PortalAssemblyPart part;
            if (string.Equals(normalAxis, "x", StringComparison.OrdinalIgnoreCase))
            {
                part = Part(name, kind, normalStart + normalSign * sizeNormal / 2.0, centerY, centerAlongPlinth, sizeNormal, sizeY, sizeAlongPlinth);
            }
            else
            {
                part = Part(name, kind, centerAlongPlinth, centerY, normalStart + normalSign * sizeNormal / 2.0, sizeAlongPlinth, sizeY, sizeNormal);
            }
            part.Shape = "box";
            parts.Add(part);
            return part;
        }

        private static void AddAdapterScrewHole(
            PortalAssemblyPart part,
            string normalAxis,
            double centerAlongPlinth,
            double centerY,
            double normalStart,
            double normalSign,
            double standOff,
            double diameter)
        {
            var normalCenter = normalStart + normalSign * standOff / 2.0;
            var hole = new PortalAssemblyHole { DiameterMm = diameter, DepthMm = standOff, Plane = normalAxis };
            var pocket = new PortalAssemblyPocket { Name = "Schroefgat adapter", Shape = "cylinder", Plane = normalAxis, IsThroughCutout = false };
            if (string.Equals(normalAxis, "x", StringComparison.OrdinalIgnoreCase))
            {
                hole.Xmm = normalStart + normalSign * standOff;
                hole.Ymm = centerY;
                hole.Zmm = centerAlongPlinth;
                pocket.Xmm = normalCenter;
                pocket.Ymm = centerY;
                pocket.Zmm = centerAlongPlinth;
                pocket.SizeXmm = standOff;
                pocket.SizeYmm = diameter;
                pocket.SizeZmm = diameter;
            }
            else
            {
                hole.Xmm = centerAlongPlinth;
                hole.Ymm = centerY;
                hole.Zmm = normalStart + normalSign * standOff;
                pocket.Xmm = centerAlongPlinth;
                pocket.Ymm = centerY;
                pocket.Zmm = normalCenter;
                pocket.SizeXmm = diameter;
                pocket.SizeYmm = diameter;
                pocket.SizeZmm = standOff;
            }
            part.Holes.Add(hole);
            part.Pockets.Add(pocket);
        }

        private static void AddAdapterCountersink(
            PortalAssemblyPart part,
            string normalAxis,
            double centerAlongPlinth,
            double centerY,
            double normalStart,
            double normalSign,
            double seatDistance,
            double holeDiameter,
            double diameter,
            double depth)
        {
            if (part == null || diameter <= 0 || depth <= 0) return;
            var normalCenter = normalStart + normalSign * (seatDistance - depth / 2.0);
            var pocket = new PortalAssemblyPocket
            {
                Name = "Conische verzinking adapterschroef",
                Shape = normalSign >= 0 ? "cone+" : "cone-",
                Plane = normalAxis,
                IsThroughCutout = false,
                MinorDiameterMm = holeDiameter
            };
            if (string.Equals(normalAxis, "x", StringComparison.OrdinalIgnoreCase))
            {
                pocket.Xmm = normalCenter;
                pocket.Ymm = centerY;
                pocket.Zmm = centerAlongPlinth;
                pocket.SizeXmm = depth;
                pocket.SizeYmm = diameter;
                pocket.SizeZmm = diameter;
            }
            else
            {
                pocket.Xmm = centerAlongPlinth;
                pocket.Ymm = centerY;
                pocket.Zmm = normalCenter;
                pocket.SizeXmm = diameter;
                pocket.SizeYmm = diameter;
                pocket.SizeZmm = depth;
            }
            part.Pockets.Add(pocket);
        }

        private static PortalAssemblyPart ApplySheetAppearance(PortalAssemblyPart part, SheetPart sheet, string thicknessAxis)
        {
            if (part == null || sheet == null || sheet.Material == null) return part;
            part.MaterialAppearance = sheet.Material.RenderAppearance;
            part.MaterialThicknessAxis = thicknessAxis;
            return part;
        }

        private static string SheetThicknessAxis(AssemblyOrientation orientation)
        {
            if (orientation == AssemblyOrientation.SheetVerticalX) return "z";
            if (orientation == AssemblyOrientation.SheetVerticalZ) return "x";
            return "y";
        }

        private static PortalAssemblyPart Part(string name, string kind, double x, double y, double z, double sx, double sy, double sz)
        {
            return new PortalAssemblyPart
            {
                Name = name,
                Kind = kind,
                Shape = "box",
                Xmm = x,
                Ymm = y,
                Zmm = z,
                SizeXmm = Math.Max(2, sx),
                SizeYmm = Math.Max(2, sy),
                SizeZmm = Math.Max(2, sz)
            };
        }

        private static SheetPart FindSheet(WorkbenchModel model, string partName)
        {
            foreach (var sheet in model.Sheets)
            {
                if (string.Equals(sheet.Name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    return sheet;
                }
            }

            return null;
        }

        private static double ValueOr(double value, double fallback)
        {
            return value > 0 ? value : fallback;
        }

        private struct Range1
        {
            public readonly double Start;
            public readonly double End;

            public Range1(double start, double end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
