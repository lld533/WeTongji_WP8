using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class GoogleMapsReverseQueryRequest : IGoogleMapsQueryRequest
    {
        private LatLng q;

        public String latlng
        {
            get { return String.Format("{0},{1}", q.lat, q.lng); }
        }

        public String language
        {
            get;
            private set;
        }

        public Boolean sensor 
        { 
            get; 
            private set; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query">reverse query</param>
        /// <param name="ss">sensor, by default is true</param>
        /// <param name="lan">language, by default is Chinese Simplified</param>
        public GoogleMapsReverseQueryRequest(LatLng query, Boolean ss = true, String lan = "zh-CN")
        {
            if (query == null)
                throw new ArgumentNullException("query");

            q = query;
            sensor = ss;
            language = lan;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="latitude">latitude of reverse query</param>
        /// <param name="longtitude">longtitude of reverse query</param>
        /// <param name="ss">sensor, by default is true</param>
        /// <param name="lan">language, by default is Chinese Simplified</param>
        public GoogleMapsReverseQueryRequest(double latitude, double longtitude, Boolean ss = true, String lan = "zh-CN")
        {
            q = new LatLng() { lat = latitude, lng = longtitude };
            sensor = ss;
            language = lan;
        }
    }
}
