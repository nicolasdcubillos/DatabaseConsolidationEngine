using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ConsolidationDashboard
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Global exception handlers
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.Run(new ConsolidationDashboard());
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                LogUnhandledException(e.Exception, "UI Thread Exception");
                ShowExceptionDialog(e.Exception, "Ocurrió una excepción en el hilo de la interfaz");
            }
            catch
            {
                // If logging/display fails, just swallow to avoid crash loop
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                LogUnhandledException(ex, "Unhandled Domain Exception");
                ShowExceptionDialog(ex, "Ocurrió una excepción no controlada en la aplicación");
            }
            catch
            {
                // ignore
            }
        }

        private static void ShowExceptionDialog(Exception ex, string title)
        {
            if (ex == null)
            {
                MessageBox.Show("Se produjo una excepción no controlada (sin información).", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string text = string.Format("{0}\n\nTipo: {1}\nMensaje: {2}\n\nStackTrace:\n{3}", title, ex.GetType().FullName, ex.Message, ex.StackTrace);
            // Show a scrollable dialog: use a custom form if stack trace long. For simplicity, use MessageBox but include details.
            MessageBox.Show(text, "Error crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void LogUnhandledException(Exception ex, string source)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                using (var sw = new StreamWriter(logPath, true))
                {
                    sw.WriteLine("====================================================");
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("Source: " + source);
                    if (ex != null)
                    {
                        sw.WriteLine(ex.ToString());
                    }
                    else
                    {
                        sw.WriteLine("Exception object was null");
                    }
                    sw.WriteLine();
                }
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
