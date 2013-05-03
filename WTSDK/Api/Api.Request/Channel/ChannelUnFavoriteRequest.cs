using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public class ChannelUnFavoriteRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]

        public ChannelUnFavoriteRequest()
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
            return "Channel.UnFavorite";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Id"] = JsonConvert.SerializeObject(Id);
            return base.dict;
        }

        public override void Validate()
        {
            if (0 > Id)
                throw new ArgumentOutOfRangeException("Id");
        }


        #endregion


    }
}
