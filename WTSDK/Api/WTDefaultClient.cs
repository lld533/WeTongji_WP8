using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using WeTongji.Api.Request;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using WeTongji.Api.Util;
using System.Security.Cryptography;

namespace WeTongji.Api
{
    public class WTDefaultClient<T> where T : WTResponse
    {
        #region [Readonly Strings]

        public static readonly String METHOD = "M";
        public static readonly String HASH = "H";
        public static readonly String DEVICE = "D";
        public static readonly String UID = "U";
        public static readonly String PAGE = "P";
        public static readonly String VERSION = "V";
        public static readonly String SESSION = "S";

        public static readonly String CURRENT_DEVICE = "WP7";
        public static readonly String CURRENT_VERSION = "3.0";

        public static readonly String SERVER = "http://we.tongji.edu.cn/api/call?";

        #endregion

        #region [EventHandlers]

        public EventHandler<WTExecuteCompletedEventArgs<T>> ExecuteCompleted;

        public EventHandler<WTExecuteFailedEventArgs<T>> ExecuteFailed;

        private void OnExecuteCompleted(IWTRequest<T> request, T response)
        {
            var handler = ExecuteCompleted;
            if (handler != null)
            {
                handler(this, new WTExecuteCompletedEventArgs<T>(request, response));
            }
        }

        private void OnExecuteFailed(IWTRequest<T> req, Exception err)
        {
            var handler = ExecuteFailed;
            if (handler != null)
            {
                handler(this, new WTExecuteFailedEventArgs<T>(req, err));
            }
        }

        #endregion

        #region [Core]

        public void Execute(IWTRequest<T> request)
        {
            #region [Validate Argument]

            if (request == null)
            {
                OnExecuteFailed(request, new ArgumentNullException("request"));
                return;
            }

            try
            {
                request.Validate();
            }
            catch (System.Exception ex)
            {
                OnExecuteFailed(request, ex);
                return;
            }

            #endregion

            #region [Create Dictionary]

            var dict = new Dictionary<String, String>(request.GetParameters());
            dict[METHOD] = request.GetApiName();
            dict[DEVICE] = CURRENT_DEVICE;
            dict[VERSION] = CURRENT_VERSION;

            #endregion

            var myWebRequest = HttpWebRequest.CreateHttp(Dictionary2Url(dict));
            Debug.WriteLine(myWebRequest.RequestUri.AbsoluteUri);

            #region [Get Response]

            myWebRequest.BeginGetResponse((args) =>
                        {
                            try
                            {
                                var response = myWebRequest.EndGetResponse(args);
                                using (var sr = new StreamReader(response.GetResponseStream()))
                                {
                                    var str = sr.ReadToEnd();
                                    var responseEXT = JsonConvert.DeserializeObject<WTResponseEx<T>>(str);

                                    if (responseEXT.Status.Id != Status.Success)
                                    {
                                        throw new WTException(responseEXT.Status);
                                    }
                                    else
                                    {
                                        OnExecuteCompleted(request, responseEXT.Data);
                                        return;
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                OnExecuteFailed(request, ex);
                            }
                        }, new object());

            #endregion
        }

        /// <summary>
        /// Execute a request that requires session and uid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="session"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public void Execute(IWTRequest<T> request, String session, String uid)
        {
            #region [Validate Argument]

            if (request == null)
            {
                OnExecuteFailed(request, new ArgumentNullException("request"));
                return;
            }

            if (String.IsNullOrEmpty(session))
            {
                OnExecuteFailed(request, new ArgumentNullException("session"));
                return;
            }

            if (String.IsNullOrEmpty(uid))
            {
                OnExecuteFailed(request, new ArgumentNullException("uid"));
                return;
            }

            try
            {
                request.Validate();
            }
            catch (System.Exception ex)
            {
                OnExecuteFailed(request, ex);
                return;
            }

            #endregion

            #region [Create Dictionary]

            var dict = new Dictionary<String, String>(request.GetParameters());
            dict[METHOD] = request.GetApiName();
            dict[DEVICE] = CURRENT_DEVICE;
            dict[VERSION] = CURRENT_VERSION;
            dict[SESSION] = session;
            dict[UID] = uid;

            #endregion

            var myWebRequest = HttpWebRequest.CreateHttp(Dictionary2Url(dict));
            Debug.WriteLine(myWebRequest.RequestUri.ToString());

            #region [Get Response]

            myWebRequest.BeginGetResponse((args) =>
            {
                try
                {
                    var response = myWebRequest.EndGetResponse(args);
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        var str = sr.ReadToEnd();
                        var responseEXT = JsonConvert.DeserializeObject<WTResponseEx<T>>(str);

                        if (responseEXT.Status.Id != Status.Success)
                        {
                            throw new WTException(responseEXT.Status);
                        }
                        else
                        {
                            OnExecuteCompleted(request, responseEXT.Data);
                            return;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    OnExecuteFailed(request, ex);
                }
            }, new object());
            #endregion
        }

        public void Post(IWTUploadRequest<T> request, String session, String uid)
        {
            #region [Validate Parameters]

            if (request == null)
                throw new ArgumentNullException("request");
            if (String.IsNullOrEmpty(session))
                throw new ArgumentNullException("session");
            if (String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid");

            #endregion

            #region [Create Dictionary]

            var dict = new Dictionary<String, String>(request.GetParameters());
            dict[METHOD] = request.GetApiName();
            dict[DEVICE] = CURRENT_DEVICE;
            dict[VERSION] = CURRENT_VERSION;
            dict[SESSION] = session;
            dict[UID] = uid;

            #endregion

            var myWebRequest = HttpWebRequest.CreateHttp(Dictionary2Url(dict));

            myWebRequest.Method = "POST";

            var req_stream = request.GetRequestStream();

            try
            {
                if (req_stream != null)
                {
                    myWebRequest.BeginGetRequestStream((args) =>
                    {
                        try
                        {
                            var requestStream = myWebRequest.EndGetRequestStream(args);
                            req_stream.Seek(0, SeekOrigin.Begin);
                            System.IO.StreamReader streamReader = new System.IO.StreamReader(req_stream);
                            var requestString = streamReader.ReadToEnd();
                            System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(requestStream);
                            streamWriter.Write(requestString);
                            streamWriter.Flush();
                            requestStream.Close();

                            Debug.WriteLine(myWebRequest.RequestUri.AbsoluteUri);

                            myWebRequest.BeginGetResponse((arg) =>
                            {
                                try
                                {
                                    var response = myWebRequest.EndGetResponse(arg);

                                    using (var sr = new StreamReader(response.GetResponseStream()))
                                    {
                                        var str = sr.ReadToEnd();
                                        var responseEXT = JsonConvert.DeserializeObject<WTResponseEx<T>>(str);

                                        if (responseEXT.Status.Id != Status.Success)
                                        {
                                            throw new WTException(responseEXT.Status);
                                        }
                                        else
                                        {
                                            OnExecuteCompleted(request, responseEXT.Data);
                                            return;
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    OnExecuteFailed(request, ex);
                                }

                            }, new object());
                        }
                        catch (System.Exception ex)
                        {
                            OnExecuteFailed(request, ex);
                        }

                    }, new object());
                }
                else
                {
                    myWebRequest.BeginGetResponse((arg) =>
                    {
                        try
                        {
                            var response = myWebRequest.EndGetResponse(arg);

                            using (var sr = new StreamReader(response.GetResponseStream()))
                            {
                                var str = sr.ReadToEnd();
                                var responseEXT = JsonConvert.DeserializeObject<WTResponseEx<T>>(str);

                                if (responseEXT.Status.Id != Status.Success)
                                {
                                    throw new WTException(responseEXT.Status);
                                }
                                else
                                {
                                    OnExecuteCompleted(request, responseEXT.Data);
                                    return;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            OnExecuteFailed(request, ex);
                        }

                    }, new object());
                }
            }
            catch (System.Exception ex)
            {
                OnExecuteFailed(request, ex);
            }


        }

        public void Post(WTUploadFileRequest<T> request, String session, String uid)
        {
            #region [Validate Parameters]

            if (request == null)
                throw new ArgumentNullException("request");
            if (String.IsNullOrEmpty(session))
                throw new ArgumentNullException("session");
            if (String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid");

            #endregion

            #region [Create Dictionary]

            var dict = new Dictionary<String, String>(request.GetParameters());
            dict[METHOD] = request.GetApiName();
            dict[DEVICE] = CURRENT_DEVICE;
            dict[VERSION] = CURRENT_VERSION;
            dict[SESSION] = session;
            dict[UID] = uid;

            #endregion

            var webReq = new MyToolkit.Networking.HttpPostRequest(Dictionary2Url(dict));

            webReq.Files.AddRange(request.GetFiles());

            Action<MyToolkit.Networking.HttpResponse> action = (response) =>
                {
                    if (response.Successful)
                    {
                        var responseEXT = JsonConvert.DeserializeObject<WTResponseEx<T>>(response.Response);

                        if (responseEXT.Status.Id != Status.Success)
                        {
                            OnExecuteFailed(request, new WTException(responseEXT.Status));
                        }
                        else
                        {
                            OnExecuteCompleted(request, responseEXT.Data);
                            return;
                        }
                    }
                    else
                    {
                        if (!response.Canceled)
                        {
                            OnExecuteFailed(request, response.Exception);
                        }
                    }
                };
            MyToolkit.Networking.Http.Post(webReq, action);
        }

        #endregion

        #region [Private Functions]

        private String Dictionary2Url(IDictionary<String, String> dict)
        {
            if (dict == null)
                throw new ArgumentNullException("null");
            StringBuilder sb = new StringBuilder(SERVER);

            foreach (var pair in dict)
            {
                sb.AppendFormat("{0}={1}&", HttpUtility.UrlEncode(pair.Key), HttpUtility.UrlEncode(pair.Value));
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        #endregion
    }
}
