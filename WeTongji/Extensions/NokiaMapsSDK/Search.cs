using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Search
    {
        public Context context { get; set; }

        public String type { get; set; }

        public URN.NLP_Type nlp_type { get { return type.ToNLP_Type(); } }

        public String href { get; set; }
    }
}
