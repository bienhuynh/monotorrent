//
// HttpTrackerListener.cs
//
// Authors:
//   Gregor Burger burger.gregor@gmail.com
//
// Copyright (C) 2006 Gregor Burger
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Net;
using System.Threading;

using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker.Listeners
{
    class HttpTrackerListener : TrackerListener
    {
        string Prefix { get; }

        public HttpTrackerListener(IPAddress address, int port)
            : this(string.Format("http://{0}:{1}/announce/", address, port))
        {

        }

        public HttpTrackerListener(IPEndPoint endpoint)
            : this(endpoint.Address, endpoint.Port)
        {

        }

        public HttpTrackerListener(string httpPrefix)
        {
            if (string.IsNullOrEmpty(httpPrefix))
                throw new ArgumentNullException("httpPrefix");

            Prefix = httpPrefix;
        }

        #region Methods
        /// <summary>
        /// Starts listening for incoming connections
        /// </summary>
        protected override void Start(CancellationToken token)
        {
			var listener = new HttpListener();
            token.Register (() => listener.Close ());

            listener.Prefixes.Add(Prefix);
            listener.Start();
            listener.BeginGetContext(EndGetRequest, listener);
        }

        private void EndGetRequest(IAsyncResult result)
        {
			HttpListenerContext context = null;
			HttpListener listener = (HttpListener) result.AsyncState;
            
            try
            {
                context = listener.EndGetContext(result);
                using (context.Response)
                    HandleRequest(context);
            }
            catch(Exception ex)
            {
                Console.Write("Exception in listener: {0}{1}", Environment.NewLine, ex);
            }
            finally
            {
                try
                {
                    if (listener.IsListening)
                        listener.BeginGetContext(EndGetRequest, listener);
                }
                catch
                {
                    Stop();
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            bool isScrape = context.Request.RawUrl.StartsWith("/scrape", StringComparison.OrdinalIgnoreCase);

            BEncodedValue responseData = Handle(context.Request.RawUrl, context.Request.RemoteEndPoint.Address, isScrape);

            byte[] response = responseData.Encode();
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = response.LongLength;
            context.Response.OutputStream.Write(response, 0, response.Length);
        }

        #endregion Methods
    }
}
