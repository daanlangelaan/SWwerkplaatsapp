using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksExportPlan
    {
        public string AssemblyName { get; set; }
        public List<string> PartNames { get; private set; }

        public SolidWorksExportPlan()
        {
            PartNames = new List<string>();
        }

        public static SolidWorksExportPlan FromWorkbench(WorkbenchModel model)
        {
            var plan = new SolidWorksExportPlan
            {
                AssemblyName = model.ProjectName + ".SLDASM"
            };

            foreach (var profile in model.Profiles)
            {
                plan.PartNames.Add(profile.Name.Replace("/", "-") + "_" + profile.Material.Name.Replace(" ", "") + "_" + profile.LengthMm.ToString("0") + "mm.SLDPRT");
            }

            foreach (var sheet in model.Sheets)
            {
                plan.PartNames.Add(sheet.Name.Replace("/", "-") + "_" + sheet.Material.Name.Replace(" ", "") + "_" + sheet.LengthMm.ToString("0") + "x" + sheet.WidthMm.ToString("0") + ".SLDPRT");
            }

            return plan;
        }
    }
}
