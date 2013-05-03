using System;
using System.Text;
using System.Xml.Linq;

namespace WeTongji.Utility
{
    public static class AppVersion
    {
        public static String Current
        {
            get 
            {
                return XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
            }
        }
    }
}
