using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Helpers
{
    internal static class FileComparisonHelper
    {
        private static MD5 _md5;
        static FileComparisonHelper()
        {
            _md5 = MD5.Create();
        }

        internal static Task<bool> AreMd5Equal(string md5hash, string fileName)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(fileName)) return false;
                try
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        var hash = _md5.ComputeHash(stream);
                        var sb = new StringBuilder();
                        foreach (var t in hash)
                        {
                            sb.Append(t.ToString("x2"));
                        }
                        var res = sb.ToString() == md5hash;
                        return res;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        internal static Task<bool> AreVersionsEqual(string version, string filePath)
        {
            
            return Task.Run(() =>
            {
                if (version == null) return true;
                if (!File.Exists(filePath)) return false;
                try
                {
                    var fv = FileVersionInfo.GetVersionInfo(filePath);
                    var fileVersion = fv.FileVersion;
                    return (fileVersion == version);
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
