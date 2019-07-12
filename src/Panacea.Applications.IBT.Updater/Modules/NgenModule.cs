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
    [Interfaces.Action("Optimizing Panacea")]
    class NgenModule : Interfaces.Module
    {
        const string path = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ngen.exe";

        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            return true;
            if (File.Exists(path))
            {
                var currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var baseDir = new DirectoryInfo(currentLocation).Parent.FullName;

                var location = Path.Combine(baseDir, "Updater", "Support", "Launcher", "PanaceaLauncher.exe");
                ReportProgress($"Installing {Path.GetFileName(location)}");
                await Install(location);

                location = Path.Combine(baseDir, "Panacea.exe");
                ReportProgress($"Installing {Path.GetFileName(location)}");
                await Install(location);

                location = Path.Combine(baseDir, "Lib", "VlcMediaPlayer.exe");
                ReportProgress($"Installing {Path.GetFileName(location)}");
                await Install(location);

                location = Path.Combine(baseDir, "ibt-plugins", "WebBrowserGeckoEngine", "WebBrowserGeckoEngineSubProcess.exe");
                ReportProgress($"Installing {Path.GetFileName(location)}");
                await Install(location);

                location = Path.Combine(baseDir, "ibt-plugins", "WebBrowserChromiumEngine", "x86", "CefSharp.BrowserSubprocess.exe");
                ReportProgress($"Installing {Path.GetFileName(location)}");
                await Install(location);

            }
            return true;
        }

        async Task Install(string name)
        {
            return;
            try
            {
                if (File.Exists(name))
                {
                    var info = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = "install /silent /queue /nologo \"" + name + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        
                    };
                    using (var process = new Process() { StartInfo = info, EnableRaisingEvents = true })
                    {
                        process.Start();
                        await Task.Run(() => process.WaitForExit());
                        //var output = process.StandardOutput.ReadToEnd();
                        //Console.WriteLine(output);
                    }
                    info = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = "update /silent /queue /nologo \"" + name + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var process = new Process() { StartInfo = info, EnableRaisingEvents = true })
                    {
                        process.Start();
                        await Task.Run(() => process.WaitForExit());
                        //var output = process.StandardOutput.ReadToEnd();
                        //Console.WriteLine(output);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
