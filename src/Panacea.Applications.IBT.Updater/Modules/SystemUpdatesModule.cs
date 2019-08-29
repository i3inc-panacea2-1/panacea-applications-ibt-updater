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
using System.Windows;
using Microsoft.Win32;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("System Updates")]
    internal class SystemUpdatesModule : Interfaces.Module
    {
        internal bool SkipUpdate(string updateId)
        {
            using (var panacea = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            using (var reg = panacea.CreateSubKey(@"SOFTWARE\Panacea\PSSUpdates", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                var ids = reg.GetSubKeyNames();
                if (ids.Contains(updateId))
                {
                    using (var regUpdate = reg.CreateSubKey(updateId, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        var exitCodeString = regUpdate.GetValue("ExitCode").ToString();
                        int exitCodeInt;
                        if (Int32.TryParse(exitCodeString, out exitCodeInt))
                        {
                            if (exitCodeInt == 0)
                            {
                                return true;
                            }
                        }
                        var attemptsString = regUpdate.GetValue("Attempts").ToString();
                        int attemptsInt;
                        if (Int32.TryParse(attemptsString, out attemptsInt))
                        {
                            if (attemptsInt > 3)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;

            }
        }
        internal void AddRegistryUpdate(Update update)
        {
            using (var panacea = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                using (var reg = panacea.CreateSubKey(@"SOFTWARE\Panacea\PSSUpdates", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    using (var regUpdate = reg.CreateSubKey(update.Id, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        regUpdate.SetValue("Name", update.Name);
                        regUpdate.SetValue("ExitCode", update.ExitCode);
                        var attempts = regUpdate.GetValue("Attempts");
                        if (attempts != null)
                        {
                            var attemptsString = attempts.ToString();
                            int attemptsInt;
                            if (Int32.TryParse(attemptsString, out attemptsInt))
                            {
                                regUpdate.SetValue("Attempts", attemptsInt + 1);
                                return;
                            }
                        }
                        regUpdate.SetValue("Attempts", 1);
                        return;
                    }
                }
            }
        }
        internal async Task ReportToServer(string hospitalServer, string updateId, string packageId, int code)
        {
            await ServerRequestHelper.GetObjectFromServerAsync<object>(hospitalServer,
                "update/result/", new
                {
                    timestamp =
                        (long)
                            DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0,
                                DateTimeKind.Utc))
                                .TotalMilliseconds,
                    updateId = updateId,
                    exitCode = code,
                    packageId = packageId
                });
        }
        WebClient _webClient = new WebClient();
        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            if (Debugger.IsAttached)
            {
                if (MessageBox.Show("Install updates?", "Caution", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return true;
                }
            }
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
                   ServerRequestHelper.GetObjectFromServerAsync<List<UpdatePackage>>(keys["hospitalserver"].ToString(), "get_terminal_updates/");
            var updates = new List<Update>();
            var freeze = FreezeHelper.IsFreezeEnabled();
            if (!response.Success) return true;
            if (nameRequiresChange)
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
                if (Debugger.IsAttached)
                {
                    if (MessageBox.Show("Change hostname?", "Question", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Common.SetMachineName(name);
                    }
                }
                else
                {
                    Common.SetMachineName(name);
                    ReportProgress("Rebooting to change hostname");
                    await Task.Delay(3000);
                    ProcessHelper.Reboot();
                    return true;
                }

            }
            foreach (var package in response.Result)
            {
                ReportProgress(package.Name);
                foreach (var update in package.Updates)
                {
                    try
                    {
                        if (SkipUpdate(update.Id))
                        {
                            continue;
                        }
                        if (await LockdownManager.IsFrozen() == true)
                        {
                            ReportProgress("Rebooting to unfreeze");
                            await Task.Delay(1500);
                            LockdownManager.Unfreeze();
                            App.ShutdownSafe(false);
                            return true;
                        }
                        updates.Add(update);
                        var tempPath = System.IO.Path.GetTempPath(); //C:\\Users\\<UserName>\\AppData\\Local\\Temp
                        var updateDir = new DirectoryInfo(tempPath + "PanaceaUpdates\\" + update.Id);
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
                        AddRegistryUpdate(update);
                        await ReportToServer(keys["hospitalserver"].ToString(), update.Id, package.Id, code);
                        try
                        {
                            File.Delete(updateDir + "\\" + Path.GetFileName(update.PatchScript));
                            foreach (var file in update.RequiredFiles)
                            {

                                File.Delete(updateDir + "\\" + Path.GetFileName(file));
                            }
                        }
                        catch { }

                        if (update.RequiresReboot != "yes") continue;
                        ProcessHelper.Reboot();
                        return false;
                    }
                    catch
                    {
                        update.ExitCode = 9999;
                        AddRegistryUpdate(update);
                        await ReportToServer(keys["hospitalserver"].ToString(), update.Id, package.Id, 9999);
                    }
                }
            }
            if (updates.All(u => u.ExitCode != 0))
            {
                foreach (var update in updates)
                {
                    AddRegistryUpdate(update);
                    await ReportToServer(keys["hospitalserver"].ToString(), update.Id, update.PackageId, update.ExitCode);
                }
            }
            if (updates.Any(u => u.RequiresReboot == "batch" && u.ExitCode == 0))
            {
                ReportProgress("Restarting...");
                await Task.Delay(1500);
                ProcessHelper.Reboot();
                return false;
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
