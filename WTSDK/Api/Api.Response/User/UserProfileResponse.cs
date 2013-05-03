using System;

namespace WeTongji.Api.Response
{
    public class UserProfileResponse : WeTongji.Api.WTResponse
    {
        WeTongji.Api.Domain.UserProfile UserProfile { get; set; }
    }
}
