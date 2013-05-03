using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeTongji.Api.Request;

namespace WeTongji.Api
{
    public class WTExecuteCompletedEventArgs<T> :EventArgs where T : WTResponse
    {
        public IWTRequest<T> Request { get; private set; }
        public T Result { get; private set; }

        /// <summary>
        /// Constructor to call for a success
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        public WTExecuteCompletedEventArgs(IWTRequest<T> request, T result)
        {
            Request = request;
            Result = result;
        }
    }
}
