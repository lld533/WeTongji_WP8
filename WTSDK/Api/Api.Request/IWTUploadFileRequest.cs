using System;
using System.Collections.Generic;

namespace WeTongji.Api.Request
{
    public class WTUploadFileRequest<T> : WTRequest<T> where T : WTResponse
    {
        public virtual IEnumerable<MyToolkit.Networking.HttpPostFile> GetFiles() { return null; }
    }
}
