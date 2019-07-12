using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace IBT.Updater
{
    internal static class BrowserSettingsHelper
    {
        private static void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                var target = key.OpenSubKey(string.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\",
                    feature), true);
                target?.SetValue(appName, value, RegistryValueKind.DWord);
            }
            if (Environment.Is64BitOperatingSystem)
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    var target = key.OpenSubKey(
                        String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\",
                            feature), true);
                    target?.SetValue(appName, value, RegistryValueKind.DWord);
                }
            }
        }

        private static uint GetIeVersion()
        {
            string ieVersionStr = null;
            using (
                var k =
                    Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Internet Explorer"))
            {
                ieVersionStr = k != null && k.GetValue("svcVersion") == null
                    ? null
                    : k.GetValue("svcVersion").ToString();
                if (string.IsNullOrEmpty(ieVersionStr))
                {
                    ieVersionStr = k.GetValue("Version") == null ? "9.0.0.0" : k.GetValue("Version").ToString();
                }
            }

            var ieVersion = new Version(ieVersionStr);
            var val = ieVersion.Major*1000;
            if (val == 9000) val = 8000;
            //if (val >= 10000) val++; //10001
            return (uint)val;
        }

        public static void UpdateSettings(string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetIeVersion());
                SetBrowserFeatureControlKey("FEATURE_AJAX_CONNECTIONEVENTS", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_DOMSTORAGE ", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_GPU_RENDERING", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_IVIEWOBJECTDRAW_DMLT9_WITH_GDI", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_DISABLE_LEGACY_COMPRESSION", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_LOCALMACHINE_LOCKDOWN", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_OBJECT", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_SCRIPT", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_SPELLCHECKING", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_STATUS_BAR_THROTTLING", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_TABBED_BROWSING", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_VALIDATE_NAVIGATE_URL", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_WEBOC_DOCUMENT_ZOOM", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_WEBOC_POPUPMANAGEMENT", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_WEBOC_MOVESIZECHILD", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_ADDON_MANAGEMENT", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_WEBSOCKET", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_WINDOW_RESTRICTIONS", fileName, 0);
                SetBrowserFeatureControlKey("FEATURE_XMLHTTP", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_NINPUT_LEGACYMODE", fileName, 1);
                SetBrowserFeatureControlKey("FEATURE_OBJECT_CACHING", fileName, 1);
            }
        }
    }
}
