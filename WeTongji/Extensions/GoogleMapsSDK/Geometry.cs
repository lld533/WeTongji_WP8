using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class Geometry
    {
        public LatLng location { get; set; }
        public LocationType location_type { get; set; }
        public BoundingBox viewport { get; set; }
        public BoundingBox bounds { get; set; } 
    }
}
