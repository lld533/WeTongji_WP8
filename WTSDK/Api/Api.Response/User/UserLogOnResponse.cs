using System;

namespace WeTongji.Api.Response
{
    public class UserLogOnResponse : WeTongji.Api.WTResponse
    {
        public UserLogOnResponse() 
        {
            User = null;
            Session = String.Empty;
        }

        public WeTongji.Api.Domain.User User { get; set; }
        public String Session { get; set; }
    }
}
