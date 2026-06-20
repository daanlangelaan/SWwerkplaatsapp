using System;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Drawing
{
    public static class SheetOperations
    {
        public static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, string note)
        {
            AddPocket(sheet, name, x, y, length, width, depth, OperationFace.CenterPlane, note);
        }

        public static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, OperationFace face, string note)
        {
            if (sheet == null || sheet.Material == null || length <= 0 || width <= 0 || depth <= 0) return;

            var safeX = Math.Max(0, Math.Min(sheet.LengthMm, x));
            var safeY = Math.Max(0, Math.Min(sheet.WidthMm, y));
            var safeLength = Math.Max(0, Math.Min(length, sheet.LengthMm - safeX));
            var safeWidth = Math.Max(0, Math.Min(width, sheet.WidthMm - safeY));
            if (safeLength <= 0 || safeWidth <= 0) return;

            sheet.Pockets.Add(new SheetPocket
            {
                Name = name,
                Xmm = Math.Round(safeX, 3),
                Ymm = Math.Round(safeY, 3),
                LengthMm = Math.Round(safeLength, 3),
                WidthMm = Math.Round(safeWidth, 3),
                DepthMm = Math.Round(Math.Min(depth, Math.Max(0.1, sheet.Material.ThicknessMm - 0.1)), 3),
                Face = face,
                DepthMode = OperationDepthMode.PocketFromFace,
                Note = note
            });
        }

        public static void AddThroughCutout(SheetPart sheet, string name, double x, double y, double length, double width, OperationFace face, string note)
        {
            if (sheet == null || sheet.Material == null || length <= 0 || width <= 0) return;

            var safeX = Math.Max(0, Math.Min(sheet.LengthMm, x));
            var safeY = Math.Max(0, Math.Min(sheet.WidthMm, y));
            var safeLength = Math.Max(0, Math.Min(length, sheet.LengthMm - safeX));
            var safeWidth = Math.Max(0, Math.Min(width, sheet.WidthMm - safeY));
            if (safeLength <= 0 || safeWidth <= 0) return;

            sheet.Pockets.Add(new SheetPocket
            {
                Name = name,
                Xmm = Math.Round(safeX, 3),
                Ymm = Math.Round(safeY, 3),
                LengthMm = Math.Round(safeLength, 3),
                WidthMm = Math.Round(safeWidth, 3),
                DepthMm = Math.Round(Math.Max(0.1, sheet.Material.ThicknessMm), 3),
                Face = face,
                DepthMode = OperationDepthMode.Through,
                Note = note
            });
        }

        public static void AddUniqueThroughHole(SheetPart sheet, double x, double y, double diameter, string name, SheetHoleSupportKind supportKind, double edgeClampMm)
        {
            if (sheet == null) return;

            x = Math.Round(Math.Max(edgeClampMm, Math.Min(sheet.LengthMm - edgeClampMm, x)), 3);
            y = Math.Round(Math.Max(edgeClampMm, Math.Min(sheet.WidthMm - edgeClampMm, y)), 3);
            if (HasHoleAt(sheet, x, y, diameter)) return;

            sheet.Holes.Add(new SheetHole
            {
                Name = name,
                Xmm = x,
                Ymm = y,
                DiameterMm = diameter,
                DepthMm = 0,
                Face = OperationFace.CenterPlane,
                DepthMode = OperationDepthMode.Through,
                Countersunk = false,
                SupportKind = supportKind
            });
        }

        public static void AddMountingLine(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing, string namePrefix, SheetHoleSupportKind supportKind)
        {
            if (sheet == null) return;

            var points = SheetPatterns.LinePoints(x1, y1, x2, y2, maxSpacing);
            foreach (var point in points)
            {
                AddUniqueThroughHole(
                    sheet,
                    point.Xmm,
                    point.Ymm,
                    diameter,
                    namePrefix + " " + (sheet.Holes.Count + 1),
                    supportKind,
                    6);
            }
        }

        public static bool HasHoleAt(SheetPart sheet, double x, double y, double diameter)
        {
            if (sheet == null) return false;

            foreach (var hole in sheet.Holes)
            {
                if (Math.Abs(hole.Xmm - x) < 0.01 && Math.Abs(hole.Ymm - y) < 0.01 && Math.Abs(hole.DiameterMm - diameter) < 0.01)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
