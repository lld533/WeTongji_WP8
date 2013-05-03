using System;

namespace WeTongji.Api.Response
{
    public class ClubNewsGetListResponse : WTResponse
    {
        public ClubNewsGetListResponse()
        {
            ClubNews = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.ClubNews[] ClubNews { get; set; }
        public int NextPager { get; set; }
    }
}
