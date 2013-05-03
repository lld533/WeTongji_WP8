using System;

namespace WeTongji.Api.Response
{
    public class AroundsGetResponse : WTResponse
    {
        public AroundsGetResponse()
        {
            Arounds = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.Around[] Arounds { get; set; }
        public int NextPager { get; set; }
    }
}
