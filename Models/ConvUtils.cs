using System;
using System.Collections.Generic;
using System.Text;

namespace csmon.Models
{
    public static class ConvUtils
    {
        public static bool AllowNegativeTime;

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string GetAge(long time)
        {
            //return UnixTimeStampToDateTime(time).ToString("dd.MM.yyyy hh:mm:ss.fff");
            var span = DateTime.Now - UnixTimeStampToDateTime(time);
            if (!AllowNegativeTime && span < TimeSpan.Zero) span = TimeSpan.Zero;
            var res = span.Days != 0 ? span.Days + "d " : "";
            res += span.Hours != 0 || span.Days != 0 ? span.Hours + "h " : "";
            res += span.Minutes + "m " + span.Seconds + "s";
            return res;
        }

        public static string ConvertHash(byte[] hash)
        {
            var hex = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] ConvertHashBack(string hash)
        {
            var bytes = new List<byte>();
            for (var i = 0; i < hash.Length / 2; i++)
                bytes.Add(Convert.ToByte(hash.Substring(i * 2, 2), 16));
            return bytes.ToArray();
        }

        public static string FormatAmount(Amount value)
        {
            if (value.Fraction == 0) return $"{value.Integral}.0";

            var fraction = value.Fraction.ToString();
            while (fraction.Length < 18)
                fraction = "0" + fraction;
            return $"{value.Integral}.{fraction}";
        }
    }
}
