using IBT.Updater.Helpers;
using IBT.Updater.Interfaces;
using Microsoft.Win32;
using PanaceaLib;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TerminalIdentification;
using Windows.Writefilters;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Management Settings")]
    internal class ManagementServerModule : Interfaces.Module
    {
        string _server, _hospitalServer, _crutch;

        [ExecutionPriority(999)]
        internal override async Task<bool> OnPreUpdate(SafeDictionary<string, object> keys)
        {
            ReportProgress("Identifying...");

            int tries = 0;

            while (await TerminalIdentificationManager.GetIdentificationInfoAsync() == null && tries < 10)
            {
                tries++;
                await TerminalIdentificationManager.IdentifyAsync();
                if (await TerminalIdentificationManager.GetIdentificationInfoAsync() == null)
                    await Task.Delay(30000);
            }

            if (await TerminalIdentificationManager.GetIdentificationInfoAsync() == null)
            {
                App.ShutdownSafe();
                return false;
            }


            keys["server"] = _server = (await PanaceaRegistry.GetServerInformation(false)).ManagementServer;
            if (string.IsNullOrEmpty(_server))
            {
                ReportProgress("Please add a terminal server in registry...");
                await Task.Delay(4000);
                App.ShutdownSafe();
                return false;
            }
            //we can allow a failure to access TS if HS server is stored locally
            BrowserSettingsHelper.UpdateSettings(new[] { "WebBrowser.SubProcess.exe", "Panacea.exe" });
            ServerResponse<GetHospitalServersResponse> getHospitalServerResponse = null;
            try
            {
                getHospitalServerResponse =
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<GetHospitalServersResponse>(_server,
                            "get_hospital_servers/");
            }
            catch
            {
                getHospitalServerResponse =
                    (await PanaceaRegistry.GetServerInformation(false)).ManagementServerResponse;
            }
            if (getHospitalServerResponse == null)
            {
                throw new Exception("Terminal Server unreachable and no information stored in registry...");
            }
            if (!getHospitalServerResponse.Success)
            {
                if (getHospitalServerResponse.Error != "self destruct")
                {
                    ReportProgress(getHospitalServerResponse.Error);
                    await Task.Delay(4000);
                    ProcessHelper.StartRegistrator(_server);
                    App.ShutdownSafe();
                    return false;
                }
                var path = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\");
                foreach (var directory in Directory.GetDirectories(path))
                {
                    try
                    {
                        if (!directory.EndsWith("\\Updater"))
                            Directory.Delete(directory, true);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                foreach (var directory in Directory.GetFiles(path))
                {
                    try
                    {
                        File.Delete(directory);
                    }
                    catch
                    {
                        // ignored
                    }
                }
               

                App.ShutdownSafe();
                return false;
            }

            if (!await UpdateRegistry(getHospitalServerResponse)) return false;

            keys["hospitalserver"] = _hospitalServer = getHospitalServerResponse.Result.HospitalServers[0];
            if (string.IsNullOrEmpty(getHospitalServerResponse.Result.Crutch)) return true;

            _crutch = getHospitalServerResponse.Result.Crutch;

            ReportProgress("Waiting for traffic controller... Please wait...");

            var req =
                BuildHttpRequest(
                    new Uri(new Uri(_crutch), "api/" + Common.GetMacAddress() + "/requestAccessLock/")
                        .ToString());
            try
            {
                var resp = await req.GetHttpResponseAsync(120000);
                if (resp.StatusCode != HttpStatusCode.Conflict && resp.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException("Not conflict");
                }
            }
            catch (WebException)
            {
                ReportProgress("Traffic controller unreachable... Please wait...");
                await Task.Delay(new Random().Next(10000, 60000));
                App.Restart();
                return false;
            }
            return true;
            
        }

        private async Task<bool> UpdateRegistry(ServerResponse<GetHospitalServersResponse> getHospitalServerResponse)
        {
            var freeze = FreezeHelper.IsFreezeEnabled();
            using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
            using (var panacea = key.CreateSubKey("Panacea"))
            {
                if (panacea.GetValue("HospitalServer", "").ToString() ==
                    getHospitalServerResponse.Result.HospitalServers[0] &&
                    panacea.GetValue("TerminalServerResponse", "").ToString() ==
                    JsonSerializer.SerializeToString(getHospitalServerResponse)) return true;
                if (await LockdownManager.IsFrozen() == true)
                {
                    ReportProgress("Rebooting to unfreeze (apply new settings)");
                    LockdownManager.Unfreeze();
                    App.ShutdownSafe(false);
                    return false;
                }
                panacea.SetValue("TerminalServer", _server);
                panacea.SetValue("HospitalServer", getHospitalServerResponse.Result.HospitalServers[0]);
                panacea.SetValue("TerminalServerResponse",
                    JsonSerializer.SerializeToString(getHospitalServerResponse));
                return true;
            }
        }

        private static HttpWebRequest BuildHttpRequest(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = false;
            req.ProtocolVersion = HttpVersion.Version11;
            req.Timeout = 5000;
            req.ReadWriteTimeout = 5000;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            return req;
        }
    }
}
