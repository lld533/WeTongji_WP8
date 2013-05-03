using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WeTongji.Api.Request
{
    public class UserActiveRequest<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        #region [Constructor]
        
        public UserActiveRequest()
        {
            NO = String.Empty;
            Name = String.Empty;
            Password = String.Empty;

            base.dict["NO"] = String.Empty;
            base.dict["Name"] = String.Empty;
            base.dict["Password"] = String.Empty;
        }

        #endregion

        #region [Properties]

        public String NO { get; set; }
        public String Name { get; set; }
        public String Password { get; set; }

        #endregion

        #region [Overridden]

        public override String GetApiName()
        {
            return "User.Active";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["NO"] = NO;
            base.dict["Name"] = Name;
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

            //...To do @_@ check length of NO

            #endregion

            #region [Name]

            if (String.IsNullOrEmpty(Name) || String.IsNullOrEmpty(Name))
            {
                throw new ArgumentNullException("Name", "Name can NOT be empty.");
            }

            #endregion

            #region [Password]

            if (String.IsNullOrEmpty(Password) || String.IsNullOrWhiteSpace(Password))
            {
                throw new ArgumentNullException("Password", "Password can NOT be empty.");
            }

            //...To do @_@ check password length

            #endregion
        }

        #endregion
    }
}
