using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class InformationFavoriteRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]

        public InformationFavoriteRequest()
        {
            base.dict["Id"] = (Id = -1).ToString();
        }

        #endregion

        #region [Property]

        public int Id { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "Information.Favorite";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Id"] = JsonConvert.SerializeObject(Id);

            return base.dict;
        }

        public override void Validate() 
        {
            if (Id < 0)
                throw new ArgumentOutOfRangeException("Id", "Id should be a positive integer.");
        }

        #endregion
    }
}
