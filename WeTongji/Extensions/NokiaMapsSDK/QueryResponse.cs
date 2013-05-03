using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class QueryResult<T> : PaginateableCollection<Place>
    {
    }

    public class QueryResponse
    {
        public QueryResult<Place> results { get; set; }
        public Search search { get; set; }
    }
}
