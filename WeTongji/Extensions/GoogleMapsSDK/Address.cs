using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace WeTongji.Extensions.GoogleMapsSDK
{
    public class Address
    {
        public AddressComponent[] address_components { get; set; }

        public String formatted_address { get; set; }

        public Geometry geometry { get; set; }

        public String[] types { get; set; }

        public Boolean partial_match { get; set; }

        public AddressType[] GetAddressTypes()
        {
            int count = 0;
            Boolean hasOtherType = false;
            if (types == null || (count = types.Count()) == 0)
            {
                return null;
            }

            var result = new AddressType[count];

            for (int i = 0; i < count; ++i)
            {
                try
                {
                    result[i] = JsonConvert.DeserializeObject<AddressType>(types[i]);
                }
                catch
                {
                    result[i] = AddressType.other;
                    hasOtherType = true;
                }
            }

            if (hasOtherType)
            {
                var q = from AddressType at in result
                        where at != AddressType.other
                        select at;
                q = q.Union(new AddressType[] { AddressType.other });
                return q.ToArray();
            }
            else
                return result;
        }
    }
}
