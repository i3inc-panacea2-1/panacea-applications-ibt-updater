using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PanaceaLib;
using ServiceStack.Text;

namespace IBT.Updater
{
    public class SystemUpdater
    {
        private string _server;

        public SystemUpdater(string server)
        {
            _server = server;
        }

        public event EventHandler<string> Update;

        protected void OnUpdate(string msg)
        {
            var h = Update;
            if (h != null)
            {
                h(this, msg);
            }
        }

        [DataContract]
        class UpdatesResult
        {
            [DataMember(Name = "_id")]
            public string Id { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "patchScript")]
            public string PatchScript { get; set; }

            [DataMember(Name = "requiredFiles")]
            public List<String> RequiredFiles { get; set; }

            [DataMember(Name = "requiresReboot")]
            public bool RequiresReboot { get; set; }

            [DataMember(Name = "timeStamp")]
            public DateTime TimeStamp { get; set; }

        }

        public async Task<ServerResponse<T>> GetObjectFromServerAsync<T>(string urlPart)
        {
            var tries = 0;
            while (tries < 2)
            {
                tries++;
                if (tries == 2) await Task.Delay(3000);
                try
                {
                    var url = CombineUri(_server, "/api/", Common.GetMacAddress(), "/test/0/", urlPart);
                    Console.WriteLine(url);
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 10000;
                    req.ReadWriteTimeout = 10000;
                    using (var resp = (HttpWebResponse)await req.GetHttpResponseAsync(10000))
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        var result = sr.ReadToEnd();
                        var ob = (ServerResponse<T>)JsonSerializer.DeserializeFromString(result, typeof(ServerResponse<T>));
                        return ob;
                    }
                }
                catch
                {
                    if (tries == 2) throw;
                }
            }
            return null;
        }

        public async Task PerformUpdate()
        {
            try
            {
                var result = await GetObjectFromServerAsync<List<UpdatesResult>>("get_sustem_updates/");
                if (result.Success)
                {
                    foreach (var update in result.Result)
                    {
                        if (!Directory.Exists(Common.Path() + "Updates/" + update.Id))
                            Directory.CreateDirectory(Common.Path() + "Updates/" + update.Id);
                        OnUpdate(update.Name);

                        using (var wc = new WebClient())
                        {
                            await wc.DownloadFileTaskAsync(
                                new Uri(CombineUri(_server, update.PatchScript)),
                                Common.Path() + "Updates/" + update.Id + "/" + Path.GetFileName(update.PatchScript));

                            foreach (var file in update.RequiredFiles)
                            {
                                await wc.DownloadFileTaskAsync(
                                new Uri(CombineUri(_server, file)),
                                Common.Path() + "Updates/" + update.Id + "/" + Path.GetFileName(file));
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                //ignore
            }
        }

        public static string CombineUri(params string[] uriParts)
        {
            string uri = string.Empty;
            if (uriParts != null && uriParts.Any())
            {
                char[] trims = new char[] { '\\', '/' };
                uri = (uriParts[0] ?? string.Empty).TrimEnd(trims);
                for (int i = 1; i < uriParts.Count(); i++)
                {
                    uri = string.Format("{0}/{1}", uri.TrimEnd(trims), (uriParts[i] ?? string.Empty).TrimStart(trims));
                }
            }
            return uri;
        }

    }
}
