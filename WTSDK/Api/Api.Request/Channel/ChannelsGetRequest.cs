using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class ChannelsGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.ChannelsGetResponse
    {
        #region [Constructor]

        public ChannelsGetRequest() { }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "Channels.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }

        #endregion
    }
}
