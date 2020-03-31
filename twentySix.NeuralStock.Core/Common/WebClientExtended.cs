namespace twentySix.NeuralStock.Core.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class WebClientExtended : WebClient
    {
        public WebClientExtended(string cookie = null)
        {
            if (cookie != null)
            {
                this.Headers.Add(HttpRequestHeader.Cookie, cookie);
            }

            this.UseDefaultCredentials = true;
        }

        public Dictionary<Cookie, string> CookiesDictionary { get; set; }

        public int Tries { get; set; } = 3;

        public int Timeout { get; set; } = 3000;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);

            if (request != null)
            {
                request.Timeout = this.Timeout;
                return request;
            }

            return null;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var deepCopiedWebRequest = this.GetWebRequest(request.RequestUri);

            try
            {
                return base.GetWebResponse(deepCopiedWebRequest ?? throw new InvalidOperationException());
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout || ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    if (--this.Tries == 0)
                    {
                        throw;
                    }

                    return this.GetWebResponse(request);
                }

                throw;
            }
        }
    }
}