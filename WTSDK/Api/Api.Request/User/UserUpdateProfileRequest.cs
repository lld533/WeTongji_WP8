using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserUpdateProfileRequest<T> : WTRequest<T>, IWTUploadRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Property]

        public WeTongji.Api.Domain.UserProfile UserProfile { get; set; }

        #endregion

        #region [Overridden]

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override String GetApiName()
        {
            return "User.Update.Profile";
        }

        public override void Validate()
        {
            if (UserProfile == null)
                throw new ArgumentNullException("UserProfile");
        }

        #endregion

        #region [Implementation]

        public System.IO.Stream GetRequestStream()
        {
            var dict = new Dictionary<String, Object>();

            dict["UserProfile"] = UserProfile;

            var str = String.Format("{0}={1}", "UserProfile", JsonConvert.SerializeObject(dict));
            var stream = new System.IO.MemoryStream();

            System.IO.StreamWriter sw = new System.IO.StreamWriter(stream);
            sw.Write(str);
            sw.Flush();
            return stream;
        }

        public String GetContentType()
        {
            return null;
        }

        #endregion
    }
}
