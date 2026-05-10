using System;
using System.Windows.Forms;
using SWWerkplaats.Configurator.UI;

namespace SWWerkplaats.Configurator
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
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
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "SWWerkplaats.Configurator fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
