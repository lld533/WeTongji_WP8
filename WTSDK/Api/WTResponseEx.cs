using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Api
{
    public class WTResponseEx<T> where T : WTResponse
    {
        public WTResponseEx() { }

        public WTStatus Status { get; set; }
        public T Data { get; set; }
    }
}
