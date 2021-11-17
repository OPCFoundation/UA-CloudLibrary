using System;
using System.Windows.Forms;

namespace Sample
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new QueryForm());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "UA Cloud Library Client");
            }
        }
    }
}
