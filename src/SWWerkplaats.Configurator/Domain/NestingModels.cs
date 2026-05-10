using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class NestingPlan
    {
        public List<NestedStockSheet> StockSheets { get; private set; }

        public NestingPlan()
        {
            StockSheets = new List<NestedStockSheet>();
        }
    }

    public sealed class NestedStockSheet
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public double StockLengthMm { get; set; }
        public double StockWidthMm { get; set; }
        public int SheetNumber { get; set; }
        public List<NestedSheetPlacement> Placements { get; private set; }

        public NestedStockSheet()
        {
            Placements = new List<NestedSheetPlacement>();
        }
    }

    public sealed class NestedSheetPlacement
    {
        public SheetPart Part { get; set; }
        public int InstanceNumber { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public bool Rotated { get; set; }

        public double LengthMm
        {
            get { return Rotated ? Part.WidthMm : Part.LengthMm; }
        }

        public double WidthMm
        {
            get { return Rotated ? Part.LengthMm : Part.WidthMm; }
        }

        public string Label
        {
            get { return Part.Name + " " + Part.LengthMm.ToString("0") + "x" + Part.WidthMm.ToString("0") + "mm #" + InstanceNumber; }
        }
    }
}
