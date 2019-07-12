using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IBT.Updater.Helpers
{
    internal static class ProcessHelper
    {
        internal static void StartRegistrator(string server)
        {
            App.ShutdownSafe();
            var localpath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            if (File.Exists(localpath + "\\PanaceaRegistrator.exe"))
                Process.Start(localpath + "\\PanaceaRegistrator.exe", server);
        }

        

        internal static void Reboot()
        {
            if (Debugger.IsAttached)
            {
                if (MessageBox.Show("About to reboot", "", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No) return;
            }
            Process.Start(@"shutdown.exe", "-f -r -t 0");
            App.ShutdownSafe();
        }
    }

}
