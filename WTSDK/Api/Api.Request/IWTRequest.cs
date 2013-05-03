using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public interface IWTRequest<T> where T : WeTongji.Api.WTResponse
    {
        String GetApiName();
        IDictionary<String, String> GetParameters();
        void SetAdditionalParameter(String key, Object value);
        void Validate();
    }
}
