using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace IBT.Updater.Helpers
{
    internal static class DotNetVersionHelper
    {

        public static bool RequiresUpdate()
        {
            using (
                RegistryKey ndpKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                        .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));

                return releaseKey < (int)DotNetVersions.v4_6;

            }
        }

        enum DotNetVersions : int
        {
            v4_5 = 378389,
            v4_5_1 = 378675,
            v4_5_2 = 379893,
            v4_6 = 393295,
            v4_6_1 = 394254,
            v4_6_2 = 394802,
            v4_7 = 460798,
            v4_7_1 = 461308,
            v4_7_2 = 461808
        }
    }
}
