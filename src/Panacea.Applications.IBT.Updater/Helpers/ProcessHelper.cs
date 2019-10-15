using PanaceaLib;
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
            var reg = Common.Path("..", "Applications", "Registrator", "PanaceaRegistrator.exe");
            if (Debugger.IsAttached)
            {
                if (MessageBox.Show("Start registrator?", "Hey developer", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;

            }
            if (File.Exists(reg))
                Process.Start(reg, server);
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
