using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NokiaMapSDK;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    #region [QueryType]

    public enum QueryType
    {
        /// <summary>
        /// Search for the first page
        /// </summary>
        Default,
        /// <summary>
        /// Search for all pages
        /// </summary>
        All
    }

    #endregion

    #region [QueryRequest]

    public class QueryRequest
    {
        public String AppId { get; set; }

        public String Token { get; set; }

        public String Query { get; set; }

        public GeoPoint CurrentPosition { get; set; }

        public QueryType QType { get; set; }

        public QueryRequest()
        {
            QType = QueryType.Default;
        }
    }

    #endregion
}
