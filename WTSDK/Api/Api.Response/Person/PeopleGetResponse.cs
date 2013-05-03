using System;

namespace WeTongji.Api.Response
{
    public class PeopleGetResponse : WTResponse
    {
        public PeopleGetResponse() 
        {
            People = null;
            NextPager = -1;
        }

        public WeTongji.Api.Domain.Person[] People { get; set; }
        public int NextPager { get; set; }
    }
}
