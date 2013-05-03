using System;

namespace WeTongji.Api.Response
{
    public class SchoolNewsGetListResponse : WTResponse
    {
        public SchoolNewsGetListResponse()
        {
            SchoolNews = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.SchoolNews[] SchoolNews { get; set; }
        public int NextPager { get; set; }
    }
}
