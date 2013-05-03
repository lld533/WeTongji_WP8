using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Context
    {
        public Location location { get; set; }

        public String type { get; set; }

        public URN.NLP_Type nlp_type { get; set; }

        public String href { get; set; }
    }
}
