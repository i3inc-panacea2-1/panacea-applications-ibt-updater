using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IBT.Updater
{
    public static class HttpWebRequestUtility
    {
        /// <summary>
        /// Gets the <see cref="HttpWebResponse"/> from an Internet resource.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A <see cref="HttpWebResponse"/> that contains the response from the Internet resource.</returns>
        /// <remarks>This method does not throw a <see cref="WebException"/> for "error" HTTP status codes; the caller should
        /// check the <see cref="HttpWebResponse.StatusCode"/> property to determine how to handle the response.</remarks>
        public static async Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request, int timeout)
        {
            try
            {
                var ct = new CancellationTokenSource(timeout);
                using (ct.Token.Register(() => request.Abort(), useSynchronizationContext: false))
                {
                    var response = await request.GetResponseAsync();
                    ct.Token.ThrowIfCancellationRequested();
                    return (HttpWebResponse)response;
                }
            }
            catch (WebException ex)
            {
                // only handle protocol errors that have valid responses
                if (ex.Response == null || ex.Status != WebExceptionStatus.ProtocolError)
                    throw;

                return (HttpWebResponse)ex.Response;
            }
        }
    }
}
