using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public interface ISolidWorksExporter
    {
        SolidWorksExportPlan CreateExportPlan(WorkbenchModel model);
    }
}
