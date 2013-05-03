using System;

namespace WeTongji.Api.Domain
{
    public interface IWTObjectExt
    {
        void SetObject(WTObject obj);
        WTObject GetObject();
        Type ExpectedType();
    }
}
