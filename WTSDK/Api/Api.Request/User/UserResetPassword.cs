using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class UserResetPassword<T> : WTRequest<T> where T : WeTongji.Api.WTResponse
    {
        public UserResetPassword() 
        {
            NO = String.Empty;
            Name = String.Empty;

            base.dict["NO"] = String.Empty;
            base.dict["Name"] = String.Empty;
        }

        public String NO { get; set; }
        public String Name { get; set; }

        public override String GetApiName()
        {
            return "User.Reset.Password";
        }

        public override IDictionary<String, String> GetParameters()
        {
            base.dict["NO"] = NO;
            base.dict["Name"] = Name;
            return base.dict;
        }

        public override void Validate()
        {
            if (String.IsNullOrEmpty(NO))
                throw new ArgumentNullException("NO");
            if (String.IsNullOrEmpty(Name))
                throw new ArgumentNullException("Name");

            //...To do @_@ check NO and Name length
        }

    }
}
