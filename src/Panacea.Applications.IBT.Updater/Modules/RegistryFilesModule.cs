using Microsoft.Win32;
using PanaceaLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Module = IBT.Updater.Interfaces.Module;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Registry Settings")]
    internal class RegistryFilesModule: Module
    {
        internal override Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            ReportProgress("Applying registry files...");
            return Task.Run(() =>
            {
                Environment.SetEnvironmentVariable("MOZ_DISABLE_GMP_SANDBOX", null, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("MOZ_DISABLE_OOP_PLUGINS", null, EnvironmentVariableTarget.Machine);

                //RegisterDefaultWebBrowser();
                var regPath =
                    Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Registry\\");
                if (!Directory.Exists(regPath)) return true;
                foreach (
                    var process in
                        Directory.GetFiles(regPath)
                            .Select(s => Process.Start("regedit.exe", "/s " + s))
                            .Where(process => process != null))
                {
                    process.WaitForExit();
                }
                return true;
            });
        }

        void RegisterDefaultWebBrowser()
        {
            var panaceaPath = Path.GetFullPath(Common.Path() + "../");
            using (var panacea = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var reg = panacea.CreateSubKey(@"SOFTWARE\Panacea\Capabilities", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue("ApplicationDescription", "Panacea");
                    reg.SetValue("ApplicationIcon", panaceaPath + "Panacea.exe,0");
                    reg.SetValue("ApplicationName", "Panacea");

                }
                using (var reg = panacea.CreateSubKey(@"SOFTWARE\Panacea\Capabilities\FileAssociations", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue(".htm", "Panacea");
                    reg.SetValue(".html", "Panacea");
                    reg.SetValue(".shtml", "Panacea");
                    reg.SetValue(".xht", "Panacea");
                    reg.SetValue(".xhtml", "Panacea");

                }
                using (var reg = panacea.CreateSubKey(@"SOFTWARE\Panacea\Capabilities\URLAssociations", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue("ftp", "Panacea");
                    reg.SetValue("http", "Panacea");
                    reg.SetValue("https", "Panacea");
                }


                using (var reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue("Panacea", "Software\\Panacea\\Capabilities");
                }

                using (var reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Classes\PanaceaURL", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue("", "Panacea Document");
                    reg.SetValue("FriendlyTypeName", "Panacea Document");
                }

                using (var reg = Registry.LocalMachine.CreateSubKey(@"Software\Classes\PanaceaURL\shell\open\command", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    reg.SetValue("", @""""+ panaceaPath+ @"Panacea.exe"" ""%1""");
                }
            }
            
        }
    }
}
