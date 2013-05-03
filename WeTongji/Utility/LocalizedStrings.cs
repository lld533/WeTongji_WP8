using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Reflection;

namespace WeTongji.Utility
{
    public class LocalizedStrings
    {
        public LocalizedStrings() { }

        private static StringLibrary localizedResources = new StringLibrary();

        public StringLibrary LocalizedResources { get { return localizedResources; } }
    }
}
