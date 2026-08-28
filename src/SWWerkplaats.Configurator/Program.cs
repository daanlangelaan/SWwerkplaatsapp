using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.SolidWorks;
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
            if (TryCloseGeneratedSolidWorksDocuments(args)) return;
            if (TryRunSolidWorksWorker(args)) return;
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
                        portal = new PortalWebServer(portalOptions);
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

        private static bool TryCloseGeneratedSolidWorksDocuments(string[] args)
        {
            if (args == null || args.Length < 2 || !string.Equals(args[0], "--solidworks-close-documents-under", StringComparison.OrdinalIgnoreCase)) return false;
            try
            {
                var closed = new SolidWorksComPartExporter().CloseGeneratedDocumentsUnder(args[1]);
                Console.WriteLine("Gesloten SolidWorks-documenten: " + closed);
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.ExitCode = 2;
            }
            return true;
        }

        private static bool TryRunSolidWorksWorker(string[] args)
        {
            if (args == null || args.Length < 3 || !string.Equals(args[0], "--solidworks-worker", StringComparison.OrdinalIgnoreCase)) return false;
            var resultPath = args[2];
            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var request = serializer.Deserialize<PortalQuoteRequest>(File.ReadAllText(args[1]));
                var factory = new PortalConfigurationFactory();
                var model = new ProductModelBuildService().Build(factory, request);
                var assemblyPath = new SolidWorksComPartExporter().ExportPartsAndAssembly(model, Path.GetDirectoryName(resultPath), request);
                File.WriteAllText(resultPath, serializer.Serialize(new SolidWorksWorkerResult { ContractVersion = 1, Ok = true, AssemblyPath = assemblyPath }));
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(resultPath, new JavaScriptSerializer().Serialize(new SolidWorksWorkerResult { ContractVersion = 1, Ok = false, Error = ex.ToString() })); } catch { }
                Environment.ExitCode = 2;
            }
            return true;
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
