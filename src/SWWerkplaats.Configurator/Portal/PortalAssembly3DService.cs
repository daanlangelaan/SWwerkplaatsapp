using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalAssembly3DService
    {
        public List<PortalAssemblyPart> Build(WorkbenchModel model, PortalQuoteRequest request)
        {
            var parts = BuildFromPlacements(model);
            if (request != null && string.Equals(request.Product, "werktafel", StringComparison.OrdinalIgnoreCase))
            {
                AddWorkbenchFrame(parts, request);
            }

            return parts;
        }

        private static List<PortalAssemblyPart> BuildFromPlacements(WorkbenchModel model)
        {
            var parts = new List<PortalAssemblyPart>();
            foreach (var placement in model.AssemblyPlacements)
            {
                var sheet = FindSheet(model, placement.PartName);
                var thickness = sheet == null || sheet.Material == null ? 18 : Math.Max(2, sheet.Material.ThicknessMm);
                var sx = placement.LengthMm;
                var sy = thickness;
                var sz = placement.WidthMm;

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
                    Kind = placement.Kind == AssemblyComponentKind.Profile ? "profile" : "sheet",
                    Xmm = placement.Xmm,
                    Ymm = placement.Ymm,
                    Zmm = placement.Zmm,
                    SizeXmm = Math.Max(2, sx),
                    SizeYmm = Math.Max(2, sy),
                    SizeZmm = Math.Max(2, sz)
                };
                ApplyHiddenDrawerInsertTrims(part);
                AddHoles(part, placement, sheet, thickness);
                AddPockets(part, placement, sheet, thickness);
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
                    var thickness = sheet.Material == null ? 18 : Math.Max(2, sheet.Material.ThicknessMm);
                    if (sheet.HasCornerNotches)
                    {
                        AddCornerNotchedHorizontalSheet(parts, placement, sheet, thickness);
                    }
                    else
                    {
                        var part = Part(sheet.Name, "sheet", 0, sheet.CenterHeightMm, 0, sheet.LengthMm, thickness, sheet.WidthMm);
                        AddHoles(part, placement, sheet, thickness);
                        AddPockets(part, placement, sheet, thickness);
                        parts.Add(part);
                    }
                }
            }

            return parts;
        }

        private static void AddCornerNotchedHorizontalSheet(List<PortalAssemblyPart> parts, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            var notch = Math.Max(0, Math.Min(sheet.CornerNotchSizeMm, Math.Min(sheet.LengthMm, sheet.WidthMm) / 2.0 - 1));
            if (notch <= 0)
            {
                var fallback = Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, sheet.LengthMm, thickness, sheet.WidthMm);
                AddHoles(fallback, placement, sheet, thickness);
                AddPockets(fallback, placement, sheet, thickness);
                parts.Add(fallback);
                return;
            }

            var centerLength = Math.Max(2, sheet.LengthMm - 2 * notch);
            var center = Part(placement.PartName + " midden", "sheet", placement.Xmm, placement.Ymm, placement.Zmm, centerLength, thickness, sheet.WidthMm);
            var left = Part(placement.PartName + " links", "sheet", placement.Xmm - sheet.LengthMm / 2.0 + notch / 2.0, placement.Ymm, placement.Zmm, notch, thickness, Math.Max(2, sheet.WidthMm - 2 * notch));
            var right = Part(placement.PartName + " rechts", "sheet", placement.Xmm + sheet.LengthMm / 2.0 - notch / 2.0, placement.Ymm, placement.Zmm, notch, thickness, Math.Max(2, sheet.WidthMm - 2 * notch));
            AddHoles(center, placement, sheet, thickness);
            AddHoles(left, placement, sheet, thickness);
            AddHoles(right, placement, sheet, thickness);
            AddPockets(center, placement, sheet, thickness);
            parts.Add(center);
            parts.Add(left);
            parts.Add(right);
        }

        private static void ApplyHiddenDrawerInsertTrims(PortalAssemblyPart part)
        {
            if (part == null || string.IsNullOrWhiteSpace(part.Name)) return;
            var insert = ProductDrawingStrategy.DefaultDrawerGrooveDepthMm;
            if (insert <= 0) return;

            if (StartsWith(part.Name, "Ladezijde") || StartsWith(part.Name, "Bovenlade zijde"))
            {
                TrimFromNegativeZ(part, insert);
                return;
            }

            if (StartsWith(part.Name, "Ladebodem") || StartsWith(part.Name, "Bovenlade bodem"))
            {
                TrimCenteredX(part, insert * 2.0);
                TrimCenteredZ(part, insert * 2.0);
                return;
            }

            if (StartsWith(part.Name, "Ladeachter") || StartsWith(part.Name, "Bovenlade achter"))
            {
                TrimCenteredX(part, insert * 2.0);
            }
        }

        private static void TrimFromNegativeZ(PortalAssemblyPart part, double amount)
        {
            var trim = Math.Min(Math.Max(0, amount), Math.Max(0, part.SizeZmm - 2));
            if (trim <= 0) return;
            part.Zmm += trim / 2.0;
            part.SizeZmm -= trim;
        }

        private static void TrimCenteredX(PortalAssemblyPart part, double amount)
        {
            var trim = Math.Min(Math.Max(0, amount), Math.Max(0, part.SizeXmm - 2));
            if (trim <= 0) return;
            part.SizeXmm -= trim;
        }

        private static void TrimCenteredZ(PortalAssemblyPart part, double amount)
        {
            var trim = Math.Min(Math.Max(0, amount), Math.Max(0, part.SizeZmm - 2));
            if (trim <= 0) return;
            part.SizeZmm -= trim;
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
                parts.Add(Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, thickness, panelHeight, panelDepth));
                return;
            }

            var upper = Part(placement.PartName + " boven uitsparing", "sheet", placement.Xmm, notchHeight + (panelHeight - notchHeight) / 2.0, placement.Zmm, thickness, panelHeight - notchHeight, panelDepth);
            var lowerDepth = panelDepth - notchDepth;
            var lowerZ = frontZ + notchDepth + lowerDepth / 2.0;
            var lower = Part(placement.PartName + " plintvoet", "sheet", placement.Xmm, notchHeight / 2.0, lowerZ, thickness, notchHeight, lowerDepth);
            AddHoles(upper, placement, sheet, thickness);
            AddHoles(lower, placement, sheet, thickness);
            AddPockets(upper, placement, sheet, thickness);
            parts.Add(upper);
            parts.Add(lower);
        }

        private static void AddHoles(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            if (sheet == null) return;
            foreach (var hole in sheet.Holes)
            {
                var localX = hole.Xmm - sheet.LengthMm / 2.0;
                var localY = hole.Ymm - sheet.WidthMm / 2.0;
                var assemblyHole = new PortalAssemblyHole { DiameterMm = Math.Max(3, hole.DiameterMm), DepthMm = hole.DepthMm };

                if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + thickness / 2.0 + 0.8;
                    assemblyHole.Zmm = placement.Zmm + localY;
                    assemblyHole.Plane = "y";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + localY;
                    assemblyHole.Zmm = placement.Zmm + VerticalXHoleFaceOffset(placement.PartName, thickness, 0.8);
                    assemblyHole.Plane = "z";
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    var side = VerticalZFaceOffset(placement.PartName, hole.Name, thickness, 0.8);
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
                    Xmm = visibleHole.Xmm,
                    Ymm = visibleHole.Ymm,
                    Zmm = placement.Zmm + (visibleHole.Zmm >= placement.Zmm ? -thickness / 2.0 - 0.8 : thickness / 2.0 + 0.8),
                    DiameterMm = visibleHole.DiameterMm,
                    DepthMm = visibleHole.DepthMm,
                    Plane = visibleHole.Plane
                };

                if (IsInsidePartBounds(part, oppositeZ)) part.Holes.Add(oppositeZ);
                return;
            }

            if (placement.Orientation != AssemblyOrientation.SheetVerticalZ) return;
            if (!StartsWith(placement.PartName, "Tussenschot ")) return;

            var opposite = new PortalAssemblyHole
            {
                Xmm = placement.Xmm + (visibleHole.Xmm >= placement.Xmm ? -thickness / 2.0 - 0.8 : thickness / 2.0 + 0.8),
                Ymm = visibleHole.Ymm,
                Zmm = visibleHole.Zmm,
                DiameterMm = visibleHole.DiameterMm,
                DepthMm = visibleHole.DepthMm,
                Plane = visibleHole.Plane
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

        private static void AddPockets(PortalAssemblyPart part, AssemblyPlacement placement, SheetPart sheet, double thickness)
        {
            if (sheet == null) return;
            foreach (var pocket in sheet.Pockets)
            {
                var localCenterX = pocket.Xmm + pocket.LengthMm / 2.0 - sheet.LengthMm / 2.0;
                var localCenterY = pocket.Ymm + pocket.WidthMm / 2.0 - sheet.WidthMm / 2.0;
                var assemblyPocket = new PortalAssemblyPocket
                {
                    Name = pocket.Name,
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

        private static void AddWorkbenchFrame(List<PortalAssemblyPart> parts, PortalQuoteRequest request)
        {
            var width = ValueOr(request.WidthMm, 1500);
            var depth = ValueOr(request.DepthMm, 750);
            var height = ValueOr(request.HeightMm, 900);
            var topT = 18.0;
            var profile = 40.0;
            var legH = height - topT;

            AddProfile(parts, "Bovenframe voor", 0, height - topT - profile / 2.0, -depth / 2.0 + profile / 2.0, width - 2 * profile, profile, profile);
            AddProfile(parts, "Bovenframe achter", 0, height - topT - profile / 2.0, depth / 2.0 - profile / 2.0, width - 2 * profile, profile, profile);
            AddProfile(parts, "Bovenframe links", -width / 2.0 + profile / 2.0, height - topT - profile / 2.0, 0, profile, profile, depth - 2 * profile);
            AddProfile(parts, "Bovenframe rechts", width / 2.0 - profile / 2.0, height - topT - profile / 2.0, 0, profile, profile, depth - 2 * profile);

            var xLeft = -width / 2.0 + profile / 2.0;
            var xRight = width / 2.0 - profile / 2.0;
            var zFront = -depth / 2.0 + profile / 2.0;
            var zBack = depth / 2.0 - profile / 2.0;
            AddProfile(parts, "Poot linksvoor", xLeft, legH / 2.0, zFront, profile, legH, profile);
            AddProfile(parts, "Poot rechtsvoor", xRight, legH / 2.0, zFront, profile, legH, profile);
            AddProfile(parts, "Poot linksachter", xLeft, legH / 2.0, zBack, profile, legH, profile);
            AddProfile(parts, "Poot rechtsachter", xRight, legH / 2.0, zBack, profile, legH, profile);
            if (request.IncludeLowerShelf)
            {
                AddFrameLayer(parts, "Onderframe", width, depth, Math.Max(80, request.LowerShelfHeightMm), profile);
            }

            if (request.IncludeMiddleShelf)
            {
                AddFrameLayer(parts, "Tussenframe", width, depth, Math.Max(120, request.MiddleShelfHeightMm), profile);
            }
        }

        private static void AddFrameLayer(List<PortalAssemblyPart> parts, string prefix, double width, double depth, double y, double profile)
        {
            AddProfile(parts, prefix + " voor", 0, y, -depth / 2.0 + profile / 2.0, width - 2 * profile, profile, profile);
            AddProfile(parts, prefix + " achter", 0, y, depth / 2.0 - profile / 2.0, width - 2 * profile, profile, profile);
            AddProfile(parts, prefix + " links", -width / 2.0 + profile / 2.0, y, 0, profile, profile, depth - 2 * profile);
            AddProfile(parts, prefix + " rechts", width / 2.0 - profile / 2.0, y, 0, profile, profile, depth - 2 * profile);
        }

        private static void AddProfile(List<PortalAssemblyPart> parts, string name, double x, double y, double z, double sx, double sy, double sz)
        {
            parts.Add(Part(name, "profile", x, y, z, sx, sy, sz));
        }

        private static PortalAssemblyPart Part(string name, string kind, double x, double y, double z, double sx, double sy, double sz)
        {
            return new PortalAssemblyPart
            {
                Name = name,
                Kind = kind,
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
