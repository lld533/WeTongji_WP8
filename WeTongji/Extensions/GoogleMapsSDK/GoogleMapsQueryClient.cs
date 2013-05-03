using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class GoogleMapsQueryClient
    {
        #region [Event Handlers]

        public EventHandler<GoogleMapsQueryCompletedEventArgs> ExecuteCompleted;

        public EventHandler<GoogleMapsQueryFailedEventArgs> ExecuteFailed;

        public EventHandler ExecuteStarted;

        private void OnExecuteCompleted(IGoogleMapsQueryRequest req, GoogleMapsQueryResponse res)
        {
            var handler = ExecuteCompleted;
            if (handler != null)
            {
                handler(new object(), new GoogleMapsQueryCompletedEventArgs(req, res));
            }
        }

        private void OnExecuteFailed(IGoogleMapsQueryRequest req, Exception err)
        {
            var handler = ExecuteFailed;
            if (handler != null)
            {
                handler(new object(), new GoogleMapsQueryFailedEventArgs(req, err));
            }
        }

        private void OnExecuteStarted()
        {
            var handler = ExecuteStarted;
            if (handler != null)
            {
                handler(new object(), new EventArgs());
            }
        }

        #endregion

        #region [Execute]

        /// <summary>
        /// Execute query to Google Maps
        /// </summary>
        /// <param name="req"></param>
        /// <remarks>
        /// This method uses reflection.
        /// </remarks>
        public void ExecuteAsync(IGoogleMapsQueryRequest req)
        {
            try
            {
                if (req == null)
                    throw new ArgumentNullException("req");

                #region [Make Url]

                var url = "https://maps.googleapis.com/maps/api/geocode/json?";

                var properties = req.GetType().GetProperties();
                String[] strs = new String[properties.Count()];

                int i = 0;
                foreach (var pi in properties)
                {
                    strs[i++] = String.Format("{0}={1}", pi.Name, pi.GetGetMethod(false).Invoke(req, null).ToString().ToLower());
                }

                url += strs.Aggregate((a, b) => a + "&" + b);

                #endregion

                var webRequest = WebRequest.CreateHttp(url);
                System.Diagnostics.Debug.WriteLine(webRequest.RequestUri.AbsoluteUri);

                OnExecuteStarted();

                webRequest.BeginGetResponse((args) =>
                {
                    try
                    {
                        var webResponse = webRequest.EndGetResponse(args);
                        using (var sr = new StreamReader(webResponse.GetResponseStream()))
                        {
                            var str = sr.ReadToEnd();
                            var res = JsonConvert.DeserializeObject<GoogleMapsQueryResponse>(str);
                            if (res.status != Status.OK && res.status!= Status.ZERO_RESULTS)
                            {
                                throw new GoogleMapsQueryException(res.status);
                            }
                            OnExecuteCompleted(req, res);
                        }
                        webResponse.Close();
                    }
                    catch (System.Exception ex)
                    {
                        OnExecuteFailed(req, ex);
                    }
                }, new object());
            }
            catch (System.Exception ex)
            {
                OnExecuteFailed(req, ex);
            }
        }

        #endregion
    }

    #region [EventArgs]

    public class GoogleMapsQueryCompletedEventArgs : EventArgs
    {
        public IGoogleMapsQueryRequest Request { get; private set; }
        public GoogleMapsQueryResponse Response { get; private set; }

        public GoogleMapsQueryCompletedEventArgs(IGoogleMapsQueryRequest req, GoogleMapsQueryResponse res)
        {
            Request = req;
            Response = res;
        }
    }

    public class GoogleMapsQueryFailedEventArgs : EventArgs
    {
        public IGoogleMapsQueryRequest Request { get; set; }
        public Exception Error { get; private set; }

        public GoogleMapsQueryFailedEventArgs(IGoogleMapsQueryRequest req, Exception err)
        {
            Request = req;
            Error = err;
        }
    }

    #endregion
}
