using System;
using System.IO;
using System.Windows.Forms;
using SWWerkplaats.Configurator.Portal;
using SWWerkplaats.Configurator.UI;

namespace SWWerkplaats.Configurator
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            PortalWebServer portal = null;
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.ToString(), "SWWerkplaats.Configurator fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(ex == null ? e.ExceptionObject.ToString() : ex.ToString(), "SWWerkplaats.Configurator fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                var portalRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PortalData");
                try
                {
                    portal = new PortalWebServer(portalRoot, "http://localhost:8088/");
                    portal.Start();
                }
                catch (Exception portalEx)
                {
                    portal = null;
                    MessageBox.Show("Webportal kon niet starten op http://localhost:8088/." + Environment.NewLine + portalEx.Message, "SW Werkplaats Portal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "SWWerkplaats.Configurator fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (portal != null) portal.Dispose();
            }
        }
    }
}
