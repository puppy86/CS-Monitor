using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace csmon.Models
{
    // Utility static class
    public static class ConvUtils
    {
        // Converts unix time stamp to DateTime
        public static DateTime UnixTimeStampToDateTimeS(long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        // Gets block age by given time stamp
        public static string GetAge(long time)
        {
            //return UnixTimeStampToDateTime(time).ToString("dd.MM.yyyy hh:mm:ss.fff");
            if (time == 0) return "0";
            var span = DateTime.Now - UnixTimeStampToDateTime(time);
            return AgeStr(span);
        }
        
        // Gets block age string by given TimeSpan
        public static string AgeStr(TimeSpan span)
        {
            if (!Settings.AllowNegativeTime && span < TimeSpan.Zero) span = TimeSpan.Zero;
            var res = span.Days != 0 ? span.Days + "d " : "";
            res += span.Hours != 0 || span.Days != 0 ? span.Hours + "h " : "";
            res += span.Minutes + "m " + span.Seconds + "s";
            return res;
        }

        // Converts binary hash into HEX string
        public static string ConvertHash(byte[] hash)
        {
            var hex = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        // Converts HEX string to binary hash
        public static byte[] ConvertHashBack(string hash)
        {
            var bytes = new List<byte>();
            for (var i = 0; i < hash.Length / 2; i++)
                bytes.Add(Convert.ToByte(hash.Substring(i * 2, 2), 16));
            return bytes.ToArray();
        }

        // Converts binary hash to ASCII string
        public static string ConvertHashAscii(byte[] hash)
        {
            return Encoding.ASCII.GetString(hash);
        }

        // Converts ASCII string to binary hash
        public static byte[] ConvertHashBackAscii(string hash)
        {
            return Encoding.ASCII.GetBytes(hash);
        }

        // Converts ASCII hash to Base58 hash
        public static string ConvertHashPartial(string hash)
        {
            return Base58Encoding.Encode(ConvertHashBack(hash));
        }

        // Converts Base58 hash to ASCII hash
        public static string ConvertHashBackPartial(string hash)
        {
            return ConvertHash(Base58Encoding.Decode(hash));
        }

        // Formats currency amount into string form (Release API)
        public static string FormatAmount(Release.Amount value)
        {
            if (value == null) return string.Empty;
            if (value.Fraction == 0) return $"{value.Integral}.0";
            var fraction = value.Fraction.ToString();
            while (fraction.Length < 18)
                fraction = "0" + fraction;
            return $"{value.Integral}.{fraction.TrimEnd('0')}";
        }

        /// <summary>
        /// Formats java source code from one line of code to multiline string
        /// </summary>
        /// <param name="code">Source code</param>
        /// <param name="lineNumbers">Need to insert line numbers</param>
        /// <returns></returns>
        public static string FormatSrc(string code, bool lineNumbers = false)
        {
            // If code is already multi-line, just return it
            if (code.Contains(Environment.NewLine) || code.Contains("\n")) return code;

            var sb = new StringBuilder();
            const int ident = 4;
            int level = 0, line = 1;
            var newLine = false;
            if(lineNumbers) sb.Append($"{line++:D3}| ".Replace('0', ' '));
            foreach (var c in code)
            {
                if (c == '{')
                {
                    if (lineNumbers) sb.AppendLine();
                    if (lineNumbers) sb.Append(' ', level * ident);
                    sb.Append(c);
                    level++;
                    newLine = true;
                }
                else if (c == '}')
                {
                    level--;
                    sb.AppendLine();
                    if (lineNumbers) sb.Append($"{line++:D3}| ".Replace('0', ' '));
                    sb.Append(' ', level * ident);
                    sb.Append(c);
                    newLine = true;
                }
                else if (c == ';')
                {
                    sb.Append(c);
                    newLine = true;
                }
                else if (c == ' ' && newLine)
                {
                }
                else
                {
                    if (newLine)
                    {
                        sb.AppendLine();
                        if (lineNumbers) sb.Append($"{line++:D3}| ".Replace('0', ' '));
                        sb.Append(' ', level * ident);
                        newLine = false;
                    }
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Calculates total number of pages
        /// </summary>
        /// <param name="count">Total count of items</param>
        /// <param name="numPerPage">Number of items displayed at one page</param>
        /// <returns></returns>
        public static int GetNumPages(long count, int numPerPage)
        {
            if (count <= 0) return 1;
            if (count % numPerPage == 0) return (int) count / numPerPage;
            return (int) count / numPerPage + 1;
        }

        /// <summary>
        /// Partially hides IP address
        /// </summary>
        /// <param name="ip">Input IP address</param>
        /// <returns>Partially hidden IP address</returns>
        public static string GetIpCut(string ip)
        {
            if (!ip.Contains(":"))
            {
                // Ipv4
                var split = ip.Split('.');
                if (split.Length != 4) return ip;
                return string.Join('.', split.Take(2)) + ".*.*";
                //return string.Join('.', split.Take(2)) + $".{new string('*', split[2].Length)}.{new string('*', split[3].Length)}";
            }
            else
            {
                // Ipv6
                var split = ip.Split(':');
                var take = split.Length > 2 ? split.Length - 2 : split.Length;
                return string.Join(':', split.Take(take)) + ":*:*";
            }
        }

        public static string GetTxId(Release.TransactionId id)
        {
            return id == null ? null : $"{ConvUtils.ConvertHash(id.PoolHash)}.{id.Index + 1}";
        }
    }
}
