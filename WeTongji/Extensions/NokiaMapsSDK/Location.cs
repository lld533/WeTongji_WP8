using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NokiaMapSDK;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Location
    {
        public double[] position { get; set; }

        public GeoPoint GeoPosition
        {
            get 
            {
                return new GeoPoint() { Latitude = position[0], Longitude = position[1] };
            }
        }
        
        public Address address { get; set; }
    }
}
