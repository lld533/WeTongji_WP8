using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class NokiaMapQueryClient
    {
        #region [Core Function]

        public void ExecuteAsync(QueryRequest req, Object state)
        {
            try
            {
                var webRequest = HttpWebRequest.CreateHttp(String.Format(
                                                        "http://places.nlp.nokia.com.cn/places/v1/discover/search?at={0}%2C{1}&q={2}&tf=plain&pretty=y&size=10&app_id={3}&app_code={4}",
                                                            req.CurrentPosition.Latitude,
                                                            req.CurrentPosition.Longitude,
                                                            HttpUtility.UrlEncode(req.Query),
                                                            req.AppId,
                                                            req.Token)
                                                        );
                if (req.QType == QueryType.Default)
                {
                    webRequest.BeginGetResponse((args) =>
                    {
                        try
                        {
                            var webResponse = webRequest.EndGetResponse(args);
                            var stream = webResponse.GetResponseStream();
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                var str = sr.ReadToEnd();
                                var queryResponse = JsonConvert.DeserializeObject<QueryResponse>(str);
                                OnExecuteCompleted(queryResponse);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            OnExecuteFailed(ex);
                            return;
                        }
                    }, state);
                }
                else if (req.QType == QueryType.All)
                {
                    QueryResponse result = new QueryResponse();

                    webRequest.BeginGetResponse((args) =>
                    {
                        try
                        {
                            #region [Get the first page]

                            var webResponse = webRequest.EndGetResponse(args);
                            using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                            {
                                var str = sr.ReadToEnd();
                                var queryResponse = JsonConvert.DeserializeObject<QueryResponse>(str);
                                result.results = queryResponse.results;
                                webResponse.Close();
                            }

                            #endregion

                            #region [Get the rest pages]

                            while (!String.IsNullOrEmpty(result.results.next))
                            {
                                webRequest = HttpWebRequest.CreateHttp(result.results.next);
                                webRequest.BeginGetResponse((arg) =>
                                {
                                    try
                                    {
                                        webResponse = webRequest.EndGetResponse(arg);

                                        using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                                        {
                                            var str = sr.ReadToEnd();
                                            var queryResponse = JsonConvert.DeserializeObject<QueryResponse>(str);
                                            result.results.items = queryResponse.results.items.Concat(result.results.items);
                                            result.results.next = queryResponse.results.next;
                                            result.results.available = queryResponse.results.available;
                                            result.results.offset = queryResponse.results.offset;

                                            webResponse.Close();
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        OnExecuteFailed(ex);
                                        return;
                                    }
                                }, new object());
                            }

                            #endregion

                            OnExecuteCompleted(result);
                        }
                        catch (System.Exception ex)
                        {
                            OnExecuteFailed(ex);
                            return;
                        }

                    }, state);
                }
                else
                {
                    //...[Impossible Case]
                }

            }
            catch (System.Exception ex)
            {
                OnExecuteFailed(ex);
                return;
            }
        }

        #endregion

        #region [Event handlers]

        public EventHandler<ExecuteCompletedEventArgs> ExecuteCompleted;

        public EventHandler<ExecuteFailedEventArgs> ExecuteFailed;

        public EventHandler ExecuteStarted;

        private void OnExecuteCompleted(QueryResponse response)
        {
            var handler = ExecuteCompleted;
            if (handler != null)
            {
                handler(new object(), new ExecuteCompletedEventArgs(response));
            }
        }

        private void OnExecuteFailed(Exception ex)
        {
            var handler = ExecuteFailed;
            if (handler != null)
            {
                handler(new object(), new ExecuteFailedEventArgs(ex));
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
    }

    #region [Event Args]

    public class ExecuteCompletedEventArgs : EventArgs
    {
        public ExecuteCompletedEventArgs(QueryResponse response)
        {
            Response = response;
        }

        public QueryResponse Response { get; private set; }
    }

    public class ExecuteFailedEventArgs : EventArgs
    {
        public ExecuteFailedEventArgs(Exception ex)
        {
            Ex = ex;
        }

        public Exception Ex { get; private set; }
    }

    #endregion


}
