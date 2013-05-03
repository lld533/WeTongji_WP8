using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public class ChannelFavoriteRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]

        public ChannelFavoriteRequest() 
        {
            Id = -1;
            base.dict["Id"] = "-1";
        }

        #endregion

        #region [Property]

        public int Id { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "Channel.Favorite";
        }

        public override IDictionary<String, String> GetParameters()
        {
            var result = new Dictionary<String, String>();
            result["Id"] = JsonConvert.SerializeObject(Id);
            return result;
        }

        public override void Validate()
        {
            if (0 > Id)
                throw new ArgumentOutOfRangeException("Id");
        }

        #endregion
    }
}
