using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class ActivityScheduleRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        public ActivityScheduleRequest() 
        { 
            Id = -1;
            base.dict["Id"] = "-1";
        }

        public int Id { get; set; }

        public override String GetApiName()
        {
            return "Activity.Schedule";
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
    }
}
