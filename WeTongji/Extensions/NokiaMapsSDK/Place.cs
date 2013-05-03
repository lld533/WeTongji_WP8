using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NokiaMapSDK;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Place
    {
        public double[] position { get; set; }

        public NokiaMapSDK.GeoPoint GeoPosition
        {
            get
            {
                return new GeoPoint() { Latitude = position[0], Longitude = position[1] };
            }
        }

        public UInt64 distance { get; set; }

        public String title { get; set; }

        public Category category { get; set; }

        public String icon { get; set; }

        /// <remarks>
        /// rich text
        /// </remarks>
        public String vicinity { get; set; }

        /// <summary>
        /// Unknown by far. It should be null.
        /// </summary>
        public Object[] having { get; set; }

        public String type { get; set; }

        public URN.NLP_Type nlp_type { get { return type.ToNLP_Type(); } }

        public String href { get; set; }

        public String placeId { get; set; }

        public RelatedPlaces related { get; set; }

        #region [Display Properties]

        public String DisplayDistance
        {
            get
            {
                if (distance < 50L)
                    return String.Format("<50m");
                else if (distance < 1000L)
                    return String.Format("{0}m", distance / 10L * 10L);
                else if (distance > 50000L)
                    return String.Format(">50km");
                else
                    return String.Format("~{0}km", (float)(distance / 100L * 100L) / 1000.0F);
            }
        }

        #endregion
    }
}
