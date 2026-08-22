using System;
using System.IO;
using System.Text;

namespace SWWerkplaats.Configurator.SolidWorks
{
    /// <summary>
    /// Maakt een enkel deelbaar HTML-bestand met zowel de GLB als de 3D-viewer ingebed.
    /// De klant heeft daardoor alleen Edge, Chrome of een andere moderne browser nodig.
    /// </summary>
    internal static class SolidWorksCustomerHtmlExporter
    {
        private const string ViewerAssetName = "model-viewer-4.3.1.min.js";

        public static string Export(string glbPath)
        {
            if (string.IsNullOrWhiteSpace(glbPath)) throw new ArgumentException("GLB-pad ontbreekt.", "glbPath");
            if (!File.Exists(glbPath)) throw new FileNotFoundException("Het GLB-klantmodel ontbreekt.", glbPath);

            var applicationFolder = Path.GetDirectoryName(typeof(SolidWorksCustomerHtmlExporter).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var viewerPath = Path.Combine(applicationFolder, "SolidWorksAssets", ViewerAssetName);
            if (!File.Exists(viewerPath))
                throw new FileNotFoundException("De ingebedde HTML-viewer ontbreekt.", viewerPath);

            var modelName = Path.GetFileNameWithoutExtension(glbPath) ?? "3D klantmodel";
            var htmlPath = Path.ChangeExtension(glbPath, ".html");
            var modelData = Convert.ToBase64String(File.ReadAllBytes(glbPath));
            var viewerScript = File.ReadAllText(viewerPath, Encoding.UTF8);
            if (viewerScript.IndexOf("</script", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidDataException("De HTML-viewer bevat een onveilige afsluitende scripttag.");

            var html = BuildHtml(HtmlEncode(modelName), modelData, viewerScript);
            File.WriteAllText(htmlPath, html, new UTF8Encoding(false));
            return htmlPath;
        }

        private static string BuildHtml(string modelName, string modelData, string viewerScript)
        {
            var sb = new StringBuilder(viewerScript.Length + modelData.Length + 9000);
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"nl\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1,viewport-fit=cover\">");
            sb.AppendLine("<meta name=\"color-scheme\" content=\"light\">");
            sb.Append("<title>").Append(modelName).AppendLine("</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(":root{font-family:Inter,'Segoe UI',Arial,sans-serif;color:#17202a;background:#eef1f4}*{box-sizing:border-box}html,body{width:100%;height:100%;margin:0;overflow:hidden}body{display:grid;grid-template-rows:auto 1fr auto;background:radial-gradient(circle at 50% 15%,#fff 0,#f4f6f8 48%,#e7ebef 100%)}");
            sb.AppendLine("header{min-height:66px;display:flex;align-items:center;gap:18px;padding:12px 18px;border-bottom:1px solid #d9dee4;background:rgba(255,255,255,.92);backdrop-filter:blur(12px);z-index:3}h1{font-size:17px;line-height:1.2;margin:0;font-weight:650;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.sub{font-size:12px;color:#697581;margin-top:4px}.brand{min-width:0;flex:1}.actions{display:flex;gap:8px;flex-wrap:wrap;justify-content:flex-end}button{appearance:none;border:1px solid #c8d0d8;background:#fff;color:#1f2933;border-radius:9px;padding:9px 12px;font:600 12px/1 'Segoe UI',Arial,sans-serif;cursor:pointer;box-shadow:0 1px 2px rgba(0,0,0,.04)}button:hover{border-color:#778899;background:#f7f9fb}button.active{background:#263746;border-color:#263746;color:#fff}");
            sb.AppendLine("main{position:relative;min-height:0}model-viewer{display:block;width:100%;height:100%;background:transparent;--poster-color:transparent;--progress-bar-color:#52697d;--progress-bar-height:3px}.hint{position:absolute;left:50%;bottom:18px;transform:translateX(-50%);pointer-events:none;background:rgba(31,41,51,.82);color:#fff;border-radius:999px;padding:8px 13px;font-size:12px;white-space:nowrap;transition:opacity .5s}.loaded .hint{opacity:.78}.error{display:none;position:absolute;inset:20px;place-items:center;text-align:center;color:#8a1f17;background:#fff;border:1px solid #ebc6c1;border-radius:12px;padding:24px}.hasError .error{display:grid}.hasError model-viewer{visibility:hidden}footer{display:flex;align-items:center;justify-content:space-between;gap:12px;min-height:38px;padding:7px 18px;border-top:1px solid #d9dee4;background:#fff;color:#64717d;font-size:11px}.offline{font-weight:650;color:#3d566b}");
            sb.AppendLine("@media(max-width:680px){header{align-items:flex-start;gap:10px;padding:10px 12px}.sub{display:none}.actions{gap:5px}button{padding:8px 9px}footer{padding:6px 10px}.hint{bottom:12px;font-size:11px}}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<header><div class=\"brand\"><h1>3D klantmodel</h1><div class=\"sub\">" + modelName + "</div></div><div class=\"actions\"><button id=\"reset\" type=\"button\">Beginweergave</button><button id=\"rotate\" type=\"button\">Automatisch draaien</button><button id=\"fullscreen\" type=\"button\">Volledig scherm</button></div></header>");
            sb.AppendLine("<main id=\"stage\">");
            sb.Append("<model-viewer id=\"viewer\" alt=\"").Append(modelName).Append("\" src=\"data:model/gltf-binary;base64,").Append(modelData).AppendLine("\" camera-controls touch-action=\"pan-y\" interaction-prompt=\"auto\" loading=\"eager\" reveal=\"auto\" shadow-intensity=\"1.15\" shadow-softness=\".75\" environment-image=\"neutral\" tone-mapping=\"commerce\" exposure=\"1\" camera-orbit=\"38deg 68deg auto\" min-camera-orbit=\"auto 15deg auto\" max-camera-orbit=\"auto 150deg auto\"></model-viewer>");
            sb.AppendLine("<div class=\"hint\">Sleep om te draaien · scrol om te zoomen</div><div class=\"error\"><div><strong>Het 3D-model kon niet worden geopend.</strong><br><br>Open dit bestand bij voorkeur in de actuele versie van Edge of Chrome.</div></div>");
            sb.AppendLine("</main><footer><span class=\"offline\">Zelfstandig bestand · geen installatie nodig</span><span>Materiaalweergave is indicatief</span></footer>");
            sb.AppendLine("<script type=\"module\">");
            sb.AppendLine(viewerScript);
            sb.AppendLine("const viewer=document.getElementById('viewer'),stage=document.getElementById('stage'),rotate=document.getElementById('rotate');");
            sb.AppendLine("viewer.addEventListener('load',()=>stage.classList.add('loaded'));viewer.addEventListener('error',()=>stage.classList.add('hasError'));");
            sb.AppendLine("document.getElementById('reset').addEventListener('click',()=>{viewer.autoRotate=false;rotate.classList.remove('active');rotate.textContent='Automatisch draaien';viewer.cameraOrbit='38deg 68deg auto';viewer.fieldOfView='30deg';if(viewer.jumpCameraToGoal)viewer.jumpCameraToGoal()});");
            sb.AppendLine("rotate.addEventListener('click',()=>{viewer.autoRotate=!viewer.autoRotate;rotate.classList.toggle('active',viewer.autoRotate);rotate.textContent=viewer.autoRotate?'Draaien stoppen':'Automatisch draaien'});");
            sb.AppendLine("document.getElementById('fullscreen').addEventListener('click',async()=>{try{if(!document.fullscreenElement)await document.documentElement.requestFullscreen();else await document.exitFullscreen()}catch(e){}});");
            sb.AppendLine("</script></body></html>");
            return sb.ToString();
        }

        private static string HtmlEncode(string value)
        {
            return (value ?? "")
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
