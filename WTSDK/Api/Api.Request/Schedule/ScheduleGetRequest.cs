using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public class ScheduleGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.ScheduleGetResponse
    {
        #region [Constructor]

        public ScheduleGetRequest() 
        {
            Begin = DateTime.MinValue;
            End = DateTime.MinValue;

            base.dict["Begin"] = Begin.ToString("yyyy/MM/dd");
            base.dict["End"] = End.ToString("yyyy/MM/dd");
        }

        #endregion

        #region [Property]

        public DateTime Begin { get; set; }
        public DateTime End { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "Schedule.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Begin"] = Begin.ToString("yyyy/MM/dd");
            base.dict["End"] = End.ToString("yyyy/MM/dd");
            return base.dict;
        }

        public override void Validate()
        {
            if (Begin == DateTime.MinValue)
                throw new ArgumentOutOfRangeException("Begin");
            if (End < Begin)
                throw new ArgumentOutOfRangeException("End");
        }

        #endregion
    }
}
