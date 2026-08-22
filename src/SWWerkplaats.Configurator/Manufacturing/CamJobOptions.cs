using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class CamJobOptions
    {
        public bool EnableMonitoringMarkers { get; set; }
        public bool EnablePencilMarking { get; set; }
        public bool EnableWoodScrewCountersinks { get; set; }
        public bool EnableOutsideEdgeChamfer { get; set; }
        public double EdgeChamferWidthMm { get; set; }
        public double ThroughCutOvertravelMm { get; set; }
        public double TabWidthMm { get; set; }
        public double TabHeightMm { get; set; }
        public double SafeTravelZMm { get; set; }
        public double ContourOnionSkinMm { get; set; }
        public double FinalContourFeedRateMmMin { get; set; }
        public double FinalContourRampLengthMm { get; set; }
        public PencilMarkingOptions PencilMarking { get; set; }
        public List<ToolDefinition> Tools { get; private set; }

        public CamJobOptions()
        {
            EnableMonitoringMarkers = true;
            PencilMarking = PencilMarkingOptions.Default();
            EdgeChamferWidthMm = 1.0;
            ThroughCutOvertravelMm = 1.0;
            TabWidthMm = 10.0;
            TabHeightMm = 1.0;
            SafeTravelZMm = 15.0;
            ContourOnionSkinMm = 1.0;
            FinalContourFeedRateMmMin = 1600.0;
            FinalContourRampLengthMm = 30.0;
            Tools = new List<ToolDefinition>();
        }

        public ToolDefinition PrimaryTool
        {
            get
            {
                if (Tools.Count == 0)
                {
                    return LibraryCatalog.DefaultEndMill(3, 2.0);
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
