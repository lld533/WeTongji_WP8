using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeTongji.Api.Request;
using WeTongji.Api.Response;

namespace WeTongji.Api
{
    public class WTExecuteFailedEventArgs<T> : EventArgs where T : WTResponse
    {
        public Exception Error { get; private set; }

        public IWTRequest<T> Request { get; private set; }

        public WTExecuteFailedEventArgs(IWTRequest<T> req, Exception err)
        {
            Request = req;
            Error = err;
        }
    }
}
