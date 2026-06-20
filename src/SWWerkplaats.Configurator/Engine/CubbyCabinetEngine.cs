using System;
using System.Globalization;
using SWWerkplaats.Configurator.Drawing;
using SWWerkplaats.Configurator.Drawing.Products.CubbyCabinet;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class CubbyCabinetEngine
    {
        public WorkbenchModel Build(CubbyCabinetConfig config)
        {
            Validate(config);

            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                SheetFastener = config.SheetFastener
            };

            var material = config.CarcassMaterial;
            var backMaterial = config.BackMaterial ?? material;
            var t = material.ThicknessMm;
            var backT = config.IncludeBackPanel ? Math.Max(2, backMaterial.ThicknessMm) : 0;
            var slotWidth = CubbyCabinetDrawingRules.CombSlotWidth(t, config.CombSlotClearanceMm);
            var slotDepth = CubbyCabinetDrawingRules.CombSlotDepth(t);
            var grooveDepth = Math.Max(0.5, config.BackGrooveDepthMm);
            var gridWidth = Math.Max(40, config.WidthMm - 2.0 * t);
            var gridHeight = Math.Max(40, config.HeightMm - 2.0 * t);
            var gridDepth = Math.Max(40, config.DepthMm - config.GridInsetMm);

            var top = Sheet("Vakjeskast bovenplaat", material, config.WidthMm, config.DepthMm);
            AddBackGrooveAlongY(top, config, grooveDepth, OperationFace.NegativeY);
            AddSheet(model, top, 0, config.HeightMm - t / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var bottom = Sheet("Vakjeskast bodemplaat", material, config.WidthMm, config.DepthMm);
            AddBackGrooveAlongY(bottom, config, grooveDepth, OperationFace.NegativeY);
            AddSheet(model, bottom, 0, t / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var left = Sheet("Vakjeskast zijwand links", material, config.DepthMm, config.HeightMm);
            AddBackGrooveAlongX(left, config, grooveDepth, OperationFace.PositiveX);
            AddSheet(model, left, -config.WidthMm / 2.0 + t / 2.0, config.HeightMm / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            var right = Sheet("Vakjeskast zijwand rechts", material, config.DepthMm, config.HeightMm);
            right.MirrorInNestingX = true;
            AddBackGrooveAlongX(right, config, grooveDepth, OperationFace.NegativeX);
            AddSheet(model, right, config.WidthMm / 2.0 - t / 2.0, config.HeightMm / 2.0, 0, AssemblyOrientation.SheetVerticalZ);

            AddVerticalCombs(model, config, material, t, gridWidth, gridHeight, gridDepth, slotWidth, slotDepth);
            AddHorizontalCombs(model, config, material, t, gridWidth, gridHeight, gridDepth, slotWidth, slotDepth);

            if (config.IncludeBackPanel)
            {
                AddBackPanelSegments(model, config, backMaterial, t, slotWidth, grooveDepth, backT);
            }

            AddHardware(model, config);
            return model;
        }

        private static void AddVerticalCombs(WorkbenchModel model, CubbyCabinetConfig config, Material material, double t, double gridWidth, double gridHeight, double gridDepth, double slotWidth, double slotDepth)
        {
            for (var column = 1; column < config.ColumnCount; column++)
            {
                var panel = Sheet("Vakjeskast staander kam " + column.ToString(CultureInfo.InvariantCulture), material, gridDepth, gridHeight);
                for (var row = 1; row < config.RowCount; row++)
                {
                    var y = row * config.CellHeightMm + (row - 0.5) * t - slotWidth / 2.0;
                    AddPocket(panel, "Halfhout sleuf ligger R" + row.ToString(CultureInfo.InvariantCulture), 0, y, Math.Max(10, gridDepth / 2.0), slotWidth, slotDepth, OperationFace.CenterPlane, "Sleuf voor horizontale kam; definitieve doorsteekstrategie apart controleren.");
                }

                AddBackEdgeFastenerHoles(panel, config, gridDepth, gridHeight);
                var x = -config.WidthMm / 2.0 + t + column * config.CellWidthMm + (column - 0.5) * t;
                AddSheet(model, panel, x, config.HeightMm / 2.0, config.GridInsetMm / 2.0, AssemblyOrientation.SheetVerticalZ);
            }
        }

        private static void AddHorizontalCombs(WorkbenchModel model, CubbyCabinetConfig config, Material material, double t, double gridWidth, double gridHeight, double gridDepth, double slotWidth, double slotDepth)
        {
            for (var row = 1; row < config.RowCount; row++)
            {
                var panel = Sheet("Vakjeskast ligger kam " + row.ToString(CultureInfo.InvariantCulture), material, gridWidth, gridDepth);
                for (var column = 1; column < config.ColumnCount; column++)
                {
                    var x = column * config.CellWidthMm + (column - 0.5) * t - slotWidth / 2.0;
                    AddPocket(panel, "Halfhout sleuf staander C" + column.ToString(CultureInfo.InvariantCulture), x, Math.Max(0, gridDepth / 2.0), slotWidth, Math.Max(10, gridDepth / 2.0), slotDepth, OperationFace.CenterPlane, "Sleuf voor verticale kam; grijpt vanaf tegengestelde zijde in de staander.");
                }

                var y = t + row * config.CellHeightMm + (row - 0.5) * t;
                AddSheet(model, panel, 0, y, config.GridInsetMm / 2.0, AssemblyOrientation.SheetHorizontal);
            }
        }

        private static void AddBackPanelSegments(WorkbenchModel model, CubbyCabinetConfig config, Material backMaterial, double t, double slotWidth, double grooveDepth, double backT)
        {
            if (FitsOnSingleSheet(backMaterial, config.WidthMm, config.HeightMm))
            {
                var back = Sheet("Vakjeskast achterwand", backMaterial, config.WidthMm, config.HeightMm);
                AddBackPanelGrooves(back, config, t, slotWidth, grooveDepth);
                AddBackPanelFasteners(back, config, t);
                AddSheet(model, back, 0, config.HeightMm / 2.0, config.DepthMm / 2.0 + backT / 2.0, AssemblyOrientation.SheetVerticalX);
                return;
            }

            var segmentWidth = config.WidthMm / config.ColumnCount;
            for (var column = 0; column < config.ColumnCount; column++)
            {
                var segment = Sheet("Vakjeskast achterwand C" + (column + 1).ToString(CultureInfo.InvariantCulture), backMaterial, segmentWidth, config.HeightMm);
                AddBackPanelSegmentGrooves(segment, config, column, t, slotWidth, grooveDepth);
                AddBackPanelSegmentFasteners(segment, config, column, t);
                var x = -config.WidthMm / 2.0 + segmentWidth * (column + 0.5);
                AddSheet(model, segment, x, config.HeightMm / 2.0, config.DepthMm / 2.0 + backT / 2.0, AssemblyOrientation.SheetVerticalX);
            }
        }

        private static bool FitsOnSingleSheet(Material material, double length, double width)
        {
            if (material == null || material.SheetLengthMm <= 0 || material.SheetWidthMm <= 0) return true;
            return (length <= material.SheetLengthMm && width <= material.SheetWidthMm)
                || (width <= material.SheetLengthMm && length <= material.SheetWidthMm);
        }

        private static void AddBackPanelGrooves(SheetPart back, CubbyCabinetConfig config, double t, double slotWidth, double grooveDepth)
        {
            var grooveWidth = t + Math.Max(0, config.BackGrooveClearanceMm);
            AddPocket(back, "Linker zijwandgroef", 0, 0, grooveWidth, back.WidthMm, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor linker zijwand.");
            AddPocket(back, "Rechter zijwandgroef", back.LengthMm - grooveWidth, 0, grooveWidth, back.WidthMm, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor rechter zijwand.");
            AddPocket(back, "Bodemgroef", 0, 0, back.LengthMm, grooveWidth, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor bodemplaat.");
            AddPocket(back, "Bovenplaatgroef", 0, back.WidthMm - grooveWidth, back.LengthMm, grooveWidth, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor bovenplaat.");

            for (var column = 1; column < config.ColumnCount; column++)
            {
                var x = t + column * config.CellWidthMm + (column - 0.5) * t - slotWidth / 2.0;
                AddPocket(back, "Staander achterwandgroef " + column.ToString(CultureInfo.InvariantCulture), x, t, slotWidth, Math.Max(10, back.WidthMm - 2.0 * t), grooveDepth, OperationFace.NegativeZ, "Positioneergroef voor staander kam.");
            }

            for (var row = 1; row < config.RowCount; row++)
            {
                var y = t + row * config.CellHeightMm + (row - 0.5) * t - slotWidth / 2.0;
                AddPocket(back, "Ligger achterwandgroef " + row.ToString(CultureInfo.InvariantCulture), t, y, Math.Max(10, back.LengthMm - 2.0 * t), slotWidth, grooveDepth, OperationFace.NegativeZ, "Positioneergroef voor ligger kam.");
            }
        }

        private static void AddBackPanelFasteners(SheetPart back, CubbyCabinetConfig config, double t)
        {
            var diameter = AssemblyHoleDiameter(config);
            AddMountingLine(back, 45, t / 2.0, back.LengthMm - 45, t / 2.0, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar bodemplaat");
            AddMountingLine(back, 45, back.WidthMm - t / 2.0, back.LengthMm - 45, back.WidthMm - t / 2.0, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar bovenplaat");
            AddMountingLine(back, t / 2.0, 45, t / 2.0, back.WidthMm - 45, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar linker zijwand");
            AddMountingLine(back, back.LengthMm - t / 2.0, 45, back.LengthMm - t / 2.0, back.WidthMm - 45, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar rechter zijwand");

            for (var column = 1; column < config.ColumnCount; column++)
            {
                var x = t + column * config.CellWidthMm + (column - 0.5) * t;
                AddMountingLine(back, x, t + 45, x, back.WidthMm - t - 45, diameter, config.DividerBackFastenerMaxSpacingMm, "Achterwand naar staander " + column.ToString(CultureInfo.InvariantCulture));
            }

            for (var row = 1; row < config.RowCount; row++)
            {
                var y = t + row * config.CellHeightMm + (row - 0.5) * t;
                AddMountingLine(back, t + 45, y, back.LengthMm - t - 45, y, diameter, config.DividerBackFastenerMaxSpacingMm, "Achterwand naar ligger " + row.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AddBackPanelSegmentGrooves(SheetPart back, CubbyCabinetConfig config, int column, double t, double slotWidth, double grooveDepth)
        {
            var grooveWidth = t + Math.Max(0, config.BackGrooveClearanceMm);
            if (column == 0)
            {
                AddPocket(back, "Linker zijwandgroef", 0, 0, grooveWidth, back.WidthMm, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor linker zijwand.");
            }

            if (column == config.ColumnCount - 1)
            {
                AddPocket(back, "Rechter zijwandgroef", back.LengthMm - grooveWidth, 0, grooveWidth, back.WidthMm, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor rechter zijwand.");
            }

            AddPocket(back, "Bodemgroef", 0, 0, back.LengthMm, grooveWidth, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor bodemplaat.");
            AddPocket(back, "Bovenplaatgroef", 0, back.WidthMm - grooveWidth, back.LengthMm, grooveWidth, grooveDepth, OperationFace.NegativeZ, "Verdiepte groef voor bovenplaat.");

            if (column < config.ColumnCount - 1)
            {
                AddPocket(back, "Staander achterwandgroef rechts", back.LengthMm - slotWidth / 2.0, t, slotWidth / 2.0, Math.Max(10, back.WidthMm - 2.0 * t), grooveDepth, OperationFace.NegativeZ, "Halfgroef voor staander op segmentnaad.");
            }

            if (column > 0)
            {
                AddPocket(back, "Staander achterwandgroef links", 0, t, slotWidth / 2.0, Math.Max(10, back.WidthMm - 2.0 * t), grooveDepth, OperationFace.NegativeZ, "Halfgroef voor staander op segmentnaad.");
            }

            for (var row = 1; row < config.RowCount; row++)
            {
                var y = t + row * config.CellHeightMm + (row - 0.5) * t - slotWidth / 2.0;
                AddPocket(back, "Ligger achterwandgroef " + row.ToString(CultureInfo.InvariantCulture), 0, y, back.LengthMm, slotWidth, grooveDepth, OperationFace.NegativeZ, "Positioneergroef voor ligger kam.");
            }
        }

        private static void AddBackPanelSegmentFasteners(SheetPart back, CubbyCabinetConfig config, int column, double t)
        {
            var diameter = AssemblyHoleDiameter(config);
            AddMountingLine(back, 45, t / 2.0, back.LengthMm - 45, t / 2.0, diameter, config.BackFastenerMaxSpacingMm, "Achterwandsegment naar bodemplaat");
            AddMountingLine(back, 45, back.WidthMm - t / 2.0, back.LengthMm - 45, back.WidthMm - t / 2.0, diameter, config.BackFastenerMaxSpacingMm, "Achterwandsegment naar bovenplaat");

            if (column == 0)
            {
                AddMountingLine(back, t / 2.0, 45, t / 2.0, back.WidthMm - 45, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar linker zijwand");
            }

            if (column == config.ColumnCount - 1)
            {
                AddMountingLine(back, back.LengthMm - t / 2.0, 45, back.LengthMm - t / 2.0, back.WidthMm - 45, diameter, config.BackFastenerMaxSpacingMm, "Achterwand naar rechter zijwand");
            }

            if (column < config.ColumnCount - 1)
            {
                AddMountingLine(back, back.LengthMm - t / 2.0, t + 45, back.LengthMm - t / 2.0, back.WidthMm - t - 45, diameter, config.DividerBackFastenerMaxSpacingMm, "Achterwand naar staander rechts");
            }

            if (column > 0)
            {
                AddMountingLine(back, t / 2.0, t + 45, t / 2.0, back.WidthMm - t - 45, diameter, config.DividerBackFastenerMaxSpacingMm, "Achterwand naar staander links");
            }

            for (var row = 1; row < config.RowCount; row++)
            {
                var y = t + row * config.CellHeightMm + (row - 0.5) * t;
                AddMountingLine(back, 45, y, back.LengthMm - 45, y, diameter, config.DividerBackFastenerMaxSpacingMm, "Achterwand naar ligger " + row.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AddBackEdgeFastenerHoles(SheetPart panel, CubbyCabinetConfig config, double panelLength, double panelHeight)
        {
            var diameter = AssemblyHoleDiameter(config);
            AddMountingLine(panel, panelLength - 12, 45, panelLength - 12, panelHeight - 45, diameter, config.DividerBackFastenerMaxSpacingMm, "Staander naar achterwand");
        }

        private static void AddBackGrooveAlongX(SheetPart sheet, CubbyCabinetConfig config, double grooveDepth, OperationFace face)
        {
            if (!config.IncludeBackPanel) return;
            var width = Math.Max(2, config.BackMaterial == null ? 18.0 : config.BackMaterial.ThicknessMm);
            AddPocket(sheet, "Achterwand-rabat", Math.Max(0, sheet.LengthMm - width), 0, width, sheet.WidthMm, grooveDepth, face, "Verdiepte achterwandgroef.");
        }

        private static void AddBackGrooveAlongY(SheetPart sheet, CubbyCabinetConfig config, double grooveDepth, OperationFace face)
        {
            if (!config.IncludeBackPanel) return;
            var width = Math.Max(2, config.BackMaterial == null ? 18.0 : config.BackMaterial.ThicknessMm);
            AddPocket(sheet, "Achterwand-rabat", 0, Math.Max(0, sheet.WidthMm - width), sheet.LengthMm, width, grooveDepth, face, "Verdiepte achterwandgroef.");
        }

        private static void AddHardware(WorkbenchModel model, CubbyCabinetConfig config)
        {
            model.Hardware.Add(new HardwareItem
            {
                Name = "Schroeven achterwand/vakverdeling",
                ArticleNumber = config.SheetFastener == null ? "PANEL_SCREW" : config.SheetFastener.Id,
                Quantity = EstimateFastenerCount(config),
                Unit = "st",
                Note = "Voor achterwand, buitenkast en vakverdeling."
            });
        }

        private static int EstimateFastenerCount(CubbyCabinetConfig config)
        {
            return 8 + Math.Max(0, config.ColumnCount - 1) * 6 + Math.Max(0, config.RowCount - 1) * 6;
        }

        private static SheetPart Sheet(string name, Material material, double length, double width)
        {
            return SheetDrawing.CreateSheet(name, material, length, width);
        }

        private static void AddSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
        }

        private static void AddPocket(SheetPart sheet, string name, double x, double y, double length, double width, double depth, OperationFace face, string note)
        {
            SheetOperations.AddPocket(sheet, name, x, y, length, width, depth, face, note);
        }

        private static void AddMountingLine(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing, string name)
        {
            SheetOperations.AddMountingLine(sheet, x1, y1, x2, y2, diameter, Math.Max(40, maxSpacing), name, SheetHoleSupportKind.PanelScrew);
        }

        private static double AssemblyHoleDiameter(CubbyCabinetConfig config)
        {
            return config.SheetFastener == null || config.SheetFastener.ClearanceHoleDiameterMm <= 0 ? 4.5 : config.SheetFastener.ClearanceHoleDiameterMm;
        }

        private static void Validate(CubbyCabinetConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.CarcassMaterial == null) throw new InvalidOperationException("Vakjeskast mist plaatmateriaal.");
            if (config.CarcassMaterial.ThicknessMm <= 0) config.CarcassMaterial.ThicknessMm = 18;
            if (config.ColumnCount < 1) config.ColumnCount = 1;
            if (config.RowCount < 1) config.RowCount = 1;
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.HeightMm <= 0) throw new InvalidOperationException("Vakjeskast heeft ongeldige buitenmaten.");
            if (config.BackFastenerMaxSpacingMm <= 0) config.BackFastenerMaxSpacingMm = 220;
            if (config.DividerBackFastenerMaxSpacingMm <= 0) config.DividerBackFastenerMaxSpacingMm = 260;
        }
    }
}
