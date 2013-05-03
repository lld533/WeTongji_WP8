using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WeTongji.Api.Request
{
    public class InformationGetListRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.InformationGetListResponse
    {
        public InformationGetListRequest()
        {
            SortEnumerator = Util.SortEnumerator.created_at;
            IsAsc = false;
            base.dict["Sort"] = @"`created_at` desc";

            Categories = new List<WeTongji.Api.Util.InformationEnumerator>();
            base.dict["Category_Ids"] = String.Empty;
        }

        /// <summary>
        /// Optional
        /// </summary>
        public WeTongji.Api.Util.SortEnumerator SortEnumerator { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public Boolean IsAsc { get; set; }

        /// <summary>
        /// Optional, refers to Category_Ids
        /// </summary>
        public List<WeTongji.Api.Util.InformationEnumerator> Categories { get; private set; }

        public override String GetApiName()
        {
            return "Information.GetList";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Sort"] = String.Format("`{0}` {1}", SortEnumerator.ToString(), (IsAsc ? "asc" : "desc"));

            var dict = new Dictionary<String, String>(base.dict);

            if (Categories.Count > 0)
            {
                int v = 0;
                foreach (var info in Categories)
                {
                    v |= (int)info;
                }

                List<String> list = new List<String>();
                if ((v & (int)WeTongji.Api.Util.InformationEnumerator.AroundNews) == (int)WeTongji.Api.Util.InformationEnumerator.AroundNews)
                {
                    list.Add(((int)WeTongji.Api.Util.InformationEnumeratorValue.AroundNewsValue).ToString());
                }
                if ((v & (int)WeTongji.Api.Util.InformationEnumerator.ClubNews) == (int)WeTongji.Api.Util.InformationEnumerator.ClubNews)
                {
                    list.Add(((int)WeTongji.Api.Util.InformationEnumeratorValue.ClubNewsValue).ToString());
                }
                if ((v & (int)WeTongji.Api.Util.InformationEnumerator.ForStaffNews) == (int)WeTongji.Api.Util.InformationEnumerator.ForStaffNews)
                {
                    list.Add(((int)WeTongji.Api.Util.InformationEnumeratorValue.ForStaffNewsValue).ToString());
                }
                if ((v & (int)WeTongji.Api.Util.InformationEnumerator.SchoolNews) == (int)WeTongji.Api.Util.InformationEnumerator.SchoolNews)
                {
                    list.Add(((int)WeTongji.Api.Util.InformationEnumeratorValue.SchoolNewsValue).ToString());
                }

                dict["Category_Ids"] = list.Aggregate((s1, s2) => s1 + "," + s2);
            }
            else
                dict.Remove("Category_Ids");


            return dict;
        }
    }
}
