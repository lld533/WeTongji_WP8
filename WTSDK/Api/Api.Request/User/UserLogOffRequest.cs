using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserLogOffRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]

        public UserLogOffRequest() { }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "User.LogOff";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }

        #endregion

    }
}
