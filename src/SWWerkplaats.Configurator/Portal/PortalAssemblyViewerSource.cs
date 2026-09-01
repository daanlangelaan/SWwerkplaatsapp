using System;
using System.IO;
using System.Text;

namespace SWWerkplaats.Configurator.Portal
{
    /// <summary>
    /// Levert de renderfuncties die de portal zelf gebruikt ook aan zelfstandige
    /// klantbestanden. Daardoor ontstaat geen tweede geometrie- of tekenengine.
    /// </summary>
    internal static class PortalAssemblyViewerSource
    {
        private static readonly Lazy<string> Renderer = new Lazy<string>(BuildRendererJavaScript);
        private static readonly Lazy<string> ThreeModule = new Lazy<string>(BuildThreeModuleDataUrl);
        private static readonly Lazy<string> OrbitControlsModule = new Lazy<string>(BuildOrbitControlsModuleDataUrl);

        public static string RendererJavaScript
        {
            get { return Renderer.Value; }
        }

        public static string ThreeModuleDataUrl
        {
            get { return ThreeModule.Value; }
        }

        public static string OrbitControlsModuleDataUrl
        {
            get { return OrbitControlsModule.Value; }
        }

        private static string BuildRendererJavaScript()
        {
            var page = PortalHtml.Page();
            var sb = new StringBuilder();
            AppendSlice(sb, page, "    function presentationObject(value,pascal,camel)", "    function applyPresentationContract(value)");
            AppendSlice(sb, page, "    function instructionAxis(p)", "    function instructionSize(p,axis)");
            AppendSlice(sb, page, "    function instructionCoreLocal(profile,core)", "    function instructionOverviewJoints(parts,step)");
            AppendSlice(sb, page, "    function isLayeredPlywoodPart(p)", "    function assemblyRenderKey(parts)");
            AppendSlice(sb, page, "    function buildThreeParts(THREE,group,parts)", "    function fitThreeCamera(THREE,camera,controls)");
            return sb.ToString();
        }

        private static void AppendSlice(StringBuilder destination, string source, string startMarker, string endMarker)
        {
            var start = source.IndexOf(startMarker, StringComparison.Ordinal);
            var end = source.IndexOf(endMarker, start < 0 ? 0 : start, StringComparison.Ordinal);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("De gedeelde portalrenderer mist bronsegment " + startMarker.Trim() + ".");

            destination.AppendLine(source.Substring(start, end - start).Trim());
        }

        private static string BuildThreeModuleDataUrl()
        {
            var applicationFolder = Path.GetDirectoryName(typeof(PortalAssemblyViewerSource).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(applicationFolder, "PortalAssets", "vendor", "three", "three.module.js");
            if (!File.Exists(path))
                throw new FileNotFoundException("De gedeelde Three.js-viewerasset ontbreekt.", path);

            return "data:text/javascript;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }

        private static string BuildOrbitControlsModuleDataUrl()
        {
            var applicationFolder = Path.GetDirectoryName(typeof(PortalAssemblyViewerSource).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(applicationFolder, "PortalAssets", "vendor", "three", "OrbitControls.js");
            if (!File.Exists(path))
                throw new FileNotFoundException("De gedeelde OrbitControls-viewerasset ontbreekt.", path);

            var source = File.ReadAllText(path)
                .Replace("from './three.module.js'", "from '" + ThreeModuleDataUrl + "'");
            return "data:text/javascript;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
        }
    }
}
