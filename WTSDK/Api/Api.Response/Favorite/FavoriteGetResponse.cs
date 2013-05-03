using System;

namespace WeTongji.Api.Response
{
    public class FavoriteGetResponse : WTResponse
    {
        public FavoriteGetResponse() 
        {
            Activities = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.Activity[] Activities { get; set; }
        public WeTongji.Api.Domain.Person[] People { get; set; }
        public WeTongji.Api.Domain.SchoolNews[] SchoolNews { get; set; }
        public WeTongji.Api.Domain.ClubNews[] ClubNews { get; set; }
        public WeTongji.Api.Domain.Around[] Arounds { get; set; }
        public WeTongji.Api.Domain.ForStaff[] ForStaffs { get; set; }

        public int NextPager { get; set; }
    }
}
