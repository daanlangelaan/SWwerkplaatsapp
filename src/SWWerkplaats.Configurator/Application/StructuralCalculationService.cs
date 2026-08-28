using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class StructuralCalculationService
    {
        public StructuralCalculationReport Calculate(string productId, string profileMaterialId, double spanMm, int parallelBeamCount)
        {
            if (spanMm <= 0 || parallelBeamCount <= 0) throw new ArgumentException("Ongeldige liggergeometrie voor constructieberekening.");
            var values = LoadRequired(productId);
            double inertiaCm4;
            if (!values.InertiaByMaterialId.TryGetValue(profileMaterialId ?? string.Empty, out inertiaCm4))
                throw new InvalidOperationException("Traagheidsmoment ontbreekt voor constructieprofiel " + profileMaterialId + ".");
            var loadPerBeam = values.ReferenceLoadN / parallelBeamCount;
            var inertiaMm4 = inertiaCm4 * 10000.0;
            var deflection = 5.0 * loadPerBeam * Math.Pow(spanMm, 3) / (384.0 * values.ElasticModulusNPerMm2 * inertiaMm4);
            var report = new StructuralCalculationReport
            {
                ProductId = productId,
                Status = "IndicativeOnly",
                ReferenceLoadN = values.ReferenceLoadN,
                ProfileMaterialId = profileMaterialId,
                SpanMm = spanMm,
                ParallelBeamCount = parallelBeamCount,
                ElasticModulusNPerMm2 = values.ElasticModulusNPerMm2,
                StrongAxisInertiaCm4 = inertiaCm4,
                CalculatedDeflectionMm = Math.Round(deflection, 3),
                Formula = "delta = 5 * (F/n) * L^3 / (384 * E * I)"
            };
            report.OpenData.Add("Projectspecifieke ontwerpbelasting en belastingspreiding ontbreken.");
            report.OpenData.Add("Toelaatbare doorbuiging en veiligheidsfactor zijn nog niet als vrijgavecriterium vastgesteld.");
            report.OpenData.Add("HTE2 dynamische capaciteit en momentbelasting zijn nog niet schriftelijk gekwalificeerd voor deze uitvoering.");
            report.OpenData.Add("Belastbaarheid van TIN 100391 en de interface met het gekozen werkbladmateriaal is niet door de leverancier opgegeven.");
            report.OpenData.Add("Bemate zijgroeven van het vaste HTE2-kolomlichaam ontbreken; vier stabilisatiebouten per zijde kunnen daarom nog niet worden gepositioneerd of op lengte berekend.");
            return report;
        }

        private static CalculationValues LoadRequired(string productId)
        {
            var rows = MasterDataRuntimeCatalog.LoadRequired().Records("productRules");
            var rule = rows.SingleOrDefault(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), productId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Constructieberekening", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Vervallen", StringComparison.OrdinalIgnoreCase));
            if (rule == null) throw new InvalidOperationException("Constructieberekeningsregel ontbreekt voor " + productId + ".");
            var result = new CalculationValues();
            foreach (var token in MasterDataRuntimeCatalog.Value(rule, "Waarde").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = token.Split(new[] { '=' }, 2);
                if (pair.Length != 2) continue;
                double value;
                if (!double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value)) continue;
                if (pair[0] == "referenceLoadN") result.ReferenceLoadN = value;
                else if (pair[0] == "elasticModulusNPerMm2") result.ElasticModulusNPerMm2 = value;
                else if (pair[0].EndsWith(".strongAxisInertiaCm4", StringComparison.Ordinal))
                    result.InertiaByMaterialId[pair[0].Substring(0, pair[0].IndexOf('.'))] = value;
            }
            if (result.ReferenceLoadN <= 0 || result.ElasticModulusNPerMm2 <= 0 || result.InertiaByMaterialId.Count == 0)
                throw new InvalidOperationException("Constructieberekeningsregel bevat onvolledige numerieke brondata.");
            return result;
        }

        private sealed class CalculationValues
        {
            public double ReferenceLoadN { get; set; }
            public double ElasticModulusNPerMm2 { get; set; }
            public Dictionary<string, double> InertiaByMaterialId { get; private set; }
            public CalculationValues() { InertiaByMaterialId = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        }
    }
}
