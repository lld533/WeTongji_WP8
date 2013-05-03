using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public class ActivitiesGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.ActivitiesGetResponse
    {
        #region [Constructor]

        public ActivitiesGetRequest()
        {
            Channel_Ids = String.Empty;
            SortEnumerator = Util.SortEnumerator.created_at;
            IsAsc = false;
            Expire = false;

            base.dict["Channel_Ids"] = String.Empty;
            base.dict["Sort"] = "`created_at` DESC";
            base.dict["Expire"] = Expire ? "1" : "0";
        }

        #endregion

        #region [Property]

        public String Channel_Ids { get; set; }
        public WeTongji.Api.Util.SortEnumerator SortEnumerator { get; set; }
        public Boolean IsAsc { get; set; }
        public Boolean Expire { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "Activities.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            var result = new Dictionary<String, String>(base.dict);

            if (!String.IsNullOrEmpty(Channel_Ids))
                result["Channel_Ids"] = Channel_Ids;
            else
                result.Remove("Channel_Ids");

            result["Sort"] = String.Format("`{0}` {1}", SortEnumerator.ToString(), ((IsAsc) ? "ASC" : "DESC"));
            result["Expire"] = Expire ? "1" : "0";

            return result;
        }

        public override void Validate()
        {
            if (!String.IsNullOrEmpty(Channel_Ids))
            {
                var channels = Channel_Ids.Split(',');
                int tmp;
                foreach (var channel in channels)
                {
                    if (String.IsNullOrEmpty(channel))
                    {
                        throw new ArgumentException("Channel_Ids");
                    }
                    if (!int.TryParse(channel, out tmp))
                    {
                        throw new ArgumentException("Channel_Ids");
                    }
                    if (tmp < 0)
                    {
                        throw new ArgumentOutOfRangeException("Channel_Ids");
                    }
                }
            }
        }

        #endregion
    }
}
