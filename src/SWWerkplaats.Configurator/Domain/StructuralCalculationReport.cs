using System.Collections.Generic;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class StructuralCalculationReport
    {
        public string ProductId { get; set; }
        public string Status { get; set; }
        public double ReferenceLoadN { get; set; }
        public string ProfileMaterialId { get; set; }
        public double SpanMm { get; set; }
        public int ParallelBeamCount { get; set; }
        public double ElasticModulusNPerMm2 { get; set; }
        public double StrongAxisInertiaCm4 { get; set; }
        public double CalculatedDeflectionMm { get; set; }
        public string Formula { get; set; }
        public List<string> OpenData { get; private set; }

        public StructuralCalculationReport() { OpenData = new List<string>(); }
    }
}
