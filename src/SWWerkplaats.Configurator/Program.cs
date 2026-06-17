using System;
using System.Net.Sockets;
using System.Windows.Forms;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Portal;
using SWWerkplaats.Configurator.UI;
using WinFormsApplication = System.Windows.Forms.Application;

namespace SWWerkplaats.Configurator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            PortalWebServer portal = null;
            var portalOptions = PortalRuntimeOptions.Load(args);
            WinFormsApplication.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
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
                try
                {
                    if (IsLocalPortOpen(portalOptions.Port))
                    {
                        if (portalOptions.PortalOnly) return;
                    }
                    else
                    {
                        portal = new PortalWebServer(portalOptions.RootFolder, portalOptions.Prefix);
                        portal.Start();
                        if (portalOptions.PortalOnly)
                        {
                            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                            return;
                        }
                    }
                }
                catch (Exception portalEx)
                {
                    portal = null;
                    if (portalOptions.PortalOnly || !IsLocalPortOpen(portalOptions.Port))
                    {
                        MessageBox.Show("Webportal kon niet starten op " + portalOptions.Prefix + "." + Environment.NewLine + portalEx.Message, "SW Werkplaats Portal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                WinFormsApplication.EnableVisualStyles();
                WinFormsApplication.SetCompatibleTextRenderingDefault(false);
                WinFormsApplication.Run(new MainForm());
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

        private static bool IsLocalPortOpen(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect("127.0.0.1", port, null, null);
                    var connected = result.AsyncWaitHandle.WaitOne(250);
                    if (!connected) return false;
                    client.EndConnect(result);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
