using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public interface IWTUploadRequest<T> : IWTRequest<T> where T : WeTongji.Api.WTResponse
    {
        System.IO.Stream GetRequestStream();
        String GetContentType();
    }
}
