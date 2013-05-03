using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public static class URN
    {
        public enum NLP_Type
        {
            place,
            category,
            search,
            search_results,
            UNKNOWN
        }

        public static NLP_Type ToNLP_Type(this String str)
        {
            var result = NLP_Type.UNKNOWN;

            var strTrimmed = str.TrimStart("urn:nlp-types:".ToCharArray());

            if (strTrimmed == NLP_Type.place.ToString())
            {
                result = NLP_Type.place;
            }
            else if(strTrimmed == NLP_Type.category.ToString())
            {
                result = NLP_Type.category;
            }
            else if (strTrimmed == NLP_Type.search.ToString())
            {
                result = NLP_Type.search;
            }
            else if (strTrimmed == NLP_Type.search_results.ToString())
            {
                result = NLP_Type.search_results;
            }


            return result;
        }
    }
}
