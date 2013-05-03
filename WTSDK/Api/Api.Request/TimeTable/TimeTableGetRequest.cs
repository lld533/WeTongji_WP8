using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class TimeTableGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.TimeTableGetResponse
    {
        #region [Constructor]
        public TimeTableGetRequest() { }
        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "TimeTable.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate() { }
        
        #endregion

    }
}
