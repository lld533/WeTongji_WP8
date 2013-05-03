using System;

namespace WeTongji.Api.Response
{
    public class ChannelsGetResponse : WTResponse
    {
        public ChannelsGetResponse() { Channels = null; }

        public WeTongji.Api.Domain.Channel[] Channels { get; set; }
    }
}
