using IBT.Updater.Helpers;
using IBT.Updater.Models;
using PanaceaLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("System Updates")]
    internal class SystemUpdatesModule : Interfaces.Module
    {
        WebClient _webClient = new WebClient();

        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            ReportProgress("Installing Updates");
            await Task.Delay(1000);
            var dir = new DirectoryInfo(Common.Path() + "Updates");
            if (!dir.Exists) dir.Create();

            var name = Environment.MachineName;
            try
            {
                var resp =
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<TerminalInfoResponse>(keys["hospitalserver"].ToString(),
                            "get_terminal_info/");
                name = resp.Result.TerminalName;
            }
            catch
            {
                //maybe get_terminal_info not implemented - compatibility
            }

            var nameRequiresChange = name != Environment.MachineName;
            ////throw new Exception("Sfgfg");
            var response =
               await
                   ServerRequestHelper.GetObjectFromServerAsync<List<UpdatePackage>>(keys["hospitalserver"].ToString(), "get_updates/");
            var updates = new List<Update>();
            var freeze = FreezeHelper.IsFreezeEnabled();
            if (!response.Success) return true;
            if (response.Result.Count > 0 || nameRequiresChange)
            {
                if (await LockdownManager.IsFrozen() == true)
                {
                    ReportProgress("Rebooting to unfreeze");
                    await Task.Delay(1500);
                    LockdownManager.Unfreeze();
                    App.ShutdownSafe(false);
                    return true;
                }
            }
            response.Result.ForEach(p => p.Updates.ForEach(u => u.PackageId = p.Id));

            if (nameRequiresChange)
            {
                Common.SetMachineName(name);
                ReportProgress("Rebooting to change hostname");
                await Task.Delay(3000);
                ProcessHelper.Reboot();
                return true;
            }
            foreach (var package in response.Result)
            {
                ReportProgress(package.Name);
                foreach (var update in package.Updates)
                {
                    updates.Add(update);
                    var updateDir = new DirectoryInfo(Common.Path() + "Updates\\" + update.Id);
                    if (!updateDir.Exists) updateDir.Create();

                    await _webClient.DownloadFileTaskAsync(
                        new Uri(keys["hospitalserver"] + "/" + update.PatchScript),
                        updateDir + "\\" + Path.GetFileName(update.PatchScript));

                    foreach (var file in update.RequiredFiles)
                    {
                        await _webClient.DownloadFileTaskAsync(
                            new Uri(keys["hospitalserver"] + "/" + file),
                            updateDir + "\\" + Path.GetFileName(file));
                    }
                    ReportProgress("Installing...");
                    var code = await Task.Run(() =>
                    {
                        var info = new ProcessStartInfo()
                        {
                            WorkingDirectory = updateDir.ToString(),
                            FileName = updateDir + "\\" + Path.GetFileName(update.PatchScript),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            Verb = "runas"
                        };
                        var p = new Process { StartInfo = info };
                        try
                        {
                            p.Start();
                            p.WaitForExit();
                            return p.ExitCode;
                        }
                        catch
                        {
                            return 9001;
                        }
                    });
                    //OnProgressFiles(code == 0 ? "Installation successful!" : "Installation failed");
                    await Task.Delay(1000);
                    update.Installed = code == 0;
                    update.ExitCode = code;

                    if (code != 0) continue;
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<object>(keys["hospitalserver"].ToString(),
                            "update/result/", new
                            {
                                timestamp =
                                    (long)
                                        DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc))
                                            .TotalMilliseconds,
                                updateId = update.Id,
                                exitCode = code,
                                packageId = package.Id
                            });
                    if (update.RequiresReboot != "yes") continue;
                    ProcessHelper.Reboot();
                    return false;
                }
            }
            if (updates.Any(u => u.RequiresReboot == "batch" && u.ExitCode == 0))
            {
                ReportProgress("Restarting...");
                await Task.Delay(1500);
                ProcessHelper.Reboot();
                return false;
            }
            if (updates.All(u => u.ExitCode != 0))
            {
                foreach (var update in updates)
                {
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<object>(keys["hospitalserver"].ToString(),
                            "update/result/", new
                            {
                                timestamp =
                                    (long)
                                        DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc))
                                            .TotalMilliseconds,
                                updateId = update.Id,
                                exitCode = update.ExitCode,
                                packageId = update.PackageId
                            });
                }
            }
                
            if (updates.Any(u => u.ExitCode != 0) && updates.Any(u => u.ExitCode == 0))
            {
                ReportProgress("Rebooting to retry updates that failed...");
                await Task.Delay(1500);
                ProcessHelper.Reboot();
                return false;
            }


            if (await LockdownManager.IsFrozen() == false && freeze)
            {

                if (updates.All(u => u.ExitCode == 0) || updates.All(u => u.ExitCode != 0))
                {
                    ReportProgress("Rebooting to freeze");
                    await Task.Delay(1500);
                    LockdownManager.Freeze();
                }
                App.ShutdownSafe(false);
                return false;
            }
            return true;
        }
    }
}
