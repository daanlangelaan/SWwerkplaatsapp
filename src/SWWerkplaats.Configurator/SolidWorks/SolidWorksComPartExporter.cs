using System;
using System.IO;
using System.Runtime.InteropServices;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksComPartExporter
    {
        private const int SaveAsCurrentVersion = 0;
        private const int SaveAsOptionsSilent = 1;

        public void ExportParts(WorkbenchModel model, string outputFolder)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("Outputmap ontbreekt.");

            var solidWorks = GetOrCreateSolidWorks();
            solidWorks.Visible = true;

            var cadFolder = Path.Combine(outputFolder, "SolidWorks");
            Directory.CreateDirectory(cadFolder);

            foreach (var profile in model.Profiles)
            {
                CreateBoxPart(
                    solidWorks,
                    Path.Combine(cadFolder, SafeName(profile.Name) + "_" + profile.LengthMm.ToString("0") + "mm.SLDPRT"),
                    profile.Material.WidthMm,
                    profile.Material.HeightMm,
                    profile.LengthMm);
            }

            foreach (var sheet in model.Sheets)
            {
                CreateBoxPart(
                    solidWorks,
                    Path.Combine(cadFolder, SafeName(sheet.Name) + "_" + sheet.LengthMm.ToString("0") + "x" + sheet.WidthMm.ToString("0") + ".SLDPRT"),
                    sheet.LengthMm,
                    sheet.WidthMm,
                    sheet.Material.ThicknessMm);
            }
        }

        private static dynamic GetOrCreateSolidWorks()
        {
            try
            {
                return Marshal.GetActiveObject("SldWorks.Application");
            }
            catch
            {
                throw new InvalidOperationException(
                    "Geen actieve SolidWorks sessie gevonden.\r\n\r\n" +
                    "Start eerst SOLIDWORKS Design via de 3DEXPERIENCE snelkoppeling of het platform. " +
                    "Laat SolidWorks volledig openen en klik daarna opnieuw op 'Genereer werktafel-output'.");
            }
        }

        private static void CreateBoxPart(dynamic solidWorks, string filePath, double xMm, double yMm, double zMm)
        {
            dynamic model = solidWorks.NewPart();
            if (model == null)
            {
                throw new InvalidOperationException("SolidWorks kon geen nieuw part-document maken. Controleer de default part template in SolidWorks.");
            }

            var x = MmToM(xMm);
            var y = MmToM(yMm);
            var z = MmToM(zMm);

            bool selected = model.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            if (!selected)
            {
                selected = model.Extension.SelectByID2("Vlak Voor", "PLANE", 0, 0, 0, false, 0, null, 0);
            }

            if (!selected)
            {
                throw new InvalidOperationException("Kon het Front Plane/Vlak Voor niet selecteren in het nieuwe part.");
            }

            model.SketchManager.InsertSketch(true);
            model.SketchManager.CreateCenterRectangle(0, 0, 0, x / 2.0, y / 2.0, 0);
            model.SketchManager.InsertSketch(true);

            // Blind extrusion in meters. This creates simple rectangular placeholder geometry.
            model.FeatureManager.FeatureExtrusion2(
                true, false, false,
                0, 0,
                z, 0,
                false, false, false, false,
                0, 0,
                false, false, false, false,
                true, true, true,
                0, 0,
                false);

            int errors = 0;
            int warnings = 0;
            model.Extension.SaveAs(filePath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            solidWorks.CloseDoc(model.GetTitle());

            if (errors != 0)
            {
                throw new InvalidOperationException("SolidWorks SaveAs gaf foutcode " + errors + " voor " + filePath);
            }
        }

        private static double MmToM(double value)
        {
            return value / 1000.0;
        }

        private static string SafeName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace("/", "-");
        }
    }
}
