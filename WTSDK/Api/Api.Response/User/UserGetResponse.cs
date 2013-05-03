using System;

namespace WeTongji.Api.Response
{
    public class UserGetResponse : WTResponse
    {
        public UserGetResponse()
        {
            User = null;
        }

        public WeTongji.Api.Domain.User User { get; set; }
    }
}
