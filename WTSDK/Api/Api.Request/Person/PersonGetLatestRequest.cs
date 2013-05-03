using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class PersonGetLatestRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.PersonGetLatestResponse
    {
        #region [Constructor]

        public PersonGetLatestRequest() 
        { 
        }

        #endregion
        
        #region [Overridden]

        public override String GetApiName()
        {
            return "Person.GetLatest";
        }

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override void Validate()
        {
        }

        #endregion
    }
}
