using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public class ForStaffGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.ForStaffGetResponse
    {
        #region [Constructor]

        public ForStaffGetRequest()
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
            return "ForStaff.Get";
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
