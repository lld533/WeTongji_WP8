using System;

namespace WeTongji.Api.Response
{
    public class ForStaffGetResponse : WTResponse
    {
        public ForStaffGetResponse() 
        {
            ForStaff = null;
        }

        public WeTongji.Api.Domain.ForStaff ForStaff { get; set; }
    }
}
