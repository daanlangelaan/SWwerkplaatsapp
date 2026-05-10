using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class SheetNestingEngine
    {
        public NestingPlan Build(WorkbenchModel model, MachineProfile machine, double spacingMm, double marginMm, double stockLengthOverrideMm, double stockWidthOverrideMm)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (machine == null) throw new ArgumentNullException("machine");

            var plan = new NestingPlan();
            var groups = GroupSheets(model.Sheets);
            foreach (var group in groups)
            {
                NestGroup(plan, group, machine, Math.Max(0, spacingMm), Math.Max(0, marginMm), stockLengthOverrideMm, stockWidthOverrideMm);
            }

            return plan;
        }

        private static void NestGroup(NestingPlan plan, SheetGroup group, MachineProfile machine, double spacingMm, double marginMm, double stockLengthOverrideMm, double stockWidthOverrideMm)
        {
            var stockLength = stockLengthOverrideMm > 0 ? stockLengthOverrideMm : (group.Material.SheetLengthMm > 0 ? group.Material.SheetLengthMm : machine.MaxXmm);
            var stockWidth = stockWidthOverrideMm > 0 ? stockWidthOverrideMm : (group.Material.SheetWidthMm > 0 ? group.Material.SheetWidthMm : machine.MaxYmm);
            stockLength = Math.Min(stockLength, machine.MaxXmm);
            stockWidth = Math.Min(stockWidth, machine.MaxYmm);
            if (stockLength <= 0) stockLength = machine.MaxXmm;
            if (stockWidth <= 0) stockWidth = machine.MaxYmm;

            group.Parts.Sort(delegate(SheetPart a, SheetPart b)
            {
                var areaCompare = (b.LengthMm * b.WidthMm).CompareTo(a.LengthMm * a.WidthMm);
                if (areaCompare != 0) return areaCompare;
                return b.LengthMm.CompareTo(a.LengthMm);
            });

            NestedStockSheet current = null;
            double cursorX = marginMm;
            double cursorY = marginMm;
            double rowHeight = 0;
            var sheetNumber = 0;

            foreach (var part in group.Parts)
            {
                var placed = false;
                while (!placed)
                {
                    if (current == null)
                    {
                        current = NewStockSheet(group.Material, stockLength, stockWidth, ++sheetNumber);
                        plan.StockSheets.Add(current);
                        cursorX = marginMm;
                        cursorY = marginMm;
                        rowHeight = 0;
                    }

                    var rotated = ShouldRotate(part, stockLength, stockWidth, marginMm);
                    var length = rotated ? part.WidthMm : part.LengthMm;
                    var width = rotated ? part.LengthMm : part.WidthMm;

                    if (length > stockLength - 2 * marginMm || width > stockWidth - 2 * marginMm)
                    {
                        throw new InvalidOperationException("Plaatdeel " + part.Name + " past niet op voorraadplaat " + stockLength.ToString("0") + "x" + stockWidth.ToString("0") + "mm.");
                    }

                    if (cursorX + length <= stockLength - marginMm && cursorY + width <= stockWidth - marginMm)
                    {
                        current.Placements.Add(new NestedSheetPlacement
                        {
                            Part = part,
                            InstanceNumber = CountExistingPlacements(plan, part.Name) + 1,
                            Xmm = cursorX,
                            Ymm = cursorY,
                            Rotated = rotated
                        });
                        cursorX += length + spacingMm;
                        rowHeight = Math.Max(rowHeight, width);
                        placed = true;
                    }
                    else
                    {
                        cursorX = marginMm;
                        cursorY += rowHeight + spacingMm;
                        rowHeight = 0;

                        if (cursorY + width > stockWidth - marginMm)
                        {
                            current = null;
                        }
                    }
                }
            }
        }

        private static bool ShouldRotate(SheetPart part, double stockLength, double stockWidth, double marginMm)
        {
            var normalFits = part.LengthMm <= stockLength - 2 * marginMm && part.WidthMm <= stockWidth - 2 * marginMm;
            var rotatedFits = part.WidthMm <= stockLength - 2 * marginMm && part.LengthMm <= stockWidth - 2 * marginMm;
            if (normalFits) return false;
            return rotatedFits;
        }

        private static NestedStockSheet NewStockSheet(Material material, double length, double width, int sheetNumber)
        {
            return new NestedStockSheet
            {
                Name = SafeName(material.Name) + "_NestPlaat_" + sheetNumber.ToString("00"),
                Material = material,
                StockLengthMm = length,
                StockWidthMm = width,
                SheetNumber = sheetNumber
            };
        }

        private static int CountExistingPlacements(NestingPlan plan, string partName)
        {
            var count = 0;
            foreach (var stock in plan.StockSheets)
            {
                foreach (var placement in stock.Placements)
                {
                    if (placement.Part.Name == partName) count++;
                }
            }

            return count;
        }

        private static List<SheetGroup> GroupSheets(IEnumerable<SheetPart> sheets)
        {
            var groups = new List<SheetGroup>();
            foreach (var sheet in sheets)
            {
                var group = FindGroup(groups, sheet.Material);
                if (group == null)
                {
                    group = new SheetGroup { Material = sheet.Material };
                    groups.Add(group);
                }

                var qty = Math.Max(1, sheet.Quantity);
                for (var i = 0; i < qty; i++)
                {
                    group.Parts.Add(sheet);
                }
            }

            return groups;
        }

        private static SheetGroup FindGroup(List<SheetGroup> groups, Material material)
        {
            foreach (var group in groups)
            {
                if (group.Material.Id == material.Id && Math.Abs(group.Material.ThicknessMm - material.ThicknessMm) < 0.001)
                {
                    return group;
                }
            }

            return null;
        }

        private static string SafeName(string value)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(" ", "_").Replace("/", "-");
        }

        private sealed class SheetGroup
        {
            public Material Material { get; set; }
            public List<SheetPart> Parts { get; private set; }

            public SheetGroup()
            {
                Parts = new List<SheetPart>();
            }
        }
    }
}
