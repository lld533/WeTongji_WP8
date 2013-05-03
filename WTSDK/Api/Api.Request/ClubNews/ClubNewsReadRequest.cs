using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class ClubNewsReadRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]

        public ClubNewsReadRequest()
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
            return "ClubNews.Read";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Id"] = JsonConvert.SerializeObject(Id);

            return base.dict;
        }

        public override void Validate()
        {
            if (Id < 0)
                throw new ArgumentOutOfRangeException("Id");
        }

        #endregion
    }
}
