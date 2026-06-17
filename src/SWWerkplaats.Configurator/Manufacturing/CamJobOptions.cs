using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class CamJobOptions
    {
        public bool EnablePencilMarking { get; set; }
        public PencilMarkingOptions PencilMarking { get; set; }
        public List<ToolDefinition> Tools { get; private set; }

        public CamJobOptions()
        {
            PencilMarking = PencilMarkingOptions.Default();
            Tools = new List<ToolDefinition>();
        }

        public ToolDefinition PrimaryTool
        {
            get
            {
                if (Tools.Count == 0)
                {
                    return LibraryCatalog.DefaultEndMill(4, 3.5);
                }

                return Tools[0];
            }
        }

        public int PencilToolNumber
        {
            get { return Tools.Count + 1; }
        }

        public static CamJobOptions FromPrimaryTool(ToolDefinition tool)
        {
            var options = new CamJobOptions();
            options.AddTool(tool);
            return options;
        }

        public void AddTool(ToolDefinition tool)
        {
            if (tool == null)
            {
                return;
            }

            foreach (var existing in Tools)
            {
                if (existing.Kind == tool.Kind && Math.Abs(existing.DiameterMm - tool.DiameterMm) < 0.001)
                {
                    return;
                }
            }

            Tools.Add(tool);
        }

        public PencilMarkingOptions BuildPencilMarkingOptions()
        {
            var source = PencilMarking ?? PencilMarkingOptions.Default();
            return new PencilMarkingOptions
            {
                ToolNumber = PencilToolNumber,
                ToolName = source.ToolName,
                WriteDepthMm = source.WriteDepthMm,
                FeedRateMmMin = source.FeedRateMmMin,
                PlungeRateMmMin = source.PlungeRateMmMin,
                TextHeightMm = source.TextHeightMm,
                PartMarginMm = source.PartMarginMm
            };
        }
    }
}
