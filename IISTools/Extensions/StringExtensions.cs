using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISTools.Extensions
{
    public static class StringExtensions
    {
        public static T? ToEnumOrNull<T>(this string text, bool ignoreCase = true) where T: struct
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                if (Enum.TryParse<T>(text, ignoreCase, out var enumValue))
                {
                    return enumValue;
                }
            }
            return null;
        }
    }
}
