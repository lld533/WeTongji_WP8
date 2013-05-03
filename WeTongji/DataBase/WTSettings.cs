using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace WeTongji.DataBase
{
    public class WTSettings
    {
        public String UID { get; set; }

        public Byte[] CryptPassword { get; set; }

        public Boolean HintOnExit { get; set; }

        public Boolean AutoRefresh { get; set; }

        public WTSettings()
        {
            HintOnExit = true;
            AutoRefresh = true;
        }
    }


    public static class WTSettingsExt
    {
        private static readonly String noise = "WeTongji_WP";

        public static readonly String SettingsFileName = "Settings.txt";

        public static String GetSerializedString(this WTSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            return JsonConvert.SerializeObject(settings);
        }

        /// <summary>
        /// Deserialize settings file string
        /// </summary>
        /// <param name="str"></param>
        /// <returns>
        /// WTSettings instance if succeeded, otherwise returns null.
        /// </returns>
        public static WTSettings DeserializeSettings(this String str)
        {
            WTSettings settings = null;

            try
            {
                settings = JsonConvert.DeserializeObject<WTSettings>(str);
            }
            catch 
            {
                settings = new WTSettings();
            }

            return settings;
        }

        public static Byte[] GetCryptPassword(this String original)
        {
            return System.Security.Cryptography.ProtectedData.Protect(Encoding.Unicode.GetBytes(original), Encoding.Unicode.GetBytes(noise));
        }

        public static String GetOriginalPassword(this Byte[] cryptPw)
        {
            if (cryptPw == null)
                throw new ArgumentNullException();

            try
            {
                var bytes = System.Security.Cryptography.ProtectedData.Unprotect(cryptPw, Encoding.Unicode.GetBytes(noise));
                return Encoding.Unicode.GetString(bytes, 0, bytes.Count());
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
