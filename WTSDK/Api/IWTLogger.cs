using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Api
{
    public interface IWTLogger
    {
        void Error(String message);
        void Info(String message);
        void Warn(String message);
    }
}
