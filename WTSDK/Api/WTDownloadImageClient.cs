using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WeTongji.Api;
using WeTongji.Api.Domain;
using WeTongji.Api.Response;
using System.Net;
using System.IO.IsolatedStorage;

namespace WeTongji.Api
{
    public class WTDownloadImageClient
    {
        public event EventHandler<WTDownloadImageStartedEventArgs> DownloadImageStarted;
        public event EventHandler<WTDownloadImageCompletedEventArgs> DownloadImageCompleted;
        public event EventHandler<WTDownloadImageFailedEventArgs> DownloadImageFailed;

        private void OnDownloadImageStarted(String url)
        {
            var handler = DownloadImageStarted;
            if (handler != null)
                handler(this, new WTDownloadImageStartedEventArgs(url));
        }

        private void OnDownloadImageCompleted(String url, String fileName)
        {
            var handler = DownloadImageCompleted;
            if (handler != null)
                handler(this, new WTDownloadImageCompletedEventArgs(url,fileName));
        }

        private void OnDownloadImageFailed(String url, Exception err)
        {
            var handler = DownloadImageFailed;
            if (handler != null)
                handler(this, new WTDownloadImageFailedEventArgs(url, err));
        }

        public void Execute(String url, String fileName)
        {
            try
            {
                var req = HttpWebRequest.CreateHttp(url);

                OnDownloadImageStarted(url);
                req.BeginGetResponse((args) =>
                {
                    try
                    {
                        var res = req.EndGetResponse(args);

                        var rs = res.GetResponseStream();

                        var store = IsolatedStorageFile.GetUserStoreForApplication();

                        using (var fs = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            rs.CopyTo(fs);
                            fs.Flush();
                            fs.Close();
                        }

                        res.Close();

                        store.Dispose();

                        OnDownloadImageCompleted(url, fileName);
                    }
                    catch (System.Exception ex)
                    {
                        OnDownloadImageFailed(url, ex);
                    }

                }, new object());

            }
            catch (Exception ex)
            {
                OnDownloadImageFailed(url, ex);
            }
        }
    }

    public class WTDownloadImageStartedEventArgs : EventArgs
    {
        public String Url { get; private set; }

        public WTDownloadImageStartedEventArgs(String url)
        {
            Url = url;
        }
    }

    public class WTDownloadImageCompletedEventArgs : EventArgs
    {
        public String Url { get; private set; }
        public String FileName { get; private set; }

        public WTDownloadImageCompletedEventArgs(String url, String fileName)
        {
            Url = url;
            FileName = fileName;
        }
    }

    public class WTDownloadImageFailedEventArgs : EventArgs
    {
        public String Url { get; private set; }
        public Exception Error { get; private set; }

        public WTDownloadImageFailedEventArgs(String url, Exception err)
        {
            Url = url;
            Error = err;
        }
    }
}
