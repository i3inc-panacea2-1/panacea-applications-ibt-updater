using IBT.Updater.Helpers;
using IBT.Updater.Interfaces;
using Microsoft.Win32.TaskScheduler;
using PanaceaLib;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Third party updates")]
    internal class DisableThirdPartyUpdatesModule : Module
    {
        String[] BadNames = new[] { "Adobe", "Skype", "OneDrive" };

        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            var unfreezeRequired = FreezeHelper.IsFreezeEnabled() && (await LockdownManager.IsFrozen()) == true;
            var filename = @"C:\Windows\SysWOW64\Macromed\Flash\FlashUtil32_21_0_0_242_Plugin.exe";
            if (File.Exists(filename))
            {
                try
                {
                    File.Delete(filename);
                }
                catch { }
            }
            ReportProgress("Removing scheduled tasks...");
            try
            {
                await System.Threading.Tasks.Task.Delay(1000);
                var res = await System.Threading.Tasks.Task.Run(() =>
                {
                    using (TaskService ts = new TaskService())
                    {
                        foreach (var task in ts.AllTasks)
                        {
                            if (BadNames.Any(str => task.Name.Contains(str)))
                            {
                                if (unfreezeRequired)
                                {
                                    LockdownManager.Unfreeze();
                                    return false;
                                }
                                task.Stop();
                                ReportProgress($"Removing scheduled task {task.Name}");
                                ts.RootFolder.DeleteTask(task.Name);
                            }
                        }
                    }
                    return true;
                });
                if (!res) return false;

                ReportProgress("Removing services...");
                await System.Threading.Tasks.Task.Delay(1000);
                res = await System.Threading.Tasks.Task.Run(() =>
                {
                    var services = ServiceController.GetServices();
                    int i = 0;
                    foreach (var service in services)
                    {
                        Debug.WriteLine(i);
                        i++;
                        if (BadNames.Any(str => service.DisplayName.Contains(str)))
                        {
                            if (unfreezeRequired)
                            {
                                LockdownManager.Unfreeze();
                                return false;
                            }
                            var ServiceInstallerObj = new ServiceInstaller();
                            var Context = new InstallContext(Common.Path(), null);

                            ServiceInstallerObj.Context = Context;
                            ServiceInstallerObj.ServiceName = service.ServiceName;
                            ReportProgress($"Removing service {service.DisplayName}");
                            ServiceInstallerObj.Uninstall(null);
                        }
                    }
                    return true;
                });
                return res;
            }
            catch
            {
            }
            return true;
        }
    }
}
