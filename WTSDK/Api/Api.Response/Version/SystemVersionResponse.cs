using System;

namespace WeTongji.Api.Response
{
    public class SystemVersionResponse : WTResponse
    {
        public SystemVersionResponse() { Version = null; }

        /// <summary>
        /// Version equals null if current version is the latest.
        /// </summary>
        public WeTongji.Api.Domain.Version Version { get; set; }
    }
}
