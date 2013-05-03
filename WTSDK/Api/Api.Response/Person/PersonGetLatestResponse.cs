using System;

namespace WeTongji.Api.Response
{
    public class PersonGetLatestResponse : WTResponse
    {
        public PersonGetLatestResponse()
        {
            Person = null;
        }

        public WeTongji.Api.Domain.Person Person { get; set; }
    }
}
