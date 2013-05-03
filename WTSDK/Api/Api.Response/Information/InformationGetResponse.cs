using System;

namespace WeTongji.Api.Response
{
    public class InformationGetResponse : WTResponse
    {
        public WeTongji.Api.Domain.Information Information { get; set; }
    }
}
