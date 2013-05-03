using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeTongji.Api.Util;

namespace WeTongji.Api
{
    public sealed class WTException : Exception
    {
        public WTStatus StatusCode { get; private set; }

        public WTException(WTStatus code)
        {
            StatusCode = code;
        }
    }
}
