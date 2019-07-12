using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using PanaceaLib;

namespace IBT.Updater
{
    public class Program
    {
	    private static App _app;
        [STAThread]
        public static void Main()
        {
            try
            {
                App.KillProcesses("PanaceaLauncher", "Panacea", "SystemSetup", "PanaceaRegistrator", "ServerCommunicator",
                    "WebBrowser.SubProcess");

            }
            catch (Exception)
            {
                //ignore
            }
            _app = new App();
#if DEBUG
            _app.Run();
#else
            
			try
            {
				Thread.Sleep(5000);
                _app.Run();
            }
            catch (Exception ex)
            {
                
            }
#endif

        }


    }
}