using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserLogOnRequest<T> : WTRequest<T> where T : WeTongji.Api.Response.UserLogOnResponse
    {
        #region [Constructor]

        public UserLogOnRequest()
        {
            NO = String.Empty;
            Password = String.Empty;

            base.dict["NO"] = String.Empty;
            base.dict["Password"] = String.Empty;
        }

        #endregion

        #region [Property]

        public String NO { get; set; }
        public String Password { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "User.LogOn";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["NO"] = NO;
            base.dict["Password"] = Password;

            return base.dict;
        }

        public override void Validate()
        {
            #region [NO]

            if (String.IsNullOrEmpty(NO) || String.IsNullOrWhiteSpace(NO))
            {
                throw new ArgumentNullException("NO", "NO can NOT be empty.");
            }

            foreach (var c in NO)
            {
                if (!Char.IsDigit(c))
                {
                    throw new ArgumentOutOfRangeException("NO", "NO can only contain digit.");
                }
            }

            #endregion

            #region [Password]

            if (String.IsNullOrEmpty(Password) || Password.Length < 6)
            {
                throw new ArgumentNullException("Password", "Password can NOT be empty.");
            }

            #endregion
        }

        #endregion
    }
}
