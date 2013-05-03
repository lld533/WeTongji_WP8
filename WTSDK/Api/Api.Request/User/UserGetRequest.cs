using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.UserGetResponse
    {
        #region [Constructor]

        public UserGetRequest() { }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "User.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }

        #endregion
    }
}
