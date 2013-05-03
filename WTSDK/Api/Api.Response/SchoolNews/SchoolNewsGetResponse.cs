using System;

namespace WeTongji.Api.Response
{
    public class SchoolNewsGetResponse : WTResponse
    {
        public SchoolNewsGetResponse() 
        {
            SchoolNews = null;
        }

        public WeTongji.Api.Domain.SchoolNews SchoolNews { get; set; }
    }
}
