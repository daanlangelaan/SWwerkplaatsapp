using System;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Drawing
{
    public static class SheetDrawing
    {
        public static SheetPart CreateSheet(string name, Material material, double length, double width)
        {
            return new SheetPart
            {
                Name = name,
                Material = material,
                LengthMm = Math.Round(length, 2),
                WidthMm = Math.Round(width, 2),
                Quantity = 1,
                UseTabs = false,
                MirrorInNestingX = false
            };
        }

        public static void AddSheetToModel(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            if (model == null || sheet == null) return;

            sheet.CenterHeightMm = y;
            sheet.UseTabs = sheet.LengthMm * sheet.WidthMm < 300 * 300;
            model.Sheets.Add(sheet);
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Sheet,
                PartName = sheet.Name,
                LengthMm = sheet.LengthMm,
                WidthMm = sheet.WidthMm,
                Xmm = x,
                Ymm = y,
                Zmm = z,
                Orientation = orientation
            });
        }
    }
}
