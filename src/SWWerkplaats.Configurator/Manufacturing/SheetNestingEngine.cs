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

            var sheetNumber = 0;
            var openSheets = new List<SheetPacker>();

            foreach (var part in group.Parts)
            {
                PlacementCandidate best = null;
                foreach (var packer in openSheets)
                {
                    var candidate = packer.FindBest(part);
                    if (candidate != null && IsBetterCandidate(candidate, best))
                    {
                        best = candidate;
                    }
                }

                if (best == null)
                {
                    var stock = NewStockSheet(group.Material, stockLength, stockWidth, ++sheetNumber);
                    plan.StockSheets.Add(stock);
                    var packer = new SheetPacker(stock, stockLength, stockWidth, marginMm, spacingMm);
                    openSheets.Add(packer);
                    best = packer.FindBest(part);
                    if (best == null)
                    {
                        throw new InvalidOperationException("Plaatdeel " + part.Name + " past niet op voorraadplaat " + stockLength.ToString("0") + "x" + stockWidth.ToString("0") + "mm.");
                    }
                }

                best.Packer.Place(best, CountExistingPlacements(plan, part.Name) + 1);
            }
        }

        private static bool IsBetterCandidate(PlacementCandidate candidate, PlacementCandidate currentBest)
        {
            if (currentBest == null) return true;
            // Prefer filling earlier stock sheets before optimizing the local fit on newer sheets.
            if (candidate.SheetNumber != currentBest.SheetNumber)
            {
                return candidate.SheetNumber < currentBest.SheetNumber;
            }

            return candidate.Score < currentBest.Score;
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

        private sealed class SheetPacker
        {
            private readonly NestedStockSheet stock;
            private readonly double spacing;
            private readonly List<FreeRect> freeRects;

            public SheetPacker(NestedStockSheet stock, double stockLength, double stockWidth, double margin, double spacing)
            {
                this.stock = stock;
                this.spacing = spacing;
                freeRects = new List<FreeRect>();
                freeRects.Add(new FreeRect
                {
                    X = margin,
                    Y = margin,
                    Width = stockLength - 2 * margin,
                    Height = stockWidth - 2 * margin
                });
            }

            public int SheetNumber
            {
                get { return stock.SheetNumber; }
            }

            public PlacementCandidate FindBest(SheetPart part)
            {
                PlacementCandidate best = null;
                TryCandidate(part, false, ref best);
                TryCandidate(part, true, ref best);
                return best;
            }

            private void TryCandidate(SheetPart part, bool rotated, ref PlacementCandidate best)
            {
                var length = rotated ? part.WidthMm : part.LengthMm;
                var width = rotated ? part.LengthMm : part.WidthMm;
                for (var i = 0; i < freeRects.Count; i++)
                {
                    var rect = freeRects[i];
                    if (length > rect.Width || width > rect.Height) continue;
                    var leftoverX = rect.Width - length;
                    var leftoverY = rect.Height - width;
                    var shortSide = Math.Min(leftoverX, leftoverY);
                    var longSide = Math.Max(leftoverX, leftoverY);
                    var score = shortSide * 100000 + longSide;
                    if (best == null || score < best.Score)
                    {
                        best = new PlacementCandidate
                        {
                            Packer = this,
                            Part = part,
                            FreeRectIndex = i,
                            X = rect.X,
                            Y = rect.Y,
                            Length = length,
                            Width = width,
                            Rotated = rotated,
                            Score = score
                        };
                    }
                }
            }

            public void Place(PlacementCandidate candidate, int instanceNumber)
            {
                stock.Placements.Add(new NestedSheetPlacement
                {
                    Part = candidate.Part,
                    InstanceNumber = instanceNumber,
                    Xmm = candidate.X,
                    Ymm = candidate.Y,
                    Rotated = candidate.Rotated
                });

                var used = new FreeRect
                {
                    X = candidate.X,
                    Y = candidate.Y,
                    Width = candidate.Length + spacing,
                    Height = candidate.Width + spacing
                };

                for (var i = freeRects.Count - 1; i >= 0; i--)
                {
                    if (SplitFreeRect(freeRects[i], used))
                    {
                        freeRects.RemoveAt(i);
                    }
                }

                PruneFreeRects();
            }

            private bool SplitFreeRect(FreeRect rect, FreeRect used)
            {
                if (used.X >= rect.Right || used.Right <= rect.X || used.Y >= rect.Top || used.Top <= rect.Y)
                {
                    return false;
                }

                if (used.X > rect.X && used.X < rect.Right)
                {
                    freeRects.Add(new FreeRect { X = rect.X, Y = rect.Y, Width = used.X - rect.X, Height = rect.Height });
                }

                if (used.Right < rect.Right)
                {
                    freeRects.Add(new FreeRect { X = used.Right, Y = rect.Y, Width = rect.Right - used.Right, Height = rect.Height });
                }

                if (used.Y > rect.Y && used.Y < rect.Top)
                {
                    freeRects.Add(new FreeRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = used.Y - rect.Y });
                }

                if (used.Top < rect.Top)
                {
                    freeRects.Add(new FreeRect { X = rect.X, Y = used.Top, Width = rect.Width, Height = rect.Top - used.Top });
                }

                return true;
            }

            private void PruneFreeRects()
            {
                for (var i = freeRects.Count - 1; i >= 0; i--)
                {
                    if (freeRects[i].Width <= 1 || freeRects[i].Height <= 1)
                    {
                        freeRects.RemoveAt(i);
                        continue;
                    }

                    for (var j = freeRects.Count - 1; j >= 0; j--)
                    {
                        if (i == j) continue;
                        if (Contains(freeRects[j], freeRects[i]))
                        {
                            freeRects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            private static bool Contains(FreeRect outer, FreeRect inner)
            {
                return inner.X >= outer.X && inner.Y >= outer.Y && inner.Right <= outer.Right && inner.Top <= outer.Top;
            }
        }

        private sealed class PlacementCandidate
        {
            public SheetPacker Packer { get; set; }
            public SheetPart Part { get; set; }
            public int FreeRectIndex { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Length { get; set; }
            public double Width { get; set; }
            public bool Rotated { get; set; }
            public double Score { get; set; }
            public int SheetNumber
            {
                get { return Packer.SheetNumber; }
            }
        }

        private sealed class FreeRect
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Right { get { return X + Width; } }
            public double Top { get { return Y + Height; } }
        }
    }
}
