using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IBT.Updater.Helpers;
using Module = IBT.Updater.Interfaces.Module;
using PanaceaLib;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Framework")]
    internal class DotNetFrameworkUpdateModule:Module
    {
        internal override async Task<bool> OnPreUpdate(SafeDictionary<string, object> keys)
        {
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NDP46-KB3045557-x86-x64-AllOS-ENU.exe"))) return true;
            ReportProgress("Checking framework version...");
            if (!DotNetVersionHelper.RequiresUpdate()) return true;
            if (await LockdownManager.IsFrozen() == true)
            {
                LockdownManager.Unfreeze();
                App.ShutdownSafe(false);
                return false;
            }
            ReportProgress("Installing Framework... (this might take a while)");
            return await Task.Run(() =>
            {
                var p = Process.Start(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NDP46-KB3045557-x86-x64-AllOS-ENU.exe"), "/q");
                p?.WaitForExit();
                return true;
            });
        }
    }
}
