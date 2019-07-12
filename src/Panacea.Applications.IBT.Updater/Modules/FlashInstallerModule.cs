using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using PanaceaLib;
using System.IO;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Flash")]
    internal class FlashInstallerModule : Interfaces.Module
    {
        const string FlashFileName = "install_flash_player_27_plugin.msi";
        const string RegistryPath = @"SOFTWARE\MozillaPlugins\@adobe.com/FlashPlayer";
        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            return true;
            if (!File.Exists(Common.Path() + FlashFileName)) return true;
            using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
            {
                if (key != null)
                {
                    if (key.GetValue("Version", "").ToString().StartsWith("27"))
                    {
                        return true;
                    }
                }
                if (await LockdownManager.IsFrozen() == true)
                {
                    LockdownManager.Unfreeze();
                    ReportProgress("Rebooting to unfreeze...");
                    await Task.Delay(2000);
                    App.ShutdownSafe();
                    return false;
                }
                ReportProgress("Installing Flash...");
                await InstallFlash();
            }
            return true;
        }

        Task InstallFlash()
        {
            return Task.Run(() =>
            {
                try
                {
                    var p = Process.Start(Common.Path() + FlashFileName, "/q");
                    p.WaitForExit();
                }
                catch
                {
                }
            });
        }
    }
}
