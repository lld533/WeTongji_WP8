using System;

namespace WeTongji.Api.Response
{
    public class ClubNewsGetResponse : WTResponse
    {
        public ClubNewsGetResponse() 
        {
            ClubNews = null;
        }

        public WeTongji.Api.Domain.ClubNews ClubNews { get; set; }
    }
}
