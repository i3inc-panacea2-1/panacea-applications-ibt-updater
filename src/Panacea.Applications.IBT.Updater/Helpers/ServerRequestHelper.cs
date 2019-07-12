using PanaceaLib;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Helpers
{
    public class ServerRequestHelper
    {
        public static async Task<ServerResponse<T>> GetObjectFromServerAsync<T>(string server, string urlPart,
            object postData = null)
        {

            var url = CombineUri(server, "/api/", (await TerminalIdentification.TerminalIdentificationManager.GetIdentificationInfoAsync()).Putik, "/test/0/", urlPart);

            var req = (HttpWebRequest) WebRequest.Create(url);

            if (postData != null)
            {
                req.ContentType = "application/json";
                req.Method = "POST";
                req.Timeout = 10000;
                req.ReadWriteTimeout = 10000;
                using (var writer = new StreamWriter(req.GetRequestStream()) {AutoFlush = true})
                {
                    await writer.WriteAsync(JsonSerializer.SerializeToString(postData));
                }
            }
            using (var resp = (HttpWebResponse) await req.GetHttpResponseAsync(10000))
            using (var sr = new StreamReader(resp.GetResponseStream()))
            {
                var result = await sr.ReadToEndAsync();
                return await Task.Run(() =>
                {
                    var ob =
                        (ServerResponse<T>)
                        JsonSerializer.DeserializeFromString(result, typeof(ServerResponse<T>));
                    return ob;
                });

            }
        }

        public static string CombineUri(params string[] uriParts)
        {
            var uri = string.Empty;
            if (uriParts != null && uriParts.Any())
            {
                char[] trims = { '\\', '/' };
                uri = (uriParts[0] ?? string.Empty).TrimEnd(trims);
                for (var i = 1; i < uriParts.Count(); i++)
                {
                    uri = string.Format("{0}/{1}", uri.TrimEnd(trims), (uriParts[i] ?? string.Empty).TrimStart(trims));
                }
            }
            return uri;
        }
    }
}
