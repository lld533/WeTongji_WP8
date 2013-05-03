using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    class GoogleMapsQueryException : Exception
    {
        public Status StatusCode { get; private set; }

        public GoogleMapsQueryException(Status status)
        {
            StatusCode = status;
        }
    }
}
