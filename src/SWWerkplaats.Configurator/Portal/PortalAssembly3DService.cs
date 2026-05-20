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
                AddHoles(part, placement, sheet, thickness);
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
                parts.Add(Part(placement.PartName, "sheet", placement.Xmm, placement.Ymm, placement.Zmm, thickness, panelHeight, panelDepth));
                return;
            }

            var upper = Part(placement.PartName + " boven uitsparing", "sheet", placement.Xmm, notchHeight + (panelHeight - notchHeight) / 2.0, placement.Zmm, thickness, panelHeight - notchHeight, panelDepth);
            var lowerDepth = panelDepth - notchDepth;
            var lowerZ = frontZ + notchDepth + lowerDepth / 2.0;
            var lower = Part(placement.PartName + " plintvoet", "sheet", placement.Xmm, notchHeight / 2.0, lowerZ, thickness, notchHeight, lowerDepth);
            AddHoles(upper, placement, sheet, thickness);
            AddHoles(lower, placement, sheet, thickness);
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
                var assemblyHole = new PortalAssemblyHole { DiameterMm = Math.Max(3, hole.DiameterMm) };

                if (placement.Orientation == AssemblyOrientation.SheetHorizontal)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + thickness / 2.0 + 0.8;
                    assemblyHole.Zmm = placement.Zmm + localY;
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalX)
                {
                    assemblyHole.Xmm = placement.Xmm + localX;
                    assemblyHole.Ymm = placement.Ymm + localY;
                    assemblyHole.Zmm = placement.Zmm - thickness / 2.0 - 0.8;
                }
                else if (placement.Orientation == AssemblyOrientation.SheetVerticalZ)
                {
                    assemblyHole.Xmm = placement.Xmm - thickness / 2.0 - 0.8;
                    assemblyHole.Ymm = placement.Ymm + localY;
                    assemblyHole.Zmm = placement.Zmm + localX;
                }
                else
                {
                    continue;
                }

                part.Holes.Add(assemblyHole);
            }
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
    }
}
