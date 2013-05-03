using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public abstract class GoogleMapsTileSourceBase :
        Microsoft.Phone.Maps.Controls.TileSource
    {
        protected GoogleMapsTileSourceBase(string uriFormat)
            : base(uriFormat)
        { }
        public override System.Uri GetUri(int x, int y, int zoomLevel)
        {
            return new System.Uri(string.Format(UriFormat,
                new System.Random().Next() % 4, x, y, zoomLevel));
        }
    }

    public class GoogleMapsRoadTileSource : GoogleMapsTileSourceBase
    {
        public GoogleMapsRoadTileSource()
            : base("http://mt1.google.com/vt/lyrs=m@62&hl=ch&x={1}&y={2}&z={3}")
        { }
    }

    public class GoogleMapsAerialTileSource : GoogleMapsTileSourceBase
    {
        public GoogleMapsAerialTileSource()
            : base("http://khm{0}.google.com/kh/v=62&x={1}&y={2}&z={3}&s=")
        { }
    }

    public class GoogleMapsLabelsTileSource : GoogleMapsTileSourceBase
    {
        public GoogleMapsLabelsTileSource()
            : base("http://mt{0}.google.com/vt/lyrs=h@128&hl=ch&x={1}&y={2}&z={3}&s=")
        { }
    }
}
