using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Firewall")]
    class FirewallModule : Interfaces.Module
    {
        //netsh advfirewall firewall add rule name=Panacea program=D:\Repositories\PanaceaV2Client\bin\Panacea.exe action=allow enable=yes profile=any protocol=any dir=
        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            ReportProgress("Applying configuration...");
            var path = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            path = path.Parent;
            AddFirewallException("Panacea", path.FullName + "\\Panacea.exe");
            AddFirewallException("PanaceaServerCommunicator", path.FullName + "\\ServerCommunicator.exe");
            AddFirewallException("PanaceaVLC", path.FullName + "\\Lib\\VlcMediaPlayer.exe");
            AddFirewallException("GeckoSubProcess", path.FullName + "\\ibt-plugins\\WebBrowserGeckoEngine\\WebBrowserGeckoEngineSubProcess.exe");
            return true;
        }

        void AddFirewallException(string name, string path)
        {
            foreach (var dir in new[] { "in", "out" })
            {
                var info = new ProcessStartInfo()
                {
                    Arguments = $"advfirewall firewall add rule name={name} program={path} action=allow enable=yes profile=any protocol=any dir={dir}",
                    FileName = "netsh",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = new Process()
                {
                    StartInfo = info
                };
                p.Start();
                p.WaitForExit();
            }
        }
    }
}
