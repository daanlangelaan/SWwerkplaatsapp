using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileConnectionHardwareSynchronizationService
    {
        public void Assign(WorkbenchModel model, int orderQuantity)
        {
            if (model == null) return;
            var count = model.AssemblyConnections.Count(c => c.JointType == AssemblyJointType.StandardConnector) * Math.Max(1, orderQuantity);
            foreach (var item in model.Hardware.Where(IsStandardConnectorOrBolt)) item.Quantity = count;
        }

        private static bool IsStandardConnectorOrBolt(HardwareItem item)
        {
            var name = item.Name ?? string.Empty;
            var article = item.ArticleNumber ?? string.Empty;
            return name.IndexOf("standaardverbinder", StringComparison.OrdinalIgnoreCase) >= 0
                || article.IndexOf("100342", StringComparison.OrdinalIgnoreCase) >= 0
                || article.IndexOf("standard_connector", StringComparison.OrdinalIgnoreCase) >= 0
                || article.IndexOf("100673", StringComparison.OrdinalIgnoreCase) >= 0
                || article.IndexOf("button_head_iso7380_m8x25", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
