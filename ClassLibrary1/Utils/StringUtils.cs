using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Utils
{
    public static class StringUtils
    {
        public static string RemoveTrailingNullTerminators(this string s)
        {
            int nullIndex = s.IndexOf('\0');
            if (nullIndex != -1)
            {
                return s.Substring(0, nullIndex);
            }
            return s;
        }
    }
}