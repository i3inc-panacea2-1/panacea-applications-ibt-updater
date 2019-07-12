using IBT.Updater.Helpers;
using IBT.Updater.Interfaces;
using IBT.Updater.Models;
using PanaceaLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("File Check")]
    internal class FileDownloaderModule : Interfaces.Module
    {
        WebClient _webClient = new WebClient();
        ServerResponse<GetAllVersionsResponse> _getAllVersionsResponse;
        Dictionary<string, object> _keys;

        public FileDownloaderModule()
        {
            _webClient.DownloadProgressChanged += _webClient_DownloadProgressChanged;
        }

        private void _webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ReportFileDownloadProgress(e.ProgressPercentage);
        }

        [ExecutionPriority(999)]
        internal override async Task<bool> OnUpdate(SafeDictionary<string, object> keys)
        {
            if (keys["noupdate"]?.ToString() == "1") return true;
            _keys = keys;
            ReportProgress("Fetching version information..");
            _getAllVersionsResponse =
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<GetAllVersionsResponse>(keys["hospitalserver"].ToString(),
                            "get_all_versions/");
            if (!_getAllVersionsResponse.Success)
            {
                ReportProgress("Terminal registered in management but not in hospital server");
                await Task.Delay(5000);
                ProcessHelper.StartRegistrator(_keys["server"].ToString());
                App.ShutdownSafe();
                return false;
            }
            var files = (await GetCoreFiles()).Concat(await GetPluginFiles()).ToList();
            var corePath =
               Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\");
            var result = await CompareAndDownloadFileList(files, corePath, new[] { "cache.db" }, new[] { "txt", "csv" }, new [] { "Lib\\VLC\\x64", "Lib\\VLC\\x86" } );
            
            return result;
        }

        private async Task<List<string>>  DeleteFiles(List<CoreFile> expected, string path,
            string[] excludedFiles = null,
            string[] excludedExtensions = null,
            string[] excludedFolders = null,
            string originalPath = null)
        {
            var list = new List<string>();
            await Task.Run(() =>
            {
                
                if (originalPath == null) originalPath = path;

                foreach (var file in Directory.GetFiles(path))
                {
                    if (excludedFiles != null && excludedFiles.Contains(Path.GetFileName(file))) continue;
                    if (excludedExtensions != null &&
                        excludedExtensions.Contains(Path.GetFileName(file).Split('.').Last())) continue;
                    if (excludedFolders != null &&
                        excludedFolders.Any(ex => file.Contains(ex))) continue;

                    if (expected.Any(f => f.LocalPath == file)) continue;
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine(file);
                        list.Add(file);
                    }
                    catch
                    {
                        list.Add("skipped - " + file);
                    }

                }

            });
            
            foreach (var dir in Directory.GetDirectories(path))
                list =
                    list.Concat(
                        await
                            DeleteFiles(expected, dir, excludedFiles, excludedExtensions, excludedFolders, originalPath)).ToList();
            if(!Directory.GetDirectories(path).Any() && !Directory.GetFiles(path).Any())Directory.Delete(path);
            return list;
        }

        async Task<bool> CompareAndDownloadFileList(
            List<CoreFile> files, 
            string filePath, 
            string[] excludedFiles = null, 
            string[] exludedExtensions = null,
            string[] excludedFolders = null)
        {
            var c = 0;
            var tmpPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "inuse");
            var tmpPathOld = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            if (!Directory.Exists(tmpPathOld))
                Directory.CreateDirectory(tmpPathOld);
            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }
            ReportProgress("Deleting old files...");
            var list = await DeleteFiles(files, filePath, excludedFiles, exludedExtensions, excludedFolders);
            using (var sw = new StreamWriter(Common.Path() + "deleted-files.txt", true))
            {
                sw.WriteLine("---  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ---");
                await sw.WriteLineAsync(string.Join(Environment.NewLine, list));
            }

            foreach (var file in files)
            {
                c++;
                ReportFunctionProgress((int)((double)c * 100.0 / (double)files.Count));
                ReportProgress(String.Format("Checking {0}", file.FileName));
                var download = !await FileComparisonHelper.AreVersionsEqual(file.Version, file.LocalPath)
                    || !await FileComparisonHelper.AreMd5Equal(file.Hash, file.LocalPath);

                if (!download) continue;

                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var target = file.LocalPath;
                var inuse = false;
                if (File.Exists(filePath + file.FileName))
                {
                    try
                    {
                        Console.WriteLine(@"Deleting file {0}", file.FileName);
                        File.Delete(filePath + file.FileName);
                    }
                    catch
                    {
                        inuse = true;
                        RequiresUpdaterRestart = true;
                        target = Path.Combine(tmpPath, file.FileName);
                        if (!Directory.Exists(Path.GetDirectoryName(target)))
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }
                }
                BeginFileDownload();
                ReportProgress(String.Format("Downloading {0}", file.FileName));
                if (!Directory.Exists(Path.GetDirectoryName(target))) Directory.CreateDirectory(Path.GetDirectoryName(target));
                
                await _webClient.DownloadFileTaskAsync(
                    new Uri(_keys["hospitalserver"].ToString() + "/" + file.DownloadPath),
                    target);
                await Task.Delay(200);
                if (inuse)
                {
                    try
                    {
                        File.Copy(target, Path.Combine(tmpPathOld, Path.GetFileName(file.FileName)));
                    }
                    catch { }
                }
                   
                EndFileDownload();
            }
            
            return true;
        }

        async Task<List<CoreFile>> GetCoreFiles()
        {
            var result = _getAllVersionsResponse.Result;
            var coreVersion = result.CoreVersion.Version;
            ReportProgress(String.Format("Fetching information for {0}...", "Core"));
            var fileList =
                await
                    ServerRequestHelper.GetObjectFromServerAsync<List<CoreFile>>(_keys["hospitalserver"].ToString(),
                        "get_core_files/core/" + coreVersion + "/");
            if (fileList.Result == null) throw new Exception("Bad result format from server");
            var corePath =
               Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\");
            foreach (var file in fileList.Result)
            {
                file.LocalPath = Path.GetFullPath(corePath + file.FileName);
            }
            return fileList.Result;
            //return 
        }

        async Task<List<CoreFile>> GetPluginFiles()
        {
            var path = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\");
            var count = _getAllVersionsResponse.Result.Plugins.Count;
            var list = new List<CoreFile>();
            foreach (var f in _getAllVersionsResponse.Result.Plugins)
            {
                ReportProgress(String.Format("Fetching information for {0}...", f.Name));
                var files =
                    await
                        ServerRequestHelper.GetObjectFromServerAsync<List<CoreFile>>(_keys["hospitalserver"].ToString(),
                            "get_plugin_files/" + f.Name + "/" + f.Version + "/");
                var pluginPath =
                    Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                                     "\\..\\ibt-plugins\\" + f.Name + "\\");
                foreach (var file in files.Result)
                {
                    file.LocalPath = Path.GetFullPath(pluginPath + file.FileName);
                }
                list.AddRange(files.Result);// await CompareAndDownloadFileList(files.Result, pluginPath);
            }
            return list;
            //return true;
        }
    }
}
