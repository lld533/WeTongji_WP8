using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserProfileRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.UserProfileResponse
    {
        #region [Overridden]

        public override String GetApiName()
        {
            return "User.Profile";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }

        #endregion
    }
}
