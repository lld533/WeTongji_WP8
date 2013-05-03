using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WeTongji.Utility
{
    public class UnicodeString
    {
         public static string StringToUnicode(string srcText)
         {
             string dst = "";
             char[] src = srcText.ToCharArray();
             for (int i = 0; i < src.Length; i++)
             {
                 byte[] bytes = Encoding.Unicode.GetBytes(src[i].ToString());
                 string str = @"\u" + bytes[1].ToString("X2") + bytes[0].ToString("X2");
                 dst += str;
             }
             return dst;
         }


         public static string UnicodeToString(string srcText)
         {
             string dst = "";
             string src = srcText;
             int len = srcText.Length / 6;
             for (int i = 0; i < len; i++)
             {
                 string str = "";
                 str = src.Substring(0, 6).Substring(2);
                 src = src.Substring(6);
                 byte[] bytes = new byte[2];
                 bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), NumberStyles.HexNumber).ToString());
                 bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), NumberStyles.HexNumber).ToString());
                 dst += Encoding.Unicode.GetString(bytes,0, bytes.Count());
             }
             return dst;
         }

    }
}
