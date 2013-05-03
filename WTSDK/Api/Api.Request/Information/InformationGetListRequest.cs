using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class InformationGetListRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.InformationGetListResponse
    {
        public InformationGetListRequest() 
        {
            SortEnumerator = Util.SortEnumerator.created_at;
            IsAsc = false;
            base.dict["Sort"] = @"`created_at` desc";

            Category_Ids = String.Empty;
            base.dict["Category_Ids"] = String.Empty;
        }

        public WeTongji.Api.Util.SortEnumerator SortEnumerator { get; set; }
        public Boolean IsAsc { get; set; }

        public String Category_Ids { get; set; }

        public override String GetApiName()
        {
            return "Information.GetList";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Sort"] = String.Format("`{0}` {1}", SortEnumerator.ToString(), (IsAsc ? "asc" : "desc"));

            var dict = new Dictionary<String, String>(base.dict);
            if (String.IsNullOrEmpty(Category_Ids))
                dict.Remove("Category_Ids");

            return dict;
        }

        public override void Validate()
        {
            if (!String.IsNullOrEmpty(Category_Ids))
            {
                int cid;
                if (int.TryParse(Category_Ids, out cid))
                {
                    if (cid < 1 || cid > 4)
                        throw new ArgumentOutOfRangeException("Category_Ids");
                }
                else
                    throw new ArgumentException("Category_Ids");
            }
        }
    }
}
