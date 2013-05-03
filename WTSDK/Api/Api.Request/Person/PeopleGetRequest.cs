using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class PeopleGetRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.PeopleGetResponse
    {
        #region [Constructor]

        public PeopleGetRequest()
        {
            SortEnumerator = Util.SortEnumerator.created_at;
            IsAsc = false;

            base.dict["Sort"] = @"`created_at` desc";
        }

        #endregion

        #region [Property]

        public WeTongji.Api.Util.SortEnumerator SortEnumerator { get; set; }
        public Boolean IsAsc { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "People.Get";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Sort"] = String.Format("`{0}` {1}", SortEnumerator.ToString(), (IsAsc ? "asc" : "desc"));
            return base.dict;
        }

        public override void Validate() { }

        #endregion
    }
}
