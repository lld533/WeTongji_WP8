using System;

namespace WeTongji.Api.Response
{
    public class InformationGetListResponse : WTResponse
    {
        public WeTongji.Api.Domain.Information[] Information { get; set; }
        public int NextPager { get; set; }
    }
}
