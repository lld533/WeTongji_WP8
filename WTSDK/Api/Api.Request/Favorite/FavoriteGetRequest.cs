using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class FavoriteGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.FavoriteGetResponse
    {
        #region [Constructor]
        public FavoriteGetRequest() { }
        #endregion

        #region [Overridden]
        public override String GetApiName()
        {
            return "Favorite.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate(){ }
        #endregion
    }
}