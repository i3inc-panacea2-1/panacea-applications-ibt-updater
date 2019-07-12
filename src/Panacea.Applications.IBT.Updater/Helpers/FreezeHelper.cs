using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace IBT.Updater.Helpers
{
    internal static class FreezeHelper
    {

        internal static bool IsFreezeEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var panacea = key.CreateSubKey("Panacea"))
                using (var updater = panacea.CreateSubKey("Updater"))
                {
                    return (int) updater.GetValue("Freeze", 1) == 1;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
