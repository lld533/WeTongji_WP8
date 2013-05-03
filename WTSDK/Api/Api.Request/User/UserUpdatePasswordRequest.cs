using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserUpdatePasswordRequest<T> : WTRequest<T>, IWTUploadRequest<T> where T : WeTongji.Api.Response.UserUpdatePasswordResponse
    {
        #region [Constructor]

        public UserUpdatePasswordRequest()
        {
            NewPassword = String.Empty;
            OldPassword = String.Empty;

            base.dict["New"] = String.Empty;
            base.dict["Old"] = String.Empty;
        }

        #endregion

        #region [Property]

        public String NewPassword { get; set; }
        public String OldPassword { get; set; }

        #endregion

        #region [Overridden]

        public KeyValuePair<String, WeTongji.Api.Util.FileItem> GetFileParameter()
        {
            return new KeyValuePair<String, WeTongji.Api.Util.FileItem>();
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["Old"] = OldPassword;
            base.dict["New"] = NewPassword;
            return base.dict;
        }

        public override String GetApiName()
        {
            return "User.Update.Password";
        }

        public override void Validate()
        {
            if (String.IsNullOrEmpty(OldPassword))
                throw new ArgumentNullException("OldPassword");
            if (String.IsNullOrEmpty(NewPassword))
                throw new ArgumentNullException("NewPassword");

            //...To do @_@ check length of Old and New Password
        }

        #endregion

        #region [Implementation]

        public System.IO.Stream GetRequestStream()
        {
            return null;
        }

        public String GetContentType()
        {
            return null;
        }

        #endregion
    }
}
