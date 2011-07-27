using System;
using System.Windows.Forms;
using NLog;

namespace SkeetNotifier
{
    static class Program
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, a) => _logger.Error(a.ExceptionObject);
            Application.ThreadException += (s, a) => _logger.Error(a.Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainHandler.Run();
            Application.Run();
        }
    }
}
