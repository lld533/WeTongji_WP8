using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace WeTongji.Api.Request
{
    public class UserUpdateRequest<T> : WTRequest<T>, IWTUploadRequest<T> where T : WeTongji.Api.WTResponse
    {

        #region [Constructor]

        public UserUpdateRequest() { }

        #endregion

        #region [Property]

        public WeTongji.Api.Domain.User User
        {
            get;
            set;
        }

        #endregion

        #region [Implementation]

        public override IDictionary<String, String> GetParameters()
        {
            return new Dictionary<String,String>(base.dict);
        }

        public override String GetApiName()
        {
            return "User.Update";
        }

        public override void Validate()
        {
            if (User == null)
            {
                throw new ArgumentNullException("User");
            }
        }

        #endregion

        public System.IO.Stream GetRequestStream()
        {
            var dict = new Dictionary<String, Object>();
            var dict_param = new Dictionary<String, String>();

            dict_param["Email"] = User.Email;
            dict_param["Phone"] = User.Phone;
            dict_param["QQ"] = User.QQ;
            dict_param["SinaWeibo"] = User.SinaWeibo;

            dict["User"] = dict_param;

            var str = String.Format("{0}={1}", "User", JsonConvert.SerializeObject(dict));
            var stream = new System.IO.MemoryStream();

            System.IO.StreamWriter sw = new System.IO.StreamWriter(stream);
            sw.Write(str);
            sw.Flush();
            return stream;
        }

        public String GetContentType()
        {
            return "application/json";
        }
    }
}
