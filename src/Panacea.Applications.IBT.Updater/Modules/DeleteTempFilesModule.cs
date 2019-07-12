using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using PanaceaLib;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Temporary files")]
    internal class DeleteTempFilesModule : Interfaces.Module
    {
        internal override Task<bool> OnPreUpdate(SafeDictionary<string, object> keys)
        {
            if (Debugger.IsAttached) return Task.FromResult<bool>(true);
            return Task.Run(() =>
            {
                ReportProgress("Deleting temporary files...");

                DeleteDir("P:\\AppData\\Local\\Temp");
                DeleteDir(Path.GetTempPath());
                try
                {
                    if (!Directory.Exists(Path.GetTempPath()))
                    {
                        Directory.CreateDirectory(Path.GetTempPath());
                    }
                }
                catch
                {
                }
                try
                {
                    foreach (var file in Directory.GetFiles(new DirectoryInfo(Common.Path()).Parent.FullName, "*.ax"))
                    {
                        Process.Start("regsvr32.exe", "/u /s " + file).WaitForExit();
                    }
                }
                catch
                {
                }
                return true;
            });
        }

        bool DeleteDir(string dir)
        {
            if (!Directory.Exists(dir)) return true;
            try
            {
                var failedToDeleteFile = false;
                foreach (var file in Directory.GetFiles(dir))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch(Exception ex)
                    {
                        // file in use? readonly? not in this scope to deal with
                        failedToDeleteFile = true;
                        Debug.WriteLine(ex.Message + " - " + file);
                    }
                }
                foreach (var direct in Directory.GetDirectories(dir))
                {
                    failedToDeleteFile |= DeleteDir(direct);
                }
                if (!failedToDeleteFile)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch(Exception ex) {
                        Console.WriteLine(ex.Message + " - " + dir);
                    }
                }
                return failedToDeleteFile;
            }
            catch
            {
                //something else went wrong.
                return false;
            }
        }
    }
}
