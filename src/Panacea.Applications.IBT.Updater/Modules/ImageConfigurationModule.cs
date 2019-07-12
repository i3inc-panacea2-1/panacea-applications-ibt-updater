using IBT.Updater.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("System configuration")]
    class ImageConfigurationModule : Interfaces.Module
    {
        const string LoudnessEqualizationKey = "{E0A941A0-88A2-4df5-8D6B-DD20BB06E8FB},4";
        internal override async Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            /*
            ReportProgress("Enabling loudness equalization");
            StringBuilder script = new StringBuilder(@"Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render]

");
            var unfreezeRequired = FreezeHelper.IsFreezeEnabled() && (await LockdownManager.IsFrozen()) == true;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using(var devices = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render"))
            {
                foreach(var key in devices.GetSubKeyNames())
                {
                    using (var device = devices.OpenSubKey(key))
                    using(var props = device.OpenSubKey("FxProperties"))
                    {
                        if (props == null) continue;
                        if (props.GetValueNames().Contains(LoudnessEqualizationKey))
                        {
                            var value = (int)props.GetValue(LoudnessEqualizationKey, 0);
                            if(value == 0 && unfreezeRequired)
                            {
                                LockdownManager.Unfreeze();
                                return false;
                            }
                            script.Append(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\" + key + "]" + Environment.NewLine + Environment.NewLine);
                            script.Append(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\" + key + @"\FxProperties]" + Environment.NewLine);
                            script.Append("\"" + LoudnessEqualizationKey + "\"=dword:00000001"+Environment.NewLine);
                            script.Append("\"{9cc064e5-7fdc-4f03-9994-f24d4908aa26}\"=hex:03,00,00,00,01,00,00,00,00,00,\\" + Environment.NewLine+ "00,00" + Environment.NewLine);
                            script.Append("\"{4b361010-def7-43a1-a5dc-071d955b62f7}\"=hex:41,00,00,00,01,00,00,00,00,00,\\" + Environment.NewLine + "00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,\\" + Environment.NewLine + "00,00,00,00,00,00,00,00,00,00,00,00,00" + Environment.NewLine);
                            script.Append(Environment.NewLine);
                        }
                    }
                }
            }
            var content = script.ToString();
            using(var writer = new StreamWriter("reg.reg"))
            {
                writer.Write(content);

            }
            var info = new ProcessStartInfo()
            {
                FileName = "regedit",
                Arguments = "/s reg.\"D:\\Repositories\\PanaceaV2Client\\bin\\Updater\\reg.reg",
                Verb = "runas"
            };
            var process = new Process()
            {
                StartInfo = info
            };
            process.Start();
            process.WaitForExit();
            */
            return true;
            
        }
    }
}
