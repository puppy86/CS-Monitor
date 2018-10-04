using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace csmon.Models
{
    public static class ConvUtils
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string GetAge(long time)
        {
            //return UnixTimeStampToDateTime(time).ToString("dd.MM.yyyy hh:mm:ss.fff");
            if (time == 0) return "0";
            var span = DateTime.Now - UnixTimeStampToDateTime(time);
            return AgeStr(span);
        }

        public static string AgeStr(TimeSpan span)
        {
            if (!Settings.AllowNegativeTime && span < TimeSpan.Zero) span = TimeSpan.Zero;
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

        public static string ConvertHashAscii(byte[] hash)
        {
            return Encoding.ASCII.GetString(hash);
        }

        public static byte[] ConvertHashBackAscii(string hash)
        {
            return Encoding.ASCII.GetBytes(hash);
        }

        public static string ConvertHashPartial(string hash)
        {
            return Base58Encoding.Encode(ConvertHashBack(hash));
        }

        public static string ConvertHashBackPartial(string hash)
        {
            return ConvertHash(Base58Encoding.Decode(hash));
        }

        public static string FormatAmount(NodeApi.Amount value)
        {
            if (value.Fraction == 0) return $"{value.Integral}.0";

            var fraction = value.Fraction.ToString();
            while (fraction.Length < 18)
                fraction = "0" + fraction;
            
            return $"{value.Integral}.{fraction.TrimEnd('0')}";
        }

        public static string FormatAmount(Release.Amount value)
        {
            if (value.Fraction == 0) return $"{value.Integral}.0";

            var fraction = value.Fraction.ToString();
            while (fraction.Length < 18)
                fraction = "0" + fraction;

            return $"{value.Integral}.{fraction.TrimEnd('0')}";
        }

        public static string FormatSrc(string code)
        {
            if (code.Contains(Environment.NewLine) || code.Contains("\n")) return code;

            var sb = new StringBuilder();
            const int ident = 4;
            #pragma warning disable 219
            int level = 0, line = 1;
            var newl = false;
            foreach (var c in code)
            {
                if (c == '{')
                {
                    sb.Append(c);
                    level++;
                    newl = true;
                }
                else if (c == '}')
                {
                    level--;
                    sb.AppendLine();
                    sb.Append(' ', level * ident);
                    sb.Append(c);
                    newl = true;
                }
                else if (c == ';')
                {
                    sb.Append(c);
                    newl = true;
                }
                else if (c == ' ' && newl)
                {
                }
                else
                {
                    if (newl)
                    {
                        sb.AppendLine();
                        sb.Append(' ', level * ident);
                        newl = false;
                    }
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static int GetNumPages(int count, int numPerPage)
        {
            if (count <= 0) return 1;
            if (count % numPerPage == 0) return count / numPerPage;
            return count / numPerPage + 1;
        }

        public static string GetIpCut(string ip)
        {
            if (!ip.Contains(":"))
            {
                var split = ip.Split('.');
                if (split.Length != 4) return ip;
                return string.Join('.', split.Take(2)) + $".{new string('*', split[2].Length)}.{new string('*', split[3].Length)}";
            }
            else
            {
                // Ipv6
                var split = ip.Split(':');
                var take = split.Length > 2 ? split.Length - 2 : split.Length;
                return string.Join(':', split.Take(take)) + ":*:*";
            }
        }
    }
}
