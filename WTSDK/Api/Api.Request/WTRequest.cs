using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace WeTongji.Api.Request
{
    public abstract class WTRequest<t> : IWTRequest<t> where t : WTResponse
    {
        protected IDictionary<String, String> dict = new Dictionary<String,String>();

        /// <returns>String.Empty</returns>
        public virtual String GetApiName()
        {
            return String.Empty;
        }

        /// <returns>null</returns>
        public virtual IDictionary<String, String> GetParameters()
        {
            return null;
        }

        public virtual void SetAdditionalParameter(String key, Object value) 
        {
            if (dict.ContainsKey(key))
            {
                throw new ArgumentOutOfRangeException("key", String.Format("Key \"{0}\" exists.", key));
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            dict[key] = value.ToString();
        }
        
        /// <summary>
        /// Do nothing.
        /// </summary>
        public virtual void Validate() { return; }
    }
}
