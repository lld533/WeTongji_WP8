using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Category
    {
        public String id { get; set; }

        public String title { get; set; }

        public String href { get; set; }

        public String type { get; set; }

        public URN.NLP_Type nlp_type { get { return type.ToNLP_Type(); } }
    }
}
