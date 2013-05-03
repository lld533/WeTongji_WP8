using System;

namespace WeTongji.Api.Response
{
    public class ForStaffsGetResponse : WTResponse
    {
        public ForStaffsGetResponse()
        {
            ForStaffs = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.ForStaff[] ForStaffs { get; set; }
        public int NextPager { get; set; }
    }
}
