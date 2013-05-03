using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;
using System.Net;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class GoogleMapsQueryRequest : IGoogleMapsQueryRequest
    {
        #region [Fields]

        private String _address;

        private GeoCoordinate _coord;

        private Boolean _sensor;

        private String _language;

        #endregion

        #region [Properties]

        public String address 
        { 
            get 
            { 
                return HttpUtility.UrlEncode(_address.Replace(' ', '+')); 
            } 
        }

        public String latlng
        {
            get
            {
                return String.Format("{0},{1}", _coord.Latitude.ToString(), _coord.Longitude.ToString());
            }
        }

        public Boolean sensor { get { return _sensor; } }

        public String language { get { return _language; } }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="coord"></param>
        /// <param name="ss"></param>
        /// <param name="lan">https://spreadsheets.google.com/pub?key=p9pdwsai2hDMsLkXsoM05KQ&gid=1</param>
        public GoogleMapsQueryRequest(String addr, GeoCoordinate coord, Boolean ss, String lan = "zh-CN")
        {
            _address = addr;
            _coord = coord;
            _sensor = ss;
            _language = lan;
        }
    }
}
