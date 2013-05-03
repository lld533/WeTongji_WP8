using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class SystemVersionRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.SystemVersionResponse
    {
        #region [Constructor]

        public SystemVersionRequest() { }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "System.Version";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }
        
        #endregion

    }
}
