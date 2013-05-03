using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class NokiaMapsTile : Microsoft.Phone.Maps.Controls.TileSource
    {
        private int _server;
        private char _mapmode;
        private NokiaMapsTileTypes _tiletypes;

        private string token = "FbGjsdJkxVvTF2OcdOuBGA ";
        private string app_id = "Or94PrudKATbv6vmtnzb ";
        private string language = "ZHS";

        public NokiaMapsTileTypes TileTypes
        {
            get { return _tiletypes; }
            set
            {
                _tiletypes = value;
                MapMode = MapModeConverter(value);
            }
        }
        public char MapMode
        {
            get { return _mapmode; }
            set { _mapmode = value; }
        }
        public int Server
        {
            get { return _server; }
            set { _server = value; }
        }

        public NokiaMapsTile()
        {
            UriFormat = @"http://maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/normal.day/{0}/{1}/{2}/256/png8?token={3}&app_id={4}&lg={5}";
            Server = 0;
        }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            if (zoomLevel > 0)
            {
                var Url = string.Format(UriFormat, zoomLevel, x, y, token, app_id, language);
                return new Uri(Url);
            }
            return null;
        }

        private char MapModeConverter(NokiaMapsTileTypes tiletype)
        {
            switch (tiletype)
            {
                case NokiaMapsTileTypes.Hybrid:
                    {
                        return 'y';
                    }
                case NokiaMapsTileTypes.Physical:
                    {
                        return 't';
                    }
                case NokiaMapsTileTypes.Satellite:
                    {
                        return 's';
                    }
                case NokiaMapsTileTypes.Street:
                    {
                        return 'm';
                    }
                case NokiaMapsTileTypes.WaterOverlay:
                    {
                        return 'r';
                    }
            }
            return ' ';
        }
        public enum NokiaMapsTileTypes
        {
            Hybrid,
            Physical,
            Street,
            Satellite,
            WaterOverlay
        }
    }

}