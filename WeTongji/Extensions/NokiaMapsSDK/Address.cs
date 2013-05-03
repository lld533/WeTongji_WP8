using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    public class Address
    {
        /// <summary>
        /// House Number
        /// </summary>
        public UInt32 house { get; set; }

        public String street { get; set; }

        public String postalCode { get; set; }

        public String district { get; set; }

        public String city { get; set; }

        public String state { get; set; }

        public String stateCode { get; set; }

        public String countryCode { get; set; }

        public String country { get; set; }

        /// <remarks>
        /// rich text
        /// </remarks>
        public String text { get; set; }

        public String href { get; set; }
    }
}
