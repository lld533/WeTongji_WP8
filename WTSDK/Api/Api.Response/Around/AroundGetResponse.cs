using System;

namespace WeTongji.Api.Response
{
    public class AroundGetResponse : WTResponse
    {
        public AroundGetResponse() 
        {
            Around = null;
        }

        public WeTongji.Api.Domain.Around Around { get; set; }
    }
}
