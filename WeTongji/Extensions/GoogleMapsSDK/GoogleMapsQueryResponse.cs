using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class GoogleMapsQueryResponse
    {
        public Address[] results { get; set; }

        public Status status { get; set; }
    }
}
